using System;
using UnityEngine;

/// <summary>
/// 현재 선택된 테마를 보관하고, 씬의 배경 오브젝트에 적용한다.
/// 지금은 "배경화면" SpriteRenderer의 스프라이트만 교체한다.
/// </summary>
public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    [SerializeField] private ThemeDatabase database;

    [Header("테마가 적용될 배경")]
    [Tooltip("비워두면 씬에서 '배경화면' 이름의 오브젝트를 찾는다.")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    public event Action<ThemeDefinition> OnThemeChanged;

    public ThemeDatabase Database { get { return database; } }
    public string CurrentThemeId { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (backgroundRenderer == null)
        {
            GameObject found = GameObject.Find("배경화면");
            if (found != null) backgroundRenderer = found.GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        ApplyTheme(GetSavedThemeId(), save: false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private string GetSavedThemeId()
    {
        string fallback = database != null && database.DefaultTheme != null ? database.DefaultTheme.themeId : null;
        if (database == null) return fallback;

        string saved = SaveManager.Instance != null ? SaveManager.Instance.CurrentData.currentThemeId : null;
        if (string.IsNullOrEmpty(saved)) return fallback;

        ThemeDefinition theme = database.Find(saved);
        if (theme == null) return fallback;

        // 해금 조건이 나중에 바뀌어 잠긴 테마가 저장돼 있을 수 있으므로 다시 확인한다.
        if (!IsUnlocked(theme))
        {
            Debug.Log($"[ThemeManager] 저장된 테마 {saved}가 잠겨 있어 기본 테마로 되돌립니다.");
            return fallback;
        }

        return saved;
    }

    /// <summary>테마를 적용한다. save=true면 세이브 파일에도 기록한다.</summary>
    public void ApplyTheme(string themeId, bool save = true)
    {
        if (database == null)
        {
            Debug.LogWarning("[ThemeManager] ThemeDatabase가 지정되지 않았습니다.");
            return;
        }

        ThemeDefinition theme = database.Find(themeId);
        if (theme == null)
        {
            Debug.LogWarning($"[ThemeManager] 알 수 없는 테마 id: {themeId}");
            return;
        }

        if (backgroundRenderer != null && theme.backgroundSprite != null)
        {
            backgroundRenderer.sprite = theme.backgroundSprite;
        }
        else if (backgroundRenderer == null)
        {
            Debug.LogWarning("[ThemeManager] 배경 SpriteRenderer를 찾지 못해 이미지 교체를 건너뜁니다.");
        }

        CurrentThemeId = theme.themeId;

        if (save && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveCurrentTheme(theme.themeId);
        }

        if (OnThemeChanged != null) OnThemeChanged(theme);
    }

    /// <summary>해금 여부. 기본 해금이거나 세이브에 해금 기록이 있으면 true.</summary>
    public bool IsUnlocked(ThemeDefinition theme)
    {
        if (theme == null) return false;
        if (theme.unlockedByDefault) return true;
        return SaveManager.Instance != null
            && SaveManager.Instance.CurrentData.unlockedThemeIds.Contains(theme.themeId);
    }

    /// <summary>해금 기록을 세이브에 남긴다.</summary>
    public void UnlockTheme(string themeId)
    {
        if (SaveManager.Instance == null) return;
        if (!SaveManager.Instance.CurrentData.unlockedThemeIds.Contains(themeId))
        {
            SaveManager.Instance.CurrentData.unlockedThemeIds.Add(themeId);
            SaveManager.Instance.SaveGameData();
        }
    }

    /// <summary>GPS 인증 시도 결과.</summary>
    public struct UnlockAttempt
    {
        public bool success;
        public string message;
    }

    /// <summary>
    /// 측정된 현재 위치가 테마의 해금 좌표 반경 안인지 판정하고, 맞으면 해금한다.
    /// </summary>
    public UnlockAttempt VerifyLocation(ThemeDefinition theme, LocationResult location)
    {
        if (theme == null)
            return new UnlockAttempt { success = false, message = "테마 정보를 찾을 수 없습니다." };

        if (IsUnlocked(theme))
            return new UnlockAttempt { success = true, message = $"'{theme.displayName}' 테마는 이미 해금되어 있습니다." };

        if (!location.IsSuccess)
            return new UnlockAttempt { success = false, message = location.UserMessage };

        if (theme.latitude == 0.0 && theme.longitude == 0.0)
            return new UnlockAttempt { success = false, message = $"'{theme.displayName}'의 해금 좌표가 설정되지 않았습니다." };

        double distance = GeoUtil.DistanceMeters(location.latitude, location.longitude, theme.latitude, theme.longitude);
        string place = string.IsNullOrEmpty(theme.landmarkName) ? theme.displayName : theme.landmarkName;

        if (distance <= theme.unlockRadiusMeters)
        {
            UnlockTheme(theme.themeId);
            Debug.Log($"[ThemeManager] 인증 성공: {theme.themeId} (거리 {distance:F0}m)");
            return new UnlockAttempt
            {
                success = true,
                message = $"인증 성공! {place}에서 '{theme.displayName}' 테마를 해금했습니다."
            };
        }

        return new UnlockAttempt
        {
            success = false,
            message = $"{place}에서 약 {GeoUtil.FormatDistance(distance)} 떨어져 있습니다. (인증 범위: {theme.unlockRadiusMeters:F0}m 이내)"
        };
    }
}
