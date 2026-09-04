using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 실루엣 게임용 모자이크 이미지를 원본 사진에서 자동 생성한다.
///
/// 실루엣 게임은 문제 이미지(mosaic_*.png)와 원본 이미지를 짝으로 요구하는데,
/// 지금까지 모자이크는 외부 도구로 따로 만들어 넣어야 했다. 이 창을 쓰면
/// 원본 폴더를 지정하고 버튼 한 번으로 빠진 모자이크를 전부 만들 수 있다.
///
/// 여는 법: 상단 메뉴 Tools > 경북 타이쿤 > 실루엣 모자이크 생성기
/// </summary>
public class MosaicGeneratorWindow : EditorWindow
{
    private const string MosaicPrefix = "mosaic_";

    [SerializeField] private DefaultAsset sourceFolder;
    [SerializeField] private DefaultAsset outputFolder;

    // 기존 수작업 모자이크 12장을 재 보니 10px 블록(1280px 기준 128칸)이 최빈값이라
    // 그 값을 기본값으로 삼는다. 새로 만든 이미지가 기존 것들과 같은 결로 보인다.
    [Tooltip("긴 변을 몇 칸으로 쪼갤지. 값이 작을수록 크게 뭉개져 문제가 어려워진다. 128이면 1280px 이미지에서 10px 블록.")]
    [SerializeField] private int blocksOnLongSide = 128;

    [SerializeField] private bool overwriteExisting;

    private Vector2 scroll;
    private Texture2D previewSource;
    private Texture2D previewMosaic;
    private string previewName = "";
    private string lastReport = "";

    [MenuItem("Tools/경북 타이쿤/실루엣 모자이크 생성기")]
    private static void Open()
    {
        GetWindow<MosaicGeneratorWindow>("모자이크 생성기").minSize = new Vector2(430, 560);
    }

    private void OnEnable()
    {
        if (sourceFolder == null)
            sourceFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Prefabs/SilueteGameImages");
        if (outputFolder == null)
            outputFolder = sourceFolder;
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("원본 -> 모자이크 일괄 생성", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "원본 폴더의 사진 중 짝이 되는 " + MosaicPrefix + "* 파일이 없는 것만 찾아 만듭니다.\n" +
            "이미 만들어 둔 모자이크는 '기존 파일 덮어쓰기'를 켜지 않는 한 건드리지 않습니다.",
            MessageType.Info);

        sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("원본 폴더", sourceFolder, typeof(DefaultAsset), false);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("저장 폴더", outputFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space(4);
        blocksOnLongSide = EditorGUILayout.IntSlider(
            new GUIContent("긴 변 블록 수", "작을수록 더 뭉개져서 어려워집니다. 기존 이미지들은 128 안팎입니다."),
            blocksOnLongSide, 20, 300);

        EditorGUILayout.LabelField(" ", DifficultyHint(blocksOnLongSide), EditorStyles.miniLabel);
        overwriteExisting = EditorGUILayout.Toggle("기존 파일 덮어쓰기", overwriteExisting);

        EditorGUILayout.Space(8);

        string srcPath = PathOf(sourceFolder);
        string outPath = PathOf(outputFolder);
        bool pathsOk = !string.IsNullOrEmpty(srcPath) && !string.IsNullOrEmpty(outPath);

        using (new EditorGUI.DisabledScope(!pathsOk))
        {
            if (GUILayout.Button("폴더 전체 생성", GUILayout.Height(30)))
                GenerateFolder(srcPath, outPath);

            if (GUILayout.Button("프로젝트 창에서 선택한 이미지만 생성", GUILayout.Height(24)))
                GenerateSelection(outPath);
        }

        if (GUILayout.Button("선택한 이미지 미리보기", GUILayout.Height(24)))
            BuildPreview();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("실루엣 게임 등록", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "만들어진 모자이크/원본 짝을 열려 있는 씬의 SilueteGameManager 문제 목록에 채워 넣습니다.\n" +
            "이미 등록된 항목은 건너뛰고, 정답 단어는 원본 파일 이름을 씁니다.",
            MessageType.None);
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(srcPath)))
        {
            if (GUILayout.Button("빠진 짝을 실루엣 문제 목록에 추가", GUILayout.Height(24)))
                RegisterToSilhouetteGame(srcPath, outPath);
        }

        if (!string.IsNullOrEmpty(lastReport))
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("결과", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(lastReport,
                EditorStyles.textArea, GUILayout.Height(110));
        }

        if (previewSource != null && previewMosaic != null)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("미리보기: " + previewName, EditorStyles.boldLabel);
            Rect r = GUILayoutUtility.GetRect(position.width - 30, 170);
            float half = r.width / 2f - 4f;
            GUI.DrawTexture(new Rect(r.x, r.y, half, r.height), previewSource, ScaleMode.ScaleToFit);
            GUI.DrawTexture(new Rect(r.x + half + 8, r.y, half, r.height), previewMosaic, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.EndScrollView();
    }

    private static string DifficultyHint(int blocks)
    {
        if (blocks <= 60) return "매우 어려움 - 형태만 겨우 남습니다";
        if (blocks <= 110) return "어려움 - 기존 이미지 중 가장 뭉개진 월정교 수준";
        if (blocks <= 160) return "기존 이미지 대부분과 같은 정도 (128 권장)";
        if (blocks <= 230) return "쉬움 - 한개마을 정도";
        return "매우 쉬움 - 소수서원 정도, 거의 알아볼 수 있습니다";
    }

    private static string PathOf(Object folder)
    {
        if (folder == null) return null;
        string p = AssetDatabase.GetAssetPath(folder);
        return AssetDatabase.IsValidFolder(p) ? p : null;
    }

    // ---------------------------------------------------------------- 생성

    private void GenerateFolder(string srcPath, string outPath)
    {
        List<string> sources = SourceImagesIn(srcPath);
        Run(sources, outPath);
    }

    private void GenerateSelection(string outPath)
    {
        List<string> sources = Selection.objects
            .Select(AssetDatabase.GetAssetPath)
            .Where(p => !string.IsNullOrEmpty(p) && IsImage(p) && !IsMosaic(p))
            .ToList();

        if (sources.Count == 0)
        {
            lastReport = "프로젝트 창에서 원본 이미지를 선택한 뒤 눌러 주세요.\n" +
                         "(mosaic_ 로 시작하는 파일은 원본이 아니라 결과물이라 제외됩니다)";
            return;
        }
        Run(sources, outPath);
    }

    private void Run(List<string> sources, string outPath)
    {
        int made = 0, skipped = 0, failed = 0;
        var lines = new List<string>();

        try
        {
            for (int i = 0; i < sources.Count; i++)
            {
                string src = sources[i];
                string name = Path.GetFileNameWithoutExtension(src);
                string dst = outPath + "/" + MosaicPrefix + name + ".png";

                EditorUtility.DisplayProgressBar("모자이크 생성",
                    name + " (" + (i + 1) + "/" + sources.Count + ")",
                    (float)i / Mathf.Max(1, sources.Count));

                if (!overwriteExisting && File.Exists(dst))
                {
                    skipped++;
                    continue;
                }

                if (GenerateOne(src, dst))
                {
                    made++;
                    lines.Add("생성: " + Path.GetFileName(dst));
                }
                else
                {
                    failed++;
                    lines.Add("실패: " + Path.GetFileName(src));
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        lastReport = string.Format("생성 {0} / 건너뜀 {1} / 실패 {2}  (긴 변 {3}칸)\n{4}",
            made, skipped, failed, blocksOnLongSide, string.Join("\n", lines.Take(12)));
        if (lines.Count > 12) lastReport += "\n... 외 " + (lines.Count - 12) + "건";

        Debug.Log("[MosaicGenerator] " + lastReport);
    }

    private bool GenerateOne(string srcAssetPath, string dstAssetPath)
    {
        return GenerateMosaic(srcAssetPath, dstAssetPath, blocksOnLongSide);
    }

    /// <summary>
    /// 창을 열지 않고도 쓸 수 있는 진입점. 원본 1장에서 모자이크 png 1장을 만들어 저장한다.
    /// 배치 스크립트나 다른 에디터 도구에서 호출할 수 있다.
    /// </summary>
    public static bool GenerateMosaic(string srcAssetPath, string dstAssetPath, int blocksOnLongSide)
    {
        Texture2D src = LoadReadable(srcAssetPath);
        if (src == null) return false;

        Texture2D mosaic = Pixelate(src, blocksOnLongSide);
        byte[] png = mosaic.EncodeToPNG();

        DestroyImmediate(src);
        DestroyImmediate(mosaic);

        if (png == null) return false;

        File.WriteAllBytes(dstAssetPath, png);
        AssetDatabase.ImportAsset(dstAssetPath, ImportAssetOptions.ForceUpdate);
        ApplySpriteImportSettings(dstAssetPath);
        return true;
    }

    /// <summary>
    /// 원본의 Read/Write 설정과 무관하게 픽셀을 읽기 위해 파일을 직접 디코드한다.
    /// (AssetDatabase로 불러온 Texture2D는 isReadable이 꺼져 있으면 GetPixels가 막힌다)
    /// </summary>
    private static Texture2D LoadReadable(string assetPath)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(assetPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
            {
                DestroyImmediate(tex);
                return null;
            }
            return tex;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MosaicGenerator] 읽기 실패 " + assetPath + " : " + e.Message);
            return null;
        }
    }

    /// <summary>블록 단위 평균색으로 채워 모자이크를 만든다. 크기는 원본과 동일하게 유지한다.</summary>
    private static Texture2D Pixelate(Texture2D src, int blocksOnLongSide)
    {
        int w = src.width;
        int h = src.height;
        int longSide = Mathf.Max(w, h);
        int block = Mathf.Max(2, Mathf.RoundToInt((float)longSide / Mathf.Max(1, blocksOnLongSide)));

        Color32[] pixels = src.GetPixels32();
        Color32[] outPixels = new Color32[w * h];

        for (int by = 0; by < h; by += block)
        {
            int yEnd = Mathf.Min(by + block, h);
            for (int bx = 0; bx < w; bx += block)
            {
                int xEnd = Mathf.Min(bx + block, w);

                long r = 0, g = 0, b = 0, a = 0;
                int count = 0;
                for (int y = by; y < yEnd; y++)
                {
                    int row = y * w;
                    for (int x = bx; x < xEnd; x++)
                    {
                        Color32 c = pixels[row + x];
                        r += c.r; g += c.g; b += c.b; a += c.a;
                        count++;
                    }
                }
                if (count == 0) continue;

                Color32 avg = new Color32(
                    (byte)(r / count), (byte)(g / count), (byte)(b / count), (byte)(a / count));

                for (int y = by; y < yEnd; y++)
                {
                    int row = y * w;
                    for (int x = bx; x < xEnd; x++)
                        outPixels[row + x] = avg;
                }
            }
        }

        var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.SetPixels32(outPixels);
        result.Apply();
        return result;
    }

    /// <summary>기존 mosaic_*.png 들과 같은 임포트 설정을 맞춰 준다.</summary>
    private static void ApplySpriteImportSettings(string assetPath)
    {
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.isReadable = true;
        ti.maxTextureSize = 2048;
        ti.SaveAndReimport();
    }

    // ------------------------------------------------------------- 미리보기

    private void BuildPreview()
    {
        string src = Selection.objects
            .Select(AssetDatabase.GetAssetPath)
            .FirstOrDefault(p => !string.IsNullOrEmpty(p) && IsImage(p) && !IsMosaic(p));

        if (src == null)
        {
            lastReport = "미리볼 원본 이미지를 프로젝트 창에서 선택해 주세요.";
            return;
        }

        if (previewSource != null) DestroyImmediate(previewSource);
        if (previewMosaic != null) DestroyImmediate(previewMosaic);

        previewSource = LoadReadable(src);
        previewMosaic = previewSource != null ? Pixelate(previewSource, blocksOnLongSide) : null;
        previewName = Path.GetFileNameWithoutExtension(src);
        Repaint();
    }

    // ------------------------------------------------- 실루엣 게임 자동 등록

    private void RegisterToSilhouetteGame(string srcPath, string outPath)
    {
        var manager = Object.FindFirstObjectByType<SilueteGameManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            lastReport = "열려 있는 씬에서 SilueteGameManager를 찾지 못했습니다.\nMiniGameScene을 연 뒤 다시 눌러 주세요.";
            return;
        }

        var so = new SerializedObject(manager);
        SerializedProperty list = so.FindProperty("quizList");

        var already = new HashSet<string>();
        for (int i = 0; i < list.arraySize; i++)
        {
            var org = list.GetArrayElementAtIndex(i).FindPropertyRelative("originalImage").objectReferenceValue;
            if (org != null) already.Add(org.name);
        }

        int added = 0;
        var lines = new List<string>();

        foreach (string src in SourceImagesIn(srcPath))
        {
            string name = Path.GetFileNameWithoutExtension(src);
            if (already.Contains(name)) continue;

            string mosaicPath = outPath + "/" + MosaicPrefix + name + ".png";
            var mosaic = AssetDatabase.LoadAssetAtPath<Sprite>(mosaicPath);
            var original = AssetDatabase.LoadAssetAtPath<Sprite>(src);
            if (mosaic == null || original == null) continue;

            list.InsertArrayElementAtIndex(list.arraySize);
            SerializedProperty el = list.GetArrayElementAtIndex(list.arraySize - 1);
            el.FindPropertyRelative("quizImage").objectReferenceValue = mosaic;
            el.FindPropertyRelative("originalImage").objectReferenceValue = original;
            el.FindPropertyRelative("correctAnswer").stringValue = name;

            added++;
            lines.Add("추가: " + name);
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        if (added > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        lastReport = added == 0
            ? "새로 추가할 짝이 없습니다. (모두 등록되어 있거나 모자이크가 아직 없습니다)"
            : added + "개 추가했습니다. 씬 저장(Ctrl+S)이 필요합니다.\n" + string.Join("\n", lines);

        Debug.Log("[MosaicGenerator] " + lastReport);
    }

    // ---------------------------------------------------------------- 유틸

    private static List<string> SourceImagesIn(string folder)
    {
        return AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => IsImage(p) && !IsMosaic(p))
            .OrderBy(p => p)
            .ToList();
    }

    private static bool IsMosaic(string path)
    {
        return Path.GetFileName(path).StartsWith(MosaicPrefix, System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImage(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
    }
}
