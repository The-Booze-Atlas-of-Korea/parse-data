namespace find_and_make;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

public static class Coord
{
    private static readonly CoordinateTransformationFactory CtFactory = new();
    private static readonly CoordinateSystemFactory CsFactory = new();
    private static readonly ICoordinateTransformation ToWgs84 = Create5174To4326();

    // EPSG:5174 (Korean 1985 / Modified Central Belt) -> WGS84
    // 네 샘플에서 합리적인 근처 값(수백 m 오차)으로 나왔던 정의
    private static ICoordinateTransformation Create5174To4326()
    {
        var wgs84 = GeographicCoordinateSystem.WGS84;

        const string epsg5174Wkt = @"
PROJCS[""Korean 1985 / Modified Central Belt"",
  GEOGCS[""Korean 1985"",
    DATUM[""Korean Datum 1985"",
      SPHEROID[""Bessel 1841"",6377397.155,299.1528128]],
    PRIMEM[""Greenwich"",0],
    UNIT[""degree"",0.0174532925199433]],
  PROJECTION[""Transverse_Mercator""],
  PARAMETER[""latitude_of_origin"",38],
  PARAMETER[""central_meridian"",127.002890277778],
  PARAMETER[""scale_factor"",1],
  PARAMETER[""false_easting"",200000],
  PARAMETER[""false_northing"",500000],
  UNIT[""metre"",1]
]";

        var cs5174 = CsFactory.CreateFromWkt(epsg5174Wkt);
        return CtFactory.CreateFromCoordinateSystems(cs5174, wgs84);
    }

    public static (double lat, double lon) Epsg5174ToWgs84(double x, double y)
    {
        var r = ToWgs84.MathTransform.Transform(new[] { x, y });
        return (lat: r[1], lon: r[0]); // (lon,lat) -> (lat,lon)
    }

    public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        static double ToRad(double d) => d * Math.PI / 180.0;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
