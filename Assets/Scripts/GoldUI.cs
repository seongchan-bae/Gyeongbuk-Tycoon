using UnityEngine;
using TMPro;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private GameManager gameManager;

    void Start()
    {
        gameManager.OnMoneyChanged += UpdateGoldText;
        UpdateGoldText(gameManager.UserMoney);
    }

    void OnDestroy()
    {
        gameManager.OnMoneyChanged -= UpdateGoldText;
    }

    void UpdateGoldText(long amount)
    {
        goldText.text = amount.ToString("N0"); // 10,000 형식
    }
}
