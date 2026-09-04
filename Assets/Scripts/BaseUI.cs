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
    public UnityEngine.UI.Slider bgmSlider;
    public UnityEngine.UI.Slider sfxSlider;

    void Start()
    {
        // 팝업이 켜질 때 슬라이더 위치를 현재 저장된 볼륨에 맞춤
        if (SaveManager.Instance != null && bgmSlider != null && sfxSlider != null)
        {
            bgmSlider.value = SaveManager.Instance.CurrentData.bgmVolume;
            sfxSlider.value = SaveManager.Instance.CurrentData.sfxVolume;

            bgmSlider.onValueChanged.AddListener(OnBGMChanged);
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }
    }

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
    public void CloseSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 닫을 때 바뀐 볼륨값을 JSON 파일로 최종 저장.
        // 슬라이더는 씬마다 연결돼 있지 않을 수 있으므로(연결이 빠지면 여기서 NullReference가 났다)
        // 비어 있으면 저장된 값을 그대로 다시 쓴다.
        if (SaveManager.Instance != null && (bgmSlider != null || sfxSlider != null))
        {
            float bgm = bgmSlider != null ? bgmSlider.value : SaveManager.Instance.CurrentData.bgmVolume;
            float sfx = sfxSlider != null ? sfxSlider.value : SaveManager.Instance.CurrentData.sfxVolume;
            SaveManager.Instance.SaveSettings(bgm, sfx);
        }
    }

    // 슬라이더를 움직일 때 실시간으로 BGM 소리 크기 조절
    public void OnBGMChanged(float value)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetBGMVolume(value);
    }

    public void OnSFXChanged(float value)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetSFXVolume(value);
    }
}
