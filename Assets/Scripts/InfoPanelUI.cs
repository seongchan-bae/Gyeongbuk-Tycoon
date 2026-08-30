using UnityEngine;
using TMPro;

/// <summary>
/// 환경설정 창의 '정보' 탭. 앱 버전을 채워 넣는다.
/// 출처/저작권 문구 자체는 CreditsText의 내용을 Inspector에서 직접 수정한다.
/// </summary>
public class InfoPanelUI : MonoBehaviour
{
    [SerializeField, Tooltip("버전을 표시할 텍스트")]
    private TextMeshProUGUI versionText;

    [SerializeField, Tooltip("버전 앞에 붙일 문구")]
    private string versionPrefix = "버전 ";

    private void OnEnable()
    {
        // Player Settings의 Version 값을 그대로 쓴다. 빌드할 때마다 손댈 필요가 없다.
        if (versionText != null) versionText.text = versionPrefix + Application.version;
    }
}
