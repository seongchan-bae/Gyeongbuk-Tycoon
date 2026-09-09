using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopTabController : MonoBehaviour
{
    [SerializeField] private GameObject basicShopPanel;
    [SerializeField] private GameObject landmarkShopPanel;
    [SerializeField] private GameObject mapShopPanel;

    [SerializeField] private Button basicTabButton;
    [SerializeField] private Button landmarkTabButton;
    [SerializeField] private Button mapTabButton;

    [Header("탭 버튼 스프라이트")]
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;

    [Header("기본건물 건물 수 표기")]
    [SerializeField] private TextMeshProUGUI buildingCountText; // "현재 / 최대" 표시 텍스트
    [SerializeField] private GameManager gameManager;

    void Start()
    {
        if (gameManager != null)
            gameManager.OnBuildingCountChanged += UpdateBuildingCountText;

        ShowBasicShop();
    }

    void OnEnable()
    {
        UpdateBuildingCountText();
    }

    void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnBuildingCountChanged -= UpdateBuildingCountText;
    }

    void UpdateBuildingCountText()
    {
        if (buildingCountText == null || gameManager == null) return;
        buildingCountText.text = $"{gameManager.BasicBuildingCount} / {gameManager.MaxBasicBuildings}";
    }

    public void ShowBasicShop()
    {
        basicShopPanel.SetActive(true);
        landmarkShopPanel.SetActive(false);
        if (mapShopPanel != null) mapShopPanel.SetActive(false);
        UpdateTabSprites(basicTabButton);
    }

    public void ShowLandmarkShop()
    {
        basicShopPanel.SetActive(false);
        landmarkShopPanel.SetActive(true);
        if (mapShopPanel != null) mapShopPanel.SetActive(false);
        UpdateTabSprites(landmarkTabButton);
    }

    public void ShowMapShop()
    {
        basicShopPanel.SetActive(false);
        landmarkShopPanel.SetActive(false);
        if (mapShopPanel != null) mapShopPanel.SetActive(true);
        UpdateTabSprites(mapTabButton);
    }

    void UpdateTabSprites(Button activeButton)
    {
        if (activeSprite == null || inactiveSprite == null) return;

        SetButtonSprite(basicTabButton,    basicTabButton    == activeButton);
        SetButtonSprite(landmarkTabButton, landmarkTabButton == activeButton);
        SetButtonSprite(mapTabButton,      mapTabButton      == activeButton);
    }

    void SetButtonSprite(Button btn, bool isActive)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null) img.sprite = isActive ? activeSprite : inactiveSprite;
    }
}
