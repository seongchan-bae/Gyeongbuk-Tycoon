using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PuzzleItemUI : MonoBehaviour
{
    public Image thumbnailImage;
    public TextMeshProUGUI titleText;
    public Button selectButton;

    private PuzzleData currentData;
    private System.Action<PuzzleData> onSelectCallback;

    public void Setup(PuzzleData data, System.Action<PuzzleData> onClickAction)
    {
        currentData = data;
        onSelectCallback = onClickAction;

        if (thumbnailImage != null) thumbnailImage.sprite = data.puzzleImage;
        if (titleText != null) titleText.text = data.puzzleTitle;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onSelectCallback?.Invoke(currentData));
    }
}