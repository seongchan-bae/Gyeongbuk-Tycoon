using UnityEngine;

public class BuildingCardUI : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;  // 이 카드가 나타내는 건물 데이터
    [SerializeField] private BuildingInstall buildingInstall;
    [SerializeField] private BaseUI baseUI;              // 닫을 상점 UI
    [SerializeField] private GameManager gameManager;

    // 카드의 Buy 버튼 OnClick에 연결
    public void OnBuyClicked()
    {
        if (buildingData == null)
        {
            Debug.LogError("BuildingCardUI: buildingData가 Inspector에 연결되지 않았습니다.");
            return;
        }
        if (buildingInstall == null)
        {
            Debug.LogError("BuildingCardUI: buildingInstall이 Inspector에 연결되지 않았습니다.");
            return;
        }

        // 골드 부족 시 구매 차단
        if (gameManager != null && !gameManager.SpendMoney(buildingData.price))
        {
            Debug.Log("골드가 부족합니다!");
            return;
        }

        // BuildingData 전달 — 프리팹 및 타일 크기 정보 포함
        buildingInstall.SelectBuilding(buildingData);

        // 상점 UI 닫기 (main도 함께 복원)
        if (baseUI != null)
            baseUI.CloseStoreUI();
        else
            Debug.LogError("BuildingCardUI: baseUI가 Inspector에 연결되지 않았습니다.");
    }
}
