using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 환경설정 창의 "테마 설정" 패널. 슬롯을 런타임에 생성해 가로로 나열하고,
/// 미해금 테마의 [인증] 버튼에서 GPS 위치 인증을 수행한다.
/// </summary>
public class ThemeSettingsUI : MonoBehaviour
{
    [SerializeField] private ThemeDatabase database;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private ThemeSlotUI slotPrefab;
    [SerializeField] private TextMeshProUGUI statusText;

    private readonly List<ThemeSlotUI> slots = new List<ThemeSlotUI>();
    private bool built;
    private bool isVerifying;

    private void OnEnable()
    {
        BuildIfNeeded();
        RefreshAll();
        SetStatus("");

        // 패널이 열려 있는 동안 위치 서비스를 켜 두면, [인증] 시점에 이미 안정된 좌표가 준비된다.
        if (GpsLocationService.Instance != null)
            GpsLocationService.Instance.BeginContinuousUpdates();
    }

    private void OnDisable()
    {
        if (GpsLocationService.Instance != null)
            GpsLocationService.Instance.EndContinuousUpdates();
    }

    private void BuildIfNeeded()
    {
        if (built) return;
        if (database == null || slotContainer == null || slotPrefab == null)
        {
            Debug.LogWarning("[ThemeSettingsUI] database / slotContainer / slotPrefab 중 비어 있는 항목이 있습니다.");
            return;
        }

        for (int i = slotContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(slotContainer.GetChild(i).gameObject);
        }
        slots.Clear();

        foreach (ThemeDefinition theme in database.Themes)
        {
            ThemeSlotUI slot = Instantiate(slotPrefab, slotContainer);
            slot.name = "ThemeSlot_" + theme.themeId;
            slot.Bind(theme, OnSlotSelected, OnSlotVerify);
            slots.Add(slot);
        }

        built = true;
    }

    private void RefreshAll()
    {
        string currentId = ThemeManager.Instance != null
            ? ThemeManager.Instance.CurrentThemeId
            : (SaveManager.Instance != null ? SaveManager.Instance.CurrentData.currentThemeId : null);

        for (int i = 0; i < slots.Count; i++)
        {
            ThemeDefinition theme = database.Find(slots[i].ThemeId);
            bool unlocked = ThemeManager.Instance != null
                ? ThemeManager.Instance.IsUnlocked(theme)
                : (theme != null && theme.unlockedByDefault);

            slots[i].Refresh(unlocked, slots[i].ThemeId == currentId);
            slots[i].SetVerifyInteractable(!isVerifying);
        }
    }

    private void OnSlotSelected(ThemeDefinition theme)
    {
        if (ThemeManager.Instance != null)
        {
            ThemeManager.Instance.ApplyTheme(theme.themeId);
        }
        else if (SaveManager.Instance != null)
        {
            // 배경이 없는 씬(타이틀 등)에서는 선택만 저장해 둔다.
            SaveManager.Instance.SaveCurrentTheme(theme.themeId);
        }

        SetStatus($"'{theme.displayName}' 테마를 적용했습니다.");
        RefreshAll();
    }

    private void OnSlotVerify(ThemeDefinition theme)
    {
        if (isVerifying) return;

        if (ThemeManager.Instance == null)
        {
            SetStatus("이 화면에서는 인증을 사용할 수 없습니다.");
            return;
        }

        isVerifying = true;
        RefreshAll();   // 인증 중에는 모든 인증 버튼 비활성화

        string place = string.IsNullOrEmpty(theme.landmarkName) ? theme.displayName : theme.landmarkName;
        SetStatus($"{place} 위치를 확인하는 중입니다...");

        GpsLocationService.Instance.RequestLocation(location =>
        {
            isVerifying = false;

            ThemeManager.UnlockAttempt attempt = ThemeManager.Instance.VerifyLocation(theme, location);
            SetStatus(attempt.message);

            RefreshAll();
        });
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
