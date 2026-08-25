using System;
using System.Collections;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// 기기의 GPS 좌표를 1회 요청한다.
/// 에디터/PC에서는 Input.location이 동작하지 않으므로, 인스펙터에 지정한
/// 목(mock) 좌표를 대신 반환해 인증 흐름 전체를 테스트할 수 있게 한다.
/// </summary>
public class GpsLocationService : MonoBehaviour
{
    private static GpsLocationService instance;

    /// <summary>씬에 배치돼 있지 않으면 기본 설정으로 자동 생성한다.</summary>
    public static GpsLocationService Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GpsLocationService>();
                if (instance == null)
                {
                    var go = new GameObject("GpsLocationService (auto)");
                    instance = go.AddComponent<GpsLocationService>();
                }
            }
            return instance;
        }
    }

    public enum SourceMode
    {
        Auto,        // 모바일이면 실제 GPS, 그 외에는 목 좌표
        DeviceOnly,  // 항상 실제 GPS (지원 안 되면 Unsupported)
        MockOnly     // 항상 목 좌표 (실기기에서도 목 사용 - 시연용)
    }

    [Header("동작 모드")]
    [SerializeField] private SourceMode sourceMode = SourceMode.Auto;

    [Header("목(mock) 좌표 - 에디터 테스트용")]
    [Tooltip("에디터/PC에서 인증 흐름을 검증할 때 사용할 가짜 현재 위치.")]
    [SerializeField] private double mockLatitude = 35.834710;   // 경주 첨성대
    [SerializeField] private double mockLongitude = 129.219170;
    [SerializeField] private float mockAccuracyMeters = 8f;
    [Tooltip("실제 GPS처럼 보이도록 목 응답을 이만큼 지연시킨다.")]
    [SerializeField] private float mockDelaySeconds = 0.6f;

    [Header("실기기 설정")]
    [Tooltip("GPS 신호를 기다릴 최대 시간(초).")]
    [SerializeField] private float timeoutSeconds = 20f;
    [SerializeField] private float desiredAccuracyMeters = 10f;
    [SerializeField] private float updateDistanceMeters = 5f;

    [Header("정확도 기준")]
    [Tooltip("이 값보다 오차가 크면 인증 판정에 쓰지 않고 재시도를 안내한다. " +
             "유적지 인증 반경이 100~300m이므로 그보다 충분히 작아야 한다.")]
    [SerializeField] private float maxAcceptableAccuracyMeters = 50f;
    [Tooltip("첫 좌표의 오차가 크면 이 시간만큼 더 기다리며 정확도가 좋아지길 기다린다.")]
    [SerializeField] private float accuracyImproveWaitSeconds = 8f;

    private bool isRequesting;
    private bool continuousActive;   // 패널이 열려 있는 동안 위치 서비스를 켜 둔 상태인지

    public bool IsRequesting { get { return isRequesting; } }
    public bool ContinuousActive { get { return continuousActive; } }
    public bool UsingMock { get { return ResolveUseMock(); } }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>
    /// 테마 패널이 열려 있는 동안 위치 서비스를 켜 둔다.
    /// 인증할 때마다 Start/Stop을 반복하면 위성 재획득 전의 캐시(네트워크) 위치가 잡혀
    /// 같은 자리에서도 정확도가 13m -> 100m로 튀는 문제가 생긴다.
    /// </summary>
    public void BeginContinuousUpdates()
    {
        if (continuousActive || ResolveUseMock()) return;
        StartCoroutine(BeginContinuousRoutine());
    }

    /// <summary>위치 서비스를 끈다. 패널을 닫을 때 반드시 호출해 배터리 소모를 막는다.</summary>
    public void EndContinuousUpdates()
    {
        if (!continuousActive) return;
        continuousActive = false;
        Input.location.Stop();
        Debug.Log("[GPS] 연속 측위 종료");
    }

    private IEnumerator BeginContinuousRoutine()
    {
        yield return StartCoroutine(EnsurePermission(_ => { }));
        if (!Input.location.isEnabledByUser) yield break;

        Input.location.Start(desiredAccuracyMeters, updateDistanceMeters);
        continuousActive = true;
        Debug.Log("[GPS] 연속 측위 시작 (패널 열림)");
    }

    private void OnApplicationPause(bool paused)
    {
        // 백그라운드로 가면 위치 서비스를 놔두지 않는다.
        if (paused) EndContinuousUpdates();
    }

    /// <summary>현재 위치를 1회 요청한다. 결과는 onComplete로 전달된다.</summary>
    public void RequestLocation(Action<LocationResult> onComplete)
    {
        if (onComplete == null) return;

        if (isRequesting)
        {
            onComplete(LocationResult.Fail(LocationStatus.Failed, "이미 위치를 확인하는 중입니다."));
            return;
        }

        StartCoroutine(RequestRoutine(onComplete));
    }

    private bool ResolveUseMock()
    {
        switch (sourceMode)
        {
            case SourceMode.MockOnly: return true;
            case SourceMode.DeviceOnly: return false;
            default:
#if UNITY_ANDROID && !UNITY_EDITOR
                return false;
#elif UNITY_IOS && !UNITY_EDITOR
                return false;
#else
                return true;   // 에디터 / PC / WebGL 등
#endif
        }
    }

    private IEnumerator RequestRoutine(Action<LocationResult> onComplete)
    {
        isRequesting = true;

        LocationResult result;
        if (ResolveUseMock())
        {
            if (mockDelaySeconds > 0f) yield return new WaitForSeconds(mockDelaySeconds);
            result = LocationResult.Ok(mockLatitude, mockLongitude, mockAccuracyMeters);
            Debug.Log($"[GPS] 목 좌표 사용: {mockLatitude:F6}, {mockLongitude:F6}");
        }
        else
        {
            LocationResult captured = LocationResult.Fail(LocationStatus.Failed, "");
            yield return StartCoroutine(RequestFromDevice(r => captured = r));
            result = captured;
        }

        isRequesting = false;
        onComplete(result);
    }

    private IEnumerator EnsurePermission(Action<LocationResult> onFail)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation)
            && !Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);

            float waited = 0f;
            while (waited < timeoutSeconds
                   && !Permission.HasUserAuthorizedPermission(Permission.FineLocation)
                   && !Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            // Android 12+ 에서는 "대략적 위치"만 허용할 수 있다. 오차가 km 단위라 인증에 못 쓴다.
            if (Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
                onFail(LocationResult.Fail(LocationStatus.CoarseOnly, ""));
            else
                onFail(LocationResult.Fail(LocationStatus.PermissionDenied, ""));
        }
#else
        yield break;
#endif
    }

    private IEnumerator RequestFromDevice(Action<LocationResult> onComplete)
    {
        // 1) 권한
        LocationResult permissionFailure = default;
        bool permissionFailed = false;
        yield return StartCoroutine(EnsurePermission(f => { permissionFailure = f; permissionFailed = true; }));
        if (permissionFailed) { onComplete(permissionFailure); yield break; }

        // 2) OS 위치 서비스 자체가 켜져 있는지
        if (!Input.location.isEnabledByUser)
        {
            onComplete(LocationResult.Fail(LocationStatus.ServiceDisabled, ""));
            yield break;
        }

        // 3) 연속 모드가 켜져 있으면 이미 돌고 있는 서비스를 그대로 쓴다.
        bool startedHere = false;
        if (!continuousActive || Input.location.status == LocationServiceStatus.Stopped)
        {
            Input.location.Start(desiredAccuracyMeters, updateDistanceMeters);
            startedHere = true;
        }

        float elapsed = 0f;
        while (Input.location.status == LocationServiceStatus.Initializing && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (Input.location.status == LocationServiceStatus.Initializing)
        {
            if (startedHere && !continuousActive) Input.location.Stop();
            onComplete(LocationResult.Fail(LocationStatus.Timeout, ""));
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            if (startedHere && !continuousActive) Input.location.Stop();
            onComplete(LocationResult.Fail(LocationStatus.Failed, ""));
            yield break;
        }

        // 4) 오차가 크면 잠시 더 기다리며 정확도가 개선되는지 본다.
        LocationInfo data = Input.location.lastData;
        float improveWaited = 0f;
        while (data.horizontalAccuracy > maxAcceptableAccuracyMeters && improveWaited < accuracyImproveWaitSeconds)
        {
            improveWaited += Time.unscaledDeltaTime;
            yield return null;

            if (Input.location.status != LocationServiceStatus.Running) break;
            data = Input.location.lastData;
        }

        // 연속 모드일 때는 끄지 않는다 (다음 인증에서 바로 안정된 좌표를 쓰기 위해)
        if (!continuousActive) Input.location.Stop();

        Debug.Log($"[GPS] 좌표 획득: {data.latitude:F6}, {data.longitude:F6} (정확도 {data.horizontalAccuracy:F0}m, 연속모드={continuousActive})");

        if (data.horizontalAccuracy > maxAcceptableAccuracyMeters)
        {
            Debug.Log($"[GPS] 정확도 부족: {data.horizontalAccuracy:F0}m > 기준 {maxAcceptableAccuracyMeters:F0}m");
            onComplete(LocationResult.Fail(
                LocationStatus.LowAccuracy,
                $"위치 정확도가 낮습니다 (오차 약 {data.horizontalAccuracy:F0}m). 건물 밖 하늘이 보이는 곳에서 다시 시도해 주세요."));
            yield break;
        }

        onComplete(LocationResult.Ok(data.latitude, data.longitude, data.horizontalAccuracy));
    }
}
