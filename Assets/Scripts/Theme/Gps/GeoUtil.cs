using System;

/// <summary>위경도 계산 유틸.</summary>
public static class GeoUtil
{
    private const double EarthRadiusMeters = 6371000.0;

    /// <summary>두 위경도 사이의 대원 거리(미터). Haversine 공식.</summary>
    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>거리를 사람이 읽기 좋은 문자열로. (1km 미만은 m, 이상은 km)</summary>
    public static string FormatDistance(double meters)
    {
        if (meters < 1000.0) return $"{meters:F0}m";
        return $"{meters / 1000.0:F1}km";
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
