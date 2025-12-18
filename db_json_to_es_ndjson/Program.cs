// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using db_json_to_es_ndjson;

var jsonPath = args.Length >= 1 ? args[0] : "bars.json";
if (!File.Exists(jsonPath))
{
    Console.WriteLine($"JSON 파일을 찾을 수 없습니다: {jsonPath}");
    return;
}

Console.WriteLine($"JSON 로드 중... ({jsonPath})");
var jsonStream = new FileStream(jsonPath, FileMode.Open);


var records = await JsonSerializer.DeserializeAsync(
    jsonStream,
    AppJsonContext.Default.ListBarDocument  // JsonTypeInfo<List<BarsDatasetDto>>
);

if (records == null || records.Count == 0)
{
    Console.WriteLine("레코드가 없습니다.");
    return;
}

Console.WriteLine($"로드된 레코드 개수: {records.Count}");


var baseDir = AppContext.BaseDirectory;
var outputPath = Path.Combine(baseDir, "output", "bars_seed.ndjson");
await NdjsonWriter.WriteBarsNdjsonAsync(records, "bars", outputPath);
Console.WriteLine($"NDJSON 생성 완료 : {outputPath}");
