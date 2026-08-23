using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

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
            if (bgmSource != null) bgmSource.volume = SaveManager.Instance.CurrentData.bgmVolume;
            if (sfxSource != null) sfxSource.volume = SaveManager.Instance.CurrentData.sfxVolume;
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
}