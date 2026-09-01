using UnityEngine;
using TMPro;

public class BuildingCardUI : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;  // 이 카드가 나타내는 건물 데이터
    [SerializeField] private BuildingInstall buildingInstall;
    [SerializeField] private BaseUI baseUI;              // 닫을 상점 UI
    [SerializeField] private GameManager gameManager;

    [Header("건물 정보 텍스트")]
    [SerializeField] private TMP_Text buildingNameText;
    [SerializeField] private TMP_Text goldProductionText;
    [SerializeField] private TMP_Text touristIncreaseText;
    [SerializeField] private TMP_Text maxTouristText;
    [SerializeField] private TMP_Text priceText;

    [Header("구매 불가 잠금")]
    [SerializeField] private GameObject lockImage;
    [SerializeField] private UnityEngine.UI.Button buyButton;

    void Start()
    {
        RefreshUI();
        UpdateLockState(gameManager != null ? gameManager.UserMoney : 0);

        if (gameManager != null)
            gameManager.OnMoneyChanged += UpdateLockState;
    }

    void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnMoneyChanged -= UpdateLockState;
    }

    void UpdateLockState(long currentMoney)
    {
        if (buildingData == null) return;
        bool locked = currentMoney < buildingData.price;
        if (lockImage != null) lockImage.SetActive(locked);
        if (buyButton != null) buyButton.interactable = !locked;
    }

    void RefreshUI()
    {
        if (buildingData == null) return;

        if (buildingNameText    != null) buildingNameText.text    = buildingData.buildingName;
        if (goldProductionText  != null) goldProductionText.text  = buildingData.goldProductionRate.ToString("#,##0.##");
        if (touristIncreaseText != null) touristIncreaseText.text = buildingData.touristIncrease.ToString("N0");
        if (maxTouristText      != null) maxTouristText.text      = buildingData.maxTouristIncrease.ToString("N0");
        if (priceText           != null) priceText.text           = buildingData.price.ToString("N0");
    }

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
