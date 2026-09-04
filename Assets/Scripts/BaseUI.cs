using UnityEngine;
using UnityEngine.UI;


public class BaseUI : MonoBehaviour
{
    //상점화면
    [SerializeField]
    private GameObject store;
    //메인화면
    [SerializeField]
    private GameObject main;
    [SerializeField]
    private Button Shopbutton;
    [SerializeField]
    private GameObject APIboard;
    //미니게임 진입 버튼 — 상점 화면/건물 설치 중에는 상점 버튼과 함께 숨긴다
    [SerializeField]
    private GameObject miniGameButton;


    [Header("환경설정 UI")]
    public GameObject settingsPanel;

    //설정화면
    void settingUI()
    {

    }
    public void ShowStoreButton()
    {
        Shopbutton.gameObject.SetActive(true);
        if (miniGameButton != null) miniGameButton.SetActive(true);
    }
    public void CloseStoreButton()
    {
        Shopbutton.gameObject.SetActive(false);
        if (miniGameButton != null) miniGameButton.SetActive(false);
    }
    //상점화면
    public void ShowStoreUI()
    {
        BuildingPopupUI.Instance?.Hide();
        store.SetActive(true);
        Shopbutton.gameObject.SetActive(false);
        if (miniGameButton != null) miniGameButton.SetActive(false);
    }
    public void CloseStoreUI()
    {
        store.SetActive(false);
        Shopbutton.gameObject.SetActive(true);
        if (miniGameButton != null) miniGameButton.SetActive(true);
    }
    public void CloseAPIBoard()
    {
        APIboard.SetActive(false);
    }
    //연구소화면
    void laboratoryUI()
    {

    }
    //유저정보화면(레벨(진척도), 현재 골드량, 해금건물 개수 등등)
    void userInformationUI()
    {

    }

    // 톱니바퀴 버튼 OnClick에 연결할 함수
    public void OpenSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // 환경설정 창 닫기(X) 버튼 OnClick에 연결할 함수
    // 볼륨 저장은 SoundSettingsUI가 패널이 꺼질 때(OnDisable) 직접 처리한다.
    public void CloseSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
}
