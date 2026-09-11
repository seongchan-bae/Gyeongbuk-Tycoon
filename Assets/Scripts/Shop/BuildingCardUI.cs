using UnityEngine;
using TMPro;

public class BuildingCardUI : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;  // 이 카드가 나타내는 건물 데이터
    [SerializeField] private BuildingInstall buildingInstall;
    [SerializeField] private BaseUI baseUI;              // 닫을 상점 UI
    [SerializeField] private GameManager gameManager;

    public BuildingCategory? Category => buildingData != null ? buildingData.category : (BuildingCategory?)null;

    [Header("건물 카드 이미지")]
    [SerializeField] private UnityEngine.UI.Image buildingThumbnail;

    [Header("카드 공통 비주얼 — 자식 Image 오브젝트 이름으로 자동 탐색")]
    // CardVisualData 에셋을 Assets/Resources/CardVisualData.asset 에 두면 자동 적용
    // 카드 장식 Image 오브젝트 이름: CardImage1 ~ CardImage4

    [Header("건물 정보 텍스트")]
    [SerializeField] private TMP_Text buildingNameText;
    [SerializeField] private TMP_Text goldProductionText;
    [SerializeField] private TMP_Text touristIncreaseText;
    [SerializeField] private TMP_Text maxTouristText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text knowledgePriceText;

    [Header("구매 불가 잠금")]
    [SerializeField] private GameObject lockImage;
    [SerializeField] private UnityEngine.UI.Button buyButton;

    void OnValidate()
    {
        RefreshUI();
    }

    void Start()
    {
        RefreshUI();
        UpdateLockState(gameManager != null ? gameManager.UserMoney : 0);

        if (gameManager != null)
        {
            gameManager.OnMoneyChanged += UpdateLockState;
            gameManager.OnKnowledgePointChanged += _ => RefreshLockState();
            gameManager.OnBuildingCountChanged += RefreshLockState;
        }
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnMoneyChanged -= UpdateLockState;
            gameManager.OnBuildingCountChanged -= RefreshLockState;
            // OnKnowledgePointChanged는 람다로 등록되어 자동 해제됨
        }
    }

    void UpdateLockState(long currentMoney)
    {
        if (buildingData == null) return;

        // 랜드마크는 설치되는 순간 슬롯 자체를 비활성화
        if (buildingData.category == BuildingCategory.Landmark
            && gameManager != null
            && gameManager.IsLandmarkInstalled(buildingData.buildingName))
        {
            gameObject.SetActive(false);
            return;
        }

        bool notEnoughMoney = currentMoney < buildingData.price;
        // TODO: 테스트 완료 후 재활성화
        // bool notEnoughKP = gameManager != null && gameManager.UserKnowledgePoint < buildingData.knowledgePrice;
        bool limitReached = buildingData.category == BuildingCategory.Basic
            && gameManager != null
            && !gameManager.CanInstallBasic();

        bool locked = notEnoughMoney || limitReached;
        if (lockImage != null) lockImage.SetActive(locked);
        if (buyButton != null) buyButton.interactable = !locked;
    }

    void RefreshLockState()
    {
        UpdateLockState(gameManager != null ? gameManager.UserMoney : 0);
    }

    void RefreshUI()
    {
        // 카드 공통 비주얼은 buildingData와 무관하게 항상 적용
        var cardVisual = Resources.Load<CardVisualData>("CardVisualData");
        if (cardVisual != null)
        {
            ApplyCardSprite("Icon1", cardVisual.image1);
            ApplyCardSprite("Icon2", cardVisual.image2);
            ApplyCardSprite("Icon3", cardVisual.image3);
            ApplyCardSprite("Icon4", cardVisual.image4);
        }

        if (buildingData == null) return;

        if (buildingThumbnail    != null) buildingThumbnail.sprite   = buildingData.thumbnail;
        if (buildingNameText     != null) buildingNameText.text     = buildingData.buildingName;
        if (goldProductionText   != null) goldProductionText.text   = buildingData.goldProductionRate.ToString("#,##0.##");
        if (touristIncreaseText  != null) touristIncreaseText.text  = buildingData.touristIncrease.ToString("N0");
        if (maxTouristText       != null) maxTouristText.text       = buildingData.maxTouristIncrease.ToString("N0");
        if (priceText            != null) priceText.text            = buildingData.price.ToString("N0");
        if (knowledgePriceText   != null) knowledgePriceText.text   = buildingData.knowledgePrice.ToString("N0");
    }

    void ApplyCardSprite(string childName, Sprite sprite)
    {
        if (sprite == null) return;  // 스프라이트 미설정 시 기존 이미지 유지
        foreach (var img in GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            if (img.gameObject.name == childName)
            {
                img.sprite = sprite;
                return;
            }
        }
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

        // 골드 또는 지식포인트 부족 시 구매 차단
        if (gameManager != null && !gameManager.SpendMoney(buildingData.price))
        {
            Debug.Log("골드가 부족합니다!");
            return;
        }
        // TODO: 테스트 완료 후 재활성화
        // if (gameManager != null && !gameManager.SpendKnowledgePoint(buildingData.knowledgePrice))
        // {
        //     gameManager.AddMoney(buildingData.price);
        //     Debug.Log("지식포인트가 부족합니다!");
        //     return;
        // }

        // BuildingData 전달 — 프리팹 및 타일 크기 정보 포함
        buildingInstall.SelectBuilding(buildingData);

        // 상점 UI 닫기 (main도 함께 복원)
        if (baseUI != null)
            baseUI.CloseStoreUI();
        else
            Debug.LogError("BuildingCardUI: baseUI가 Inspector에 연결되지 않았습니다.");
    }
}
