using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인화면의 "미니게임" 버튼 하나로 4가지 미니게임(카드맞추기 / 야바위 / 퍼즐 / 실루엣)을
/// 골라서 실행할 수 있게 묶어주는 허브 UI.
/// 각 게임의 UI 자체는 아직 통일하지 않고, 진입/복귀 흐름만 한 곳으로 모은다.
/// 흐름: 메인화면 → [미니게임] → 선택 패널 → 게임 패널 → (나가기) → 선택 패널 → (닫기) → 메인화면
/// </summary>
public class MiniGameHubUI : MonoBehaviour
{
    public enum MiniGameType
    {
        CardMatching,
        Yabawi,
        Puzzle,
        Siluete
    }

    [System.Serializable]
    public class MiniGameEntry
    {
        public MiniGameType type;
        [Tooltip("선택 패널에 표시할 이름")]
        public string displayName;
        [Tooltip("이 미니게임의 루트 패널 오브젝트")]
        public GameObject panel;
        [Tooltip("선택 패널에서 이 게임을 고르는 버튼")]
        public Button selectButton;
    }

    /// <summary>각 미니게임 스크립트가 "허브로 돌아가기"를 호출할 수 있도록 노출하는 참조</summary>
    public static MiniGameHubUI Instance { get; private set; }

    [Header("화면 전환 대상")]
    [SerializeField] private GameObject mainUI;
    [Tooltip("미니게임 4종을 고르는 선택 패널")]
    [SerializeField] private GameObject hubPanel;

    [Header("허브 버튼")]
    [Tooltip("메인화면에 두는 [미니게임] 버튼")]
    [SerializeField] private Button openHubButton;
    [Tooltip("선택 패널의 [닫기] 버튼")]
    [SerializeField] private Button closeHubButton;

    [Header("보유 재화 표시")]
    [Tooltip("선택 패널에 현재 보유 골드/지식포인트를 보여줄 텍스트")]
    [SerializeField] private TMPro.TextMeshProUGUI currencyText;

    [Header("미니게임 목록")]
    [SerializeField] private List<MiniGameEntry> miniGames = new List<MiniGameEntry>();

    [Header("게임별 컨트롤러 (연결되어 있으면 우선 사용)")]
    [SerializeField] private CardMatchingGame cardMatchingGame;
    [SerializeField] private YabawiGameUI yabawiGame;

    private bool hasRunningGame;
    private MiniGameType runningGame;
    // 허브가 직접 패널을 닫는 동안에는 게임 쪽에서 올라오는 복귀 요청을 무시한다 (무한 재귀 방지)
    private bool isSwitching;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MiniGameHubUI] 씬에 허브가 두 개 이상 있습니다. 나중에 깨어난 쪽을 사용합니다.");
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (openHubButton != null)
        {
            openHubButton.onClick.RemoveListener(OpenHub);
            openHubButton.onClick.AddListener(OpenHub);
        }
        if (closeHubButton != null)
        {
            closeHubButton.onClick.RemoveListener(CloseHub);
            closeHubButton.onClick.AddListener(CloseHub);
        }

        foreach (var entry in miniGames)
        {
            if (entry == null || entry.selectButton == null) continue;

            MiniGameType captured = entry.type;
            entry.selectButton.onClick.AddListener(() => OpenMiniGame(captured));
        }

        CloseAllGamePanels();
        if (hubPanel != null) hubPanel.SetActive(false);
    }

    /// <summary>메인화면의 [미니게임] 버튼에서 호출</summary>
    public void OpenHub()
    {
        CloseAllGamePanels();
        if (mainUI != null) mainUI.SetActive(false);
        if (hubPanel != null) hubPanel.SetActive(true);
        RefreshCurrency();
    }

    /// <summary>선택 패널의 보유 재화 표시를 최신 값으로 갱신한다.</summary>
    private void RefreshCurrency()
    {
        if (currencyText == null) return;

        GameManager gm = GameManager.Instance != null
            ? GameManager.Instance
            : FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

        currencyText.text = gm != null
            ? $"보유  {gm.UserMoney} 골드   /   {gm.UserKnowledgePoint} 지식 포인트"
            : string.Empty;
    }

    /// <summary>선택 패널의 [닫기] 버튼에서 호출. 메인화면으로 복귀</summary>
    public void CloseHub()
    {
        CloseAllGamePanels();
        if (hubPanel != null) hubPanel.SetActive(false);
        if (mainUI != null) mainUI.SetActive(true);
    }

    /// <summary>게임 중 [나가기]에서 호출. 선택 패널로 복귀</summary>
    public void ReturnToHub()
    {
        if (isSwitching) return;

        CloseAllGamePanels();
        if (mainUI != null) mainUI.SetActive(false);
        if (hubPanel != null) hubPanel.SetActive(true);
        RefreshCurrency(); // 방금 얻은 보상이 바로 반영되도록
    }

    // 인스펙터/버튼에서 enum 없이 직접 부를 수 있도록 만든 래퍼들
    public void OpenCardMatchingGame() { OpenMiniGame(MiniGameType.CardMatching); }
    public void OpenYabawiGame() { OpenMiniGame(MiniGameType.Yabawi); }
    public void OpenPuzzleGame() { OpenMiniGame(MiniGameType.Puzzle); }
    public void OpenSilueteGame() { OpenMiniGame(MiniGameType.Siluete); }

    public void OpenMiniGame(MiniGameType type)
    {
        MiniGameEntry entry = Find(type);
        if (entry == null)
        {
            Debug.LogWarning($"[MiniGameHubUI] '{type}' 항목이 등록되어 있지 않습니다.");
            return;
        }

        CloseAllGamePanels();
        if (mainUI != null) mainUI.SetActive(false);
        if (hubPanel != null) hubPanel.SetActive(false);

        hasRunningGame = true;
        runningGame = type;

        // 자체 진입 로직이 있는 게임은 그쪽 함수를 태워야 초기화가 같이 돌아간다.
        switch (type)
        {
            case MiniGameType.CardMatching:
                if (cardMatchingGame != null)
                {
                    cardMatchingGame.OpenGamePanel();
                    return;
                }
                break;

            case MiniGameType.Yabawi:
                if (yabawiGame != null)
                {
                    yabawiGame.OpenPanel();
                    return;
                }
                break;
        }

        if (entry.panel != null) entry.panel.SetActive(true);
    }

    /// <summary>현재 실행 중인 게임을 닫고 선택 패널로 돌아온다.</summary>
    public void CloseCurrentMiniGame()
    {
        if (hasRunningGame)
        {
            CloseMiniGame(runningGame);
        }
        ReturnToHub();
    }

    public void CloseMiniGame(MiniGameType type)
    {
        MiniGameEntry entry = Find(type);

        switch (type)
        {
            case MiniGameType.CardMatching:
                if (cardMatchingGame != null)
                {
                    cardMatchingGame.CloseGamePanel();
                    if (hasRunningGame && runningGame == type) hasRunningGame = false;
                    return;
                }
                break;

            case MiniGameType.Yabawi:
                if (yabawiGame != null)
                {
                    yabawiGame.CloseGame();
                    if (hasRunningGame && runningGame == type) hasRunningGame = false;
                    return;
                }
                break;
        }

        if (entry != null && entry.panel != null) entry.panel.SetActive(false);
        if (hasRunningGame && runningGame == type) hasRunningGame = false;
    }

    private void CloseAllGamePanels()
    {
        isSwitching = true;
        try
        {
            CloseAllGamePanelsInternal();
        }
        finally
        {
            isSwitching = false;
        }
    }

    private void CloseAllGamePanelsInternal()
    {
        foreach (var entry in miniGames)
        {
            if (entry == null) continue;

            if (entry.type == MiniGameType.CardMatching && cardMatchingGame != null)
            {
                cardMatchingGame.CloseGamePanel();
                continue;
            }
            if (entry.type == MiniGameType.Yabawi && yabawiGame != null)
            {
                yabawiGame.CloseGame();
                continue;
            }

            if (entry.panel != null) entry.panel.SetActive(false);
        }

        hasRunningGame = false;
    }

    private MiniGameEntry Find(MiniGameType type)
    {
        foreach (var entry in miniGames)
        {
            if (entry != null && entry.type == type) return entry;
        }
        return null;
    }
}
