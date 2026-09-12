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

    // 설정 창에서 정한 값. 음소거는 음량과 따로 기억해 둬야
    // 음소거를 풀었을 때 원래 음량으로 돌아온다.
    private float bgmVolume = 0.5f;
    private float sfxVolume = 0.8f;
    private bool bgmMuted;
    private bool sfxMuted;

    // 씬 안의 버튼들이 OnClick 에서 직접 가리키는 효과음 AudioSource.
    // SoundManager 는 DontDestroyOnLoad 라 씬 오브젝트가 참조할 수 없어, 씬 쪽에서 등록해 준다.
    private AudioSource sceneSfxSource;

    /// <summary>
    /// 지금 재생 중인 BGM 의 이름. 재생 중이 아니면 빈 문자열.
    /// 잠시 다른 곡을 틀었다가 원래 곡으로 되돌려야 할 때 쓴다(도움말 오버레이).
    /// </summary>
    public string CurrentBGMName { get; private set; } = string.Empty;

    private float EffectiveBgmVolume { get { return bgmMuted ? 0f : bgmVolume; } }
    private float EffectiveSfxVolume { get { return sfxMuted ? 0f : sfxVolume; } }

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

        LoadSavedVolumes();

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
    /// <summary>세이브에 기록된 음량/음소거를 읽어 실제 AudioSource 에 반영한다.</summary>
    private void LoadSavedVolumes()
    {
        if (SaveManager.Instance == null) return;

        GameSaveData data = SaveManager.Instance.CurrentData;
        bgmVolume = Mathf.Clamp01(data.bgmVolume);
        sfxVolume = Mathf.Clamp01(data.sfxVolume);
        bgmMuted = data.bgmMuted;
        sfxMuted = data.sfxMuted;

        SetBGMVolume(EffectiveBgmVolume);
        SetSFXVolume(EffectiveSfxVolume);
    }

    /// <summary>
    /// 씬에 놓인 효과음 AudioSource 를 등록한다.
    /// 버튼 OnClick 의 AudioSource.PlayOneShot 이 이 AudioSource 를 직접 호출하므로,
    /// 설정 창에서 정한 효과음 음량이 그쪽에도 그대로 걸리게 해 준다.
    /// </summary>
    public void AttachSceneSfxSource(AudioSource source)
    {
        sceneSfxSource = source;
        if (sceneSfxSource != null) sceneSfxSource.volume = EffectiveSfxVolume;
    }

    #region BGM Methods
    //인스펙터 호출용
public void PlayBGM(string clipName)
{
    // 설정 창에서 정한 음량을 무시하고 항상 1.0 으로 재생하면
    // BGM 이 바뀔 때마다 소리가 최대로 돌아가 버린다.
    PlayBGM(clipName, EffectiveBgmVolume);
}
public void PlayBGM(string clipName, float volume = 1.0f)
{
    if (bgmDictionary == null) return;

    if (bgmDictionary.TryGetValue(clipName, out AudioClip clip))
    {
        // 1. 이미 같은 BGM이 재생 중이라면 중복 재생 방지 후 종료
        if (bgmSource.isPlaying && bgmSource.clip == clip)
        {
            CurrentBGMName = clipName;
            return;
        }

        // 2. 다른 BGM이 재생 중이라면 명시적으로 정지
        if (bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }

        // 3. 새 BGM 클립 할당 및 재생
        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.loop = true;
        bgmSource.Play();
        CurrentBGMName = clipName;
    }
    else
    {
        Debug.LogWarning($"[SoundManager] BGM 클립을 찾을 수 없습니다: {clipName}");
    }
}
#endregion

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
        CurrentBGMName = string.Empty;
    }

    // 설정창/UI 호출용 BGM 볼륨 적용 메서드
    public void ApplyBGM(float volume, bool muted)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmMuted = muted;
        SetBGMVolume(EffectiveBgmVolume);
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(volume);
        }
    }
    

    #region SFX Methods
    public void PlaySFX(string clipName, float volume = 1.0f)
    {
        if (sfxDictionary == null || sfxSource == null) return;

        if (sfxDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    // 설정창/UI 호출용 SFX 볼륨 적용 메서드
    public void ApplySFX(float volume, bool muted)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxMuted = muted;
        SetSFXVolume(EffectiveSfxVolume);
    }

    public void SetSFXVolume(float volume)
    {
        float v = Mathf.Clamp01(volume);

        if (sfxSource != null) sfxSource.volume = v;

        // 버튼 클릭음은 씬 쪽 AudioSource 가 내므로 같이 맞춰 준다.
        if (sceneSfxSource != null) sceneSfxSource.volume = v;
    }
    #endregion
}