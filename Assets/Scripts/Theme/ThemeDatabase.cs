using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임에 존재하는 모든 테마 목록. 개수가 바뀌어도 이 에셋만 수정하면 UI가 따라간다.
/// </summary>
[CreateAssetMenu(fileName = "ThemeDatabase", menuName = "Gyeongbuk Tycoon/Theme Database")]
public class ThemeDatabase : ScriptableObject
{
    [SerializeField] private List<ThemeDefinition> themes = new List<ThemeDefinition>();

    public IReadOnlyList<ThemeDefinition> Themes { get { return themes; } }

    public ThemeDefinition Find(string themeId)
    {
        if (string.IsNullOrEmpty(themeId)) return null;
        for (int i = 0; i < themes.Count; i++)
        {
            if (themes[i].themeId == themeId) return themes[i];
        }
        return null;
    }

    /// <summary>세이브에 기록된 테마가 없을 때 사용할 기본 테마.</summary>
    public ThemeDefinition DefaultTheme
    {
        get { return themes.Count > 0 ? themes[0] : null; }
    }
}
