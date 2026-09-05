using UnityEngine;

public class ShopFilter : MonoBehaviour
{
    // 카드들이 들어있는 Content 오브젝트 (Scroll View의 Content)
    [SerializeField] private Transform cardContent;

    public void ShowAll()
    {
        SetFilter(null);
    }

    public void ShowBasic()
    {
        SetFilter(BuildingCategory.Basic);
    }

    public void ShowLandmark()
    {
        SetFilter(BuildingCategory.Landmark);
    }

    void SetFilter(BuildingCategory? category)
    {
        foreach (var card in cardContent.GetComponentsInChildren<BuildingCardUI>(true))
        {
            bool show = category == null || card.Category == category;
            card.gameObject.SetActive(show);
        }
    }
}
