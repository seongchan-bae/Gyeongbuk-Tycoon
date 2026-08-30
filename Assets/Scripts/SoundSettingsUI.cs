using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 환경설정 창의 '기본 설정' 탭. 배경음악/효과음의 음량 슬라이더, 0~100 수치 표시,
/// 채널별 음소거 버튼과 음량 단계 아이콘을 한 곳에서 관리한다.
///
/// 저장은 창을 닫을 때(OnDisable) 한 번만 한다. 슬라이더를 드래그하는 동안에는
/// JSON 파일 전체를 매 프레임 다시 쓰게 되므로 저장하지 않는다.
/// </summary>
public class SoundSettingsUI : MonoBehaviour
{
    [System.Serializable]
    public class Channel
    {
        [Tooltip("음량 슬라이더 (0~1 범위)")]
        public Slider slider;
        [Tooltip("슬라이더 위치를 0~100 숫자로 보여줄 텍스트")]
        public TextMeshProUGUI valueText;
        [Tooltip("음소거 토글 버튼")]
        public Button muteButton;
        [Tooltip("음소거 버튼 안의 스피커 아이콘")]
        public Image icon;
    }

    [Header("채널")]
    [SerializeField] private Channel bgm = new Channel();
    [SerializeField] private Channel sfx = new Channel();

    [Header("음량 단계 아이콘")]
    [Tooltip("음소거 상태 (또는 음량 0)")]
    [SerializeField] private Sprite mutedIcon;
    [Tooltip("낮음 - 0% 초과 ~ lowThreshold 미만")]
    [SerializeField] private Sprite lowIcon;
    [Tooltip("보통 - lowThreshold ~ highThreshold 미만")]
    [SerializeField] private Sprite midIcon;
    [Tooltip("높음 - highThreshold 이상")]
    [SerializeField] private Sprite highIcon;

    [Header("아이콘 전환 기준")]
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.4f;
    [SerializeField, Range(0f, 1f)] private float highThreshold = 0.9f;

    [Header("음소거 표시 색")]
    [SerializeField] private Color activeColor = new Color(0.20f, 0.20f, 0.20f, 1f);
    [SerializeField] private Color mutedColor = new Color(0.60f, 0.60f, 0.60f, 1f);

    private bool bgmMuted;
    private bool sfxMuted;
    private bool suppressCallbacks;   // 초기값 주입 중 콜백이 되돌아오는 것을 막는다

    private void Awake()
    {
        Wire(bgm, OnBgmSliderChanged, ToggleBgmMute);
        Wire(sfx, OnSfxSliderChanged, ToggleSfxMute);
    }

    private void Wire(Channel c, UnityEngine.Events.UnityAction<float> onSlider, UnityEngine.Events.UnityAction onMute)
    {
        if (c.slider != null)
        {
            c.slider.minValue = 0f;
            c.slider.maxValue = 1f;
            c.slider.wholeNumbers = false;
            c.slider.onValueChanged.RemoveListener(onSlider);
            c.slider.onValueChanged.AddListener(onSlider);
        }
        if (c.muteButton != null)
        {
            c.muteButton.onClick.RemoveListener(onMute);
            c.muteButton.onClick.AddListener(onMute);
        }
    }

    private void OnEnable()
    {
        // 창을 열 때마다 저장된 값을 다시 읽어 화면과 실제 음량을 맞춘다.
        float bgmVolume = 0.5f;
        float sfxVolume = 0.8f;

        if (SaveManager.Instance != null)
        {
            var data = SaveManager.Instance.CurrentData;
            bgmVolume = data.bgmVolume;
            sfxVolume = data.sfxVolume;
            bgmMuted = data.bgmMuted;
            sfxMuted = data.sfxMuted;
        }

        suppressCallbacks = true;
        if (bgm.slider != null) bgm.slider.value = bgmVolume;
        if (sfx.slider != null) sfx.slider.value = sfxVolume;
        suppressCallbacks = false;

        ApplyAll();
        RefreshAll();
    }

    private void OnDisable()
    {
        // 창이 닫히는 시점에만 파일로 저장한다.
        if (SaveManager.Instance == null) return;

        SaveManager.Instance.SaveSettings(
            bgm.slider != null ? bgm.slider.value : SaveManager.Instance.CurrentData.bgmVolume,
            sfx.slider != null ? sfx.slider.value : SaveManager.Instance.CurrentData.sfxVolume,
            bgmMuted, sfxMuted);
    }

    private void OnBgmSliderChanged(float value)
    {
        if (suppressCallbacks) return;

        // 음소거 상태에서 슬라이더를 움직이면 소리를 켜려는 의도로 보고 자동 해제한다.
        if (bgmMuted && value > 0f) bgmMuted = false;

        ApplyAll();
        RefreshAll();
    }

    private void OnSfxSliderChanged(float value)
    {
        if (suppressCallbacks) return;
        if (sfxMuted && value > 0f) sfxMuted = false;

        ApplyAll();
        RefreshAll();
    }

    private void ToggleBgmMute()
    {
        bgmMuted = !bgmMuted;
        ApplyAll();
        RefreshAll();
    }

    private void ToggleSfxMute()
    {
        sfxMuted = !sfxMuted;
        ApplyAll();
        RefreshAll();
    }

    /// <summary>현재 화면 상태를 실제 오디오에 반영한다.</summary>
    private void ApplyAll()
    {
        if (SoundManager.Instance == null) return;

        if (bgm.slider != null) SoundManager.Instance.ApplyBGM(bgm.slider.value, bgmMuted);
        if (sfx.slider != null) SoundManager.Instance.ApplySFX(sfx.slider.value, sfxMuted);
    }

    private void RefreshAll()
    {
        Refresh(bgm, bgmMuted);
        Refresh(sfx, sfxMuted);
    }

    private void Refresh(Channel c, bool muted)
    {
        if (c.slider == null) return;
        float value = c.slider.value;

        if (c.valueText != null)
        {
            // 0~1 슬라이더를 사람이 읽는 0~100으로 바꿔 보여준다.
            c.valueText.text = Mathf.RoundToInt(value * 100f).ToString();
            c.valueText.color = muted ? mutedColor : activeColor;
        }

        if (c.icon != null)
        {
            Sprite sprite = PickIcon(value, muted);
            if (sprite != null) c.icon.sprite = sprite;
        }

        // 음소거 중에는 채워진 막대도 흐리게 해서 소리가 나지 않는 상태임을 드러낸다.
        if (c.slider.fillRect != null)
        {
            var fill = c.slider.fillRect.GetComponent<Graphic>();
            if (fill != null)
            {
                Color col = fill.color;
                col.a = muted ? 0.35f : 1f;
                fill.color = col;
            }
        }
    }

    /// <summary>
    /// 음량 구간에 맞는 스피커 아이콘을 고른다.
    /// 음소거가 아니어도 음량이 0이면 실제로 소리가 나지 않으므로 음소거 아이콘을 쓴다.
    /// </summary>
    private Sprite PickIcon(float value, bool muted)
    {
        if (muted || value <= 0f) return mutedIcon;
        if (value < lowThreshold) return lowIcon;
        if (value < highThreshold) return midIcon;
        return highIcon;
    }
}
