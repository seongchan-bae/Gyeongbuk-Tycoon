using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 종료 확인 창.
/// 좌상단 종료 버튼이 창을 열고, 창 안의 '게임 종료' / '취소' 버튼이 각각 종료와 닫기를 처리한다.
/// </summary>
public class QuitConfirmUI : MonoBehaviour
{
    [Header("창")]
    [Tooltip("어두운 배경까지 포함한 확인 창 루트. 평소에는 꺼져 있다.")]
    [SerializeField] private GameObject panelRoot;

    [Header("버튼")]
    [Tooltip("좌상단 HUD 의 종료 버튼")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Tooltip("어두운 배경을 눌러도 취소되게 한다.")]
    [SerializeField] private Button dimBackgroundButton;

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (confirmButton != null) confirmButton.onClick.AddListener(QuitGame);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
        if (dimBackgroundButton != null) dimBackgroundButton.onClick.AddListener(Close);

        // 씬에 켜진 채로 저장돼도 시작할 때는 항상 닫힌 상태로 맞춘다.
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// 종료 직전에 저장한다. OnApplicationQuit 은 에디터 플레이 종료나
    /// 모바일 강제 종료에서 호출이 보장되지 않아 여기서 명시적으로 저장한다.
    /// </summary>
    public void QuitGame()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGameData();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
