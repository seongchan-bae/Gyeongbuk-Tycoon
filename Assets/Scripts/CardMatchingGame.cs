using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CardData
{
    public int id;             // 유적지 고유 ID
    public string siteName;    // 유적지 이름
    public Sprite siteImage;   // 유적지 사진
}

public class CardMatchingGame : MonoBehaviour
{
    [Header("=== Game Data ===")]
    [Tooltip("경북 유적지 데이터 목록 (비워둘 경우 테스트용 데이터가 자동 채워집니다)")]
    [SerializeField] private List<CardData> cardDataList = new List<CardData>();

    [Header("=== Game Settings ===")]
    [Tooltip("한 판에 배치할 짝 개수 (15쌍 = 총 30장 카드, 5x6 배치)")]
    [SerializeField] private int stagePairCount = 15;
    [Tooltip("제한 시간 (초)")]
    [SerializeField] private float timeLimit = 200f;
    [Tooltip("최대 기회 (틀릴 수 있는 횟수)")]
    [SerializeField] private int maxChances = 50;
    [Tooltip("게임 성공 시 지급할 지식 포인트")]
    [SerializeField] private long rewardKnowledgePoint = 50L;
    [Tooltip("게임 성공 시 지급할 골드")]
    [SerializeField] private long rewardGold = 100L;

    [Header("=== Direct Hierarchy References ===")]
    [SerializeField] private GameObject startButton;      // StartButton
    [SerializeField] private GameObject cardGrid;        // CardGrid (카드가 배치되는 패널)
    [SerializeField] private TextMeshProUGUI timerText;   // GameStatus 내 제한시간 텍스트
    [SerializeField] private TextMeshProUGUI chanceText;  // GameStatus 내 남은기회 텍스트
    [SerializeField] private GameObject gameOverPanel;   // 결과 팝업 패널
    [SerializeField] private TextMeshProUGUI resultText;  // GameOverPanel 내 결과 텍스트

    [Header("=== Prefab ===")]
    [SerializeField] private GameObject cardPrefab;       // Project 창의 CardPrefab

    [Header("=== Fallback Image ===")]
    [Tooltip("아직 사진이 없는 유적지 카드에 임시로 쓸 단색 이미지")]
    [SerializeField] private Sprite placeholderImage;

    // 내부 상태 변수
    private List<CardUI> spawnedCards = new List<CardUI>();
    private CardUI firstSelectedCard = null;
    private CardUI secondSelectedCard = null;

    private float currentTimer;
    private int remainingChances;
    private int matchedPairCount;
    private bool isGameActive = false;
    private bool isCheckingMatch = false;
    private bool hasOpened = false;

    public bool IsBusy => isCheckingMatch;

    // 예전에는 Awake()에서 gameObject.SetActive(false)로 스스로를 숨겼는데,
    // 패널이 씬에서 비활성 상태로 시작하기 때문에 OpenGamePanel()이 SetActive(true)를
    // 하는 순간 그제서야 Awake가 돌면서 곧바로 다시 꺼버리는 문제가 있었다.
    // 패널의 표시 여부는 MiniGameHubUI가 관리하므로 자기 비활성화는 제거한다.

    public void OpenGamePanel()
    {
        gameObject.SetActive(true);
        hasOpened = true;
        isGameActive = false;

        if (startButton != null) startButton.SetActive(true);
        if (cardGrid != null) cardGrid.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        ClearCards();

        ResetStatusUI();
    }

    private void ResetStatusUI()
    {
        // 원하는 초기 기본 문구로 적어주시면 됩니다.
        if (timerText != null) timerText.text = "남은 시간";
        if (chanceText != null) chanceText.text = "남은 기회";

    }

    public void CloseGamePanel()
    {
        bool wasOpen = hasOpened;

        hasOpened = false;
        isGameActive = false;
        StopAllCoroutines();
        ClearCards();
        gameObject.SetActive(false);

        // 게임 안의 [나가기] 버튼으로 닫힌 경우 미니게임 선택 패널로 돌아간다.
        if (wasOpen && MiniGameHubUI.Instance != null)
        {
            MiniGameHubUI.Instance.ReturnToHub();
        }
    }

    public void OnClickStartGame()
    {
        if (startButton != null) startButton.SetActive(false);
        if (cardGrid != null) cardGrid.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        InitializeGame();
    }

    private void InitializeGame()
    {
        ClearCards();

        firstSelectedCard = null;
        secondSelectedCard = null;
        matchedPairCount = 0;
        currentTimer = timeLimit;
        remainingChances = maxChances;
        isCheckingMatch = false;

        UpdateStatusUI();

        // ⭐ 이미지가 없고 데이터가 비어있다면, 테스트용 유적지 15개 텍스트 자동 생성
        List<CardData> activeDataList = GetActiveCardDataList();

        List<CardData> selectedData = GetRandomCardData(activeDataList, stagePairCount);

        List<System.Action<Transform>> cardSpawnActions = new List<System.Action<Transform>>();
        foreach (var data in selectedData)
        {
            CardData currentData = data;
            cardSpawnActions.Add((parent) => SpawnCard(currentData, parent));
            cardSpawnActions.Add((parent) => SpawnCard(currentData, parent));
        }

        ShuffleList(cardSpawnActions);

        foreach (var spawnAction in cardSpawnActions)
        {
            spawnAction(cardGrid.transform);
        }

        isGameActive = true;
    }

    // ⭐ 실제 유적지 목록은 CardMatchingGamePanel 프리팹의 cardDataList에 채워져 있다.
    //    (실루엣 게임 + 퍼즐 게임 유적지의 합집합 28개, 여기서 stagePairCount 만큼 무작위로 뽑는다)
    //    아래 배열은 목록이 stagePairCount보다 적을 때만 쓰이는 비상용 텍스트 데이터.
    private List<CardData> GetActiveCardDataList()
    {
        if (cardDataList != null && cardDataList.Count >= stagePairCount)
        {
            return cardDataList;
        }

        // 실루엣 및 퍼즐 게임과 통일된 15개의 경북 랜드마크 데이터
        string[] sampleNames = new string[]
        {
            "첨성대", "불국사", "석굴암", "동궁과 월지", "하회마을",
            "도산서원", "호미곶", "문경새재", "부석사", "월정교",
            "대릉원", "가실성당", "주왕산", "영일대 해상누각", "포석정"
        };

        List<CardData> testList = new List<CardData>();
        for (int i = 0; i < sampleNames.Length; i++)
        {
            testList.Add(new CardData
            {
                id = i + 1,
                siteName = sampleNames[i],
                siteImage = null // 이미지 없이 텍스트로만 테스트
            });
        }

        return testList;
    }

    private void ClearCards()
    {
        if (cardGrid != null)
        {
            foreach (Transform child in cardGrid.transform)
            {
                Destroy(child.gameObject);
            }
        }
        spawnedCards.Clear();
    }

    private void SpawnCard(CardData data, Transform parent)
    {
        GameObject go = Instantiate(cardPrefab, parent, false);
        CardUI card = go.GetComponent<CardUI>();

        if (card != null)
        {
            Sprite sprite = data.siteImage != null ? data.siteImage : placeholderImage;
            card.SetupCard(data.id, sprite, data.siteName, this);
            spawnedCards.Add(card);
        }
    }

    private void Update()
    {
        if (!isGameActive) return;

        currentTimer -= Time.deltaTime;
        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            GameOver(false, "시간 초과!\n다시 도전해 보세요.");
        }

        UpdateStatusUI();
    }

    private void UpdateStatusUI()
    {
        if (timerText != null) timerText.text = $"남은 시간: {Mathf.CeilToInt(currentTimer)}초";
        if (chanceText != null) chanceText.text = $"남은 기회: {remainingChances}회";
    }

    public void OnCardSelected(CardUI selectedCard)
    {
        if (!isGameActive || isCheckingMatch) return;

        if (firstSelectedCard == null)
        {
            firstSelectedCard = selectedCard;
        }
        else if (secondSelectedCard == null && selectedCard != firstSelectedCard)
        {
            secondSelectedCard = selectedCard;
            StartCoroutine(CheckMatchCoroutine());
        }
    }

    private IEnumerator CheckMatchCoroutine()
    {
        isCheckingMatch = true;

        yield return new WaitForSeconds(0.5f);

        if (firstSelectedCard.CardID == secondSelectedCard.CardID)
        {
            firstSelectedCard.SetMatched();
            secondSelectedCard.SetMatched();
            matchedPairCount++;

            if (matchedPairCount >= stagePairCount)
            {
                GameOver(true, $"성공!\n모든 유적지 짝을 맞추셨습니다!\n보상: {rewardGold} 골드 / {rewardKnowledgePoint} 지식 포인트");
            }
        }
        else
        {
            firstSelectedCard.FlipToBack();
            secondSelectedCard.FlipToBack();

            remainingChances--;
            if (remainingChances <= 0)
            {
                GameOver(false, "기회를 모두 소진하였습니다!\n다시 도전해 보세요.");
            }
        }

        firstSelectedCard = null;
        secondSelectedCard = null;
        isCheckingMatch = false;
    }

    private void GameOver(bool isSuccess, string message)
    {
        isGameActive = false;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (resultText != null) resultText.text = message;

        if (isSuccess)
        {
            GameManager.GrantReward(rewardGold, rewardKnowledgePoint);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private List<CardData> GetRandomCardData(List<CardData> sourceList, int count)
    {
        List<CardData> shuffled = new List<CardData>(sourceList);
        ShuffleList(shuffled);
        return shuffled.GetRange(0, Mathf.Min(count, shuffled.Count));
    }
}