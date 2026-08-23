using System;
using System.Collections.Generic;

// 맵에 배치된 개별 건물 저장 단위
[Serializable]
public class PlacedBuildingSaveData
{
    public string buildingName; // 건물 고유 이름 (BuildingData의 이름)
    public int gridX;
    public int gridY;
    public int gridZ;
    public int level = 1;       // 건물 업그레이드 레벨
}

// 게임 전체 세이브 데이터 (설정 + 재화 + 마을 상태 통합)
[Serializable]
public class GameSaveData
{
    // 1. 환경설정 데이터
    public float bgmVolume = 0.5f;
    public float sfxVolume = 0.8f;

    // 2. 인게임 재화 데이터
    public long userMoney = 10000L;
    public long userKnowledgePoint = 0L;

    // 3. 인게임 건물 및 해금 상태
    public List<string> unlockedBuildingList = new List<string>();
    public List<PlacedBuildingSaveData> placedBuildings = new List<PlacedBuildingSaveData>();

    // (선택) 마지막 접속 시간 기록
    public string lastSavedTime = "";
}