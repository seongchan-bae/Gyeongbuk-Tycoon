using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 유저가 가지고 있는 돈(GameManager에서만 관리)
    [SerializeField] private long userMoney = 10000L;

    // 유저가 가지고 있는 지식포인트(GameManager에서만 관리)
    [SerializeField] private long userKnowledgePoint = 0L;

    // 관광객 수치
    private int currentTourists = 0;
    private int maxTourists = 0;
    private int touristRatePerSecond = 0;
    private float touristTimer = 0f;
    public int CurrentTourists => currentTourists;
    public int MaxTourists => maxTourists;
    public int TouristRatePerSecond => touristRatePerSecond;
    public event System.Action<int, int> OnTouristsChanged;

    // 건물 설치 제한
    [Header("게임 단계 (0~3 → 기본건물 최대 10/20/30/40개)")]
    [SerializeField] private int currentStage = 0;
    private static readonly int[] stageLimits = { 10, 20, 30, 40 };

    private int basicBuildingCount = 0;
    private System.Collections.Generic.HashSet<string> installedLandmarks = new System.Collections.Generic.HashSet<string>();
    public int BasicBuildingCount => basicBuildingCount;
    public int CurrentStage => currentStage;
    public int MaxBasicBuildings => stageLimits[Mathf.Clamp(currentStage, 0, stageLimits.Length - 1)];
    public event System.Action OnBuildingCountChanged;

    public void SetStage(int stage)
    {
        currentStage = Mathf.Clamp(stage, 0, stageLimits.Length - 1);
        OnBuildingCountChanged?.Invoke();
    }

    public bool CanInstallBasic() => basicBuildingCount < MaxBasicBuildings;
    public bool IsLandmarkInstalled(string buildingName) => installedLandmarks.Contains(buildingName);

    public void RegisterBuilding(BuildingData data)
    {
        if (data.category == BuildingCategory.Basic)
            basicBuildingCount++;
        else if (data.category == BuildingCategory.Landmark)
            installedLandmarks.Add(data.buildingName);
        OnBuildingCountChanged?.Invoke();
    }

    public void UnregisterBuilding(BuildingData data)
    {
        if (data.category == BuildingCategory.Basic)
            basicBuildingCount = Mathf.Max(0, basicBuildingCount - 1);
        else if (data.category == BuildingCategory.Landmark)
            installedLandmarks.Remove(data.buildingName);
        OnBuildingCountChanged?.Invoke();
    }

    [Header("미니게임 UI 참조 (미니게임 씬에서만 연결)")]
    [SerializeField] private GameObject puzzleUI;
    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject HintPopupUI;

    [HideInInspector] public bool installingActivation;
    [HideInInspector] public bool destroyingActivation;

    public long UserMoney => userMoney;
    public long UserKnowledgePoint => userKnowledgePoint;
    public event System.Action<long> OnMoneyChanged;
    public event System.Action<long> OnKnowledgePointChanged;

    public static GameManager Instance { get; private set; } // 프로퍼티 개방

    void Awake()
    {
        Instance = this;
        installingActivation = false;
        destroyingActivation = false;
    }

    void Update()
    {
        if (touristRatePerSecond > 0 && currentTourists < maxTourists)
        {
            touristTimer += Time.deltaTime;
            if (touristTimer >= 1f)
            {
                touristTimer = 0f;
                int delta = Mathf.Min(touristRatePerSecond, maxTourists - currentTourists);
                currentTourists += delta;
                OnTouristsChanged?.Invoke(currentTourists, maxTourists);
            }
        }
    }

    void Start()
    {
        if (SaveManager.Instance != null)
        {
            userMoney = SaveManager.Instance.CurrentData.userMoney;
            userKnowledgePoint = SaveManager.Instance.CurrentData.userKnowledgePoint;
            currentTourists = SaveManager.Instance.CurrentData.currentTourists;
        }
        if (SoundManager.Instance != null) SoundManager.Instance.PlayBGM("baseBGM");
        OnMoneyChanged?.Invoke(userMoney);
        OnKnowledgePointChanged?.Invoke(userKnowledgePoint);
    }

    // 건물 설치 시 관광객 수치 추가 (rate = 초당 증가량 기여분)
    public void AddTourists(int tourist, int maxTourist, int rate = 0)
    {
        currentTourists += tourist;
        maxTourists += maxTourist;
        touristRatePerSecond += rate;
        OnTouristsChanged?.Invoke(currentTourists, maxTourists);
    }

    // 건물 삭제 시 관광객 수치 차감
    public void RemoveTourists(int tourist, int maxTourist, int rate = 0)
    {
        currentTourists -= tourist;
        maxTourists -= maxTourist;
        touristRatePerSecond -= rate;
        if (currentTourists > maxTourists) currentTourists = maxTourists;
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

    // 지식포인트 차감 — 부족하면 false 반환
    public bool SpendKnowledgePoint(long amount)
    {
        if (userKnowledgePoint < amount) return false;
        userKnowledgePoint -= amount;
        OnKnowledgePointChanged?.Invoke(userKnowledgePoint);
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
        OnKnowledgePointChanged?.Invoke(userKnowledgePoint);
    }
    //유저 지식포인트 차감
    void subUserknowledgePoint(long knowledgePoint)
    {
        userKnowledgePoint -= knowledgePoint;
        OnKnowledgePointChanged?.Invoke(userKnowledgePoint);
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
        OnKnowledgePointChanged?.Invoke(userKnowledgePoint);
    }

    /// <summary>미니게임으로 하루에 얻을 수 있는 지식포인트 상한.</summary>
    public const long DailyKnowledgePointLimit = 100L;

    /// <summary>오늘 이미 얻은 지식포인트. 날짜가 바뀌었으면 0으로 본다.</summary>
    public static long KnowledgePointEarnedToday
    {
        get
        {
            if (SaveManager.Instance == null) return 0L;

            GameSaveData data = SaveManager.Instance.CurrentData;
            return data.knowledgeEarnedDate == Today ? data.knowledgeEarnedToday : 0L;
        }
    }

    /// <summary>오늘 더 받을 수 있는 지식포인트.</summary>
    public static long RemainingDailyKnowledgePoint =>
        System.Math.Max(0L, DailyKnowledgePointLimit - KnowledgePointEarnedToday);

    private static string Today => System.DateTime.Now.ToString("yyyy-MM-dd");

    /// <summary>
    /// 골드와 지식포인트를 한 번에 지급하고 저장은 한 번만 한다.
    /// 지식포인트는 하루 상한까지만 쌓이며, 실제로 지급된 지식포인트를 돌려준다.
    /// </summary>
    public long AddReward(long gold, long knowledgePoint)
    {
        long grantedKnowledge = 0L;

        if (knowledgePoint > 0)
        {
            grantedKnowledge = System.Math.Min(knowledgePoint, RemainingDailyKnowledgePoint);
        }

        if (gold <= 0 && grantedKnowledge <= 0) return 0L;

        if (gold > 0) userMoney += gold;
        if (grantedKnowledge > 0)
        {
            userKnowledgePoint += grantedKnowledge;
            RecordDailyKnowledge(grantedKnowledge);
        }
        SaveCurrency();

        Debug.Log($"[GameManager] 보상 지급: +{gold} 골드 / +{grantedKnowledge} 지식포인트 (요청 {knowledgePoint}) (누적 {userMoney} 골드 / {userKnowledgePoint} 지식포인트)");
        return grantedKnowledge;
    }

    private static void RecordDailyKnowledge(long amount)
    {
        if (SaveManager.Instance == null) return;

        GameSaveData data = SaveManager.Instance.CurrentData;
        if (data.knowledgeEarnedDate != Today)
        {
            data.knowledgeEarnedDate = Today;
            data.knowledgeEarnedToday = 0L;
        }
        data.knowledgeEarnedToday += amount;
    }

    /// <summary>
    /// 미니게임에서 부르는 진입점. GameManager 를 직접 참조하지 않아도 보상을 지급할 수 있다.
    /// </summary>
    public static long GrantReward(long gold, long knowledgePoint)
    {
        GameManager gm = Instance;
        if (gm == null) gm = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);

        if (gm == null)
        {
            Debug.LogWarning("[GameManager] 씬에서 GameManager를 찾지 못해 보상을 지급하지 못했습니다.");
            return 0L;
        }

        return gm.AddReward(gold, knowledgePoint);
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
