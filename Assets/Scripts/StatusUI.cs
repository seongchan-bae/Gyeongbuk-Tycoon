using UnityEngine;
using TMPro;

public class StatusUI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [Header("상태 텍스트 연결")]
    [SerializeField] private TextMeshProUGUI knowledgeText;
    [SerializeField] private TextMeshProUGUI touristText;
    [SerializeField] private TextMeshProUGUI goldText;

    void Start()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (gameManager == null) return;

        // 3개의 이벤트를 한 곳에서 모두 구독
        gameManager.OnKnowledgePointChanged += UpdateKnowledgeText;
        gameManager.OnTouristsChanged += UpdateTouristText;
        gameManager.OnMoneyChanged += UpdateGoldText;

        // 초기값 설정
        UpdateKnowledgeText(gameManager.UserKnowledgePoint);
        UpdateTouristText(gameManager.CurrentTourists, gameManager.MaxTourists);
        UpdateGoldText(gameManager.UserMoney);
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnKnowledgePointChanged -= UpdateKnowledgeText;
            gameManager.OnTouristsChanged -= UpdateTouristText;
            gameManager.OnMoneyChanged -= UpdateGoldText;
        }
    }

    void UpdateKnowledgeText(long amount)
    {
        if (knowledgeText != null) knowledgeText.text = amount.ToString("N0");
    }

    void UpdateTouristText(int current, int max)
    {
        if (touristText != null) touristText.text = $"{current:N0} / {max:N0}";
    }

    void UpdateGoldText(long amount)
    {
        if (goldText != null) goldText.text = amount.ToString("N0");
    }
}