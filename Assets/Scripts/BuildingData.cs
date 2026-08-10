using UnityEngine;

[CreateAssetMenu(menuName = "Building/BuildingData")]
public class BuildingData : ScriptableObject
{
    public string buildingName;         // 상점에서 표시될 건물 이름
    public int price;                   // 구매 비용
    public float goldProductionRate;    // 골드 생산량
    public GameObject prefab;           // 실제 설치될 건물 프리팹
    public int tileWidth;               // 건물 가로 타일 수
    public int tileHeight;              // 건물 세로 타일 수
}
