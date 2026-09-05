using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class SilhouetteQuizData
{
    [Tooltip("모자이크/실루엣 처리된 문제 이미지 (예: mosaic_가실성당.jpg)")]
    public Sprite quizImage;
    [Tooltip("짝을 이루는 원본 이미지 (예: 가실성당.jpg)")]
    public Sprite originalImage;
    [Tooltip("이 이미지의 정답 단어 (예: 가실성당)")]
    public string correctAnswer;
}

public class SilueteGameManager : MonoBehaviour
{
    [Header("UI Components")]
    public Image targetImage;                  // TargetIamge (퀴즈용 왜곡 이미지)
    public Button[] answerButtons;             // AnswerText 1~4가 붙어있는 버튼 배열 (4개)
    public TextMeshProUGUI[] answerTexts;      // AnswerText1 ~ AnswerText4 텍스트 컴포넌트 (4개)

    [Header("Unified Result Popup")]
    public GameObject resultPopupUI;           // 통일된 결과 팝업 GameObject
    public Image resultImage;                  // 팝업 내부의 원본 사진 표시용 Image 컴포넌트
    public TextMeshProUGUI resultMessageText;  // 팝업 내부의 안내 텍스트 (정답/오답 및 보상 안내)
    public Button nextQuizButton;              // 다음 문제 버튼
    public Button exitGameButton;              // 나가기 버튼

    [Header("Reward Settings (Inspector)")]
    public int rewardGold = 100;               // 정답 시 제공할 n 골드
    public int rewardKnowledgePoint = 50;      // 정답 시 제공할 m 지식 포인트

    [Header("Root Panel")]
    [Tooltip("실루엣 게임 전체를 감싸는 루트 패널 (SilueteGameUI). 나가기 시 이 패널을 끈다.")]
    public GameObject rootPanel;

    [Header("Start Screen")]
    [Tooltip("게임에 들어오면 먼저 보여줄 시작 화면. 비워두면 예전처럼 바로 첫 문제를 낸다.")]
    public GameObject startScreen;
    [Tooltip("우측 상단 나가기(X) 버튼. 시작 화면에서는 숨기고 문제가 나오면 보여준다.")]
    public GameObject closeButton;

    [Header("Quiz Data Pool")]
    public List<SilhouetteQuizData> quizList = new List<SilhouetteQuizData>(); // 전체 문제 데이터셋
    [Tooltip("오답지에 나올 수 있는 전체 단어 후보 리스트")]
    public List<string> dummyAnswerPool = new List<string>();                  

    private SilhouetteQuizData currentQuiz;
    private int currentCorrectIndex;
    private bool isInitialized;

    private void Start()
    {
        Initialize();
        ShowStartScreen();
    }

    private void OnEnable()
    {
        // 허브에서 다시 들어왔을 때도 시작 화면부터 보여준다.
        // 첫 활성화 때는 OnEnable 이 Start 보다 먼저 돌기 때문에 Start 쪽에 맡긴다.
        if (isInitialized) ShowStartScreen();
    }

    /// <summary>시작 화면의 [게임 시작] 버튼에서 호출.</summary>
    public void StartGame()
    {
        if (startScreen != null) startScreen.SetActive(false);
        if (closeButton != null) closeButton.SetActive(true);
        NextQuiz();
    }

    /// <summary>문제를 내기 전에 시작 화면을 띄운다. 연결돼 있지 않으면 예전처럼 바로 시작.</summary>
    private void ShowStartScreen()
    {
        if (startScreen == null)
        {
            NextQuiz();
            return;
        }

        if (resultPopupUI != null) resultPopupUI.SetActive(false);
        // 제목 + [게임 시작]만 보이는 화면에는 나가기 버튼을 두지 않는다.
        if (closeButton != null) closeButton.SetActive(false);
        startScreen.SetActive(true);
    }

    private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        // 팝업 버튼 이벤트 1회 연결
        if (nextQuizButton != null) nextQuizButton.onClick.AddListener(NextQuiz);
        if (exitGameButton != null) exitGameButton.onClick.AddListener(ExitGame);

        // 보기 버튼 클릭 이벤트 연동
        if (answerButtons != null)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null) continue;
                int index = i;
                answerButtons[i].onClick.AddListener(() => OnSelectAnswer(index));
            }
        }
    }

    /// <summary>
    /// 무작위 문제를 뽑아 4지선다 세팅
    /// </summary>
    public void NextQuiz()
    {
        // 팝업 비활성화
        if (resultPopupUI != null) resultPopupUI.SetActive(false);
        if (closeButton != null) closeButton.SetActive(true);

        if (quizList == null || quizList.Count == 0)
        {
            Debug.LogWarning("[SilueteGameManager] 등록된 퀴즈 데이터가 없습니다.");
            return;
        }

        // 1. n개 이미지 중 무작위 1개 추출
        int randomIndex = Random.Range(0, quizList.Count);
        currentQuiz = quizList[randomIndex];

        // 2. 퀴즈 화면에 왜곡된 이미지 할당
        if (targetImage != null)
        {
            targetImage.sprite = currentQuiz.quizImage;
        }

        // 3. 정답 1개 + 오답 3개로 4지선다 목록 만들기
        List<string> options = GenerateOptions(currentQuiz.correctAnswer);

        // 4. UI 버튼에 텍스트 할당 (인덱스 범주 안전 검사)
        int maxLoop = Mathf.Min(options.Count, answerButtons != null ? answerButtons.Length : 0);
        maxLoop = Mathf.Min(maxLoop, answerTexts != null ? answerTexts.Length : 0);

        for (int i = 0; i < maxLoop; i++)
        {
            if (answerTexts[i] != null)
            {
                answerTexts[i].text = options[i];
            }

            // 정답 인덱스 기록
            if (options[i] == currentQuiz.correctAnswer)
            {
                currentCorrectIndex = i;
            }
        }
    }

    /// <summary>
    /// 정답 단어 1개와 더미 단어 풀에서 3개를 뽑아 섞은 리스트 생성
    /// </summary>
    private List<string> GenerateOptions(string correctAnswer)
    {
        List<string> options = new List<string> { correctAnswer };

        List<string> tempPool = new List<string>(dummyAnswerPool);
        tempPool.Remove(correctAnswer);

        while (options.Count < 4 && tempPool.Count > 0)
        {
            int randIdx = Random.Range(0, tempPool.Count);
            options.Add(tempPool[randIdx]);
            tempPool.RemoveAt(randIdx);
        }

        for (int i = 0; i < options.Count; i++)
        {
            string temp = options[i];
            int randIdx = Random.Range(i, options.Count);
            options[i] = options[randIdx];
            options[randIdx] = temp;
        }

        return options;
    }

    /// <summary>
    /// 사용자가 4개의 보기 중 하나를 클릭했을 때
    /// </summary>
    private void OnSelectAnswer(int selectedIndex)
    {
        // 🎯 결과 팝업에 현재 문제와 짝을 이루는 원본 이미지 띄우기
        if (resultImage != null && currentQuiz != null)
        {
            resultImage.sprite = currentQuiz.originalImage;
        }

        if (resultPopupUI != null) resultPopupUI.SetActive(true);
        // 결과 화면에서는 우측 상단 나가기 대신 패널 안의 버튼을 쓴다.
        if (closeButton != null) closeButton.SetActive(false);

        if (selectedIndex == currentCorrectIndex)
        {
            // [정답 처리]
            long grantedKnowledge = GameManager.GrantReward(rewardGold, rewardKnowledgePoint);

            if (resultMessageText != null)
            {
                string text = $"<b><color=#00FF00>정답입니다!</color></b>\n보상: <color=#FFD700>{rewardGold} 골드</color> / <color=#00FFFF>{grantedKnowledge} 지식 포인트</color>를 획득했습니다.";
                if (grantedKnowledge < rewardKnowledgePoint) text += "\n<size=80%>(오늘 지식포인트 한도를 모두 채웠습니다)</size>";
                resultMessageText.text = text;
            }
        }
        else
        {
            // [오답 처리]
            if (resultMessageText != null)
            {
                resultMessageText.text = $"<b><color=#FF0000>오답입니다!</color></b>\n정답은 <b>[{currentQuiz.correctAnswer}]</b> 입니다.\n다른 문제에 도전하시겠습니까?";
            }
        }
    }

    public void ExitGame()
    {
        if (resultPopupUI != null) resultPopupUI.SetActive(false);

        // 매니저 오브젝트만이 아니라 게임 UI 전체(루트 패널)를 닫아야 화면에서 사라진다.
        GameObject root = rootPanel != null ? rootPanel : gameObject;
        root.SetActive(false);

        if (MiniGameHubUI.Instance != null)
        {
            MiniGameHubUI.Instance.ReturnToHub();
        }
    }
}