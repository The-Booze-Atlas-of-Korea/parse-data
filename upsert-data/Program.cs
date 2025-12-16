using System.ClientModel;
using System.Text.Json;
using OpenAI;
using OpenAI.Embeddings;
using parse_data;
using Qdrant.Client;
using Qdrant.Client.Grpc;


var baseDir = AppContext.BaseDirectory; // bin/Debug/net8.0/...
var jsonPath = Path.Combine(baseDir, "data", "음식점-데이터셋.json");
if (!File.Exists(jsonPath))
{
    Console.WriteLine($"JSON 파일을 찾을 수 없습니다: {jsonPath}");
    return;
}

Console.WriteLine($"JSON 로드 중... ({jsonPath})");
var jsonStream = new FileStream(jsonPath, FileMode.Open);


var records = await JsonSerializer.DeserializeAsync(
    jsonStream,
    BarsJsonContext.Default.ListBarsDatasetDto  // JsonTypeInfo<List<BarsDatasetDto>>
);

if (records == null || records.Count == 0)
{
    Console.WriteLine("레코드가 없습니다.");
    return;
}

Console.WriteLine($"로드된 레코드 개수: {records.Count}");

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

// 5. 컬렉션 생성 (이미 있으면 예외 날 수 있으니, try/catch로 한 번 감쌀 수도 있음)
Console.WriteLine($"Qdrant 컬렉션 생성/초기화 중: {CollectionName}");

try
{
    await qdrant.DeleteCollectionAsync(CollectionName);
}
catch
{
    // 없으면 무시
}

await qdrant.CreateCollectionAsync(
    collectionName: CollectionName,
    vectorsConfig: new VectorParams
    {
        Size = VectorSize, Distance = Distance.Cosine
    });

Console.WriteLine("컬렉션 생성 완료");

const int BatchSize = 64;
var batch = new List<PointStruct>(BatchSize);
ulong autoId = 1;
ulong count = 0;

foreach (var r in records)
{
    // 6-1. 레스토랑 하나를 텍스트로 합치기
    string text = r.BuildTextForEmbedding();

    // 6-2. 임베딩 생성 (동기 호출, 필요하면 비동기 버전 사용 가능)
    var embedding = embeddingClient.GenerateEmbedding(text);
    var vector = embedding.Value.ToFloats().ToArray();

    if (vector.Length != VectorSize)
    {
        Console.WriteLine($"[경고] 벡터 차원이 예상과 다릅니다. (id={r.RestId}, dim={vector.Length})");
    }

    // 6-3. Qdrant 포인트 ID (REST_ID 숫자로 되면 그대로 쓰고, 아니면 autoId 사용)
    if (!ulong.TryParse(r.RestId, out var pointId))
    {
        pointId = autoId++;
    }
    
    var point = new PointStruct()
    {
        Id = pointId,
        Vectors = vector,
        Payload =
        {
            ["rest_id"] = r.RestId,
            ["name"] = r.RestName,
            ["address"] = r.Address,
            ["tob_info"] = r.TobInfo,
            ["menu"] = r.MenuKoreanAddInfoRaw,
            ["open_info"] = r.OpenHourInfo
        }
    };

    batch.Add(point);
    
    // 배치 사이즈마다 업서트
    if (batch.Count >= BatchSize)
    {
        count += (ulong)batch.Count;
        await qdrant.UpsertBatchAsync(CollectionName, batch);
        Console.WriteLine($"진행도 : {batch.Count} / {records.Count}");
        batch.Clear();
    }
    
}

// 마지막 남은 배치 업서트
if (batch.Count > 0)
{
    await qdrant.UpsertBatchAsync(CollectionName, batch);
    batch.Clear();
}

Console.WriteLine("모든 임베딩 업서트 완료");