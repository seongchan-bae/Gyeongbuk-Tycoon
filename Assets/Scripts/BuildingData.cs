using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "경북타이쿤/건물 데이터")]
public class BuildingData : ScriptableObject
{
    [Header("상점 UI 표시 정보")]
    public int buildingID;            // 건물 고유 ID
    public string buildingName;       // 건물 이름 (예: 첨성대, 불국사)
    public int price;                 // 구매 가격 (Gold)
    public Sprite buildingIcon;       // 상점 슬롯 카드에 출력할 아이콘 Sprite
    public bool isUnlocked = false;   // 해금 여부

    [Header("인게임 맵 설치용 건물 프리팹")]
    public GameObject buildingPrefab; // 필드 타일맵에 직접 설치될 실제 건물 프리팹

    [Header("건물 스펙 및 생산 속성")]
    public int tileWidth = 1;         // 타일 가로 규격
    public int tileHeight = 1;        // 타일 세로 규격
    public float goldProductionRate;  // 초당 골드 생산량
}