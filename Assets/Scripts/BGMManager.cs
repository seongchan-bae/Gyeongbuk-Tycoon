using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    
    void Start()
    {
        SoundManager.Instance.PlayBGM("MinigameBGM");
    }
}