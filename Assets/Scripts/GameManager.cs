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

    [HideInInspector] public bool installingActivation;
    [HideInInspector] public bool destroyingActivation;

    public long UserMoney => userMoney;
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



}
