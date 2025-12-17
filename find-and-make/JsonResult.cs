using System.Text.Json.Serialization;

namespace find_and_make;

public class JsonResult
{
    //주소 siteWhlAddr
    public required string Adress { get; set; }

    //사업장명 bplcNm
    public required string Name { get; set; }

    //카테고리이름 
    public required string CategoryName { get; set; }

    //좌표정보X(EPSG5174) x
    public required float X { get; set; }

    //좌표정보Y(EPSG5174) y
    public required float Y { get; set; }

    //Dataset
    
    //영업시간 OPEN_HR_INFO
    public string? OpenInfo { get; set; }

    //메뉴 
    public string? Menu { get; set; }
    
    //restid
    public ulong? RestID { get; set; }
    
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(List<JsonResult>))]
internal partial class AppJsonContext : JsonSerializerContext
{
}