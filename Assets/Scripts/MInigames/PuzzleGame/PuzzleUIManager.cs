using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;



public class PuzzleUIManager : MonoBehaviour
{
    [Header("Dependencies")]
    public PuzzleManager puzzleManager;
    public RuntimeJigsawGenerator puzzleGenerator;

    [Header("Board Background Settings")]
    [Tooltip("퍼즐 판 뒤에 깔리는 FrameImage (RawImage 또는 Image)")]
    public RawImage boardFrameImage; // 👈 추가된 부분: RawImage 제어용 변수

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

    private float currentTimer;
    private bool isGameActive = false;
    private Sprite currentSelectedSprite; 

    private void OnEnable()
    {
        ShowSelectionPanel();
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
            foreach (Transform child in selectionContentParent)
            {
                DestroyImmediate(child.gameObject);
            }

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
    /// </summary>
    private void OnSelectPuzzle(PuzzleData selectedData)
    {
        currentSelectedSprite = selectedData.puzzleImage;

        // 🎯 1. 퍼즐 틀(FrameImage) 배경 이미지 가변 변경
        if (boardFrameImage != null && currentSelectedSprite != null)
        {
            boardFrameImage.texture = currentSelectedSprite.texture;
        }

        if (selectionPanel != null) selectionPanel.SetActive(false);

        // 🎯 2. 선택된 이미지로 퍼즐 조각 생성 및 게임 시작
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
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void OnTimeOut()
    {
        isGameActive = false;
        if (gameOverPopup != null) gameOverPopup.SetActive(true);
        if (timerText != null) timerText.text = "time over!";
    }

    private void OnPuzzleComplete()
    {
        isGameActive = false;

        if (rewardText != null)
        {
            rewardText.text = $"Puzzle Complete!\nReward: {rewardGold}golds, {rewardKnowledge}Points";
        }

        if (clearPopup != null) clearPopup.SetActive(true);
    }

    public void OnClickRetry()
    {
        ResetAndInitializeUI();
    }

    public void OnClickExitToMain()
    {
        isGameActive = false;

        if (clearPopup != null) clearPopup.SetActive(false);
        if (clearUI != null) clearUI.SetActive(false);

        SceneManager.LoadScene("SampleScene");
    }
}