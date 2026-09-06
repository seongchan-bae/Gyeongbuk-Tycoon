using UnityEngine;

public class Building : MonoBehaviour
{
    public BuildingData buildingData;

    private SpriteRenderer sr;
    private GameManager gameManager;
    private float goldTimer = 0f;
    private const float goldInterval = 1f; // 1초마다 골드 생산

    public static bool AnyBuildingDragging { get; set; }

    // 업그레이드로 누적된 런타임 보너스 (ScriptableObject 원본은 건드리지 않음)
    [HideInInspector] public float bonusGoldRate = 0f;       // 골드 생산량 추가
    [HideInInspector] public int   bonusTourist  = 0;        // 관광객 추가 증가량

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            sr.sortingLayerName = "Default";
            UpdateSortingOrder();
        }
        else
        {
            Debug.LogError($"[Building] {gameObject.name}에서 SpriteRenderer를 찾을 수 없습니다.");
        }
    }

    // BuildingInstall에서 설치 후 호출해 GameManager 연결
    public void Initialize(GameManager gm)
    {
        gameManager = gm;
    }

    public void EarnMoney()
    {

    }

    void Update()
    {
        UpdateSortingOrder();

        // 1초마다 goldProductionRate만큼 골드 생산
        if (gameManager != null && buildingData != null)
        {
            goldTimer += Time.deltaTime;
            if (goldTimer >= goldInterval)
            {
                goldTimer = 0f;
                gameManager.AddMoney((long)(buildingData.goldProductionRate + bonusGoldRate));
            }
        }
    }

    void UpdateSortingOrder()
    {
        if (sr != null)
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + 5000;
    }

    public void ToggleFlip()
    {
        if (sr != null) sr.flipX = !sr.flipX;
    }
}
