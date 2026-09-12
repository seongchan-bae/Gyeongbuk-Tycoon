using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 도움말 투어(<see cref="HelpOverlayUI"/>)의 단계 목록을 한 번에 만들어 주는 에디터 도구.
///
/// 단계가 서른 개가 넘고 설명글도 길어서 인스펙터에서 손으로 채우는 것은 현실적이지 않다.
/// 그래서 "어떤 UI 를, 어떤 화면 상태에서, 어떻게 설명할지" 를 이 파일의 표 하나에 모아 두고,
/// 메뉴를 실행하면 씬의 실제 오브젝트를 찾아 인스펙터 참조로 채워 넣는다.
/// 채운 뒤에는 평범한 씬 참조라서, 런타임 코드는 경로 문자열을 전혀 알지 못한다.
///
/// UI 이름이나 계층이 바뀌면 찾지 못한 경로를 콘솔에 남기고 그 단계만 건너뛴다.
/// 그때는 아래 경로 상수를 고친 뒤 메뉴를 다시 실행하면 된다.
/// </summary>
public static class HelpTourBuilder
{
    private const string MenuRoot = "Tools/경북 타이쿤/";

    // ───────────────────────── 씬 계층 경로 ─────────────────────────
    // 루트 오브젝트 이름부터 시작한다. 꺼져 있는 자식도 찾을 수 있다.

    private const string MainRoot = "UI/MainUI";
    private const string GoldRoot = "UI/GoldUI";
    private const string TouristRoot = "UI/TouristUI";
    private const string KnowledgeRoot = "UI/KnowledgeUI";

    private const string GoldBar = GoldRoot + "/Canvas/GoldBar";
    private const string KnowledgeBar = KnowledgeRoot + "/Canvas/KnowledgeBar";
    private const string TouristBar = TouristRoot + "/Canvas/TouristBar";
    private const string ShopButton = MainRoot + "/ShopButton";
    private const string MiniGameButton = MainRoot + "/MiniGameButton";

    private const string HudButtons = "Canvas/SafeArea/Buttons";
    private const string SettingsButton = HudButtons + "/SettingsButton";
    private const string HelpButton = HudButtons + "/HelpButton";
    private const string QuitButton = HudButtons + "/QuitButton";

    private const string SettingsPanel = "Canvas/SettingsPanel";
    private const string SettingsTabs = SettingsPanel + "/TabGroup";
    private const string SoundPanel = SettingsPanel + "/SoundSettingsPanel";
    private const string ThemePanel = SettingsPanel + "/ThemeSettingsPanel";
    private const string InfoPanel = SettingsPanel + "/InfoSettingsPanel";
    private const string BgmRow = SoundPanel + "/SoundSliderGroup/BGMRow";
    private const string SfxRow = SoundPanel + "/SoundSliderGroup/SFXRow";
    private const string BgmMuteButton = BgmRow + "/MuteButton";
    private const string ThemeSlots = ThemePanel + "/ThemeSlotContainer";
    private const string ThemeStatus = ThemePanel + "/ThemeStatusText";
    private const string SettingsClose = SettingsPanel + "/SafeArea/CloseButton";

    private const string ShopPanel = "UI/ShopUI";
    private const string ShopImage = ShopPanel + "/ShopImage";
    private const string BasicTab = ShopImage + "/BasicShopButton";
    private const string LandmarkTab = ShopImage + "/LandMarkShopButton";
    private const string MapTab = ShopImage + "/MapShopButton";
    private const string BasicShop = ShopImage + "/BasicShop";
    private const string LandmarkShop = ShopImage + "/LandMarkShop";
    private const string MapShop = ShopImage + "/MapShop";
    private const string BuildingCountText = BasicShop + "/CountBuildingText";
    private const string BasicCard = BasicShop + "/Viewport/Content/CardPanel 1";
    private const string LandmarkCard = LandmarkShop + "/Viewport/Content/CardPanel 1";
    private const string ShopClose = ShopImage + "/ShopCloseButton";

    private const string MiniGamePanelRoot = "UI/MiniGameCanvas/Panel";
    private const string MiniGameSelect = MiniGamePanelRoot + "/MiniGameSelectPanel";
    private const string MiniGameTitle = MiniGameSelect + "/Title";
    private const string GameSelectButtons = MiniGameSelect + "/GameSelectButtons";
    private const string CardGameButton = GameSelectButtons + "/CardGameButton";
    private const string YabawiGameButton = GameSelectButtons + "/YabawiGameButton";
    private const string PuzzleGameButton = GameSelectButtons + "/PuzzleGameButton";
    private const string SilueteGameButton = GameSelectButtons + "/SilueteGameButton";
    private const string MiniGameClose = MiniGameSelect + "/CloseButton";

    // 게임 패널들은 미니게임 선택 구간에서 "꺼 둘 대상" 으로만 쓴다.
    private const string CardGamePanel = MiniGamePanelRoot + "/CardMatchingGamePanel";
    private const string YabawiPanel = MiniGamePanelRoot + "/YabawiGamePanel";
    private const string PuzzlePanel = MiniGamePanelRoot + "/PuzzleUI";
    private const string SiluetePanel = MiniGamePanelRoot + "/SilueteGameUI";

    // 미니게임 화면에 들어가면 메인 HUD 는 전부 내려간다 (MiniGameHubUI.OpenHub 와 같은 처리).
    private static readonly string[] MainHud = { MainRoot, GoldRoot, TouristRoot, KnowledgeRoot };

    private static readonly string[] None = new string[0];

    /// <summary>창마다 새로 놓는 도움말 버튼의 오브젝트 이름. 이 이름으로 다시 찾아 갱신한다.</summary>
    private const string SectionHelpButtonName = "HelpButton";

    /// <summary>닫기 버튼과 도움말 버튼 사이 간격(px).</summary>
    private const float SectionHelpButtonGap = 16f;

    /// <summary>도움말 캔버스를 다른 캔버스보다 이만큼 위로 올린다.</summary>
    private const int SortingOrderMargin = 50;

    private const string SectionMain = "메인 화면";
    private const string SectionSettings = "환경설정";
    private const string SectionShop = "상점";
    private const string SectionMiniGame = "미니게임";

    // ───────────────────────── 메뉴 ─────────────────────────

    [MenuItem(MenuRoot + "도움말 투어 구성")]
    private static void BuildTour()
    {
        HelpOverlayUI overlay = FindOverlay();
        if (overlay == null) return;

        Undo.RegisterFullObjectHierarchyUndo(overlay.gameObject, "도움말 투어 구성");

        var missing = new List<string>();
        var built = new List<HelpOverlayUI.HelpEntry>();

        foreach (StepDef def in Steps())
        {
            Transform target = Find(def.target, missing);
            if (target == null) continue;

            var rect = target as RectTransform;
            if (rect == null)
            {
                Debug.LogWarning($"[HelpTourBuilder] '{def.target}' 은 RectTransform 이 아니어서 대상으로 쓸 수 없습니다.");
                continue;
            }

            built.Add(new HelpOverlayUI.HelpEntry
            {
                label = def.label,
                section = def.section,
                target = rect,
                description = def.description,
                offset = def.offset,
                activate = FindObjects(def.activate, missing),
                deactivate = FindObjects(def.deactivate, missing),
            });
        }

        overlay.EditorReplaceEntries(built);
        overlay.EditorSetOpenButtonSection(SectionMain);

        string tailReport = FixTailLayout(overlay);
        string sortingReport = RaiseOverlayAboveAllCanvases(overlay);
        string buttonReport = BuildSectionButtons(overlay, missing);

        EditorUtility.SetDirty(overlay);
        EditorSceneManager.MarkSceneDirty(overlay.gameObject.scene);

        var report = new StringBuilder();
        report.AppendLine($"[HelpTourBuilder] 도움말 단계 {built.Count}개를 구성했습니다. (표에 정의된 단계 {CountDefs()}개)");
        report.AppendLine(tailReport);
        report.AppendLine(sortingReport);
        report.AppendLine(buttonReport);
        if (missing.Count > 0)
        {
            report.AppendLine("찾지 못한 경로 (해당 단계는 빠졌습니다):");
            foreach (string path in missing) report.AppendLine("  - " + path);
        }
        report.Append("씬을 저장해야 반영됩니다.");

        if (missing.Count > 0) Debug.LogWarning(report.ToString(), overlay);
        else Debug.Log(report.ToString(), overlay);
    }

    [MenuItem(MenuRoot + "도움말 말풍선 꼬리 정리")]
    private static void FixTailOnly()
    {
        HelpOverlayUI overlay = FindOverlay();
        if (overlay == null) return;

        Undo.RegisterFullObjectHierarchyUndo(overlay.gameObject, "도움말 말풍선 꼬리 정리");

        string report = FixTailLayout(overlay);

        EditorUtility.SetDirty(overlay);
        EditorSceneManager.MarkSceneDirty(overlay.gameObject.scene);
        Debug.Log("[HelpTourBuilder] " + report, overlay);
    }

    // ───────────────────────── 캔버스 정렬 순서 ─────────────────────────

    /// <summary>
    /// 도움말 캔버스를 씬의 다른 모든 캔버스보다 위로 올린다.
    ///
    /// 메인 HUD 가 올라가 있는 루트 Canvas 의 정렬 순서가 200 인데 도움말 캔버스는 100 이었다.
    /// 그래서 그 캔버스에 얹힌 환경설정 창이 도움말 위에 그려져, 말풍선과 [이전]/[다음] 버튼이
    /// 가려지고 클릭도 먹히지 않았다. 어두운 판 역시 그 창을 덮지 못했다.
    ///
    /// 값을 고정으로 박으면 나중에 다른 캔버스를 더 올렸을 때 같은 문제가 되돌아오므로,
    /// 지금 씬에 있는 가장 높은 정렬 순서를 보고 그보다 위로 잡는다.
    /// </summary>
    private static string RaiseOverlayAboveAllCanvases(HelpOverlayUI overlay)
    {
        Canvas overlayCanvas = overlay.GetComponentInParent<Canvas>();
        if (overlayCanvas == null) return "도움말 캔버스를 찾지 못해 정렬 순서는 건너뜀.";

        Canvas root = overlayCanvas.rootCanvas != null ? overlayCanvas.rootCanvas : overlayCanvas;

        int highest = int.MinValue;
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas == root) continue;
            // 정렬을 물려받는 캔버스는 부모 값을 따르므로 자기 값은 의미가 없다.
            if (canvas.rootCanvas != canvas && !canvas.overrideSorting) continue;
            highest = Mathf.Max(highest, canvas.sortingOrder);
        }

        if (highest == int.MinValue) return $"도움말 캔버스 정렬 순서 {root.sortingOrder} 유지 (다른 캔버스 없음).";

        int wanted = highest + SortingOrderMargin;
        if (root.sortingOrder >= wanted) return $"도움말 캔버스 정렬 순서 {root.sortingOrder} 유지 (이미 가장 위).";

        Undo.RecordObject(root, "도움말 캔버스 정렬 순서");
        int before = root.sortingOrder;
        root.sortingOrder = wanted;
        EditorUtility.SetDirty(root);

        return $"도움말 캔버스 정렬 순서를 {before} → {wanted} 로 올림 (다음으로 높은 캔버스 {highest}).";
    }

    // ───────────────────────── 창별 도움말 버튼 ─────────────────────────

    /// <summary>
    /// 환경설정·상점·미니게임 창의 닫기 버튼 왼쪽에 도움말 버튼을 하나씩 두고,
    /// 각 버튼이 자기 창의 구간만 열도록 <see cref="HelpOverlayUI"/> 에 연결한다.
    ///
    /// 버튼 모양은 메인 화면의 도움말 버튼을 그대로 복제해서 쓰고, 크기와 위치는 옆에 둘
    /// 닫기 버튼에서 읽어 온다. 그래야 창마다 다른 버튼 크기에 저절로 맞는다.
    /// 이미 만들어 둔 버튼이 있으면 새로 만들지 않고 위치만 다시 맞춘다.
    /// </summary>
    private static string BuildSectionButtons(HelpOverlayUI overlay, List<string> missing)
    {
        Transform source = FindInScene(HelpButton);
        if (source == null)
        {
            missing.Add(HelpButton);
            return "메인 도움말 버튼을 찾지 못해 창별 버튼은 만들지 못했습니다.";
        }

        var triggers = new List<HelpOverlayUI.SectionTrigger>();
        var notes = new List<string>();

        foreach (SectionButtonDef def in SectionButtonDefs())
        {
            Transform close = Find(def.closeButton, missing);
            if (close == null) continue;

            var closeRect = close as RectTransform;
            if (closeRect == null)
            {
                notes.Add($"{def.section}: 닫기 버튼이 RectTransform 이 아니어서 건너뜀");
                continue;
            }

            Button button = EnsureHelpButton(source.gameObject, closeRect, notes, def.section);
            if (button == null) continue;

            triggers.Add(new HelpOverlayUI.SectionTrigger { section = def.section, button = button });
        }

        overlay.EditorReplaceSectionTriggers(triggers);

        string head = $"창별 도움말 버튼 {triggers.Count}개 연결";
        return notes.Count == 0 ? head + "." : head + " (" + string.Join(", ", notes.ToArray()) + ").";
    }

    private static Button EnsureHelpButton(GameObject source, RectTransform closeRect, List<string> notes, string section)
    {
        Transform existing = closeRect.parent != null ? closeRect.parent.Find(SectionHelpButtonName) : null;

        GameObject go;
        bool created = existing == null;
        if (created)
        {
            go = Object.Instantiate(source, closeRect.parent);
            go.name = SectionHelpButtonName;
            Undo.RegisterCreatedObjectUndo(go, "창별 도움말 버튼 추가");
            notes.Add($"{section}: 버튼 새로 만듦");
        }
        else
        {
            go = existing.gameObject;
        }

        var rect = go.GetComponent<RectTransform>();
        if (rect == null)
        {
            notes.Add($"{section}: 복제한 버튼에 RectTransform 이 없음");
            return null;
        }

        Undo.RecordObject(rect, "창별 도움말 버튼 배치");

        // 닫기 버튼과 같은 기준·같은 크기로 맞춘 뒤 왼쪽으로 한 칸 옮긴다.
        // anchoredPosition 은 부모의 로컬 축이라, 앵커가 어디에 붙어 있어도 x 를 줄이면 왼쪽이다.
        rect.anchorMin = closeRect.anchorMin;
        rect.anchorMax = closeRect.anchorMax;
        rect.pivot = closeRect.pivot;
        rect.localRotation = closeRect.localRotation;
        rect.localScale = closeRect.localScale;
        rect.sizeDelta = closeRect.sizeDelta;
        rect.anchoredPosition = closeRect.anchoredPosition
                                + new Vector2(-(closeRect.rect.width + SectionHelpButtonGap), 0f);

        // 새로 만든 버튼만 닫기 버튼 바로 앞으로 옮긴다. 이미 있던 버튼의 순서는 건드리지 않는다
        // (SetSiblingIndex 는 '뺀 뒤 끼워 넣기' 라서, 다시 실행할 때마다 앞뒤가 뒤바뀐다).
        if (created) rect.SetSiblingIndex(closeRect.GetSiblingIndex());

        EditorUtility.SetDirty(rect);

        var button = go.GetComponent<Button>();
        if (button == null) notes.Add($"{section}: 복제한 버튼에 Button 컴포넌트가 없음");
        return button;
    }

    private struct SectionButtonDef
    {
        public string section;
        public string closeButton;
    }

    private static IEnumerable<SectionButtonDef> SectionButtonDefs()
    {
        yield return new SectionButtonDef { section = SectionSettings, closeButton = SettingsClose };
        yield return new SectionButtonDef { section = SectionShop, closeButton = ShopClose };
        yield return new SectionButtonDef { section = SectionMiniGame, closeButton = MiniGameClose };
    }

    // ───────────────────────── 말풍선 꼬리 ─────────────────────────

    /// <summary>
    /// 꼬리를 말풍선의 '형제' 로 옮기고 말풍선보다 먼저 그려지게 한다.
    ///
    /// 꼬리가 말풍선의 자식이면 유니티 UI 는 언제나 부모 뒤에(=위에) 자식을 그리므로,
    /// 두 도형의 색을 똑같이 맞춰도 몸통 변의 안티에일리어싱과 꼬리의 빗변이 겹친 자리에
    /// 이음선이 남는다. 순서를 뒤집어 몸통이 꼬리의 안쪽 절반을 덮게 하면 경계가 아예 생기지
    /// 않고, 바깥으로 나온 절반만 꼬리로 보인다.
    ///
    /// 런타임 계산이 두 사각형의 anchoredPosition 을 그대로 더하므로 앵커를 말풍선과 맞추고
    /// 피벗은 가운데로 둔다.
    /// </summary>
    private static string FixTailLayout(HelpOverlayUI overlay)
    {
        RectTransform bubble = overlay.EditorBubble;
        RectTransform tail = overlay.EditorTail;

        if (bubble == null || tail == null) return "말풍선/꼬리 참조가 비어 있어 꼬리 정리는 건너뜀.";

        Transform parent = bubble.parent;
        if (parent == null) return "말풍선에 부모가 없어 꼬리 정리는 건너뜀.";

        var changes = new List<string>();

        if (tail.parent != parent)
        {
            Undo.SetTransformParent(tail, parent, "도움말 말풍선 꼬리 정리");
            changes.Add($"꼬리를 '{parent.name}' 아래로 옮김");
        }

        // 꼬리를 말풍선 바로 앞 칸으로 보낸다. SetSiblingIndex 는 '뺀 뒤 끼워 넣기' 라서
        // 앞으로 이동할 때는 목표 인덱스를 하나 줄여야 의도한 자리에 들어간다.
        int tailIndex = tail.GetSiblingIndex();
        int bubbleIndex = bubble.GetSiblingIndex();
        int wanted = tailIndex < bubbleIndex ? bubbleIndex - 1 : bubbleIndex;
        if (tailIndex != wanted)
        {
            tail.SetSiblingIndex(wanted);
            changes.Add("꼬리를 말풍선보다 먼저 그리도록 순서 변경");
        }

        if (tail.anchorMin != bubble.anchorMin || tail.anchorMax != bubble.anchorMax)
        {
            tail.anchorMin = bubble.anchorMin;
            tail.anchorMax = bubble.anchorMax;
            changes.Add("꼬리 앵커를 말풍선과 일치시킴");
        }

        if (tail.pivot != new Vector2(0.5f, 0.5f))
        {
            tail.pivot = new Vector2(0.5f, 0.5f);
            changes.Add("꼬리 피벗을 가운데로 맞춤");
        }

        var bubbleGraphic = bubble.GetComponent<UnityEngine.UI.Graphic>();
        var tailGraphic = tail.GetComponent<UnityEngine.UI.Graphic>();
        if (bubbleGraphic != null && tailGraphic != null && tailGraphic.color != bubbleGraphic.color)
        {
            Undo.RecordObject(tailGraphic, "도움말 말풍선 꼬리 색 맞추기");
            tailGraphic.color = bubbleGraphic.color;
            EditorUtility.SetDirty(tailGraphic);
            changes.Add("꼬리 색을 말풍선과 일치시킴");
        }

        return changes.Count == 0
            ? "말풍선 꼬리 배치는 이미 올바릅니다."
            : "말풍선 꼬리 정리: " + string.Join(", ", changes.ToArray()) + ".";
    }

    // ───────────────────────── 찾기 ─────────────────────────

    private static HelpOverlayUI FindOverlay()
    {
        HelpOverlayUI[] found = Object.FindObjectsByType<HelpOverlayUI>(FindObjectsInactive.Include);
        if (found.Length == 0)
        {
            EditorUtility.DisplayDialog("도움말 투어 구성",
                "열려 있는 씬에서 HelpOverlayUI 를 찾지 못했습니다.\n인게임 씬(SampleScene)을 먼저 열어 주세요.", "확인");
            return null;
        }
        if (found.Length > 1)
        {
            Debug.LogWarning($"[HelpTourBuilder] 씬에 HelpOverlayUI 가 {found.Length}개 있습니다. '{found[0].name}' 을 사용합니다.", found[0]);
        }
        return found[0];
    }

    private static Transform Find(string path, List<string> missing)
    {
        Transform t = FindInScene(path);
        if (t == null && !missing.Contains(path)) missing.Add(path);
        return t;
    }

    private static List<GameObject> FindObjects(string[] paths, List<string> missing)
    {
        var list = new List<GameObject>();
        if (paths == null) return list;

        foreach (string path in paths)
        {
            Transform t = Find(path, missing);
            if (t != null) list.Add(t.gameObject);
        }
        return list;
    }

    /// <summary>"루트이름/자식/손자" 형태의 경로를 찾는다. Transform.Find 라서 꺼진 자식도 찾힌다.</summary>
    private static Transform FindInScene(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        string rest = slash < 0 ? null : path.Substring(slash + 1);

        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            if (rest == null) return root.transform;

            Transform found = root.transform.Find(rest);
            if (found != null) return found;
        }
        return null;
    }

    // ───────────────────────── 단계 표 ─────────────────────────

    private class StepDef
    {
        public string section;
        public string label;
        public string target;
        public string description;
        public Vector2 offset = new Vector2(0f, 44f);
        public string[] activate = None;
        public string[] deactivate = None;
    }

    private static int CountDefs()
    {
        int n = 0;
        foreach (StepDef unused in Steps()) n++;
        return n;
    }

    private static IEnumerable<StepDef> Steps()
    {
        // ── 메인 화면 ──
        // 메인 화면 단계는 화면을 건드리지 않는다. 도움말을 열기 전 상태가 곧 메인 화면이다.

        yield return new StepDef
        {
            section = SectionMain,
            label = "골드",
            target = GoldBar,
            description = "<b>골드</b>\n지금 가진 골드예요. 상점에서 건물을 사는 데 쓰고, 마을에 세운 건물이 1초마다 저마다의 생산량만큼 골드를 벌어 줍니다. 건물이 늘면 수입도 늘어나요.",
        };

        yield return new StepDef
        {
            section = SectionMain,
            label = "지식 포인트",
            target = KnowledgeBar,
            description = "<b>지식 포인트</b>\n미니게임을 풀어서 모으는 재화예요. 랜드마크처럼 골드만으로는 살 수 없는 건물에 함께 필요합니다. 하루에 모을 수 있는 양은 100포인트까지예요.",
        };

        yield return new StepDef
        {
            section = SectionMain,
            label = "관광객",
            target = TouristBar,
            description = "<b>관광객</b>\n'지금 온 관광객 / 수용 한도'예요. 건물을 세우면 한도가 늘고, 관광객은 매초 조금씩 한도까지 차오릅니다. 한도에 닿으면 더 늘지 않으니 건물로 한도를 키워 주세요.",
        };

        yield return new StepDef
        {
            section = SectionMain,
            label = "상점 버튼",
            target = ShopButton,
            description = "<b>상점</b>\n건물을 구경하고 사는 곳이에요. 기본 건물·랜드마크·지도 확장 탭으로 나뉘어 있고, 산 건물은 바로 마을에 배치하게 됩니다.",
        };

        yield return new StepDef
        {
            section = SectionMain,
            label = "미니게임 버튼",
            target = MiniGameButton,
            description = "<b>미니게임</b>\n경북의 문화재를 소재로 한 게임 4종이 들어 있어요. 문제를 풀면 골드와 지식 포인트를 받습니다.",
        };

        yield return new StepDef
        {
            section = SectionMain,
            label = "환경설정 버튼",
            target = SettingsButton,
            description = "<b>환경설정</b>\n배경음악·효과음 크기를 조절하고, 마을 배경 테마를 바꿀 수 있어요.",
        };

        yield return new StepDef
        {
            section = SectionMain,
            label = "도움말 버튼",
            target = HelpButton,
            description = "<b>도움말</b>\n지금 보고 있는 이 안내예요. 화면이 헷갈릴 때 다시 눌러 처음부터 볼 수 있습니다.",
        };

        yield return new StepDef
        {
            section = SectionMain,
            label = "종료 버튼",
            target = QuitButton,
            description = "<b>게임 종료</b>\n확인 창을 띄우고, [게임 종료]를 누르면 진행 상황을 저장한 뒤 게임을 끝냅니다.",
        };

        // ── 환경설정 ──

        yield return new StepDef
        {
            section = SectionSettings,
            label = "환경설정 탭",
            target = SettingsTabs,
            offset = new Vector2(0f, 56f),
            description = "<b>환경설정</b>\n위쪽 탭으로 화면을 바꿉니다. [기본 설정]은 소리, [테마 설정]은 마을 배경을 다뤄요.",
            activate = new[] { SettingsPanel, SoundPanel },
            deactivate = new[] { ThemePanel, InfoPanel },
        };

        yield return new StepDef
        {
            section = SectionSettings,
            label = "배경음악 크기",
            target = BgmRow,
            description = "<b>배경음악 크기</b>\n막대를 끌어 배경음악 크기를 0~100으로 맞춰요. 움직이는 즉시 소리에 반영되고, 창을 닫을 때 저장됩니다.",
            activate = new[] { SettingsPanel, SoundPanel },
            deactivate = new[] { ThemePanel, InfoPanel },
        };

        yield return new StepDef
        {
            section = SectionSettings,
            label = "효과음 크기",
            target = SfxRow,
            description = "<b>효과음 크기</b>\n버튼 소리처럼 짧게 나는 소리의 크기예요. 배경음악과 따로 조절할 수 있습니다.",
            activate = new[] { SettingsPanel, SoundPanel },
            deactivate = new[] { ThemePanel, InfoPanel },
        };

        yield return new StepDef
        {
            section = SectionSettings,
            label = "음소거 버튼",
            target = BgmMuteButton,
            description = "<b>음소거</b>\n스피커 아이콘을 누르면 그 소리만 끕니다. 아이콘 모양은 현재 크기에 따라 바뀌고, 음소거 중에 막대를 움직이면 자동으로 다시 켜져요.",
            activate = new[] { SettingsPanel, SoundPanel },
            deactivate = new[] { ThemePanel, InfoPanel },
        };

        yield return new StepDef
        {
            section = SectionSettings,
            label = "테마 목록",
            target = ThemeSlots,
            offset = new Vector2(0f, 56f),
            description = "<b>테마 설정</b>\n마을 배경을 고르는 곳이에요. 열린 테마는 눌러서 바로 적용되고, 잠긴 테마는 그 테마가 가리키는 실제 장소 근처에서 [인증]을 눌러야 열립니다.",
            activate = new[] { SettingsPanel, ThemePanel },
            deactivate = new[] { SoundPanel, InfoPanel },
        };

        yield return new StepDef
        {
            section = SectionSettings,
            label = "테마 안내 문구",
            target = ThemeStatus,
            description = "<b>인증 안내</b>\n테마를 적용했는지, 위치 인증이 됐는지 결과가 여기에 표시돼요. 인증에는 휴대폰의 위치 정보를 씁니다.",
            activate = new[] { SettingsPanel, ThemePanel },
            deactivate = new[] { SoundPanel, InfoPanel },
        };

        yield return new StepDef
        {
            section = SectionSettings,
            label = "환경설정 닫기",
            target = SettingsClose,
            description = "<b>닫기</b>\n환경설정을 닫고 마을로 돌아갑니다. 이때 소리 설정이 파일에 저장돼요.",
            activate = new[] { SettingsPanel, SoundPanel },
            deactivate = new[] { ThemePanel, InfoPanel },
        };

        // ── 상점 ──
        // 실제 게임에서도 상점을 열면 메인 화면의 [상점]·[미니게임] 버튼은 내려간다.

        yield return new StepDef
        {
            section = SectionShop,
            label = "기본 건물 탭",
            target = BasicTab,
            description = "<b>기본 건물 탭</b>\n골드를 벌어 주는 일반 건물이에요. 같은 건물을 여러 채 반복해서 지을 수 있습니다.",
            activate = new[] { ShopPanel, BasicShop },
            deactivate = new[] { LandmarkShop, MapShop, ShopButton, MiniGameButton },
        };

        yield return new StepDef
        {
            section = SectionShop,
            label = "기본 건물 개수",
            target = BuildingCountText,
            description = "<b>기본 건물 개수</b>\n'지은 개수 / 지을 수 있는 최대 개수'예요. 단계가 오르면 최대 개수가 10 → 20 → 30 → 40으로 늘어납니다. 꽉 차면 기본 건물은 더 살 수 없어요.",
            activate = new[] { ShopPanel, BasicShop },
            deactivate = new[] { LandmarkShop, MapShop, ShopButton, MiniGameButton },
        };

        yield return new StepDef
        {
            section = SectionShop,
            label = "건물 카드",
            target = BasicCard,
            offset = new Vector2(0f, 40f),
            description = "<b>건물 카드</b>\n가격, 초당 골드 생산량, 늘어나는 관광객 수와 수용 한도, 필요한 지식 포인트가 적혀 있어요. 골드나 지식 포인트가 모자라면 자물쇠가 걸려 살 수 없습니다.",
            activate = new[] { ShopPanel, BasicShop },
            deactivate = new[] { LandmarkShop, MapShop, ShopButton, MiniGameButton },
        };

        yield return new StepDef
        {
            section = SectionShop,
            label = "랜드마크 탭",
            target = LandmarkTab,
            description = "<b>랜드마크 탭</b>\n첨성대처럼 경북의 실제 명소를 세우는 탭이에요. 관광객을 크게 늘려 주지만 지식 포인트가 함께 필요하고, 같은 랜드마크는 한 번만 세울 수 있습니다.",
            activate = new[] { ShopPanel, LandmarkShop },
            deactivate = new[] { BasicShop, MapShop, ShopButton, MiniGameButton },
        };

        yield return new StepDef
        {
            section = SectionShop,
            label = "랜드마크 카드",
            target = LandmarkCard,
            offset = new Vector2(0f, 40f),
            description = "<b>랜드마크 카드</b>\n이미 세운 랜드마크는 자물쇠가 걸려 다시 살 수 없어요. 마을에 하나씩 모아 가는 수집 목록처럼 쓰면 됩니다.",
            activate = new[] { ShopPanel, LandmarkShop },
            deactivate = new[] { BasicShop, MapShop, ShopButton, MiniGameButton },
        };

        yield return new StepDef
        {
            section = SectionShop,
            label = "지도 확장 탭",
            target = MapTab,
            description = "<b>지도 확장 탭</b>\n구름에 덮인 땅을 열어 마을 부지를 넓히는 탭이에요. 부지가 넓어지면 건물을 놓을 자리가 늘어납니다.",
            activate = new[] { ShopPanel, MapShop },
            deactivate = new[] { BasicShop, LandmarkShop, ShopButton, MiniGameButton },
        };

        yield return new StepDef
        {
            section = SectionShop,
            label = "상점 닫기",
            target = ShopClose,
            description = "<b>상점 닫기</b>\n상점을 닫고 마을로 돌아갑니다. 건물을 사면 상점이 저절로 닫히고 배치 모드로 넘어가요.",
            activate = new[] { ShopPanel, BasicShop },
            deactivate = new[] { LandmarkShop, MapShop, ShopButton, MiniGameButton },
        };

        // ── 미니게임 ──
        // 미니게임에 들어가면 메인 HUD 는 모두 내려간다.

        string[] selectOn = { MiniGameSelect };
        var selectOff = new List<string>(MainHud) { ShopPanel, CardGamePanel, YabawiPanel, PuzzlePanel, SiluetePanel };

        yield return new StepDef
        {
            section = SectionMiniGame,
            label = "미니게임 선택 창",
            target = MiniGameTitle,
            offset = new Vector2(0f, 56f),
            description = "<b>미니게임</b>\n네 가지 게임으로 골드와 지식 포인트를 얻어요. 지식 포인트는 랜드마크처럼 특별한 건물을 살 때 필요하고, 하루에 100포인트까지만 쌓입니다. 한도를 채운 뒤에는 골드만 들어와요.",
            activate = selectOn,
            deactivate = selectOff.ToArray(),
        };

        yield return new StepDef
        {
            section = SectionMiniGame,
            label = "카드 맞추기 버튼",
            target = CardGameButton,
            description = "<b>카드 맞추기</b>\n뒤집힌 카드에서 같은 문화재 그림 두 장을 찾는 게임이에요. 모두 맞히면 100골드와 지식 포인트 30을 받습니다.",
            activate = selectOn,
            deactivate = selectOff.ToArray(),
        };

        yield return new StepDef
        {
            section = SectionMiniGame,
            label = "야바위 버튼",
            target = YabawiGameButton,
            description = "<b>야바위</b>\n섞은 그릇 중 구슬이 든 것을 맞히는 게임이에요. 난이도에 따라 그릇이 3·4·5개로 늘고, 맞히면 50골드와 지식 포인트 5·10·15를 받습니다.",
            activate = selectOn,
            deactivate = selectOff.ToArray(),
        };

        yield return new StepDef
        {
            section = SectionMiniGame,
            label = "퍼즐 버튼",
            target = PuzzleGameButton,
            description = "<b>퍼즐</b>\n경북 명소 사진을 조각으로 나눠 맞추는 게임이에요. 제한 시간 5분 안에 완성하면 100골드와 지식 포인트 30을 받습니다.",
            activate = selectOn,
            deactivate = selectOff.ToArray(),
        };

        yield return new StepDef
        {
            section = SectionMiniGame,
            label = "실루엣 퀴즈 버튼",
            target = SilueteGameButton,
            description = "<b>실루엣 퀴즈</b>\n그림이나 설명만 보고 어떤 곳인지 네 개의 답에서 고르는 게임이에요. 맞히면 100골드와 지식 포인트 3을, 틀리면 지식 포인트 1을 잃습니다. 잃은 만큼은 하루 한도가 되돌아가 다시 벌 수 있어요.",
            activate = selectOn,
            deactivate = selectOff.ToArray(),
        };

        yield return new StepDef
        {
            section = SectionMiniGame,
            label = "미니게임 닫기",
            target = MiniGameClose,
            description = "<b>미니게임 닫기</b>\n선택 창을 닫고 마을로 돌아갑니다. 게임 중에는 게임 화면의 [나가기]로 이 선택 창까지 돌아올 수 있어요.",
            activate = selectOn,
            deactivate = selectOff.ToArray(),
        };

        // 각 게임의 시작 화면("게임 시작" 화면)에는 도움말을 두지 않는다.
        // 게임 내용은 위의 선택 버튼 설명에서 이미 알려 주므로, 선택 패널까지만 안내한다.
    }
}
