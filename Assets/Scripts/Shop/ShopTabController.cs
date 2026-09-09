using UnityEngine;
using UnityEngine.UI;

public class ShopTabController : MonoBehaviour
{
    [SerializeField] private GameObject basicShopPanel;
    [SerializeField] private GameObject landmarkShopPanel;
    [SerializeField] private GameObject mapShopPanel;

    [SerializeField] private Button basicTabButton;
    [SerializeField] private Button landmarkTabButton;
    [SerializeField] private Button mapTabButton;

    [Header("탭 버튼 스프라이트")]
    [SerializeField] private Sprite activeSprite;   // 초록 버튼
    [SerializeField] private Sprite inactiveSprite; // 회색 버튼

    void Start()
    {
        ShowBasicShop();
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
