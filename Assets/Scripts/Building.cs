using UnityEngine;

public class Building : MonoBehaviour
{
    public BuildingData buildingData;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            // 타일맵과 같은 Sorting Layer에서 비교되도록 명시적으로 지정
            sr.sortingLayerName = "Default";
            UpdateSortingOrder();
        }
        else
        {
            Debug.LogError($"[Building] {gameObject.name}에서 SpriteRenderer를 찾을 수 없습니다.");
        }
    }

    public void EarnMoney()
    {

    }

    void Update()
    {
        UpdateSortingOrder();
    }

    void UpdateSortingOrder()
    {
        // Y가 낮을수록(화면 아래) 앞에 렌더링, 타일맵(-1000)보다 항상 높은 값
        if (sr != null)
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + 5000;
    }
}
