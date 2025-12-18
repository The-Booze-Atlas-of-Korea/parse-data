using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotSpatial.Projections;
using Microsoft.EntityFrameworkCore;

// ------------------- Main -------------------
var jsonPath = args.Length >= 1 ? args[0] : "bars.json";
var connectionString = args.Length >= 2
    ? args[1]
    : "Server=localhost;Port=33060;Database=sulmap;Uid=ssafy;Pwd=ssafy;CharSet=utf8mb4;";

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
    .Options;

var json = await File.ReadAllTextAsync(jsonPath);
var items = JsonSerializer.Deserialize<List<InputBarDto>>(json, AppJsonContext.Default.ListInputBarDto) ?? new();

await using var db = new AppDbContext(options);

// 대량 적재 성능 옵션
db.ChangeTracker.AutoDetectChangesEnabled = false;

var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var toInsert = new List<Bar>(items.Count);

foreach (var dto in items)
{
    var name = dto.Name?.Trim();
    var address = dto.Adress?.Trim();

    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
        continue;

    // 간단 중복 제거: (이름|주소)
    if (!seen.Add($"{name}|{address}"))
        continue;

    var (lat, lng) = Epsg5174.ToWgs84(dto.X, dto.Y);

    toInsert.Add(new Bar
    {
        Name = name,
        Address = address,
        Latitude = lat,
        Longitude = lng,
        BaseCategoryName = string.IsNullOrWhiteSpace(dto.CategoryName) ? null : dto.CategoryName.Trim(),
        OpenInformation = dto.OpenInfo,
        Menu = dto.Menu.HasValue ? dto.Menu.Value.GetRawText() : null
    });
}

// 배치 저장
const int batchSize = 2000;
for (int i = 0; i < toInsert.Count; i += batchSize)
{
    var batch = toInsert.Skip(i).Take(batchSize);
    await db.Bars.AddRangeAsync(batch);
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();

    Console.WriteLine($"Inserted: {Math.Min(i + batchSize, toInsert.Count)}/{toInsert.Count}");
}

Console.WriteLine($"DONE. total inserted = {toInsert.Count}");

#region DTO (입력 JSON)
public sealed class InputBarDto
{
    [JsonPropertyName("Adress")]
    public string? Adress { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("CategoryName")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("X")]
    public double X { get; set; }   // EPSG:5174 X

    [JsonPropertyName("Y")]
    public double Y { get; set; }   // EPSG:5174 Y

    [JsonPropertyName("OpenInfo")]
    public string? OpenInfo { get; set; }

    // null/객체/배열 다 가능 -> JsonElement
    [JsonPropertyName("Menu")]
    public JsonElement? Menu { get; set; }

    [JsonIgnore]
    public string? RestID { get; set; }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(List<InputBarDto>))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

#endregion

#region EF Entity
public sealed class Bar
{
    public ulong Id { get; set; } // BIGINT UNSIGNED
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public decimal Latitude { get; set; }  // DECIMAL(10,7)
    public decimal Longitude { get; set; } // DECIMAL(10,7)
    public string? BaseCategoryName { get; set; }
    public string? OpenInformation { get; set; }
    public string? Menu { get; set; } // MySQL json 컬럼에 넣을 raw json string
}

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Bar> Bars => Set<Bar>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Bar>();
        e.ToTable("bars");

        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();

        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        e.Property(x => x.Address).HasColumnName("address").HasMaxLength(255).IsRequired();

        e.Property(x => x.Latitude).HasColumnName("latitude").HasPrecision(10, 7).IsRequired();
        e.Property(x => x.Longitude).HasColumnName("longitude").HasPrecision(10, 7).IsRequired();

        e.Property(x => x.BaseCategoryName).HasColumnName("base_category_name").HasMaxLength(50);
        e.Property(x => x.OpenInformation).HasColumnName("open_information").HasColumnType("text");
        e.Property(x => x.Menu).HasColumnName("menu").HasColumnType("json");
    }
}
#endregion

#region 좌표 변환 (EPSG:5174 -> WGS84)
public static class Epsg5174
{
    // EPSG:5174 PROJ.4 정의 (epsg.io) :contentReference[oaicite:1]{index=1}
    private const string Proj4_5174 =
        "+proj=tmerc +lat_0=38 +lon_0=127.002890277778 +k=1 +x_0=200000 +y_0=500000 " +
        "+ellps=bessel +towgs84=-145.907,505.034,685.756,-1.162,2.347,1.592,6.342 " +
        "+units=m +no_defs +type=crs";

    private static readonly ProjectionInfo Src = ProjectionInfo.FromProj4String(Proj4_5174);
    private static readonly ProjectionInfo Dst = KnownCoordinateSystems.Geographic.World.WGS1984;

    public static (decimal lat, decimal lng) ToWgs84(double x, double y)
    {
        // 1) (x,y)로 시도
        if (TryProject(x, y, out var lat, out var lng))
            return (lat, lng);

        // 2) 혹시 축이 뒤집혀 들어오면 (y,x)로 재시도
        if (TryProject(y, x, out lat, out lng))
            return (lat, lng);

        throw new InvalidOperationException($"EPSG:5174 → WGS84 변환 실패: x={x}, y={y}");
    }

    private static bool TryProject(double x, double y, out decimal lat, out decimal lng)
    {
        lat = 0; lng = 0;

        double[] xy = { x, y };
        double[] z = { 0 };

        Reproject.ReprojectPoints(xy, z, Src, Dst, 0, 1);

        // DotSpatial 결과: xy[0]=lon, xy[1]=lat
        var lon = xy[0];
        var la = xy[1];

        // 한국 대략 범위 sanity check
        if (lon is < 122 or > 134 || la is < 32 or > 40)
            return false;

        lat = Round7(la);
        lng = Round7(lon);
        return true;
    }

    private static decimal Round7(double v) =>
        Math.Round((decimal)v, 7, MidpointRounding.AwayFromZero);
}
#endregion

