using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Pretendard 로 되어 있던 글꼴을 새로 받은 esamanru 로 바꿔 주는 에디터 도구.
///
/// TMP 는 .otf 파일을 직접 쓰지 못하고 폰트 에셋(SDF)을 거치므로, 먼저 esamanru 용 폰트
/// 에셋을 만든 다음 Pretendard 폰트 에셋을 가리키던 TMP 텍스트를 모두 갈아탄다.
/// 아틀라스는 Dynamic 으로 만든다. 한글은 글자 수가 많아서 전부 미리 구워 두면 아틀라스가
/// 지나치게 커지는데, Dynamic 은 화면에 실제로 나온 글자만 그때그때 채운다.
///
/// Pretendard 를 쓰지 않는 글꼴(KERISKEDU, seoulSyber 등)은 건드리지 않는다.
/// Assets/_Recovery 아래의 백업 씬도 손대지 않는다.
/// </summary>
public static class EsamanruFontTool
{
    private const string MenuRoot = "Tools/경북 타이쿤/";

    private const string SourceFontPath = "Assets/Fonts/esamanru OTF Light.otf";
    private const string TargetFontAssetPath = "Assets/Fonts/esamanru OTF Light SDF.asset";
    private const string PretendardFontAssetPath = "Assets/Fonts/Pretendard-Black SDF.asset";

    // 폰트 에셋 생성 설정. Dynamic 이라 아틀라스는 필요한 만큼만 채워진다.
    private const int SamplingPointSize = 90;
    private const int AtlasPadding = 9;
    private const int AtlasSize = 1024;

    /// <summary>백업/복구용 사본이라 바꾸지 않는 경로.</summary>
    private const string SkipFolder = "Assets/_Recovery/";

    [MenuItem(MenuRoot + "esamanru 폰트 에셋 만들기")]
    private static void CreateFontAssetOnly()
    {
        TMP_FontAsset created = LoadOrCreateEsamanru();
        if (created != null)
        {
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
        }
    }

    [MenuItem(MenuRoot + "Pretendard → esamanru 글꼴 교체")]
    private static void SwapAll()
    {
        TMP_FontAsset pretendard = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PretendardFontAssetPath);
        if (pretendard == null)
        {
            EditorUtility.DisplayDialog("글꼴 교체",
                $"Pretendard 폰트 에셋을 찾지 못했습니다.\n{PretendardFontAssetPath}", "확인");
            return;
        }

        TMP_FontAsset esamanru = LoadOrCreateEsamanru();
        if (esamanru == null) return;

        // 열려 있는 씬을 저장하지 않은 채 다른 씬을 열면 작업이 사라진다. 사용자에게 먼저 묻는다.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var report = new StringBuilder();
        report.AppendLine($"[EsamanruFontTool] '{pretendard.name}' → '{esamanru.name}' 교체 결과");

        int prefabTotal = SwapInPrefabs(pretendard, esamanru, report);
        int sceneTotal = SwapInScenes(pretendard, esamanru, report);
        bool settingsChanged = SwapDefaultFontAsset(pretendard, esamanru, report);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        report.AppendLine($"합계: 프리팹 {prefabTotal}개 텍스트, 씬 {sceneTotal}개 텍스트"
                          + (settingsChanged ? ", TMP 기본 글꼴 1건" : string.Empty));

        if (prefabTotal + sceneTotal == 0 && !settingsChanged)
        {
            report.Append("바꿀 것이 없었습니다. 이미 교체돼 있는지 확인해 보세요.");
        }

        Debug.Log(report.ToString());
    }

    // ───────────────────────── 폰트 에셋 ─────────────────────────

    private static TMP_FontAsset LoadOrCreateEsamanru()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontAssetPath);
        if (existing != null) return existing;

        Font source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (source == null)
        {
            EditorUtility.DisplayDialog("esamanru 폰트 에셋",
                $"글꼴 파일을 찾지 못했습니다.\n{SourceFontPath}", "확인");
            return null;
        }

        TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(
            source, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
            AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic);

        if (created == null)
        {
            Debug.LogError("[EsamanruFontTool] 폰트 에셋을 만들지 못했습니다.");
            return null;
        }

        string assetName = Path.GetFileNameWithoutExtension(TargetFontAssetPath);
        created.name = assetName;

        AssetDatabase.CreateAsset(created, TargetFontAssetPath);

        // 아틀라스 텍스처와 머티리얼은 CreateFontAsset 이 메모리에만 만들어 둔다.
        // 폰트 에셋의 하위 에셋으로 붙여야 저장되고, 다음에 열었을 때도 그대로 이어진다.
        if (created.atlasTextures != null && created.atlasTextures.Length > 0 && created.atlasTextures[0] != null)
        {
            created.atlasTextures[0].name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(created.atlasTextures[0], created);
        }
        if (created.material != null)
        {
            created.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(created.material, created);
        }

        EditorUtility.SetDirty(created);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(TargetFontAssetPath);

        Debug.Log($"[EsamanruFontTool] 폰트 에셋을 만들었습니다: {TargetFontAssetPath}", created);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontAssetPath);
    }

    // ───────────────────────── 교체 ─────────────────────────

    private static int SwapInPrefabs(TMP_FontAsset from, TMP_FontAsset to, StringBuilder report)
    {
        int total = 0;
        string fromGuid = AssetDatabase.AssetPathToGUID(PretendardFontAssetPath);

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || path.StartsWith(SkipFolder)) continue;
            if (!MayReference(path, fromGuid)) continue;

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (contents == null) continue;

            try
            {
                int changed = Swap(contents.GetComponentsInChildren<TMP_Text>(true), from, to);
                if (changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    report.AppendLine($"  프리팹 {path} — {changed}개");
                    total += changed;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        return total;
    }

    private static int SwapInScenes(TMP_FontAsset from, TMP_FontAsset to, StringBuilder report)
    {
        var scenePaths = new List<string>();
        string fromGuid = AssetDatabase.AssetPathToGUID(PretendardFontAssetPath);

        foreach (string guid in AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || path.StartsWith(SkipFolder)) continue;
            if (!MayReference(path, fromGuid)) continue;
            scenePaths.Add(path);
        }

        // 작업이 끝나면 열려 있던 씬으로 돌아온다.
        string returnTo = SceneManager.GetActiveScene().path;
        int total = 0;

        foreach (string path in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!scene.IsValid()) continue;

            int changed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                changed += Swap(root.GetComponentsInChildren<TMP_Text>(true), from, to);
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.AppendLine($"  씬 {path} — {changed}개");
                total += changed;
            }
        }

        if (!string.IsNullOrEmpty(returnTo)) EditorSceneManager.OpenScene(returnTo, OpenSceneMode.Single);

        return total;
    }

    /// <summary>
    /// 그 파일이 해당 폰트 에셋을 참조할 가능성이 있는지 파일 내용으로 먼저 걸러낸다.
    /// 씬 83개·프리팹 전부를 하나하나 열어 보면 느리고, 상관없는 파일까지 다시 저장될 수 있다.
    /// 텍스트 직렬화가 아니면 이 검사를 믿을 수 없으므로 전부 검사 대상으로 넘긴다.
    /// </summary>
    private static bool MayReference(string assetPath, string fontAssetGuid)
    {
        if (string.IsNullOrEmpty(fontAssetGuid)) return true;
        if (EditorSettings.serializationMode != SerializationMode.ForceText) return true;

        try
        {
            return File.ReadAllText(assetPath).Contains(fontAssetGuid);
        }
        catch
        {
            return true;
        }
    }

    private static int Swap(TMP_Text[] texts, TMP_FontAsset from, TMP_FontAsset to)
    {
        int changed = 0;

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || text.font != from) continue;

            text.font = to;

            // 글꼴을 바꾸면 머티리얼도 새 글꼴의 것으로 맞춰야 한다. 그대로 두면 예전 아틀라스를
            // 가리켜 글자가 깨진다. 이 프로젝트에는 Pretendard 를 변형한 머티리얼이 없어서
            // 기본 머티리얼로 덮어도 잃는 설정이 없다.
            text.fontSharedMaterial = to.material;

            EditorUtility.SetDirty(text);
            changed++;
        }

        return changed;
    }

    /// <summary>
    /// TMP 설정의 기본 글꼴도 함께 바꾼다. 새로 만드는 텍스트가 다시 Pretendard 로 생기지 않게 한다.
    /// </summary>
    private static bool SwapDefaultFontAsset(TMP_FontAsset from, TMP_FontAsset to, StringBuilder report)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_Settings", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(path);
            if (settings == null) continue;

            var serialized = new SerializedObject(settings);
            SerializedProperty property = serialized.FindProperty("m_defaultFontAsset");
            if (property == null) continue;
            if (property.objectReferenceValue != from) continue;

            property.objectReferenceValue = to;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);

            report.AppendLine($"  TMP 기본 글꼴 {path}");
            return true;
        }

        return false;
    }
}
