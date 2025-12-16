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
}