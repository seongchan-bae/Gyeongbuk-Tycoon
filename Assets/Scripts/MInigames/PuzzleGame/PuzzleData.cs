using UnityEngine;

[System.Serializable] // 👈 이 줄이 반드시 있어야 인스펙터에 목록 형태로 나타납니다!
public class PuzzleData
{
    public string puzzleTitle;
    public Sprite puzzleImage;
    [Tooltip("TourAPI contentId. puzzleImage 가 없을 때 런타임에 API 로 이미지를 받아온다.")]
    public string contentId;
}