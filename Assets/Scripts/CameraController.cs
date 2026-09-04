using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 메인 카메라의 이동 제한(배경 밖으로 못 나가게)과 확대/축소를 담당한다.
/// 확대/축소 입력: 마우스 휠(PC) / 두 손가락 핀치(모바일) / +,- 키 / UI 버튼.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    /// <summary>두 손가락 핀치 중인지 여부. 핀치 중에는 건물/카메라 드래그를 막는 데 쓴다.</summary>
    public static bool IsPinching { get; private set; }

    [Header("카메라 이동 제한")]
    [SerializeField] private SpriteRenderer backgroundSprite;

    [Header("확대/축소")]
    [Tooltip("가장 확대했을 때의 orthographicSize (작을수록 크게 보임)")]
    [SerializeField] private float minZoom = 3f;
    [Tooltip("가장 축소했을 때의 orthographicSize. 배경 밖이 보이지 않도록 자동으로 더 줄어들 수 있다.")]
    [SerializeField] private float maxZoom = 10f;
    [Tooltip("마우스 휠 한 칸당 변화량")]
    [SerializeField] private float wheelZoomSpeed = 1.5f;
    [Tooltip("버튼/키보드 한 번당 변화량")]
    [SerializeField] private float buttonZoomStep = 1.2f;
    [Tooltip("핀치 감도. 1이 기본, 값이 클수록 민감해진다.")]
    [SerializeField] private float pinchSensitivity = 1f;
    [Tooltip("확대/축소가 부드럽게 따라오는 시간(초). 0이면 즉시 반영.")]
    [SerializeField] private float zoomSmoothTime = 0.1f;
    [Tooltip("켜면 마우스 커서/핀치 중심을 기준으로 확대된다. 끄면 항상 화면 중앙 기준.")]
    [SerializeField] private bool zoomTowardPointer = true;

    [Header("확대/축소 버튼 (선택)")]
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;

    private Camera cam;
    private float targetSize = -1f;
    private float zoomVelocity;

    // 확대 기준점 — 이 화면 좌표 아래의 월드 위치가 그대로 유지되도록 카메라를 보정한다.
    private bool hasAnchor;
    private Vector2 anchorScreenPos;

    /// <summary>현재 orthographicSize. 작을수록 확대된 상태.</summary>
    public float CurrentZoom => ResolveCamera() != null ? ResolveCamera().orthographicSize : 0f;

    void Awake()
    {
        // backgroundSprite가 연결된 쪽이 Instance가 되도록 우선순위 부여
        if (Instance == null || (backgroundSprite != null && Instance.backgroundSprite == null))
        {
            Instance = this;
        }
    }

    void Start()
    {
        cam = ResolveCamera();
        if (cam != null) targetSize = cam.orthographicSize;

        if (zoomInButton != null) zoomInButton.onClick.AddListener(ZoomIn);
        if (zoomOutButton != null) zoomOutButton.onClick.AddListener(ZoomOut);
    }

    void OnDestroy()
    {
        if (zoomInButton != null) zoomInButton.onClick.RemoveListener(ZoomIn);
        if (zoomOutButton != null) zoomOutButton.onClick.RemoveListener(ZoomOut);
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // 씬에 CameraController가 여러 개 있어도 확대/축소 입력은 대표 인스턴스만 처리한다.
        if (Instance != this) return;
        HandleZoomInput();
    }

    void LateUpdate()
    {
        if (Instance == this) ApplyZoom();

        // 매 프레임 후처리로 카메라가 어떤 이유로든 범위를 벗어나면 즉시 보정
        ClampCamera();
    }

    // ---------------- 확대/축소 ----------------

    /// <summary>한 단계 확대. UI 버튼 OnClick에 그대로 연결할 수 있다.</summary>
    public void ZoomIn() => SetTargetSize(GetTargetSize() - buttonZoomStep);

    /// <summary>한 단계 축소. UI 버튼 OnClick에 그대로 연결할 수 있다.</summary>
    public void ZoomOut() => SetTargetSize(GetTargetSize() + buttonZoomStep);

    /// <summary>orthographicSize를 직접 지정한다(화면 중앙 기준).</summary>
    public void SetZoom(float size) => SetTargetSize(size);

    /// <summary>가장 축소한 상태로 되돌린다(맵 전체 보기).</summary>
    public void ResetZoom() => SetTargetSize(maxZoom);

    private void HandleZoomInput()
    {
        if (ResolveCamera() == null) return;

        // 두 손가락 핀치 (모바일)
        if (Input.touchCount >= 2)
        {
            HandlePinch();
            return;
        }
        IsPinching = false;

        // 마우스 휠 (PC/에디터) — UI 위에서는 무시
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f && !IsPointerOverUI())
        {
            // 휠을 위로 굴리면 확대 = orthographicSize 감소
            SetTargetSize(GetTargetSize() - wheel * wheelZoomSpeed, Input.mousePosition);
        }

        // 키보드 보조 입력
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) ZoomIn();
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) ZoomOut();
    }

    private void HandlePinch()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        // 손가락을 막 올려놓은 프레임은 delta가 튀므로 기준만 잡고 넘어간다.
        if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            IsPinching = true;
            return;
        }

        float prevDistance = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
        float currentDistance = (t0.position - t1.position).magnitude;
        if (prevDistance < 0.01f || currentDistance < 0.01f) return;

        IsPinching = true;

        // 손가락 간격이 벌어지면 ratio < 1 → orthographicSize 감소 → 확대
        float ratio = Mathf.Pow(prevDistance / currentDistance, Mathf.Max(0.01f, pinchSensitivity));
        Vector2 pinchCenter = (t0.position + t1.position) * 0.5f;
        SetTargetSize(GetTargetSize() * ratio, pinchCenter);
    }

    private void SetTargetSize(float size)
    {
        targetSize = Mathf.Clamp(size, GetMinSize(), GetMaxSize());
        hasAnchor = false; // 기준점 없음 = 화면 중앙 기준
    }

    private void SetTargetSize(float size, Vector2 anchorScreen)
    {
        targetSize = Mathf.Clamp(size, GetMinSize(), GetMaxSize());
        anchorScreenPos = anchorScreen;
        hasAnchor = zoomTowardPointer;
    }

    private float GetTargetSize()
    {
        if (targetSize <= 0f)
        {
            Camera c = ResolveCamera();
            targetSize = c != null ? c.orthographicSize : minZoom;
        }
        return targetSize;
    }

    private void ApplyZoom()
    {
        Camera c = ResolveCamera();
        if (c == null || !c.orthographic) return;

        // 화면 회전 등으로 한계가 바뀌었을 수 있으므로 매 프레임 다시 조인다.
        targetSize = Mathf.Clamp(GetTargetSize(), GetMinSize(), GetMaxSize());

        float current = c.orthographicSize;
        if (Mathf.Abs(current - targetSize) < 0.0005f)
        {
            c.orthographicSize = targetSize;
            hasAnchor = false;
            return;
        }

        Vector3 worldBefore = hasAnchor ? c.ScreenToWorldPoint(anchorScreenPos) : Vector3.zero;

        float next = zoomSmoothTime > 0f
            ? Mathf.SmoothDamp(current, targetSize, ref zoomVelocity, zoomSmoothTime)
            : targetSize;
        c.orthographicSize = Mathf.Clamp(next, GetMinSize(), GetMaxSize());

        if (hasAnchor)
        {
            // 기준점 아래의 월드 좌표가 그대로 유지되도록 카메라를 반대로 밀어준다.
            Vector3 worldAfter = c.ScreenToWorldPoint(anchorScreenPos);
            Vector3 shift = worldBefore - worldAfter;
            shift.z = 0f;
            c.transform.position += shift;
        }
    }

    /// <summary>확대 한계. 축소 한계보다 커지지 않도록 보정한다.</summary>
    private float GetMinSize() => Mathf.Min(Mathf.Max(0.1f, minZoom), GetMaxSize());

    /// <summary>축소 한계. 배경 밖이 보이지 않도록 배경 크기로도 제한한다.</summary>
    private float GetMaxSize()
    {
        float limit = Mathf.Max(0.1f, maxZoom);

        Camera c = ResolveCamera();
        if (backgroundSprite != null && c != null)
        {
            Bounds bg = backgroundSprite.bounds;
            float byHeight = bg.extents.y;
            float byWidth = c.aspect > 0.0001f ? bg.extents.x / c.aspect : byHeight;
            limit = Mathf.Min(limit, byHeight, byWidth);
        }

        return Mathf.Max(0.1f, limit);
    }

    private Camera ResolveCamera()
    {
        if (cam == null) cam = Camera.main;
        return cam;
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // ---------------- 이동 제한 ----------------

    public void ClampCamera()
    {
        if (backgroundSprite == null) return;

        Camera c = ResolveCamera();
        if (c == null) return;

        Bounds bg = backgroundSprite.bounds;
        float camH = c.orthographicSize;
        float camW = camH * c.aspect;

        float minX = bg.min.x + camW;
        float maxX = bg.max.x - camW;
        float minY = bg.min.y + camH;
        float maxY = bg.max.y - camH;

        Vector3 pos = c.transform.position;
        // 시야가 배경보다 넓은 축은 가둘 수 없으므로 배경 중앙에 맞춘다.
        pos.x = minX <= maxX ? Mathf.Clamp(pos.x, minX, maxX) : bg.center.x;
        pos.y = minY <= maxY ? Mathf.Clamp(pos.y, minY, maxY) : bg.center.y;
        c.transform.position = pos;
    }
}
