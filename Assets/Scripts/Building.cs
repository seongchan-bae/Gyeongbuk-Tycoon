using UnityEngine;

public class Building : MonoBehaviour
{
    public BuildingData buildingData;

    private SpriteRenderer sr;
    private GameManager gameManager;
    private float goldTimer = 0f;
    private float goldTimer1 = 0f;
    private const float goldInterval = 1f; // 1초마다 골드 생산

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
                gameManager.AddMoney((long)buildingData.goldProductionRate);
            }
        }
        if (goldTimer1 >= goldInterval)
        {
            goldTimer1 = 0f;
            //gameManager.AddMoney((long)buildingData.goldProductionRate);
            Debug.Log($"골드 추가! 현재: {gameManager.UserMoney}");
        }
    }

    void UpdateSortingOrder()
    {
        if (sr != null)
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + 5000;
    }
}
