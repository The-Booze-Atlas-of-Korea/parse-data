using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace db_json_to_es_ndjson;

public class NdjsonWriter
{
    public static async Task WriteBarsNdjsonAsync(
        IEnumerable<BarDocument> documents,
        string indexName,
        string filePath)
    {
        // 출력 폴더 없으면 생성
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream); // 기본 UTF-8

        foreach (var doc in documents)
        {
            // 1) meta 라인: { "index": { "_index": "bars", "_id": 2 } }
            //   - 익명타입은 JsonContext에 없어서 Serialize 못 쓰니까, 그냥 문자열로 직접 씀
            var metaLine = $"{{\"index\":{{\"_index\":\"{indexName}\",\"_id\":{doc.id}}}}}";
            await writer.WriteLineAsync(metaLine);

            // 2) 실제 문서 라인
            var docJson = JsonSerializer.Serialize(doc, AppJsonContext.Default.BarDocument);
            await writer.WriteLineAsync(docJson);
        }

        await writer.FlushAsync();
    }
}