using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class TitleUIController : MonoBehaviour
{
    [Header("Main Title Panels")]
    [SerializeField] private GameObject titlePanel;          // 메인 타이틀 패널
    [SerializeField] private GameObject settingsPanel;       // 환경설정 팝업창

    [Header("Loading UI Elements")]
    [SerializeField] private GameObject loadingPanel;        // 로딩 화면 패널
    [SerializeField] private Slider loadingSlider;           // 로딩바 Slider
    [SerializeField] private TextMeshProUGUI progressText;   // "로딩 중... 75%" 텍스트
    [SerializeField] private TextMeshProUGUI tipText;        // 경북 관광 팁 텍스트

    [Header("경북타이쿤 관광 팁 목록")]
    [SerializeField]
    private string[] gyeongbukTips = new string[]
    {
        "[TIP] 첨성대는 신라 선덕여왕 때 건립된 동양 최고의 천문대입니다.",
        "[TIP] 연구소에서 퀴즈를 풀면 지식 포인트를 획득할 수 있습니다.",
        "[TIP] 실제 경북 유적지에 방문하면 특별한 건물을 해금할 수 있습니다!",
        "[TIP] 관광객 수가 늘어날수록 골드 생산 속도가 빨라집니다."
    };

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

        // 앱 실행 시 시작 로딩 연출 실행 (1.5초간 진행 후 타이틀 화면 출력)
        StartCoroutine(StartAppFakeLoading(1.5f));
    }

    /// <summary>
    /// [게임 시작] 버튼 ➔ 월드 선택 팝업 열기
    /// </summary>
    public void OnClickStartButton()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        StartCoroutine(LoadMainSceneAsync());
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

        StartCoroutine(LoadMainSceneAsync());
    }

    private IEnumerator LoadMainSceneAsync()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(true);

        SetRandomTip();
        if (loadingSlider != null) loadingSlider.value = 0f;

        // 비동기 씬 로드 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(mainGameSceneName);

        // 씬 로드 예외 처리 (씬이 없는 경우 에러 방지)
        if (op == null)
        {
            Debug.LogError($"[오류] '{mainGameSceneName}' 씬을 찾을 수 없습니다! Build Profiles와 Inspector 씬 이름을 확인해 주세요.");
            yield break;
        }

        op.allowSceneActivation = false;

        float minLoadingTime = 1.0f;
        float timer = 0f;

        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            float targetProgress = Mathf.Clamp01(op.progress / 0.9f);

            if (loadingSlider != null)
            {
                loadingSlider.value = Mathf.MoveTowards(loadingSlider.value, targetProgress, Time.deltaTime * 2f);
                if (progressText != null) progressText.text = $"마을 불러오는 중... {(loadingSlider.value * 100f):F0}%";
            }

            if (loadingSlider != null && loadingSlider.value >= 1.0f && timer >= minLoadingTime)
            {
                op.allowSceneActivation = true;
            }
        }
    }

    private IEnumerator StartAppFakeLoading(float duration)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        SetRandomTip();

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            if (loadingSlider != null) loadingSlider.value = progress;
            if (progressText != null) progressText.text = $"게임 준비 중... {(progress * 100f):F0}%";

            yield return null;
        }

        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (titlePanel != null) titlePanel.SetActive(true);
    }

    private void SetRandomTip()
    {
        if (gyeongbukTips != null && gyeongbukTips.Length > 0 && tipText != null)
        {
            int randomIndex = Random.Range(0, gyeongbukTips.Length);
            tipText.text = gyeongbukTips[randomIndex];
        }
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
        // sfxSlider도 동일하게 적용
    }

    /// <summary>
    /// BGM 슬라이더 이동 시 실시간 작동 및 저장
    /// </summary>
    public void OnBGMVolumeChanged(float value)
    {
        if (bgmAudioSource != null) bgmAudioSource.volume = value;
        // SaveManager 통합 저장 호출
        SaveManager.Instance?.SaveSettings(value, sfxSlider != null ? sfxSlider.value : 0.8f);
    }

    /// <summary>
    /// SFX 슬라이더 이동 시 실시간 작동 및 저장
    /// </summary>
    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    #endregion
}