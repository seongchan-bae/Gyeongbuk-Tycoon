using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("SFX 클립")]
    public AudioClip clipCardFlip;    // "card flip"
    public AudioClip clipButtonClick; // "Button Click"
    public AudioClip clipSuccess;     // "Success"
    public AudioClip clipGameOver;    // "GameOver"

    void Awake()
    {
        // 씬이 넘어가도 파괴되지 않는 싱글톤 구조
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // SaveManager에서 로드된 볼륨값을 시작 시 적용
        ApplySavedVolume();
    }

    public void ApplySavedVolume()
    {
        if (SaveManager.Instance != null)
        {
            var data = SaveManager.Instance.CurrentData;
            ApplyBGM(data.bgmVolume, data.bgmMuted);
            ApplySFX(data.sfxVolume, data.sfxMuted);
        }
    }

    // 볼륨 + 음소거를 한 번에 적용 (SoundSettingsUI에서 호출)
    public void ApplyBGM(float volume, bool muted)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = muted ? 0f : volume;
            bgmSource.mute = muted;
        }
    }

    public void ApplySFX(float volume, bool muted)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = muted ? 0f : volume;
            sfxSource.mute = muted;
        }
    }

    // 환경설정 슬라이더에서 호출할 실시간 볼륨 조절 함수
    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null) bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null) sfxSource.volume = volume;
    }

    public void PlaySFX(string clipName)
    {
        if (sfxSource == null) return;
        AudioClip clip = clipName switch
        {
            "card flip"    => clipCardFlip,
            "Button Click" => clipButtonClick,
            "Success"      => clipSuccess,
            "GameOver"     => clipGameOver,
            _              => null
        };
        if (clip != null) sfxSource.PlayOneShot(clip);
    }
}