using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace parse_data;

public static class Helper
{
    // 레코드 → 임베딩용 텍스트로 합치는 로직
    public static string BuildTextForEmbedding(this BarsDatasetDto r)
    {
        // 필요하면 업종, 메뉴, 설명 같은 걸 더 붙여도 됨
        return string.Join(" | ",
            r.RestName?.Trim() ?? "",
            r.Address?.Trim() ?? "",
            string.IsNullOrWhiteSpace(r.TobInfo) ? "" : $"업종: {r.TobInfo.Trim()}"
        );
    }

    // 배치 업서트 헬퍼
    public static async Task UpsertBatchAsync(this QdrantClient qdrant, String CollectionName, List<PointStruct> batch)
    {
        if (batch.Count == 0) return;

        Console.WriteLine($"Qdrant 업서트: {batch.Count} points...");
        await qdrant.UpsertAsync(CollectionName, points: batch);
    }
}