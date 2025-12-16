using System.ClientModel;
using System.Xml.Serialization;
using find_and_make;
using OpenAI;
using OpenAI.Embeddings;
using Qdrant.Client;

var baseDir = AppContext.BaseDirectory; // bin/Debug/net8.0/...
var xmlPath = Path.Combine(baseDir, "data", "일반음식점.xml");
if (!File.Exists(xmlPath))
{
    Console.WriteLine($"xml 파일을 찾을 수 없습니다: {xmlPath}");
    return;
}

Console.WriteLine($"XML 로드 중... ({xmlPath})");
RestaurantXmlResult xmlResult;
var serializer = new XmlSerializer(typeof(RestaurantXmlResult));

await using (var stream = File.OpenRead(xmlPath))
{
    xmlResult = (RestaurantXmlResult)serializer.Deserialize(stream)!;
}

var rows = xmlResult.Body.Rows.Where(x => x.TrdCode == "영업/정상").ToList();

Console.WriteLine($"로드된 레코드 개수: {rows.Count}");

// 3. OpenAI 임베딩 클라이언트 준비
string? envApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(envApiKey))
{
    Console.WriteLine("환경 변수 OPENAI_API_KEY 가 설정되어 있지 않습니다.");
    return;
}

Console.WriteLine(envApiKey);

// 모델 이름은 필요에 따라 변경 가능 (text-embedding-3-large 등)
EmbeddingClient embeddingClient = new EmbeddingClient(
    model: "text-embedding-3-small",
    credential: new ApiKeyCredential(envApiKey),
    options: new OpenAIClientOptions()
    {
        Endpoint = new Uri("https://gms.ssafy.io/gmsapi/api.openai.com/v1")
    }
);

// 4. Qdrant 클라이언트 준비 
var qdrant = new QdrantClient("localhost", 6334);

const string CollectionName = "barsDataset";
const int VectorSize = 1536;

// ======================
// 5. XML → 임베딩 → Qdrant 검색
// ======================

// 테스트로 너무 오래 걸리지 않게 일부만 돌려보기
int maxRows = 100; // 전체 돌릴 때는 rows.Count로 바꾸면 됨

foreach (var row in rows.Skip(9700).Take(maxRows))
{
    var queryText = row.BuildSearchText();

    if (string.IsNullOrWhiteSpace(queryText))
        continue;

    // 5-1. 임베딩 생성
    var embedding = await embeddingClient.GenerateEmbeddingAsync(queryText);
    var vector = embedding.Value.ToFloats().ToArray();  // float[]로 변환

    // 5-2. Qdrant 검색
    // JSON 쪽에서 넣을 때와 동일한 dimension으로 collection을 만들어놔야 함
    var searchResult = await qdrant.SearchAsync(
        CollectionName,
        vector,
        limit: 3 // top3 매칭
    );

    // 5-3. 결과 출력
    Console.WriteLine("=================================================");
    Console.WriteLine($"[XML #{row.RowNum}] {row.BplcNm} | {row.SiteWhlAddr}");
    Console.WriteLine($"QueryText: {queryText}");

    int rank = 0;

    if (searchResult.First().Score < 0.8)
    {
        
    }
    foreach (var point in searchResult)
    {
        if(point.Score < 0.8f) continue; 
        // JSON 업서트할 때 payload에 이런 식으로 넣었다고 가정:
        //  ["rest_id"] = ..., ["rest_nm"] = ..., ["addr"] = ...
        var payload = point.Payload;

        // payload는 Qdrant.Client.Grpc.Value 타입 딕셔너리
        /*
            ["rest_id"] = r.RestId,
            ["name"] = r.RestName,
            ["address"] = r.Address,
            ["tob_info"] = r.TobInfo,
            ["lat"] = r.Lat,
            ["lot"] = r.Lon
        */
        payload.TryGetValue("rest_id", out var restIdVal);
        payload.TryGetValue("name", out var restNmVal);
        payload.TryGetValue("address", out var addrVal);
        
        string restId = restIdVal?.StringValue ?? restIdVal?.IntegerValue.ToString() ?? "?";
        string restNm = restNmVal?.StringValue ?? "?";
        string addr   = addrVal?.StringValue ?? "?";

        Console.WriteLine(
            $"  #{++rank} Score={point.Score:F4} " +
            $"ID={restId} | REST_NM={restNm} | ADDR={addr}"
        );
        
    }
    
    if(rank == 0) Console.WriteLine("일치하는 데이터 없음");

    Console.WriteLine();
}

Console.WriteLine("검색 완료");
