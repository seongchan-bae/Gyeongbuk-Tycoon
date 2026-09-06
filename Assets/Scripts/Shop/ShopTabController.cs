using UnityEngine;
using UnityEngine.UI;

public class ShopTabController : MonoBehaviour
{
    [SerializeField] private GameObject basicShopPanel;     // 기본건물 상점 패널
    [SerializeField] private GameObject landmarkShopPanel;  // 랜드마크 상점 패널

    [SerializeField] private Button basicTabButton;
    [SerializeField] private Button landmarkTabButton;

    void Start()
    {
        ShowBasicShop();
    }

    public void ShowBasicShop()
    {
        basicShopPanel.SetActive(true);
        landmarkShopPanel.SetActive(false);
    }

    public void ShowLandmarkShop()
    {
        basicShopPanel.SetActive(false);
        landmarkShopPanel.SetActive(true);
    }
}
