using Google.Protobuf.Collections;

namespace find_and_make;

public static class Helper
{
    public static string BuildSearchText(this RestaurantRow r)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(r.BplcNm))
            parts.Add(r.BplcNm.Trim());

        if (!string.IsNullOrWhiteSpace(r.SiteWhlAddr))
            parts.Add(r.SiteWhlAddr.Trim());

        if (!string.IsNullOrWhiteSpace(r.UptaeNm))
            parts.Add($"업종: {r.UptaeNm.Trim()}");

        // 필요하다면 "대전광역시" 같이 시/구도 붙여도 됨
        return string.Join(" | ", parts);
    }

    public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        static double ToRad(double d) => d * Math.PI / 180.0;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public static string Norm(this string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        // 공백/괄호/층/지상/번지 표기 등 아주 거친 정규화(필요하면 더 강화 가능)
        s = s.Trim();
        s = s.Replace(" ", "")
            .Replace("\t", "")
            .Replace("\r", "")
            .Replace("\n", "");
        s = s.Replace("번지", "");
        return s;
    }

    public static bool Exact(this string? a, string? b)
        => Norm(a) == Norm(b);

    // 도로명/지번 섞일 때를 대비해 "앞부분 N자" 비교 (너무 공격적이면 N 줄여)
    public static bool PrefixMatch(string? a, string? b, int n = 10)
    {
        var na = Norm(a);
        var nb = Norm(b);
        if (na.Length < n || nb.Length < n) return false;
        return na[..n] == nb[..n];
    }
    
    public static string? GetPayloadString(this MapField<string, Qdrant.Client.Grpc.Value>? payload, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!payload.TryGetValue(k, out var v) || v is null) continue;
            if (!string.IsNullOrWhiteSpace(v.StringValue)) return v.StringValue;
            if (v.IntegerValue != 0) return v.IntegerValue.ToString();
            if (Math.Abs(v.DoubleValue) > 0) return v.DoubleValue.ToString();
        }
        return null;
    }

    public static double? GetPayloadDouble(this MapField<string, Qdrant.Client.Grpc.Value>? payload, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!payload.TryGetValue(k, out var v) || v is null) continue;

            if (v.KindCase == Qdrant.Client.Grpc.Value.KindOneofCase.DoubleValue)
                return v.DoubleValue;

            if (!string.IsNullOrWhiteSpace(v.StringValue) && double.TryParse(v.StringValue, out var d))
                return d;
        }
        return null;
    }

    public static ulong? GetPayloadULong(this MapField<string, Qdrant.Client.Grpc.Value>? payload, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (!payload.TryGetValue(k, out var v) || v is null) continue;

            if (v.KindCase == Qdrant.Client.Grpc.Value.KindOneofCase.IntegerValue && v.IntegerValue > 0)
                return (ulong)v.IntegerValue;

            if (!string.IsNullOrWhiteSpace(v.StringValue) && ulong.TryParse(v.StringValue, out var u))
                return u;
        }
        return null;
    }
}