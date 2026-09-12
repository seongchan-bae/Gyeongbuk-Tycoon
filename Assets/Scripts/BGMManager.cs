using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    
    void Start()
    {
        // 타이틀 씬을 거치지 않고 실행하면 SoundManager 가 아직 없을 수 있다.
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.PlayBGM("MinigameBGM");
    }
}