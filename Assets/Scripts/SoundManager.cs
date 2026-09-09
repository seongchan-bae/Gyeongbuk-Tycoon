using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] bgmClips;
    [SerializeField] private AudioClip[] sfxClips;

    private Dictionary<string, AudioClip> bgmDictionary;
    private Dictionary<string, AudioClip> sfxDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        // AudioSource 자동 생성 및 할당
        AudioSource[] sources = GetComponents<AudioSource>();
        
        if (sources.Length >= 2)
        {
            bgmSource = sources[0];
            sfxSource = sources[1];
        }
        else
        {
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.loop = true;

        // BGM 딕셔너리 초기화
        bgmDictionary = new Dictionary<string, AudioClip>();
        if (bgmClips != null)
        {
            foreach (var clip in bgmClips)
            {
                if (clip != null && !bgmDictionary.ContainsKey(clip.name))
                    bgmDictionary.Add(clip.name, clip);
            }
        }

        // SFX 딕셔너리 초기화
        sfxDictionary = new Dictionary<string, AudioClip>();
        if (sfxClips != null)
        {
            foreach (var clip in sfxClips)
            {
                if (clip != null && !sfxDictionary.ContainsKey(clip.name))
                    sfxDictionary.Add(clip.name, clip);
            }
        }
    }

    #region BGM Methods
    public void PlayBGM(string clipName, float volume = 1.0f)
    {
        if (bgmDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            bgmSource.volume = volume;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    // 설정창/UI 호출용 BGM 볼륨 적용 메서드
    public void ApplyBGM(float volume, bool bgmMuted)
    {
        SetBGMVolume(volume);
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(volume);
        }
    }
    #endregion

    #region SFX Methods
    public void PlaySFX(string clipName, float volume = 1.0f)
    {
        if (sfxDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    // 설정창/UI 호출용 SFX 볼륨 적용 메서드
    public void ApplySFX(float volume, bool sfxMuted )
    {
        SetSFXVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(volume);
        }
    }
    #endregion
}