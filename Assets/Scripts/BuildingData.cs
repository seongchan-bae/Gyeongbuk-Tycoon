using UnityEngine;

public enum BuildingCategory
{
    Basic,      // 기본건물
    Landmark    // 랜드마크
}

[CreateAssetMenu(menuName = "Building/BuildingData")]
public class BuildingData : ScriptableObject
{
    public string buildingName;         // 상점에서 표시될 건물 이름
    public int price;                   // 구매 비용
    public float goldProductionRate;    // 골드 생산량
    public Sprite thumbnail;            // 상점 카드에 표시될 이미지
    public GameObject prefab;           // 실제 설치될 건물 프리팹
    public int tileWidth;               // 건물 가로 타일 수
    public int tileHeight;              // 건물 세로 타일 수
    public string contentId;            // 한국관광공사 TourAPI contentId
    public int maxTouristIncrease;      // 최대 관광객 한도 증가량
    public int touristIncrease;         // 관광객 수 정적 증가량
    public bool requiresWaterTile;      // 설치 시 물 프리팹 배치 여부
    public GameObject waterTilePrefab;  // 건물 크기에 맞는 물 프리팹
    public BuildingCategory category;   // 상점 필터 카테고리
}
