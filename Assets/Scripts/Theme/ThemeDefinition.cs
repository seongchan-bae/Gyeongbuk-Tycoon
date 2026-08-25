using System;
using UnityEngine;

/// <summary>
/// 테마 1개의 정의. 배경 이미지와 (추후) GPS 해금 좌표를 담는다.
/// </summary>
[Serializable]
public class ThemeDefinition
{
    [Header("식별")]
    public string themeId = "theme_id";        // 세이브에 기록되는 고유 키
    public string displayName = "테마 이름";

    [Header("이미지")]
    public Sprite backgroundSprite;            // 인게임 "배경화면" 오브젝트에 적용될 스프라이트
    public Sprite thumbnail;                   // 비어 있으면 backgroundSprite를 그대로 사용

    [Header("해금 조건")]
    public bool unlockedByDefault = true;      // false면 GPS 인증이 필요

    [Header("GPS 해금 좌표 (아직 미사용)")]
    public double latitude;
    public double longitude;
    public float unlockRadiusMeters = 150f;
    public string landmarkName = "";           // 인증 실패 안내 문구에 사용

    public Sprite ThumbnailOrBackground
    {
        get { return thumbnail != null ? thumbnail : backgroundSprite; }
    }
}
