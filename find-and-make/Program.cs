using System.ClientModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Xml.Serialization;
using find_and_make;
using OpenAI;
using OpenAI.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;


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

var rows = xmlResult.Body.Rows;

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

var results = new List<JsonResult>();

foreach (var row in rows)
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
        limit: 20
    );
    

    double.TryParse(row.X, out var xmlX);
    double.TryParse(row.Y, out var xmlY);
    double xmlLat = 0, xmlLon = 0;
    (xmlLat, xmlLon) = Coord.Epsg5174ToWgs84(xmlX, xmlY);
    
    ScoredPoint? best = null;
    double bestFinal = double.NegativeInfinity;

    // 거리 컷 기준 (너 데이터 특성상 수백 m 오차가 있을 수 있어서 400m까지 허용)
    const double MaxDist = 400.0;
    const double HardRejectDist = 1200.0; // 1.2km 넘어가면 거의 오탐
    
    
    foreach (var c in searchResult)
    {
        var embedScore = c.Score;
        if (embedScore < 0.80) continue;

        var p = c.Payload;

        // --- payload에서 값 꺼내기(키 여러 케이스 지원)
        string? candName = p.GetPayloadString("REST_NM", "name", "REST_NM_KR");
        string? candAddr = p.GetPayloadString("ADDR", "address");
        string? candOpen = p.GetPayloadString("OPEN_HR_INFO", "open_info");
        string? candMenu = p.GetPayloadString("MENU_NM", "menu");

        ulong? candRestId = p.GetPayloadULong("REST_ID", "rest_id");

        double? candLat = p.GetPayloadDouble("LAT", "lat");
        double? candLon = p.GetPayloadDouble("LOT", "lot", "LON", "lon");

        // 4-1) 거리 계산 (가능할 때만)
        double dist = double.NaN;
        bool hasCandCoord = candLat.HasValue && candLon.HasValue;

        bool passGeo = true;
        double geoScore = 0.0;

        if (hasCandCoord)
        {
            dist = Helper.HaversineMeters(xmlLat, xmlLon, candLat.Value, candLon.Value);

            if (dist > HardRejectDist)
                continue; // 너무 멀면 즉시 컷

            // 0~MaxDist: 1.0 -> 0.0 선형 감점
            geoScore = Math.Clamp(1.0 - (dist / MaxDist), 0.0, 1.0);

            // MaxDist 초과면 geoScore=0, 그래도 주소가 완전 동일하면 살릴 수도 있게 passGeo는 유지
            // (하지만 보통은 주소가 동일하면 dist도 근처여야 정상)
        }

        // 4-2) 주소 보너스
        // XML은 지번/도로명 둘 다 있으니 둘 중 하나라도 맞으면 보너스
        var addrExact =
            row.SiteWhlAddr.Exact(candAddr) ||
            row.RdnWhlAddr.Exact(candAddr);
        

        double addrBonus = 0.0;
        if (addrExact) addrBonus = 0.70;       // ✅ 거의 확정급

        // 4-3) 최종 점수(주소/거리 중심으로 가중)
        // embed 0.5 + geo 0.5 + addrBonus(최대 0.7)
        double final = (0.5 * embedScore) + (0.5 * geoScore) + addrBonus;

        // 거리/주소 둘 다 없으면 너무 위험하니 컷(오탐 방지)
        bool hasAnyStrongSignal = addrExact || (hasCandCoord && !double.IsNaN(dist) && dist <= MaxDist);
        if (!hasAnyStrongSignal && embedScore < 0.90)
            continue;

        if (final > bestFinal)
        {
            bestFinal = final;
            best = c;
        }
    }
    Console.WriteLine("=================================================");
    if (best is null)
    {
        if (float.TryParse(row.X, out var x) && float.TryParse(row.Y, out var y))
        {
            results.Add(new JsonResult
            {
                Adress = row.SiteWhlAddr,
                Name = row.BplcNm,
                CategoryName = row.UptaeNm,
                X = x,
                Y = y,
                OpenInfo = null,
                Menu = null,
                RestID = null
            });
        }
    }
    else
    {
        Console.WriteLine($"[XML #{row.RowNum}] {row.BplcNm} | {row.SiteWhlAddr}");
        Console.WriteLine($"QueryText: {queryText}");

        var bp = best.Payload;

        var bestRestId = bp.GetPayloadULong("REST_ID", "rest_id");
        var bestName   = bp.GetPayloadString("REST_NM", "name");
        var bestAddr   = bp.GetPayloadString("ADDR", "address");
        var bestOpen   = bp.GetPayloadString("OPEN_HR_INFO", "open_info");
        var bestMenu   = bp.GetPayloadString("MENU_NM", "menu");

        Console.WriteLine($"  BEST FinalScore={bestFinal:F4} Embed={best.Score:F4}");
        Console.WriteLine($"  ID={bestRestId?.ToString() ?? "?"} | NAME={bestName ?? "?"} | ADDR={bestAddr ?? "?"}");

        if (float.TryParse(row.X, out var x) && float.TryParse(row.Y, out var y))
        {
            results.Add(new JsonResult
            {
                Adress = row.SiteWhlAddr,
                Name = row.BplcNm,
                CategoryName = row.UptaeNm,
                X = x,
                Y = y,
                OpenInfo = bestOpen,
                Menu = bestMenu,
                RestID = bestRestId
            });
        }
    }
    Console.WriteLine($"진행도 : {results.Count} / {rows.Count}");
}

Console.WriteLine("검색 완료");

var outputDir = Path.Combine(baseDir, "output");
Directory.CreateDirectory(outputDir);

var outputPath = Path.Combine(outputDir, "matched_results.json");

var options = new JsonSerializerOptions
{
    TypeInfoResolver = AppJsonContext.Default,                  // 소스 제너레이터 사용
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,      // 한글 등 유니코드 이스케이프 안 함
    WriteIndented = true                                        // 보기 좋게 들여쓰기
};

// ⚠️ 중요: context 기반 오버로드 사용
var json = JsonSerializer.Serialize(results, options);

await File.WriteAllTextAsync(outputPath, json);

Console.WriteLine($"결과 파일 저장 완료: {outputPath}"); Console.WriteLine($"총 저장된 레코드 수: {results.Count}");
