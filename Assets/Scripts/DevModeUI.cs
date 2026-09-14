using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 환경설정 창의 [개발자 모드] 탭에 있는 개발자 코드 입력.
/// "test" 입력 후 확인 버튼을 누르면 개발자 모드로 전환되어 돈·관광객·최대관광객을 대량 지급한다.
/// </summary>
public class DevModeUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private const string DEV_CODE = "test";

    private const long MONEY_BONUS       = 999_999_999L;
    private const int  TOURIST_BONUS     = 9_999_999;
    private const int  MAX_TOURIST_BONUS = 9_999_999;

    [Header("결과 문구 색상")]
    [SerializeField] private Color successColor = new Color(0.15f, 0.55f, 0.25f, 1f);
    [SerializeField] private Color failColor    = new Color(0.8f, 0.2f, 0.2f, 1f);

    void Start()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        codeInputField.onSubmit.AddListener(_ => OnConfirm());
    }

    // 탭을 다시 열 때마다 이전 입력/결과 문구를 지운다.
    void OnEnable()
    {
        if (feedbackText != null) feedbackText.text = "";
        if (codeInputField != null) codeInputField.text = "";
    }

    void OnConfirm()
    {
        // 타이틀 화면의 설정창에는 GameManager 가 없어서 재화를 줄 대상이 없다.
        if (GameManager.Instance == null)
        {
            ShowFeedback("게임 화면에서만 사용할 수 있습니다.", failColor);
            codeInputField.text = "";
            return;
        }

        if (codeInputField.text.Trim().ToLower() == DEV_CODE)
        {
            GameManager.Instance.AddMoney(MONEY_BONUS);
            GameManager.Instance.AddTourists(TOURIST_BONUS, MAX_TOURIST_BONUS);

            ShowFeedback("개발자 모드 활성화! 골드와 관광객이 지급되었습니다.", successColor);
        }
        else
        {
            ShowFeedback("잘못된 코드입니다.", failColor);
        }

        codeInputField.text = "";
    }

    void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null) return;
        feedbackText.text = message;
        feedbackText.color = color;
    }
}
