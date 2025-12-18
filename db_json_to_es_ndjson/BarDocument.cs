namespace db_json_to_es_ndjson;

using System;
using System.Text.Json.Serialization;

public class BarDocument
{
    public int id { get; set; }

    public string name { get; set; } = string.Empty;

    public string address { get; set; } = string.Empty;

    public double latitude { get; set; }

    public double longitude { get; set; }

    public string? base_category_name { get; set; }

    public string? open_information { get; set; }

    public string? menu { get; set; }

    public string created_at { get; set; } = string.Empty;

    public string updated_at { get; set; } = string.Empty;

    public string? deleted_at { get; set; }

    // ES geo_point에 쓸 location 필드
    public Location location => new Location
    {
        lat = latitude,
        lon = longitude
    };
}

// geo_point용 타입
public class Location
{
    public double lat { get; set; }
    public double lon { get; set; }
}

[JsonSerializable(typeof(BarDocument))]
[JsonSerializable(typeof(List<BarDocument>))]
internal partial class AppJsonContext : JsonSerializerContext
{
}