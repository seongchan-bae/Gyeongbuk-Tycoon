using UnityEngine;

public class Building : MonoBehaviour
{
    public BuildingData buildingData;

    private SpriteRenderer sr;
    private GameManager gameManager;
    private float goldTimer = 0f;
    private float touristTimer = 0f;
    private const float productionInterval = 1f;

    public static bool AnyBuildingDragging { get; set; }

    // 업그레이드로 누적된 런타임 보너스 (ScriptableObject 원본은 건드리지 않음)
    [HideInInspector] public float bonusGoldRate = 0f;
    [HideInInspector] public int   bonusTourist  = 0;        // 초당 관광객 추가 생산량

    private int currentTourists = 0;
    public int CurrentTourists => currentTourists;

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

        if (gameManager != null && buildingData != null)
        {
            goldTimer += Time.deltaTime;
            touristTimer += Time.deltaTime;

            if (goldTimer >= productionInterval)
            {
                goldTimer = 0f;
                gameManager.AddMoney((long)(buildingData.goldProductionRate + bonusGoldRate));
            }

            if (touristTimer >= productionInterval)
            {
                touristTimer = 0f;
                int max = buildingData.maxTouristIncrease;
                int rate = buildingData.touristIncrease + bonusTourist;
                if (currentTourists < max)
                {
                    int delta = Mathf.Min(rate, max - currentTourists);
                    currentTourists += delta;
                    gameManager.AddTourists(delta, 0);
                }
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
