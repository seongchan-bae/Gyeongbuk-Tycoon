using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 랜드마크 BuildingData 의 contentId 를 읽어
/// SilueteGameManager / PuzzleUIManager 목록을 자동으로 채운다.
/// 이미지는 런타임에 TourImageLoader 가 API 로 받아온다.
///
/// Unity 메뉴 → Tools → Tour ContentId Filler
/// </summary>
public class TourImageDownloaderWindow : EditorWindow
{
    private Vector2 scroll;
    private string log = "";

    [MenuItem("Tools/Tour ContentId Filler")]
    public static void ShowWindow() =>
        GetWindow<TourImageDownloaderWindow>("Tour ContentId Filler");

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label("랜드마크 ContentId 자동 채우기", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "BuildingData 의 contentId 를 읽어 퀴즈·퍼즐 목록에 채웁니다.\n" +
            "이미지는 게임 실행 시 TourAPI 에서 자동으로 받아옵니다.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("① 퀴즈 목록 채우기  (SilueteGameManager)", GUILayout.Height(36)))
            FillQuizList();

        EditorGUILayout.Space(4);

        if (GUILayout.Button("② 퍼즐 목록 채우기  (PuzzleUIManager)", GUILayout.Height(36)))
            FillPuzzleList();

        EditorGUILayout.Space(8);
        GUILayout.Label("로그", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(log, GUILayout.MinHeight(200));

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────
    // ① 퀴즈 목록
    // ─────────────────────────────────────────────────────────────
    private void FillQuizList()
    {
        log = "";

        SilueteGameManager manager = FindObjectOfType<SilueteGameManager>();
        if (manager == null)
        {
            Log("[오류] 씬에 SilueteGameManager 가 없습니다.");
            return;
        }

        List<(string name, string contentId)> buildings = CollectBuildings();
        if (buildings.Count == 0) { Log("[오류] 랜드마크 BuildingData 를 찾지 못했습니다."); return; }

        SerializedObject   so   = new SerializedObject(manager);
        SerializedProperty list = so.FindProperty("quizList");
        list.ClearArray();

        foreach (var (name, contentId) in buildings)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            SerializedProperty elem = list.GetArrayElementAtIndex(list.arraySize - 1);

            elem.FindPropertyRelative("correctAnswer").stringValue = name;
            elem.FindPropertyRelative("contentId").stringValue     = contentId;
            elem.FindPropertyRelative("originalImage").objectReferenceValue = null;
            elem.FindPropertyRelative("description").stringValue   = "";
            // description 은 빈칸 → SilueteGameManager 가 QuizDescriptions.cs 에서 자동으로 찾음

            Log($"[추가] {name}  (contentId: {contentId})");
        }

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Log($"\n퀴즈 목록 {buildings.Count}개 완료. Ctrl+S 로 씬을 저장하세요.");
    }

    // ─────────────────────────────────────────────────────────────
    // ② 퍼즐 목록
    // ─────────────────────────────────────────────────────────────
    private void FillPuzzleList()
    {
        log = "";

        PuzzleUIManager manager = FindObjectOfType<PuzzleUIManager>();
        if (manager == null)
        {
            Log("[오류] 씬에 PuzzleUIManager 가 없습니다.");
            return;
        }

        List<(string name, string contentId)> buildings = CollectBuildings();
        if (buildings.Count == 0) { Log("[오류] 랜드마크 BuildingData 를 찾지 못했습니다."); return; }

        SerializedObject   so   = new SerializedObject(manager);
        SerializedProperty list = so.FindProperty("puzzleList");
        list.ClearArray();

        foreach (var (name, contentId) in buildings)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            SerializedProperty elem = list.GetArrayElementAtIndex(list.arraySize - 1);

            elem.FindPropertyRelative("puzzleTitle").stringValue = name;
            elem.FindPropertyRelative("contentId").stringValue   = contentId;
            elem.FindPropertyRelative("puzzleImage").objectReferenceValue = null;

            Log($"[추가] {name}  (contentId: {contentId})");
        }

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Log($"\n퍼즐 목록 {buildings.Count}개 완료. Ctrl+S 로 씬을 저장하세요.");
    }

    // ─────────────────────────────────────────────────────────────
    // 공통
    // ─────────────────────────────────────────────────────────────
    private List<(string name, string contentId)> CollectBuildings()
    {
        string[] guids = AssetDatabase.FindAssets("t:BuildingData",
            new[] { "Assets/BuildingData/랜드마크", "Assets/BuildingData/랜드마크1" });

        var result = new List<(string, string)>();
        var seen   = new HashSet<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BuildingData data = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            if (data == null || string.IsNullOrEmpty(data.contentId)) continue;
            if (!seen.Add(data.contentId)) continue;   // 중복 contentId 스킵
            result.Add((data.buildingName, data.contentId));
        }

        return result;
    }

    private void Log(string msg) { log += msg + "\n"; Repaint(); }
}
