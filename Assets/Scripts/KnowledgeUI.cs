using UnityEngine;
using TMPro;

// 메인화면 상단의 지식포인트 표시. GoldUI / TouristUI 와 같은 구조로,
// GameManager 가 쏘는 변경 이벤트만 구독해서 텍스트를 갱신한다.
public class KnowledgeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI knowledgeText;
    [SerializeField] private GameManager gameManager;

    void Start()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (gameManager == null) return;

        gameManager.OnKnowledgePointChanged += UpdateKnowledgeText;
        UpdateKnowledgeText(gameManager.UserKnowledgePoint);
    }

    void OnDestroy()
    {
        if (gameManager != null) gameManager.OnKnowledgePointChanged -= UpdateKnowledgeText;
    }

    void UpdateKnowledgeText(long amount)
    {
        if (knowledgeText != null) knowledgeText.text = amount.ToString("N0"); // 10,000 형식
    }
}
