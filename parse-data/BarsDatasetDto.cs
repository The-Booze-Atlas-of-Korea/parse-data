namespace parse_data;

using System.Text.Json.Serialization;

public class BarsDatasetDto
{
    [JsonPropertyName("REST_ID")]
    public string RestId { get; set; } = "";

    [JsonPropertyName("REST_NM")]
    public string RestName { get; set; } = "";

    [JsonPropertyName("ADDR")]
    public string Address { get; set; } = "";

    [JsonPropertyName("DADDR")]
    public string DetailAddress { get; set; } = "";

    [JsonPropertyName("TELNO")]
    public string TelNo { get; set; } = "";

    [JsonPropertyName("OPEN_HR_INFO")]
    public string OpenHourInfo { get; set; } = "";

    [JsonPropertyName("TOB_INFO")]
    public string TobInfo { get; set; } = "";

    [JsonPropertyName("MENU_NM")]
    public string MenuName { get; set; } = "";

    [JsonPropertyName("LAT")]
    public string Lat { get; set; } = "";

    [JsonPropertyName("LOT")]
    public string Lon { get; set; } = "";   // LOT = 경도라고 가정

    [JsonPropertyName("MENU_ID")]
    public string MenuId { get; set; } = "";

    [JsonPropertyName("MENU_KORN_NM")]
    public string MenuKoreanNamesRaw { get; set; } = "";

    [JsonPropertyName("MENU_ENG_NM")]
    public string MenuEnglishNamesRaw { get; set; } = "";

    [JsonPropertyName("MENU_AMT")]
    public string MenuAmountsRaw { get; set; } = "";

    [JsonPropertyName("MENU_KOR_ADD_INFO")]
    public string MenuKoreanAddInfoRaw { get; set; } = "";

    [JsonPropertyName("MENU_ENG_ADD_INFO")]
    public string MenuEnglishAddInfoRaw { get; set; } = "";

    [JsonPropertyName("SD_ID")]
    public string SdId { get; set; } = "";

    [JsonPropertyName("SD_NM")]
    public string SdName { get; set; } = "";

    [JsonPropertyName("SD_URL")]
    public string SdUrl { get; set; } = "";

    [JsonPropertyName("SD_ADD_INFO")]
    public string SdAddInfo { get; set; } = "";
}
