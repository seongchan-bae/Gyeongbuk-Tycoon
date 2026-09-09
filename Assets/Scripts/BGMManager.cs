using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource bgmSource;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip[] bgmClips;

    private Dictionary<string, AudioClip> bgmDictionary;

    private void Awake()
    {
        // 싱글톤 패턴 구현 및 씬 파괴 방지
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
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.loop = true; // BGM 루프 재생 기본 설정
        bgmSource.playOnAwake = false;

        bgmDictionary = new Dictionary<string, AudioClip>();
        if (bgmClips != null)
        {
            foreach (var clip in bgmClips)
            {
                if (clip != null && !bgmDictionary.ContainsKey(clip.name))
                {
                    bgmDictionary.Add(clip.name, clip);
                }
            }
        }
    }

    // BGM 재생 (동일한 BGM이 재생 중이면 중복 실행 방지)
    public void PlayBGM(string bgmName, float volume = 1.0f)
    {
        if (bgmDictionary.TryGetValue(bgmName, out AudioClip clip))
        {
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.volume = volume;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"[BGMManager] BGM 클립을 찾을 수 없습니다: {bgmName}");
        }
    }

    // BGM 일시정지 및 정지
    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PauseBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.UnPause();
        }
    }

    // BGM 볼륨 조절 (0.0 ~ 1.0)
    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(volume);
        }
    }
}