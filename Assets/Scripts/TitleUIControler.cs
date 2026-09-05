using UnityEngine;
using UnityEngine.UI;

public class TitleUIController : MonoBehaviour
{
    [Header("Main Title Panels")]
    [SerializeField] private GameObject titlePanel;          // 메인 타이틀 패널
    [SerializeField] private GameObject settingsPanel;       // 환경설정 팝업창

    [Header("Audio Settings")]
    [SerializeField] private AudioSource bgmAudioSource;     // 배경음악 AudioSource
    [SerializeField] private Slider bgmSlider;               // 환경설정창 배경음 슬라이더
    [SerializeField] private Slider sfxSlider;               // 환경설정창 효과음 슬라이더


    [Header("Scene Settings")]
    [SerializeField] private string mainGameSceneName = "SampleScene"; // 씬 이름 (기본값: SampleScene)

    private void Start()
    {
        // 초기 패널 상태 세팅
        if (titlePanel != null) titlePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        InitVolumeSettings();

        if (titlePanel != null) titlePanel.SetActive(true);
    }

    /// <summary>
    /// [게임 시작] 버튼 ➔ 월드 선택 팝업 열기
    /// </summary>
    public void OnClickStartButton()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        SceneTransition.LoadScene(mainGameSceneName);
    }

    /// <summary>
    /// 월드 선택 팝업 닫기 (X 버튼)
    /// </summary>

    /// <summary>
    /// [환경설정] 버튼 ➔ 설정 팝업 열기
    /// </summary>
    public void OnClickSettingsButton()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            if (titlePanel != null) titlePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 환경설정 팝업 닫기 (X 버튼)
    /// </summary>
    public void OnClickCloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            if (titlePanel != null) titlePanel.SetActive(true);
        }
    }

    /// <summary>
    /// [로그인] 버튼 클릭
    /// </summary>
    public void OnClickLoginButton()
    {
        Debug.Log("[타이틀] 게스트 상태로 접속 중입니다.");
    }

    /// <summary>
    /// [게임 종료] 버튼 클릭
    /// </summary>
    public void OnClickQuitButton()
    {
        Debug.Log("[타이틀] 게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 슬롯 버튼 클릭 시 호출 (월드 번호: 1, 2, 3)
    /// </summary>
    public void OnClickWorldSlot(int slotIndex)
    {
        Debug.Log($"[타이틀] 월드 {slotIndex}번 슬롯이 선택되었습니다!");

        PlayerPrefs.SetInt("SelectedWorldSlot", slotIndex);
        PlayerPrefs.Save();

        SceneTransition.LoadScene(mainGameSceneName);
    }

    #region 사운드 및 볼륨 조절 로직

    private void InitVolumeSettings()
    {
        // SaveManager에서 볼륨 값 가져오기
        float savedBGM = SaveManager.Instance != null ? SaveManager.Instance.CurrentData.bgmVolume : 0.5f;
        float savedSFX = SaveManager.Instance != null ? SaveManager.Instance.CurrentData.sfxVolume : 0.8f;

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = savedBGM;
            if (!bgmAudioSource.isPlaying) bgmAudioSource.Play();
        }

        if (bgmSlider != null)
        {
            bgmSlider.value = savedBGM;
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = savedSFX;
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    /// <summary>
    /// BGM 슬라이더 이동 시 실시간 작동 및 저장
    /// </summary>
    public void OnBGMVolumeChanged(float value)
    {
        if (bgmAudioSource != null) bgmAudioSource.volume = value;
        // SaveManager 통합 저장 호출
        float sfx = sfxSlider != null ? sfxSlider.value
            : (SaveManager.Instance != null ? SaveManager.Instance.CurrentData.sfxVolume : 0.8f);
        SaveManager.Instance?.SaveSettings(value, sfx);
    }

    /// <summary>
    /// SFX 슬라이더 이동 시 저장. 배경음악과 동일하게 SaveManager를 거친다.
    /// 실제 효과음 반영은 효과음 리소스와 AudioSource가 준비된 뒤에 연결한다.
    /// </summary>
    public void OnSFXVolumeChanged(float value)
    {
        float bgm = bgmSlider != null ? bgmSlider.value
            : (SaveManager.Instance != null ? SaveManager.Instance.CurrentData.bgmVolume : 0.5f);
        SaveManager.Instance?.SaveSettings(bgm, value);
    }

    #endregion
}