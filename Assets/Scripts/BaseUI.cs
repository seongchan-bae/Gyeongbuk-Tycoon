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
    }
    public void CloseStoreButton()
    {
        Shopbutton.gameObject.SetActive(false);
    }
    //상점화면
    public void ShowStoreUI()
    {
        BuildingPopupUI.Instance?.Hide();
        store.SetActive(true);
        Shopbutton.gameObject.SetActive(false);
    }
    public void CloseStoreUI()
    {
        store.SetActive(false);
        Shopbutton.gameObject.SetActive(true);
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

        // 닫을 때 바뀐 볼륨값을 JSON 파일로 최종 저장
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveSettings(bgmSlider.value, sfxSlider.value);
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
