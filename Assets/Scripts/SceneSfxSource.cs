using UnityEngine;

/// <summary>
/// 씬에 하나 놓아두는 효과음 전용 AudioSource.
///
/// 버튼 OnClick 에 걸어 둔 AudioSource.PlayOneShot 은 "이 씬 안에 있는 특정 AudioSource"를
/// 가리켜야 한다. 그런데 SoundManager 는 DontDestroyOnLoad 로 타이틀 씬에서 넘어오는 오브젝트라
/// 씬에 저장된 버튼이 직접 가리킬 수 없다(가리켜 두면 병합·씬 전환 과정에서 참조가 끊긴다).
///
/// 그래서 효과음을 받아 줄 AudioSource 는 씬에 따로 두고, 음량 설정만 SoundManager 를 따라가게 했다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SceneSfxSource : MonoBehaviour
{
    public static SceneSfxSource Instance { get; private set; }

    private AudioSource source;

    public AudioSource Source { get { return source; } }

    private void Awake()
    {
        Instance = this;

        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;

        if (SoundManager.Instance != null)
        {
            // SoundManager 가 현재 음량/음소거를 바로 밀어넣어 준다.
            SoundManager.Instance.AttachSceneSfxSource(source);
        }
        else
        {
            // 타이틀 씬을 거치지 않고 이 씬만 단독으로 실행한 경우.
            ApplySavedVolume();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void ApplySavedVolume()
    {
        if (SaveManager.Instance == null) return;

        GameSaveData data = SaveManager.Instance.CurrentData;
        source.volume = data.sfxMuted ? 0f : Mathf.Clamp01(data.sfxVolume);
    }
}
