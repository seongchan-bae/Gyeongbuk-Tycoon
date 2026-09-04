using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 유저가 가지고 있는 돈(GameManager에서만 관리)
    [SerializeField] private long userMoney = 10000L;

    // 유저가 가지고 있는 지식포인트(GameManager에서만 관리)
    private long userKnowledgePoint = 0L;

    // 관광객 수치
    private int currentTourists = 0;
    private int maxTourists = 0;
    public int CurrentTourists => currentTourists;
    public int MaxTourists => maxTourists;
    public event System.Action<int, int> OnTouristsChanged;

    [Header("미니게임 UI 참조 (미니게임 씬에서만 연결)")]
    [SerializeField] private GameObject puzzleUI;
    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject HintPopupUI;

    [HideInInspector] public bool installingActivation;
    [HideInInspector] public bool destroyingActivation;

    public long UserMoney => userMoney;
    public long UserKnowledgePoint => userKnowledgePoint;
    public event System.Action<long> OnMoneyChanged;

    public static GameManager Instance { get; private set; } // 프로퍼티 개방

    void Awake()
    {
        Instance = this;
        installingActivation = false;
        destroyingActivation = false;
    }

    void Start()
    {
        if (SaveManager.Instance != null)
        {
            userMoney = SaveManager.Instance.CurrentData.userMoney;
            userKnowledgePoint = SaveManager.Instance.CurrentData.userKnowledgePoint;
        }
        OnMoneyChanged?.Invoke(userMoney);
    }

    // 건물 설치 시 관광객 수치 추가
    public void AddTourists(int tourist, int maxTourist)
    {
        currentTourists += tourist;
        maxTourists += maxTourist;
        OnTouristsChanged?.Invoke(currentTourists, maxTourists);
    }

    // 건물 삭제 시 관광객 수치 차감
    public void RemoveTourists(int tourist, int maxTourist)
    {
        currentTourists -= tourist;
        maxTourists -= maxTourist;
        OnTouristsChanged?.Invoke(currentTourists, maxTourists);
    }

    //유저머니 추가
    public void AddMoney(long money)
    {
        userMoney += money;
        OnMoneyChanged?.Invoke(userMoney);
    }

    // 구매 시 차감 — 잔액 부족이면 false 반환
    public bool SpendMoney(long money)
    {
        if (userMoney < money) return false;
        userMoney -= money;
        OnMoneyChanged?.Invoke(userMoney);
        return true;
    }

    //유저머니 차감
    void subUserMoney(long money)
    {
        userMoney -= money;
    }
    //유저 지식포인트 추가
    void addUserKnowledgePoint(long knowledgePoint)
    {
        userKnowledgePoint += knowledgePoint;
    }
    //유저 지식포인트 차감
    void subUserknowledgePoint(long knowledgePoint)
    {
        userKnowledgePoint -= knowledgePoint;
    }
    /// <summary>
    /// 로딩 화면을 띄워주는 함수
    /// </summary>
    void showLoadingUI()
    {

    }

    // ───────────────────────── 미니게임 연동 ─────────────────────────

    /// <summary>재화 변경을 SaveManager 에 반영하고 UI 갱신 이벤트를 쏜다.</summary>
    private void SaveCurrency()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentData.userMoney = userMoney;
            SaveManager.Instance.CurrentData.userKnowledgePoint = userKnowledgePoint;
        }
        OnMoneyChanged?.Invoke(userMoney);
    }

    /// <summary>골드와 지식포인트를 한 번에 지급하고 저장은 한 번만 한다.</summary>
    public void AddReward(long gold, long knowledgePoint)
    {
        if (gold <= 0 && knowledgePoint <= 0) return;

        if (gold > 0) userMoney += gold;
        if (knowledgePoint > 0) userKnowledgePoint += knowledgePoint;
        SaveCurrency();

        Debug.Log($"[GameManager] 보상 지급: +{gold} 골드 / +{knowledgePoint} 지식포인트  (누적 {userMoney} 골드 / {userKnowledgePoint} 지식포인트)");
    }

    /// <summary>
    /// 미니게임에서 부르는 진입점. GameManager 를 직접 참조하지 않아도 보상을 지급할 수 있다.
    /// </summary>
    public static void GrantReward(long gold, long knowledgePoint)
    {
        GameManager gm = Instance;
        if (gm == null) gm = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

        if (gm == null)
        {
            Debug.LogWarning("[GameManager] 씬에서 GameManager를 찾지 못해 보상을 지급하지 못했습니다.");
            return;
        }

        gm.AddReward(gold, knowledgePoint);
    }

    // 아래 4개는 미니게임 씬의 버튼 OnClick 에 이름으로 연결되어 있으므로 시그니처를 바꾸지 말 것.

    public void OpenPuzzleUI()
    {
        if (puzzleUI != null) puzzleUI.SetActive(true);
        if (mainUI != null) mainUI.SetActive(false);
    }

    public void ClosePuzzleUI()
    {
        if (puzzleUI != null) puzzleUI.SetActive(false);
        if (mainUI != null) mainUI.SetActive(true);
    }

    public void OpenHintPopupUI()
    {
        if (HintPopupUI != null) HintPopupUI.SetActive(true);
    }

    public void CloseHintPopupUI()
    {
        if (HintPopupUI != null) HintPopupUI.SetActive(false);
    }
}
