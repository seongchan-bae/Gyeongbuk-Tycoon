using UnityEngine;

/// <summary>
/// RectTransform을 기기의 안전 영역(Screen.safeArea)에 맞춘다.
/// 노치·펀치홀·둥근 모서리 때문에 화면 구석의 UI가 가려지는 것을 막는다.
/// 이 오브젝트의 자식으로 UI를 넣으면 자동으로 안쪽에 배치된다.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Tooltip("안전 영역에서 추가로 더 안쪽으로 들일 여백(픽셀). " +
             "둥근 모서리는 safeArea에 반영되지 않는 기기가 많아 약간의 여유가 필요하다.")]
    [SerializeField] private float extraInsetPixels = 24f;

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreen;

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply(force: true);
    }

    private void Update()
    {
        Apply(force: false);
    }

    private void Apply(bool force)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safe = Screen.safeArea;
        var screen = new Vector2Int(Screen.width, Screen.height);
        if (!force && safe == lastSafeArea && screen == lastScreen) return;

        lastSafeArea = safe;
        lastScreen = screen;

        // 둥근 모서리 여유
        float inset = Mathf.Max(0f, extraInsetPixels);
        safe.xMin += inset;
        safe.xMax -= inset;
        safe.yMin += inset;
        safe.yMax -= inset;
        if (safe.width <= 0f || safe.height <= 0f) return;

        Vector2 anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
        Vector2 anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
