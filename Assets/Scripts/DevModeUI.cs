using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 기존 설정창 안에 추가되는 개발자 코드 입력.
/// "test" 입력 후 확인 버튼을 누르면 돈·관광객·최대관광객을 대량 지급한다.
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

    void Start()
    {
        if (feedbackText != null) feedbackText.text = "";
        confirmButton.onClick.AddListener(OnConfirm);
        codeInputField.onSubmit.AddListener(_ => OnConfirm());
    }

    void OnConfirm()
    {
        if (codeInputField.text.Trim().ToLower() == DEV_CODE)
        {
            GameManager.Instance.AddMoney(MONEY_BONUS);
            GameManager.Instance.AddTourists(TOURIST_BONUS, MAX_TOURIST_BONUS);

            if (feedbackText != null) feedbackText.text = "개발자 모드 활성화!";
        }
        else
        {
            if (feedbackText != null) feedbackText.text = "잘못된 코드입니다.";
        }

        codeInputField.text = "";
    }
}
