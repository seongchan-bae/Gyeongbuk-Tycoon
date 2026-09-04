using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PuzzleUIManager : MonoBehaviour
{
    [Header("Dependencies")]
    public PuzzleManager puzzleManager;
    public RuntimeJigsawGenerator puzzleGenerator;

    [Header("Board Background Settings")]
    [Tooltip("퍼즐 판 뒤에 깔리는 FrameImage (RawImage 또는 Image)")]
    public RawImage boardFrameImage; 

    [Header("Puzzle Selection Settings")]
    public GameObject selectionPanel; 
    public Transform selectionContentParent; 
    public GameObject puzzleItemPrefab; 
    public List<PuzzleData> puzzleList = new List<PuzzleData>(); 

    [Header("1. Timer Settings")]
    public float timeLimit = 180f; 
    public TextMeshProUGUI timerText; 
    public GameObject gameOverPopup;
    public Button retryButton;
    public Button exitToMainButton;

    [Header("2. Clear Popup Settings")]
    public GameObject clearPopup;
    public TextMeshProUGUI rewardText;
    public Button clearExitButton;
    public int rewardGold = 100;
    public int rewardKnowledge = 50;

    [Header("UI Panels")]
    public GameObject clearUI;
    [Tooltip("퍼즐 게임 전체를 감싸는 루트 패널 (PuzzleUI). 나가기 시 이 패널을 끈다.")]
    public GameObject rootPanel;

    private float currentTimer;
    private bool isGameActive = false;
    private Sprite currentSelectedSprite; 

    private void OnEnable()
    {
        StartRandomPuzzle();
    }

    /// <summary>
    /// 그림 선택 화면을 거치지 않고 puzzleList에서 무작위로 하나를 골라 바로 시작한다.
    /// </summary>
    public void StartRandomPuzzle()
    {
        isGameActive = false;
        if (selectionPanel != null) selectionPanel.SetActive(false);

        if (puzzleList == null || puzzleList.Count == 0)
        {
            Debug.LogWarning("[PuzzleUIManager] 등록된 퍼즐 이미지가 없습니다.");
            return;
        }

        PuzzleData picked = puzzleList[Random.Range(0, puzzleList.Count)];
        ApplyPuzzle(picked);
    }

    private void Start()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnClickRetry);
        }
        if (exitToMainButton != null)
        {
            exitToMainButton.onClick.RemoveAllListeners();
            exitToMainButton.onClick.AddListener(OnClickExitToMain);
        }
        if (clearExitButton != null)
        {
            clearExitButton.onClick.RemoveAllListeners();
            clearExitButton.onClick.AddListener(OnClickExitToMain);
        }
    }

    private void OnDisable()
    {
        isGameActive = false;
    }

    public void ShowSelectionPanel()
    {
        isGameActive = false;
        if (selectionPanel != null) selectionPanel.SetActive(true);

        if (selectionContentParent != null)
        {
            // 🎯 [핵심 수정] 기존 목록을 안전하게 분리 후 삭제 (중복 생성 방지)
            int childCount = selectionContentParent.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = selectionContentParent.GetChild(i);
                child.SetParent(null); // 즉시 부모 관계를 끊어 중복 순회 및 UI 재배치 문제 방지
                Destroy(child.gameObject);
            }

            // 새로운 프리팹 목록 생성
            foreach (var data in puzzleList)
            {
                if (puzzleItemPrefab != null)
                {
                    GameObject itemGO = Instantiate(puzzleItemPrefab, selectionContentParent);
                    PuzzleItemUI itemScript = itemGO.GetComponent<PuzzleItemUI>();
                    if (itemScript != null)
                    {
                        itemScript.Setup(data, OnSelectPuzzle);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 사용자가 ScrollView 목록에서 그림을 클릭했을 때 호출됨
    /// (현재 게임은 무작위 시작이라 쓰이지 않지만, 선택 방식으로 되돌릴 때를 위해 남겨둔다)
    /// </summary>
    private void OnSelectPuzzle(PuzzleData selectedData)
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        ApplyPuzzle(selectedData);
    }

    /// <summary>
    /// 고른 그림을 퍼즐 틀에 반영하고 게임을 시작한다.
    /// </summary>
    private void ApplyPuzzle(PuzzleData data)
    {
        currentSelectedSprite = data != null ? data.puzzleImage : null;

        // 퍼즐 틀(FrameImage) 배경 이미지 교체
        if (boardFrameImage != null && currentSelectedSprite != null)
        {
            boardFrameImage.texture = currentSelectedSprite.texture;
        }

        ResetAndInitializeUI();
    }

    public void ResetAndInitializeUI()
    {
        if (gameOverPopup != null) gameOverPopup.SetActive(false);
        if (clearPopup != null) clearPopup.SetActive(false);

        currentTimer = timeLimit;
        isGameActive = true;
        UpdateTimerUI();

        // 선택된 이미지가 있다면 전달하여 퍼즐 조각 생성
        if (puzzleGenerator != null && currentSelectedSprite != null)
        {
            puzzleGenerator.GeneratePuzzleWithImage(currentSelectedSprite);
        }
    }

    private void Update()
    {
        if (!isGameActive) return;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T))
        {
            OnPuzzleComplete();
            return;
        }
#endif

        currentTimer -= Time.deltaTime;
        UpdateTimerUI();

        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            OnTimeOut();
            return;
        }

        if (puzzleManager != null && puzzleManager.CheckIsComplete())
        {
            OnPuzzleComplete();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(currentTimer / 60f);
        int seconds = Mathf.FloorToInt(currentTimer % 60f);
        // 카드맞추기 게임과 동일하게 "남은 시간: " 접두사를 붙인다.
        timerText.text = string.Format("남은 시간: {0:00}:{1:00}", minutes, seconds);
    }

    private void OnTimeOut()
    {
        isGameActive = false;
        if (gameOverPopup != null) gameOverPopup.SetActive(true);
        if (timerText != null) timerText.text = "남은 시간: 시간 초과!";
    }

    private void OnPuzzleComplete()
    {
        isGameActive = false;

        GameManager.GrantReward(rewardGold, rewardKnowledge);

        if (rewardText != null)
        {
            rewardText.text = $"퍼즐 완성!\n보상: {rewardGold} 골드 / {rewardKnowledge} 지식 포인트";
        }

        if (clearPopup != null) clearPopup.SetActive(true);
    }

    public void OnClickRetry()
    {
        // 진입할 때마다 그림을 새로 뽑으므로, 재도전도 새 그림으로 시작한다.
        StartRandomPuzzle();
    }

    public void OnClickExitToMain()
    {
        isGameActive = false;

        if (gameOverPopup != null) gameOverPopup.SetActive(false);
        if (clearPopup != null) clearPopup.SetActive(false);
        if (clearUI != null) clearUI.SetActive(false);

        // 씬을 다시 로드하지 않고 미니게임 허브(선택 패널)로 되돌아간다.
        GameObject root = rootPanel != null ? rootPanel : gameObject;
        root.SetActive(false);

        if (MiniGameHubUI.Instance != null)
        {
            MiniGameHubUI.Instance.ReturnToHub();
        }
    }
}