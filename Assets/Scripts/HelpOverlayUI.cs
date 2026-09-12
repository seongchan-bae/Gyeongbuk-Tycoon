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
///
/// 한 단계는 "설명할 대상" 과 "그 설명을 보려면 화면이 어떤 상태여야 하는가" 를 함께 들고 있다
/// (<see cref="HelpEntry.activate"/> / <see cref="HelpEntry.deactivate"/>).
/// 그래서 메인 화면뿐 아니라 환경설정·상점·미니게임 창 안까지 한 번에 이어서 안내할 수 있다.
/// 도움말을 닫으면 건드린 오브젝트는 모두 열기 전 켜짐/꺼짐 상태로 되돌린다.
///
/// 단계 목록은 손으로 채워도 되지만 항목이 많아서, 에디터 메뉴
/// [Tools/경북 타이쿤/도움말 투어 구성] 으로 한 번에 만들어 두는 것을 권한다.
/// </summary>
public class HelpOverlayUI : MonoBehaviour
{
    [Serializable]
    public class HelpEntry
    {
        [Tooltip("인스펙터에서 알아보기 위한 이름. 동작에는 쓰이지 않는다.")]
        public string label;

        [Tooltip("진행 표시에 함께 보여줄 구간 이름. 예: 메인 화면 / 환경설정 / 상점 / 미니게임")]
        public string section;

        [Tooltip("설명을 붙일 UI 요소")]
        public RectTransform target;

        [TextArea(2, 6)]
        public string description;

        [Tooltip("x = 좌우 보정, y = 대상 가장자리와 말풍선 사이의 간격. " +
                 "말풍선은 대상이 화면 위쪽이면 아래, 아래쪽이면 위에 자동으로 붙는다.")]
        public Vector2 offset = new Vector2(0f, 44f);

        [Tooltip("이 단계로 들어올 때 켤 오브젝트. 설명할 창을 여는 데 쓴다.")]
        public List<GameObject> activate = new List<GameObject>();

        [Tooltip("이 단계로 들어올 때 끌 오브젝트. 설명할 창을 가리는 것을 치우는 데 쓴다.")]
        public List<GameObject> deactivate = new List<GameObject>();
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

    [Tooltip("말풍선 꼬리(45도 돌린 마름모). 말풍선과 형제로 두고 말풍선보다 먼저 그려야 " +
             "두 도형의 이음선이 보이지 않는다. [Tools/경북 타이쿤/도움말 말풍선 꼬리 정리] 가 이 배치를 맞춰 준다.")]
    [SerializeField] private RectTransform tail;

    [Serializable]
    public class SectionTrigger
    {
        [Tooltip("이 버튼이 열 구간 이름. HelpEntry.section 과 글자가 똑같아야 한다.")]
        public string section;

        [Tooltip("그 창 안에 놓인 도움말 버튼")]
        public Button button;
    }

    [Header("버튼")]
    [Tooltip("메인 화면의 도움말 버튼")]
    [SerializeField] private Button openButton;

    [Tooltip("openButton 이 열 구간 이름. 비워두면 모든 단계를 순서대로 보여준다.")]
    [SerializeField] private string openButtonSection = "메인 화면";

    [Tooltip("환경설정·상점·미니게임 창 안에 각각 놓인 도움말 버튼. 버튼마다 자기 창의 설명만 보여 준다.")]
    [SerializeField] private List<SectionTrigger> sectionTriggers = new List<SectionTrigger>();

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

    [Header("말풍선 크기")]
    [Tooltip("설명 길이에 맞춰 말풍선 높이를 늘린다. 폭은 씬에 설정한 값을 그대로 쓴다.")]
    [SerializeField] private bool autoBubbleHeight = true;
    [SerializeField] private float bubbleMinHeight = 160f;
    [SerializeField] private float bubbleMaxHeight = 560f;

    [Header("꼬리")]
    [Tooltip("말풍선 변 바깥으로 꼬리가 나오는 길이. 0 이면 마름모의 절반만 나오도록 자동 계산한다.")]
    [SerializeField] private float tailProtrusion = 0f;

    [Tooltip("꼬리가 말풍선의 둥근 모서리를 넘어가지 않도록 좌우에서 띄우는 거리")]
    [SerializeField] private float tailCornerInset = 24f;

    [Header("배경음악")]
    [Tooltip("도움말이 미니게임 화면을 띄우면 그 화면의 배경음악이 시작될 수 있다. " +
             "켜 두면 도움말을 닫을 때 열기 전 곡으로 되돌린다.")]
    [SerializeField] private bool restoreBgmOnClose = true;

    private Canvas overlayCanvas;
    private int index;
    private bool isOpen;

    /// <summary>실제로 보여줄 단계만 모은 목록. 대상과 설명이 모두 빈 항목은 건너뛴다.</summary>
    private readonly List<HelpEntry> steps = new List<HelpEntry>();

    /// <summary>도움말을 열기 전의 켜짐/꺼짐 상태. 닫을 때 그대로 되돌린다.</summary>
    private readonly Dictionary<GameObject, bool> originalActive = new Dictionary<GameObject, bool>();

    /// <summary>상태를 적용하는 순서. 부모가 먼저 오도록 계층 깊이 오름차순으로 둔다.</summary>
    private readonly List<GameObject> stateOrder = new List<GameObject>();

    private readonly Dictionary<GameObject, bool> desiredActive = new Dictionary<GameObject, bool>();

    private Graphic bubbleGraphic;
    private Graphic tailGraphic;

    /// <summary>도움말을 열기 전에 흐르던 배경음악. 닫을 때 이 곡으로 되돌린다.</summary>
    private string bgmBeforeOpen;

    /// <summary>지금 보여 주는 구간. 비어 있으면 모든 단계를 순서대로 보여준다.</summary>
    private string activeSection;

    private void Awake()
    {
        overlayCanvas = overlayArea != null ? overlayArea.GetComponentInParent<Canvas>() : null;

        if (bubble != null) bubbleGraphic = bubble.GetComponent<Graphic>();
        if (tail != null) tailGraphic = tail.GetComponent<Graphic>();

        // 꼬리와 몸통의 색이 어긋나면 이음선이 드러난다. 몸통 색을 그대로 따르게 한다.
        if (bubbleGraphic != null && tailGraphic != null) tailGraphic.color = bubbleGraphic.color;

        if (openButton != null) openButton.onClick.AddListener(Open);

        // 창마다 놓인 도움말 버튼은 그 창의 구간만 연다.
        for (int i = 0; i < sectionTriggers.Count; i++)
        {
            SectionTrigger trigger = sectionTriggers[i];
            if (trigger == null || trigger.button == null) continue;

            string section = trigger.section;   // 람다가 반복 변수를 붙잡지 않도록 복사한다
            trigger.button.onClick.AddListener(() => OpenSection(section));
        }

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (prevButton != null) prevButton.onClick.AddListener(Prev);
        if (nextButton != null) nextButton.onClick.AddListener(Next);

        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    private void OnDisable()
    {
        // 이 오브젝트가 꺼질 때 도움말이 열어 둔 창을 그대로 남기지 않는다.
        // 단, 씬이 내려가는 중이라면 손대지 않는다. 파괴 중인 오브젝트에 SetActive 를 부르면 경고가 난다.
        if (isOpen && gameObject.scene.isLoaded)
        {
            RestoreScreenState();
            RestoreBgm();
        }
        isOpen = false;
    }

    /// <summary>메인 화면의 도움말 버튼에서 호출. openButtonSection 구간을 연다.</summary>
    public void Open()
    {
        OpenSection(openButtonSection);
    }

    /// <summary>
    /// 한 구간만 골라서 안내한다. 창마다 놓인 도움말 버튼이 각자 자기 구간 이름으로 부른다.
    /// 구간 이름을 비우면 전체 단계를 순서대로 보여준다.
    /// </summary>
    public void OpenSection(string section)
    {
        if (overlayRoot == null) return;

        activeSection = section;

        BuildSteps();
        if (steps.Count == 0)
        {
            Debug.LogWarning($"[HelpOverlayUI] '{section}' 구간에 보여줄 단계가 없습니다. " +
                             "HelpEntry.section 의 글자가 버튼에 지정한 구간 이름과 같은지 확인하세요.", this);
            return;
        }

        CaptureScreenState();
        bgmBeforeOpen = SoundManager.Instance != null ? SoundManager.Instance.CurrentBGMName : null;
        isOpen = true;
        overlayRoot.SetActive(true);
        GoTo(0);
    }

    public void Close()
    {
        if (isOpen)
        {
            RestoreScreenState();
            RestoreBgm();
        }
        isOpen = false;
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    /// <summary>
    /// 미니게임 화면을 띄우면 그 화면의 BGMManager 가 미니게임 곡을 걸어 버린다.
    /// 도움말은 화면만 보여 준 것이므로 닫을 때 원래 곡으로 돌려놓는다.
    /// </summary>
    private void RestoreBgm()
    {
        if (!restoreBgmOnClose) return;
        if (string.IsNullOrEmpty(bgmBeforeOpen)) return;
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.PlayBGM(bgmBeforeOpen);
    }

    public void Next()
    {
        if (index >= steps.Count - 1) { Close(); return; }
        GoTo(index + 1);
    }

    public void Prev()
    {
        if (index <= 0) return;
        GoTo(index - 1);
    }

    /// <summary>에디터에서 배치를 미리 확인할 때 쓰는 진입점.</summary>
    public void RefreshLayout() => Layout();

    /// <summary>에디터 검증용. 특정 단계를 바로 띄운다.</summary>
    public void ShowStep(int i)
    {
        if (overlayRoot == null) return;

        BuildSteps();
        if (steps.Count == 0) return;

        if (!isOpen)
        {
            CaptureScreenState();
            isOpen = true;
            overlayRoot.SetActive(true);
        }
        GoTo(i);
    }

    public int StepCount
    {
        get
        {
            BuildSteps();
            return steps.Count;
        }
    }

    /// <summary>열려 있는 동안 매 프레임 다시 맞춘다. 화면 회전이나 SafeArea 변화에도 따라붙는다.</summary>
    private void LateUpdate()
    {
        if (isOpen && overlayRoot != null && overlayRoot.activeSelf) Layout();
    }

    private void BuildSteps()
    {
        steps.Clear();
        bool everySection = string.IsNullOrEmpty(activeSection);

        for (int i = 0; i < entries.Count; i++)
        {
            HelpEntry e = entries[i];
            if (e == null) continue;
            if (e.target == null && string.IsNullOrEmpty(e.description)) continue;
            if (!everySection && e.section != activeSection) continue;
            steps.Add(e);
        }
    }

    private void GoTo(int i)
    {
        index = Mathf.Clamp(i, 0, steps.Count - 1);
        ApplyScreenState();
        ApplyBubbleContent();
        Layout();
    }

    // ───────────────────────── 화면 상태 ─────────────────────────

    /// <summary>단계들이 건드릴 모든 오브젝트의 현재 상태를 기억한다.</summary>
    private void CaptureScreenState()
    {
        originalActive.Clear();
        stateOrder.Clear();

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] == null) continue;
            Remember(entries[i].activate);
            Remember(entries[i].deactivate);
        }

        stateOrder.Sort((a, b) => HierarchyDepth(a).CompareTo(HierarchyDepth(b)));
    }

    private void Remember(List<GameObject> list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            GameObject go = list[i];
            if (go == null || originalActive.ContainsKey(go)) continue;
            originalActive.Add(go, go.activeSelf);
            stateOrder.Add(go);
        }
    }

    /// <summary>
    /// 지금 단계가 요구하는 화면을 만든다. 기준은 "도움말을 열기 전 상태 + 이 단계의 지정값" 이라서
    /// '이전' 으로 되돌아가도 뒤 단계가 켜 둔 창이 남지 않는다.
    /// </summary>
    private void ApplyScreenState()
    {
        if (stateOrder.Count == 0) return;

        desiredActive.Clear();
        foreach (KeyValuePair<GameObject, bool> pair in originalActive) desiredActive[pair.Key] = pair.Value;

        HelpEntry e = steps[index];
        Override(e.deactivate, false);
        Override(e.activate, true);

        ApplyStates(desiredActive);
    }

    private void Override(List<GameObject> list, bool value)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null) desiredActive[list[i]] = value;
        }
    }

    private void RestoreScreenState()
    {
        ApplyStates(originalActive);
    }

    /// <summary>
    /// 부모부터 차례로 적용한다. 창을 켤 때 그 창의 OnEnable 이 자식 패널을 다시 정리하는 경우가
    /// 있어서(예: 환경설정 창은 열릴 때마다 첫 번째 탭으로 돌아간다) 자식을 나중에 적용해야
    /// 보여주려던 탭이 남는다.
    /// </summary>
    private void ApplyStates(Dictionary<GameObject, bool> states)
    {
        for (int i = 0; i < stateOrder.Count; i++)
        {
            GameObject go = stateOrder[i];
            if (go == null) continue;

            bool want;
            if (!states.TryGetValue(go, out want)) continue;
            if (go.activeSelf != want) go.SetActive(want);
        }
    }

    private static int HierarchyDepth(GameObject go)
    {
        int depth = 0;
        Transform t = go != null ? go.transform : null;
        while (t != null && t.parent != null) { depth++; t = t.parent; }
        return depth;
    }

    // ───────────────────────── 말풍선 ─────────────────────────

    /// <summary>글과 진행 표시를 채우고, 설명 길이에 맞춰 말풍선 높이를 늘린다.</summary>
    private void ApplyBubbleContent()
    {
        HelpEntry e = steps[index];

        if (bubbleText != null)
        {
            bubbleText.text = e.description;

            if (autoBubbleHeight && bubble != null)
            {
                Vector2 pad = TextPadding();
                float textWidth = Mathf.Max(1f, bubble.rect.width - pad.x);
                float textHeight = bubbleText.GetPreferredValues(e.description, textWidth, 0f).y;
                float height = Mathf.Clamp(textHeight + pad.y, bubbleMinHeight, bubbleMaxHeight);
                bubble.sizeDelta = new Vector2(bubble.sizeDelta.x, height);
            }
        }

        if (stepText != null)
        {
            // 구간 이름은 한 줄 위에 작게 얹는다. 한 줄로 붙이면 좌우의 이전/다음 버튼을 침범한다.
            string progress = (index + 1) + " / " + steps.Count;
            stepText.text = string.IsNullOrEmpty(e.section)
                ? progress
                : "<size=60%>" + e.section + "</size>\n" + progress;
        }

        if (nextButtonLabel != null) nextButtonLabel.text = index >= steps.Count - 1 ? "완료" : "다음 ▶";
        if (prevButton != null) prevButton.interactable = index > 0;
    }

    /// <summary>글 영역이 말풍선에 꽉 늘어나 있으면 그 여백을 그대로 쓴다.</summary>
    private Vector2 TextPadding()
    {
        RectTransform tr = bubbleText != null ? bubbleText.rectTransform : null;
        if (tr != null && tr.anchorMin == Vector2.zero && tr.anchorMax == Vector2.one)
        {
            return new Vector2(Mathf.Abs(tr.sizeDelta.x), Mathf.Abs(tr.sizeDelta.y));
        }
        return new Vector2(56f, 48f);
    }

    private void Layout()
    {
        if (overlayArea == null || steps.Count == 0) return;
        index = Mathf.Clamp(index, 0, steps.Count - 1);
        HelpEntry e = steps[index];

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

        if (bubble == null) return;

        if (!hasTarget)
        {
            // 대상이 없는 안내는 화면 가운데에 띄우고 꼬리는 숨긴다.
            bubble.anchoredPosition = new Vector2(0f, bottomReservedHeight * 0.5f);
            if (tail != null) tail.gameObject.SetActive(false);
            return;
        }

        Vector2 half = bubble.rect.size * 0.5f;

        // 대상이 화면 위쪽이면 아래에, 아래쪽이면 위에 붙인다.
        // 간격은 대상의 '가장자리' 를 기준으로 재서, 큰 대상을 말풍선이 덮어버리지 않게 한다.
        float dir = targetRect.center.y >= 0f ? -1f : 1f;
        float targetEdge = dir < 0f ? targetRect.yMin : targetRect.yMax;
        float gap = Mathf.Abs(e.offset.y);

        Vector2 desired = new Vector2(
            targetRect.center.x + e.offset.x,
            targetEdge + dir * (gap + half.y));

        float minX = area.xMin + half.x + screenPadding;
        float maxX = area.xMax - half.x - screenPadding;
        float minY = area.yMin + half.y + screenPadding + bottomReservedHeight;
        float maxY = area.yMax - half.y - screenPadding;

        // 말풍선이 화면보다 크면 클램프 범위가 뒤집히므로 중앙에 둔다.
        desired.x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : 0f;
        desired.y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : 0f;
        bubble.anchoredPosition = desired;

        LayoutTail(desired, half, targetRect, dir);
    }

    /// <summary>
    /// 꼬리를 말풍선의 위/아래 변에 맞춘다.
    ///
    /// 꼬리는 말풍선과 형제로 두고 말풍선보다 먼저 그리는 것을 전제로 한다. 그러면 몸통이
    /// 꼬리의 안쪽 절반을 덮어버려서 두 도형의 경계선이 아예 생기지 않는다. 꼬리를 말풍선의
    /// 자식으로 두면 언제나 몸통 위에 그려지므로, 색을 맞춰도 테두리와 안티에일리어싱이 겹친
    /// 선이 남는다. 예전 배치(자식)도 그대로 동작하게 두되 그 경우에는 이음선이 보일 수 있다.
    /// </summary>
    private void LayoutTail(Vector2 bubblePos, Vector2 half, Rect targetRect, float dir)
    {
        if (tail == null) return;
        tail.gameObject.SetActive(true);

        float halfTail = TailHalfHeight();
        float protrusion = tailProtrusion > 0f ? tailProtrusion : halfTail;

        // dir < 0 이면 말풍선이 대상보다 아래에 있으므로, 꼬리는 위쪽 변에서 위로 나간다.
        float outward = dir < 0f ? 1f : -1f;
        float localY = outward * (half.y + protrusion - halfTail);

        float limit = Mathf.Max(0f, half.x - tailCornerInset);
        float localX = Mathf.Clamp(targetRect.center.x - bubblePos.x, -limit, limit);

        bool childOfBubble = tail.parent == bubble;
        tail.anchoredPosition = childOfBubble
            ? new Vector2(localX, localY)
            : bubblePos + new Vector2(localX, localY);
    }

    /// <summary>돌아간 꼬리의 세로 반지름. 45도 돌린 정사각형이면 대각선의 절반이 된다.</summary>
    private float TailHalfHeight()
    {
        Vector2 size = tail.rect.size;
        float angle = tail.localEulerAngles.z * Mathf.Deg2Rad;
        return (Mathf.Abs(size.x * Mathf.Sin(angle)) + Mathf.Abs(size.y * Mathf.Cos(angle))) * 0.5f;
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

#if UNITY_EDITOR
    // 아래는 에디터 도구(HelpTourBuilder)가 씬을 정리할 때만 쓴다. 런타임 코드에서 부르지 말 것.

    public RectTransform EditorOverlayArea => overlayArea;
    public RectTransform EditorBubble => bubble;
    public RectTransform EditorTail => tail;
    public Button EditorOpenButton => openButton;

    public void EditorReplaceEntries(List<HelpEntry> newEntries)
    {
        entries = newEntries ?? new List<HelpEntry>();
    }

    public void EditorReplaceSectionTriggers(List<SectionTrigger> newTriggers)
    {
        sectionTriggers = newTriggers ?? new List<SectionTrigger>();
    }

    public void EditorSetOpenButtonSection(string section)
    {
        openButtonSection = section;
    }
#endif
}
