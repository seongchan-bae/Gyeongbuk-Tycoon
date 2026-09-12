using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 퀴즈 맞추기 게임의 문제 한 개.
///
/// 원래는 모자이크 이미지를 보고 맞추는 실루엣 게임이었으나, 모자이크 없이
/// 원본 사진 또는 글 설명을 보고 4지선다로 장소/유적을 맞추는 방식으로 바뀌었다.
/// 클래스 이름과 필드 이름(correctAnswer, originalImage)은 씬에 직렬화된
/// 기존 데이터와의 호환을 위해 그대로 둔다.
/// </summary>
[System.Serializable]
public class SilhouetteQuizData
{
    [Tooltip("이 문제의 정답 장소/유적 이름 (예: 첨성대)")]
    public string correctAnswer;

    [Tooltip("이미지 퀴즈에 쓸 원본 사진(모자이크 아님). 비워두면 이 장소는 글 퀴즈로만 출제된다.")]
    public Sprite originalImage;

    [TextArea(2, 6)]
    [Tooltip("글 퀴즈 지문. 비워두면 QuizDescriptions 에서 정답 이름으로 찾는다. 둘 다 없으면 이미지 퀴즈로만 출제.")]
    public string description;
}

public class SilueteGameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // 문제 화면 UI. 모두 씬에 미리 만들어 둔 오브젝트를 연결한다.
    // 스크립트는 오브젝트를 새로 만들지 않고, 유형에 맞게 켜고/끄고 텍스트만 채운다.
    // ─────────────────────────────────────────────────────────────
    [Header("문제 화면")]
    [Tooltip("문제 이미지 표시용. 글 퀴즈일 때는 지문 배경으로도 쓰인다.")]
    public Image targetImage;
    [Tooltip("이미지 문제일 때 켜지는 '질문 문구' 텍스트. 문구 내용은 이 오브젝트에 직접 입력.")]
    [SerializeField] private TextMeshProUGUI imagePromptText;
    [Tooltip("글 문제일 때 켜지는 '질문 문구' 텍스트. 문구 내용은 이 오브젝트에 직접 입력.")]
    [SerializeField] private TextMeshProUGUI textPromptText;
    [Tooltip("글 문제의 지문(문제 내용)이 채워지는 텍스트.")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [Tooltip("이미지 문제일 때 상단 질문 문구 (스크립트가 imagePromptText 에 채운다)")]
    [SerializeField] private string imageQuizPrompt = "다음 장소에 해당하는 곳은?";
    [Tooltip("글 문제일 때 상단 질문 문구 (스크립트가 textPromptText 에 채운다)")]
    [SerializeField] private string textQuizPrompt = "다음 설명에 해당하는 곳은?";
    [Tooltip("글 문제에서 targetImage 에 깔 배경 이미지 (칠판). 비워두면 아래 배경색으로 채운다.")]
    [SerializeField] private Sprite textQuizBackgroundSprite;
    [Tooltip("글 문제에서 targetImage 를 채울 배경색. textQuizBackgroundSprite 가 없을 때만 쓰인다.")]
    [SerializeField] private Color textQuizBackgroundColor = new Color(0.98f, 0.96f, 0.90f, 1f);
    [Tooltip("글 문제(칠판)일 때 targetImage 의 크기(width, height). (0,0)이면 이미지 문제와 같은 크기를 쓴다.")]
    [SerializeField] private Vector2 textQuizImageSize = new Vector2(900f, 520f);
    [Tooltip("글 문제(칠판)일 때 칠판 이미지를 프레임에 꽉 채울지 여부. 끄면 비율을 유지한다.")]
    [SerializeField] private bool textQuizImageStretch = true;

    [Header("보기 버튼")]
    public Button[] answerButtons;             // 4지선다 버튼 배열 (4개)
    public TextMeshProUGUI[] answerTexts;      // 보기 텍스트 컴포넌트 (4개)

    [Header("결과 팝업")]
    public GameObject resultPopupUI;
    [Tooltip("결과창에서 정답 장소/유적의 사진이 채워지는 이미지. 씬의 ResultRelicImage 오브젝트를 연결한다.")]
    public Image resultImage;
    [Tooltip("정답/오답 및 보상 안내 텍스트")]
    public TextMeshProUGUI resultMessageText;
    public Button nextQuizButton;
    public Button exitGameButton;

    [Tooltip("사진이 따로 없는 장소의 결과 이미지를 찾을 Resources 경로. 파일 이름은 정답 이름과 같아야 한다.")]
    [SerializeField] private string resultImageResourceFolder = "QuizResultImages";

    [Header("Reward Settings")]
    public int rewardGold = 100;

    [Tooltip("정답을 맞혔을 때 얻는 지식포인트. 하루 상한은 GameManager 가 관리한다.")]
    public int rewardKnowledgePoint = 3;

    [Tooltip("틀렸을 때 잃는 지식포인트. 잃은 만큼 하루 상한도 되돌아가므로 그만큼 다시 벌 수 있다.")]
    public int penaltyKnowledgePoint = 1;

    [Header("Root / Start Screen")]
    [Tooltip("게임 전체를 감싸는 루트 패널 (SilueteGameUI). 나가기 시 이 패널을 끈다.")]
    public GameObject rootPanel;
    [Tooltip("게임에 들어오면 먼저 보여줄 시작 화면. 비워두면 바로 첫 문제를 낸다.")]
    public GameObject startScreen;
    [Tooltip("우측 상단 나가기(X) 버튼. 시작 화면에서는 숨기고 문제가 나오면 보여준다.")]
    public GameObject closeButton;

    [Header("Quiz Data Pool")]
    [Tooltip("사진이 있는 장소들. 각 항목은 이미지 퀴즈 + (지문이 있으면) 글 퀴즈로 출제된다.")]
    public List<SilhouetteQuizData> quizList = new List<SilhouetteQuizData>();
    [Tooltip("오답 보기로만 등장할 수 있는 추가 장소 이름들")]
    public List<string> dummyAnswerPool = new List<string>();

    [Header("Quiz Mode")]
    [Range(0f, 1f)]
    [Tooltip("사진과 지문이 둘 다 있는 문제에서 '글 퀴즈'로 낼 확률")]
    public float textQuizChance = 0.5f;

    /// <summary>런타임에서 다루는 문제 표현. 이미지/지문이 하나로 합쳐진 형태.</summary>
    private class RuntimeQuiz
    {
        public string answer;
        public Sprite image;
        public string description;
        public bool HasImage => image != null;
        public bool HasText => !string.IsNullOrEmpty(description);
    }

    private readonly List<RuntimeQuiz> pool = new List<RuntimeQuiz>();
    private readonly List<string> answerNamePool = new List<string>();
    private readonly Dictionary<string, Sprite> resultImageCache = new Dictionary<string, Sprite>();

    private RuntimeQuiz currentQuiz;
    private bool currentIsTextQuiz;
    private int currentCorrectIndex;
    private bool isInitialized;
    private Color targetImageDefaultColor = Color.white;
    private Vector2 targetImageDefaultSize = new Vector2(900f, 400f);

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

    /// <summary>문제를 내기 전에 시작 화면을 띄운다. 연결돼 있지 않으면 바로 시작.</summary>
    private void ShowStartScreen()
    {
        if (startScreen == null)
        {
            NextQuiz();
            return;
        }

        if (resultPopupUI != null) resultPopupUI.SetActive(false);
        if (closeButton != null) closeButton.SetActive(false);
        startScreen.SetActive(true);
    }

    private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (targetImage != null)
        {
            targetImageDefaultColor = targetImage.color;
            targetImageDefaultSize = targetImage.rectTransform.sizeDelta;
        }

        BuildPool();

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

        WarnIfUnassigned();
    }

    private void WarnIfUnassigned()
    {
        if (imagePromptText == null) Debug.LogWarning("[SilueteGameManager] imagePromptText 가 연결되지 않았습니다.");
        if (textPromptText == null) Debug.LogWarning("[SilueteGameManager] textPromptText 가 연결되지 않았습니다.");
        if (descriptionText == null) Debug.LogWarning("[SilueteGameManager] descriptionText 가 연결되지 않았습니다.");
        if (resultImage == null) Debug.LogWarning("[SilueteGameManager] resultImage 가 연결되지 않았습니다.");
    }

    /// <summary>
    /// 씬 데이터(quizList) + QuizDescriptions 를 합쳐 출제 가능한 문제 풀과 오답 후보 풀을 만든다.
    /// </summary>
    private void BuildPool()
    {
        pool.Clear();
        answerNamePool.Clear();
        var seen = new HashSet<string>();

        if (quizList != null)
        {
            foreach (var q in quizList)
            {
                if (q == null || string.IsNullOrEmpty(q.correctAnswer)) continue;

                string desc = !string.IsNullOrEmpty(q.description)
                    ? q.description
                    : QuizDescriptions.Get(q.correctAnswer);

                var rq = new RuntimeQuiz { answer = q.correctAnswer, image = q.originalImage, description = desc };
                if (!rq.HasImage && !rq.HasText) continue;   // 낼 방법이 없는 문제는 제외

                pool.Add(rq);
                if (seen.Add(q.correctAnswer)) answerNamePool.Add(q.correctAnswer);
            }
        }

        // 사진은 없지만 지문이 있는 장소 → 글 전용 문제로 추가
        foreach (var kv in QuizDescriptions.All)
        {
            if (!seen.Add(kv.Key)) continue;
            pool.Add(new RuntimeQuiz { answer = kv.Key, image = null, description = kv.Value });
            answerNamePool.Add(kv.Key);
        }

        // 더미 풀은 오답 후보로만 합친다
        if (dummyAnswerPool != null)
        {
            foreach (var name in dummyAnswerPool)
            {
                if (!string.IsNullOrEmpty(name) && seen.Add(name)) answerNamePool.Add(name);
            }
        }

        if (pool.Count == 0)
            Debug.LogWarning("[SilueteGameManager] 출제 가능한 퀴즈가 하나도 없습니다. quizList / QuizDescriptions 를 확인하세요.");
    }

    /// <summary>무작위 문제를 뽑아 4지선다 세팅.</summary>
    public void NextQuiz()
    {
        if (resultPopupUI != null) resultPopupUI.SetActive(false);
        if (closeButton != null) closeButton.SetActive(true);

        if (pool.Count == 0)
        {
            Debug.LogWarning("[SilueteGameManager] 등록된 퀴즈 데이터가 없습니다.");
            return;
        }

        currentQuiz = pool[Random.Range(0, pool.Count)];

        // 이미지 / 글 모드 결정
        if (currentQuiz.HasImage && currentQuiz.HasText)
            currentIsTextQuiz = Random.value < textQuizChance;
        else
            currentIsTextQuiz = !currentQuiz.HasImage;

        ShowQuestion();

        // 정답 1개 + 오답 3개
        List<string> options = GenerateOptions(currentQuiz.answer);

        int maxLoop = Mathf.Min(options.Count, answerButtons != null ? answerButtons.Length : 0);
        maxLoop = Mathf.Min(maxLoop, answerTexts != null ? answerTexts.Length : 0);

        for (int i = 0; i < maxLoop; i++)
        {
            if (answerTexts[i] != null) answerTexts[i].text = options[i];
            if (options[i] == currentQuiz.answer) currentCorrectIndex = i;
        }
    }

    /// <summary>
    /// 현재 문제를 이미지/글 모드에 맞게, 미리 만들어 둔 텍스트 오브젝트에 채워 넣는다.
    /// 오브젝트를 새로 만들지 않는다.
    /// </summary>
    private void ShowQuestion()
    {
        // 질문 문구: 유형에 맞는 것만 켜고, 기본 문구를 채운다.
        if (imagePromptText != null)
        {
            imagePromptText.text = imageQuizPrompt;
            imagePromptText.gameObject.SetActive(!currentIsTextQuiz);
        }
        if (textPromptText != null)
        {
            textPromptText.text = textQuizPrompt;
            textPromptText.gameObject.SetActive(currentIsTextQuiz);
        }

        // 글 지문 본문
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(currentIsTextQuiz);
            if (currentIsTextQuiz) descriptionText.text = currentQuiz.description;
        }

        // 이미지 / 배경
        if (targetImage != null)
        {
            targetImage.enabled = true;
            if (currentIsTextQuiz)
            {
                // 글 문제일 때는 칠판 크기를 별도로 지정한다. (0,0)이면 이미지 문제와 동일.
                targetImage.rectTransform.sizeDelta =
                    textQuizImageSize == Vector2.zero ? targetImageDefaultSize : textQuizImageSize;

                if (textQuizBackgroundSprite != null)
                {
                    targetImage.sprite = textQuizBackgroundSprite;
                    targetImage.preserveAspect = !textQuizImageStretch;
                    targetImage.color = Color.white;
                }
                else
                {
                    targetImage.sprite = null;
                    targetImage.preserveAspect = false;
                    targetImage.color = textQuizBackgroundColor;
                }
            }
            else
            {
                targetImage.rectTransform.sizeDelta = targetImageDefaultSize;
                targetImage.sprite = currentQuiz.image;
                targetImage.preserveAspect = true;
                targetImage.color = targetImageDefaultColor;
            }
        }
    }

    /// <summary>정답 1개와 오답 후보 풀에서 3개를 뽑아 섞은 4지선다 리스트를 만든다.</summary>
    private List<string> GenerateOptions(string correctAnswer)
    {
        List<string> options = new List<string> { correctAnswer };

        List<string> tempPool = new List<string>(answerNamePool);
        tempPool.Remove(correctAnswer);

        while (options.Count < 4 && tempPool.Count > 0)
        {
            int r = Random.Range(0, tempPool.Count);
            options.Add(tempPool[r]);
            tempPool.RemoveAt(r);
        }

        for (int i = 0; i < options.Count; i++)
        {
            int r = Random.Range(i, options.Count);
            (options[i], options[r]) = (options[r], options[i]);
        }

        return options;
    }

    /// <summary>
    /// 정답 유적의 결과 이미지를 구한다.
    /// 1) 문제에 원본 사진이 있으면 그 사진을 그대로 쓴다.
    /// 2) 없으면 Resources/{resultImageResourceFolder}/{정답 이름} 에서 찾는다.
    ///    (tourapi 사진 갤러리 링크나 인터넷에서 받아 넣어 둔 이미지)
    /// 3) 둘 다 없으면 null (결과창에서 이미지 숨김).
    /// </summary>
    private Sprite ResolveResultImage(RuntimeQuiz quiz)
    {
        if (quiz == null) return null;
        if (quiz.image != null) return quiz.image;
        if (string.IsNullOrEmpty(quiz.answer)) return null;

        if (resultImageCache.TryGetValue(quiz.answer, out Sprite cached)) return cached;

        string path = string.IsNullOrEmpty(resultImageResourceFolder)
            ? quiz.answer
            : $"{resultImageResourceFolder}/{quiz.answer}";
        Sprite loaded = Resources.Load<Sprite>(path);
        if (loaded == null)
            Debug.LogWarning($"[SilueteGameManager] 결과 이미지를 찾지 못했습니다: Resources/{path}");
        resultImageCache[quiz.answer] = loaded;
        return loaded;
    }

    /// <summary>사용자가 4개의 보기 중 하나를 클릭했을 때.</summary>
    private void OnSelectAnswer(int selectedIndex)
    {
        bool isCorrect = selectedIndex == currentCorrectIndex;

        if (resultPopupUI != null) resultPopupUI.SetActive(true);
        if (closeButton != null) closeButton.SetActive(false);

        string answerName = currentQuiz != null ? currentQuiz.answer : string.Empty;

        // 정답 장소/유적의 사진 — 미리 만들어 둔 ResultRelicImage 오브젝트에 채운다.
        // 사진이 있는 문제면 그 사진을, 없으면 Resources 에서 정답 이름으로 찾는다. 둘 다 없으면 숨긴다.
        if (resultImage != null)
        {
            Sprite relicSprite = ResolveResultImage(currentQuiz);
            resultImage.sprite = relicSprite;
            resultImage.preserveAspect = true;
            resultImage.gameObject.SetActive(relicSprite != null);
        }

        if (isCorrect)
        {
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
            // 틀리면 지식포인트를 깎는다. 보유량이 0이면 깎이지 않으므로 실제로 잃은 양을 받아서 보여준다.
            long lostKnowledge = GameManager.DeductKnowledgePoint(penaltyKnowledgePoint);

            if (resultMessageText != null)
            {
                string text = $"<b><color=#FF0000>오답입니다!</color></b>\n정답은 <b>[{answerName}]</b> 입니다.";
                if (lostKnowledge > 0)
                {
                    text += $"\n<color=#FF8080>지식 포인트 {lostKnowledge}을 잃었습니다.</color>";
                }
                else if (penaltyKnowledgePoint > 0)
                {
                    text += "\n<size=80%>(지식 포인트가 없어 더 깎이지 않았습니다)</size>";
                }
                resultMessageText.text = text;
            }
        }
    }

    public void ExitGame()
    {
        if (resultPopupUI != null) resultPopupUI.SetActive(false);

        GameObject root = rootPanel != null ? rootPanel : gameObject;
        root.SetActive(false);

        if (MiniGameHubUI.Instance != null)
        {
            MiniGameHubUI.Instance.ReturnToHub();
        }
    }
}
