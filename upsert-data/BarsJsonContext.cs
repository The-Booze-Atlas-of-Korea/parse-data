using System.Text.Json.Serialization;

namespace parse_data;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true  // 기존 options의 CaseInsensitive 역할
)]
[JsonSerializable(typeof(List<BarsDatasetDto>))]
internal partial class BarsJsonContext : JsonSerializerContext
{
}