using UnityEngine;

public class BGMManager : MonoBehaviour
{
    private static BGMManager instance;

    [Header("BGM Settings")]
    public AudioSource bgmAudioSource;
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 0.3f; // 은은하게 들리도록 기본 볼륨 30% 설정

   void Awake()
{
    // 싱글톤 패턴: 씬이 바뀌어도 BGM 매니저가 중복 생성되지 않도록 유지
    if (instance == null)
    {
        instance = this;
        transform.SetParent(null); // Canvas 등의 자식에서 벗어나 루트 오브젝트로 분리
        DontDestroyOnLoad(gameObject); // 단독 오브젝트만 DontDestroyOnLoad 적용
    }
    else
    {
        Destroy(gameObject); // 이미 존재하면 새로 생긴 것만 파괴
        return;
    }
}

    void Start()
    {
        PlayBGM();
    }

    void PlayBGM()
    {
        if (bgmAudioSource != null && bgmClip != null)
        {
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.loop = true;          // 무한 반복 설정
            bgmAudioSource.volume = bgmVolume;   // 은은한 볼륨 적용
            bgmAudioSource.playOnAwake = false;
            
            bgmAudioSource.Play();
        }
    }
}