using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public GameSaveData CurrentData { get; private set; } = new GameSaveData();
    private string saveFilePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 매니저 파괴 방지

            // 모바일/PC 로컬 단일 저장 경로 지정
            saveFilePath = Path.Combine(Application.persistentDataPath, "GyeongbukTycoonSave.json");
            LoadGameData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGameData()
    {
        // 1. 현재 재화 동기화
        if (GameManager.Instance != null)
        {
            CurrentData.userMoney = GameManager.Instance.UserMoney;
            // 지식 포인트 프로퍼티가 열려있다면 아래 주석 해제
            // CurrentData.userKnowledgePoint = GameManager.Instance.UserKnowledgePoint;
        }

        // 2. 맵에 설치된 건물 위치 동기화
        BuildingInstall installSystem = FindFirstObjectByType<BuildingInstall>();
        if (installSystem != null && installSystem.BaseGrid != null)
        {
            CurrentData.placedBuildings.Clear();
            Building[] activeBuildings = FindObjectsByType<Building>(FindObjectsSortMode.None);

            foreach (Building b in activeBuildings)
            {
                if (b.buildingData != null)
                {
                    Vector3Int cell = installSystem.BaseGrid.WorldToCell(b.transform.position);
                    CurrentData.placedBuildings.Add(new PlacedBuildingSaveData
                    {
                        buildingName = b.buildingData.buildingName,
                        gridX = cell.x,
                        gridY = cell.y,
                        gridZ = cell.z,
                        level = 1
                    });
                }
            }
        }

        CurrentData.lastSavedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        // 3. 파일로 저장
        try
        {
            string json = JsonUtility.ToJson(CurrentData, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"[SaveManager] 게임 저장 완료: {saveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] 저장 실패: {e.Message}");
        }
    }

    public void LoadGameData()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                CurrentData = JsonUtility.FromJson<GameSaveData>(json);
                Debug.Log("[SaveManager] 세이브 데이터 로드 성공");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] 로드 에러 (기본값 초기화): {e.Message}");
                CurrentData = new GameSaveData();
            }
        }
        else
        {
            CurrentData = new GameSaveData(); // 파일이 없으면 새 데이터 생성
        }
    }

    // 볼륨 조절 시 호출할 편의 함수
    public void SaveSettings(float bgm, float sfx, bool bgmMuted = false, bool sfxMuted = false)
    {
        CurrentData.bgmVolume = bgm;
        CurrentData.sfxVolume = sfx;
        CurrentData.bgmMuted = bgmMuted;
        CurrentData.sfxMuted = sfxMuted;
        SaveGameData();
    }

    // 테마 선택 시 호출할 편의 함수
    public void SaveCurrentTheme(string themeId)
    {
        CurrentData.currentThemeId = themeId;
        SaveGameData();
    }

    // 게임 종료 또는 백그라운드 전환 시 자동 저장
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGameData();
    }

    private void OnApplicationQuit()
    {
        SaveGameData();
    }
}