using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도움말 오버레이. 물음표 버튼을 누르면 화면이 어두워지고 설명 대상 하나만 밝게 남는다.
/// 말풍선은 한 번에 하나씩 뜨고 '이전' / '다음' 으로 넘긴다.
///
/// 어둡게 처리는 반투명 판 하나를 화면 전체에 덮는 대신, 대상 사각형을 뺀
/// 위·아래·왼쪽·오른쪽 네 장으로 나눠 덮는다. 그래야 설명 중인 UI 만 원래 밝기로 보인다.
///
/// 말풍선은 대상과 다른 캔버스에 있을 수 있으므로 좌표를 직접 복사하지 않고
/// 화면 좌표를 거쳐 오버레이 기준으로 환산한다. 대상 캔버스의 스케일 설정이 달라도 정확히 붙는다.
/// </summary>
public class HelpOverlayUI : MonoBehaviour
{
    [Serializable]
    public class HelpEntry
    {
        [Tooltip("인스펙터에서 알아보기 위한 이름. 동작에는 쓰이지 않는다.")]
        public string label;

        [Tooltip("설명을 붙일 HUD 요소")]
        public RectTransform target;

        [TextArea(2, 5)]
        public string description;

        [Tooltip("대상 중심에서 말풍선까지의 거리. y 부호는 화면 위/아래에 따라 자동으로 뒤집힌다.")]
        public Vector2 offset = new Vector2(0f, -130f);
    }

    [Header("오버레이")]
    [Tooltip("어두운 판과 말풍선을 모두 담은 루트. 평소에는 꺼져 있다.")]
    [SerializeField] private GameObject overlayRoot;

    [Tooltip("좌표 계산의 기준이 되는 RectTransform (오버레이 캔버스 전체)")]
    [SerializeField] private RectTransform overlayArea;

    [Header("어둡게 덮는 판 (대상만 남기고 4방향)")]
    [SerializeField] private RectTransform dimTop;
    [SerializeField] private RectTransform dimBottom;
    [SerializeField] private RectTransform dimLeft;
    [SerializeField] private RectTransform dimRight;

    [Tooltip("설명 중인 대상을 감싸는 강조 테두리")]
    [SerializeField] private RectTransform highlight;

    [Header("말풍선")]
    [SerializeField] private RectTransform bubble;
    [SerializeField] private TMP_Text bubbleText;
    [SerializeField] private RectTransform tail;

    [Header("버튼")]
    [SerializeField] private Button openButton;

    [Tooltip("배경을 눌러도 닫히게 하는 전체 화면 버튼")]
    [SerializeField] private Button closeButton;

    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Tooltip("'다음' 버튼의 글자. 마지막 단계에서는 '완료' 로 바뀐다.")]
    [SerializeField] private TMP_Text nextButtonLabel;

    [Tooltip("'2 / 4' 같은 진행 표시")]
    [SerializeField] private TMP_Text stepText;

    [Header("설명 목록")]
    [SerializeField] private List<HelpEntry> entries = new List<HelpEntry>();

    [Header("여백")]
    [SerializeField] private float screenPadding = 24f;
    [SerializeField] private float highlightPadding = 10f;

    [Tooltip("화면 아래쪽에서 말풍선이 들어오지 못하게 비워두는 높이. 내비게이션 바 자리를 지킨다.")]
    [SerializeField] private float bottomReservedHeight = 190f;

    private Canvas overlayCanvas;
    private int index;

    private void Awake()
    {
        overlayCanvas = overlayArea != null ? overlayArea.GetComponentInParent<Canvas>() : null;

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (prevButton != null) prevButton.onClick.AddListener(Prev);
        if (nextButton != null) nextButton.onClick.AddListener(Next);

        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    public void Open()
    {
        if (overlayRoot == null || entries.Count == 0) return;
        index = 0;
        overlayRoot.SetActive(true);
        Layout();
    }

    public void Close()
    {
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    public void Next()
    {
        if (index >= entries.Count - 1) { Close(); return; }
        index++;
        Layout();
    }

    public void Prev()
    {
        if (index <= 0) return;
        index--;
        Layout();
    }

    /// <summary>에디터에서 배치를 미리 확인할 때 쓰는 진입점.</summary>
    public void RefreshLayout() => Layout();

    /// <summary>에디터 검증용. 특정 단계를 바로 띄운다.</summary>
    public void ShowStep(int i)
    {
        if (entries.Count == 0) return;
        index = Mathf.Clamp(i, 0, entries.Count - 1);
        if (overlayRoot != null) overlayRoot.SetActive(true);
        Layout();
    }

    public int StepCount => entries.Count;

    /// <summary>열려 있는 동안 매 프레임 다시 맞춘다. 화면 회전이나 SafeArea 변화에도 따라붙는다.</summary>
    private void LateUpdate()
    {
        if (overlayRoot != null && overlayRoot.activeSelf) Layout();
    }

    private void Layout()
    {
        if (overlayArea == null || entries.Count == 0) return;
        index = Mathf.Clamp(index, 0, entries.Count - 1);
        HelpEntry e = entries[index];

        // Screen Space - Overlay 캔버스는 카메라를 넘기면 안 된다.
        Camera cam = (overlayCanvas != null && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? overlayCanvas.worldCamera
            : null;

        Rect area = overlayArea.rect;

        // ---- 대상 사각형을 오버레이 기준 좌표로 ----
        Rect targetRect = new Rect(0f, 0f, 0f, 0f);
        bool hasTarget = e.target != null && e.target.gameObject.activeInHierarchy;
        if (hasTarget)
        {
            var corners = new Vector3[4];
            e.target.GetWorldCorners(corners);
            Vector2 min, max;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayArea, RectTransformUtility.WorldToScreenPoint(cam, corners[0]), cam, out min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayArea, RectTransformUtility.WorldToScreenPoint(cam, corners[2]), cam, out max);
            targetRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        ApplyDim(area, targetRect, hasTarget);

        if (highlight != null)
        {
            highlight.gameObject.SetActive(hasTarget);
            if (hasTarget)
            {
                highlight.anchoredPosition = targetRect.center;
                highlight.sizeDelta = targetRect.size + Vector2.one * (highlightPadding * 2f);
            }
        }

        // ---- 말풍선 ----
        if (bubbleText != null) bubbleText.text = e.description;
        if (stepText != null) stepText.text = (index + 1) + " / " + entries.Count;
        if (nextButtonLabel != null) nextButtonLabel.text = index >= entries.Count - 1 ? "완료" : "다음 ▶";
        if (prevButton != null) prevButton.interactable = index > 0;

        if (bubble == null || !hasTarget) return;

        // 대상이 화면 위쪽이면 아래에, 아래쪽이면 위에 붙인다.
        float dir = targetRect.center.y >= 0f ? -1f : 1f;
        Vector2 desired = targetRect.center + new Vector2(e.offset.x, Mathf.Abs(e.offset.y) * dir);

        Vector2 half = bubble.rect.size * 0.5f;
        float minX = area.xMin + half.x + screenPadding;
        float maxX = area.xMax - half.x - screenPadding;
        float minY = area.yMin + half.y + screenPadding + bottomReservedHeight;
        float maxY = area.yMax - half.y - screenPadding;

        // 말풍선이 화면보다 크면 클램프 범위가 뒤집히므로 중앙에 둔다.
        desired.x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : 0f;
        desired.y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : 0f;
        bubble.anchoredPosition = desired;

        if (tail != null)
        {
            float tailY = dir < 0f ? half.y : -half.y;
            float tailX = Mathf.Clamp(targetRect.center.x - desired.x, -half.x + 24f, half.x - 24f);
            tail.anchoredPosition = new Vector2(tailX, tailY);
        }
    }

    /// <summary>대상 사각형을 뺀 나머지를 네 장의 판으로 덮는다.</summary>
    private void ApplyDim(Rect area, Rect target, bool hasTarget)
    {
        if (!hasTarget)
        {
            // 대상이 없으면 위쪽 판 하나로 전체를 덮고 나머지는 접는다.
            SetPanel(dimTop, area);
            SetPanel(dimBottom, Rect.zero);
            SetPanel(dimLeft, Rect.zero);
            SetPanel(dimRight, Rect.zero);
            return;
        }

        // 대상이 화면을 벗어나 있어도 판이 음수 크기가 되지 않도록 자른다.
        float tx0 = Mathf.Clamp(target.xMin, area.xMin, area.xMax);
        float tx1 = Mathf.Clamp(target.xMax, area.xMin, area.xMax);
        float ty0 = Mathf.Clamp(target.yMin, area.yMin, area.yMax);
        float ty1 = Mathf.Clamp(target.yMax, area.yMin, area.yMax);

        SetPanel(dimTop, Rect.MinMaxRect(area.xMin, ty1, area.xMax, area.yMax));
        SetPanel(dimBottom, Rect.MinMaxRect(area.xMin, area.yMin, area.xMax, ty0));
        SetPanel(dimLeft, Rect.MinMaxRect(area.xMin, ty0, tx0, ty1));
        SetPanel(dimRight, Rect.MinMaxRect(tx1, ty0, area.xMax, ty1));
    }

    private static void SetPanel(RectTransform rt, Rect r)
    {
        if (rt == null) return;
        bool visible = r.width > 0.01f && r.height > 0.01f;
        rt.gameObject.SetActive(visible);
        if (!visible) return;
        rt.anchoredPosition = r.center;
        rt.sizeDelta = r.size;
    }
}
