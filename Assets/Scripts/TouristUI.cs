using UnityEngine;
using TMPro;

public class TouristUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI touristText;
    [SerializeField] private GameManager gameManager;

    void Start()
    {
        gameManager.OnTouristsChanged += UpdateTouristText;
        UpdateTouristText(gameManager.CurrentTourists, gameManager.MaxTourists);
    }

    void OnDestroy()
    {
        gameManager.OnTouristsChanged -= UpdateTouristText;
    }

    void UpdateTouristText(int current, int max)
    {
        touristText.text = $"{current:N0} / {max:N0}";
    }
}
