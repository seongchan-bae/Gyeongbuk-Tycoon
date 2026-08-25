/// <summary>위치 요청 결과 상태.</summary>
public enum LocationStatus
{
    Success,             // 정상적으로 좌표를 얻음
    PermissionDenied,    // 사용자가 위치 권한을 거부
    CoarseOnly,          // Android 12+ 에서 "대략적 위치"만 허용됨 (오차가 커서 인증 불가)
    ServiceDisabled,     // 기기의 위치 서비스가 꺼져 있음
    Timeout,             // 제한 시간 내에 GPS 신호를 못 잡음
    LowAccuracy,         // 좌표는 얻었지만 오차가 커서 인증 판정에 쓸 수 없음
    Failed,              // 그 외 실패
    Unsupported          // 이 플랫폼에서 위치 기능을 쓸 수 없음 (에디터/PC 등)
}

/// <summary>위치 요청 1회의 결과.</summary>
public struct LocationResult
{
    public LocationStatus status;
    public double latitude;
    public double longitude;
    public float accuracyMeters;
    public string message;

    public bool IsSuccess { get { return status == LocationStatus.Success; } }

    public static LocationResult Ok(double lat, double lon, float accuracy)
    {
        return new LocationResult
        {
            status = LocationStatus.Success,
            latitude = lat,
            longitude = lon,
            accuracyMeters = accuracy,
            message = ""
        };
    }

    public static LocationResult Fail(LocationStatus status, string message)
    {
        return new LocationResult { status = status, message = message };
    }

    /// <summary>사용자에게 그대로 보여줄 수 있는 안내 문구.</summary>
    public string UserMessage
    {
        get
        {
            switch (status)
            {
                case LocationStatus.PermissionDenied:
                    return "위치 권한이 필요합니다. 설정에서 위치 권한을 허용해 주세요.";
                case LocationStatus.CoarseOnly:
                    return "'정확한 위치' 권한이 필요합니다. 설정에서 정확한 위치를 켜 주세요.";
                case LocationStatus.ServiceDisabled:
                    return "기기의 위치 서비스가 꺼져 있습니다. 위치 서비스를 켜 주세요.";
                case LocationStatus.Timeout:
                    return "위치를 확인하지 못했습니다. 하늘이 보이는 곳에서 다시 시도해 주세요.";
                case LocationStatus.LowAccuracy:
                    return string.IsNullOrEmpty(message)
                        ? "위치 정확도가 낮습니다. 하늘이 보이는 곳에서 다시 시도해 주세요."
                        : message;
                case LocationStatus.Unsupported:
                    return "이 환경에서는 위치 인증을 사용할 수 없습니다.";
                case LocationStatus.Failed:
                    return string.IsNullOrEmpty(message) ? "위치 확인에 실패했습니다." : message;
                default:
                    return message;
            }
        }
    }
}
