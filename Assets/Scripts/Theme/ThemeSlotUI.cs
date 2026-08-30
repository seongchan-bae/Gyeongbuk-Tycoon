using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 테마 설정 패널에 가로로 나열되는 슬롯 1칸.
/// </summary>
public class ThemeSlotUI : MonoBehaviour
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Button selectButton;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject lockOverlay;     // 미해금일 때만 표시 (자물쇠)
    [SerializeField] private GameObject selectedFrame;   // 현재 선택된 테마 표시
    [SerializeField] private Button verifyButton;        // 미해금일 때만 표시 (GPS 인증)

    private ThemeDefinition theme;
    private System.Action<ThemeDefinition> onSelect;
    private System.Action<ThemeDefinition> onVerify;

    private const float LockedAlpha = 0.45f;

    public string ThemeId { get { return theme != null ? theme.themeId : null; } }

    public void Bind(ThemeDefinition definition,
                     System.Action<ThemeDefinition> selectCallback,
                     System.Action<ThemeDefinition> verifyCallback)
    {
        theme = definition;
        onSelect = selectCallback;
        onVerify = verifyCallback;

        if (nameText != null) nameText.text = definition.displayName;
        if (thumbnailImage != null) thumbnailImage.sprite = definition.ThumbnailOrBackground;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(HandleSelect);
        }
        if (verifyButton != null)
        {
            verifyButton.onClick.RemoveAllListeners();
            verifyButton.onClick.AddListener(HandleVerify);
        }
    }

    /// <summary>해금/선택 상태에 따라 자물쇠·인증버튼·투명도·선택테두리를 갱신한다.</summary>
    public void Refresh(bool unlocked, bool isSelected)
    {
        if (lockOverlay != null) lockOverlay.SetActive(!unlocked);
        if (verifyButton != null) verifyButton.gameObject.SetActive(!unlocked);
        if (selectedFrame != null) selectedFrame.SetActive(unlocked && isSelected);

        if (thumbnailImage != null)
        {
            Color c = thumbnailImage.color;
            c.a = unlocked ? 1f : LockedAlpha;
            thumbnailImage.color = c;
        }

        if (selectButton != null) selectButton.interactable = unlocked;
    }

    /// <summary>인증 요청 중에는 버튼을 눌리지 않게 한다.</summary>
    public void SetVerifyInteractable(bool value)
    {
        if (verifyButton != null) verifyButton.interactable = value;
    }

    private void HandleSelect()
    {
        if (onSelect != null && theme != null) onSelect(theme);
    }

    private void HandleVerify()
    {
        if (onVerify != null && theme != null) onVerify(theme);
    }
}
