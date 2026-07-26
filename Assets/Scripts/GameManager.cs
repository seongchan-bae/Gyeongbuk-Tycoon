using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 어디서든 접근 가능한 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

    [Header("실행 시 기본 제공할 시작 재화 설정")]
    [SerializeField] private long initialMoney = 99999L;          // 기본 제공 골드
    [SerializeField] private long initialKnowledgePoint = 0L;   // 기본 제공 지식 포인트

    // 현재 보유 중인 실제 재화 변수 (인게임에서 가변됨)
    private long userMoney;
    private long userKnowledgePoint;

    // 외부 스크립트(Store 등)에서 읽을 수 있는 프로퍼티
    public long UserMoney => userMoney;
    public long UserKnowledgePoint => userKnowledgePoint;


    private void Awake()
    {
        // 싱글톤 세팅
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 게임이 시작될 때마다 지정한 기본 금액으로 초기화
            InitCurrency();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 게임 실행 시 재화를 시작 기본값으로 초기화하는 함수
    /// </summary>
    public void InitCurrency()
    {
        userMoney = initialMoney;
        userKnowledgePoint = initialKnowledgePoint;
    }

    //유저머니 추가
    void addUserMoney(long money)
    {
        userMoney += money;
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
    /// <summary>
    /// 건물 설치 기능 활성화 상태. 함수 호출 시 건물 설치 기능활성화
    /// </summary>
    /// <returns>
    /// 현재 활성화 상태면 true
    /// </returns>
    public bool installingActivation()
    {
        //dummy return value
        return false;
    }
    /// <summary>
    /// 건물 삭제 기능 활성화 상태. 함수 호출 시 건물 삭제 기능 활성화
    /// </summary>
    /// <returns>
    /// 현재 활성화 상태면 true
    /// </returns>
    public bool destroyingActivation()
    {
        //dummy return value
        return false;
    }


}
