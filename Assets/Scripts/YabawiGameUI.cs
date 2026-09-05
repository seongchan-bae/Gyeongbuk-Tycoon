using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class YabawiGameUI : MonoBehaviour
{
    [Header("UI Panels & Buttons")]
    public GameObject yabawiGamePanel;
    public Button startButton;
    [Tooltip("시작 화면에만 보여줄 타이틀 이미지. 시작 버튼과 같이 켜고 끈다.")]
    public GameObject titleImage;
    public GameObject difficultySelectGroup;
    public Button easyBtn, normalBtn, hardBtn;
    public GameObject gameFinishPanel;
    public Button gameCloseButton;
    [Tooltip("결과 패널 안의 [나가기] 버튼")]
    public Button gameExitButton;

    [Header("Game Status UI")]
    public TextMeshProUGUI difficultyText;
    [Tooltip("결과(종료) 패널에 표시할 안내 문구")]
    public TextMeshProUGUI resultText;
    [Tooltip("결과 패널 가운데에 띄울 성공/실패 이미지")]
    public Image resultImage;
    public Sprite successSprite;
    public Sprite failSprite;

    [Header("Reward Settings")]
    [Tooltip("맞출 때마다 지급할 골드")]
    public int rewardGoldPerWin = 50;

    [Header("Game Elements (Dynamic Prefabs)")]
    public RectTransform yabawiGrid;
    public GameObject cupPrefab;
    public GameObject treasurePrefab;

    [Header("Game Settings & Grid Layout")]
    public float cupLiftHeight = 500f;       // 컵 들리는 높이
    public float cupSpacingX = 500f;         // 컵 가로 간격
    public float cupSpacingY = 400f;         // 컵 세로 간격 (2행 배치용)
    public Vector2 treasureOffset = new Vector2(0f, -20f); // 보물 위치 미세조정 (X: 좌우, Y: 위아래)

    [System.Serializable]
    public struct DifficultySetting
    {
        public string diffName;
        public int cupCount;
        public int shuffleCount;
        public float startSpeed; // 처음 섞기 속도 (1회 이동 소요 시간)
        public float endSpeed;   // 최종 도달 속도 (1회 이동 소요 시간)
        public int rewardKnowledgePoint;
    }

    private DifficultySetting[] diffSettings = new DifficultySetting[]
    {
        new DifficultySetting { diffName = "쉬움 (Easy - 3개)", cupCount = 3, shuffleCount = 10, startSpeed = 0.8f, endSpeed = 0.35f, rewardKnowledgePoint = 100 },
        new DifficultySetting { diffName = "중간 (Normal - 4개)", cupCount = 4, shuffleCount = 15, startSpeed = 0.65f, endSpeed = 0.25f, rewardKnowledgePoint = 200 },
        new DifficultySetting { diffName = "어려움 (Hard - 5개)", cupCount = 5, shuffleCount = 20, startSpeed = 0.5f, endSpeed = 0.20f, rewardKnowledgePoint = 300 }
    };

    private DifficultySetting currentSetting;
    private List<RectTransform> activeCups = new List<RectTransform>();
    private RectTransform currentTreasure;

    private int targetIndex;
    private bool isPlaying = false;
    private bool canSelect = false;
    private bool hasOpened = false;


    // 교환용 데이터 구조체
    private struct SwapPair
    {
        public int idx1;
        public int idx2;
        public Vector2 startPos1;
        public Vector2 startPos2;
    }

    void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(ShowDifficultyButtons);
        if (easyBtn != null) easyBtn.onClick.AddListener(() => StartGameWithDifficulty(0));
        if (normalBtn != null) normalBtn.onClick.AddListener(() => StartGameWithDifficulty(1));
        if (hardBtn != null) hardBtn.onClick.AddListener(() => StartGameWithDifficulty(2));
        if (gameCloseButton != null) gameCloseButton.onClick.AddListener(CloseGame);
        if (gameExitButton != null) gameExitButton.onClick.AddListener(CloseGame);

        // Start()는 패널이 처음 활성화될 때 실행되므로, 여기서 CloseGame()을 부르면
        // OpenPanel()로 막 연 패널을 그 프레임에 도로 닫아버린다.
        // 패널은 씬에서 비활성으로 시작하고 표시 여부는 MiniGameHubUI가 관리한다.
        ResetBoardUI();
    }

    public void OpenPanel()
    {
        if (yabawiGamePanel != null) yabawiGamePanel.SetActive(true);
        hasOpened = true;
        ResetBoardUI();
    }

    /// <summary>결과 패널의 [다시 도전]에서 호출. 난이도 선택부터 다시 시작한다.</summary>
    public void RestartGame()
    {
        if (isPlaying) return;

        ResetBoardUI();
        ShowDifficultyButtons();
    }

    public void CloseGame()
    {
        // 컵을 섞는 도중에도 나갈 수 있어야 하므로, 진행 중이면 연출을 끊고 닫는다.
        StopAllCoroutines();
        isPlaying = false;
        canSelect = false;

        bool wasOpen = hasOpened;
        hasOpened = false;

        if (yabawiGamePanel != null) yabawiGamePanel.SetActive(false);
        ClearAllElements();

        // 게임 안의 [나가기] 버튼으로 닫힌 경우 미니게임 선택 패널로 돌아간다.
        if (wasOpen && MiniGameHubUI.Instance != null)
        {
            MiniGameHubUI.Instance.ReturnToHub();
        }
    }

    private void ResetBoardUI()
    {
        if (startButton != null) startButton.gameObject.SetActive(true);
        if (titleImage != null) titleImage.SetActive(true);
        if (difficultySelectGroup != null) difficultySelectGroup.SetActive(false);
        if (gameFinishPanel != null) gameFinishPanel.SetActive(false);
        // 제목 + [게임 시작]만 보이는 화면에는 나가기 버튼도 상태 표시도 두지 않는다.
        if (gameCloseButton != null) gameCloseButton.gameObject.SetActive(false);
        if (difficultyText != null) difficultyText.text = string.Empty;

        ClearAllElements();
    }

    private void ShowDifficultyButtons()
    {
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (titleImage != null) titleImage.SetActive(false);
        if (difficultySelectGroup != null) difficultySelectGroup.SetActive(true);
        if (gameCloseButton != null) gameCloseButton.gameObject.SetActive(true);
    }

    private void StartGameWithDifficulty(int diffLevel)
    {
        if (isPlaying) return;

        currentSetting = diffSettings[diffLevel];
        if (difficultySelectGroup != null) difficultySelectGroup.SetActive(false);
        UpdateStatusText(currentSetting.diffName);

        SetupGameElements();
        StartCoroutine(YabawiRoutine());
    }

    private void ClearAllElements()
    {
        foreach (var cup in activeCups)
        {
            if (cup != null) Destroy(cup.gameObject);
        }
        activeCups.Clear();

        if (currentTreasure != null)
        {
            Destroy(currentTreasure.gameObject);
            currentTreasure = null;
        }
    }

    // ⭐ [핵심 1] 컵 개수에 따른 1행 / 2행 2열 / 2행 3열 그리드 좌표 계산 함수
    private Vector2 GetGridPosition(int index, int totalCups)
    {
        if (totalCups <= 3) // 3개: 1행 3열
        {
            float totalWidth = (totalCups - 1) * cupSpacingX;
            float startX = -totalWidth / 2f;
            return new Vector2(startX + index * cupSpacingX, 0);
        }
        else if (totalCups == 4) // 4개: 2행 2열
        {
            int row = index / 2; // 0: 상단행, 1: 하단행
            int col = index % 2; // 0: 좌, 1: 우

            float x = (col == 0) ? -cupSpacingX / 2f : cupSpacingX / 2f;
            float y = (row == 0) ? cupSpacingY / 2f : -cupSpacingY / 2f;
            return new Vector2(x, y);
        }
        else // 5개: 2행 3열 (상단 3개, 하단 2개 중앙)
        {
            if (index < 3) // 상단행 3개 (0, 1, 2)
            {
                float startX = -cupSpacingX;
                return new Vector2(startX + index * cupSpacingX, cupSpacingY / 2f);
            }
            else // 하단행 2개 (3, 4) - 중앙 정렬
            {
                int col = index - 3;
                float x = (col == 0) ? -cupSpacingX / 2f : cupSpacingX / 2f;
                return new Vector2(x, -cupSpacingY / 2f);
            }
        }
    }

    private void SetupGameElements()
    {
        ClearAllElements();

        // 계산된 그리드 좌표에 맞게 컵 동적 생성
        for (int i = 0; i < currentSetting.cupCount; i++)
        {
            GameObject newCupObj = Instantiate(cupPrefab, yabawiGrid, false);
            RectTransform cupRect = newCupObj.GetComponent<RectTransform>();
            cupRect.anchoredPosition = GetGridPosition(i, currentSetting.cupCount);
            activeCups.Add(cupRect);

            Button cupBtn = newCupObj.GetComponent<Button>();
            if (cupBtn != null) cupBtn.onClick.AddListener(() => OnCupClicked(cupRect));
        }

        targetIndex = Random.Range(0, activeCups.Count);

        GameObject treasureObj = Instantiate(treasurePrefab, yabawiGrid, false);
        currentTreasure = treasureObj.GetComponent<RectTransform>();
        currentTreasure.anchoredPosition = activeCups[targetIndex].anchoredPosition + treasureOffset;

        currentTreasure.SetAsFirstSibling();
        currentTreasure.gameObject.SetActive(true);
    }

    IEnumerator YabawiRoutine()
    {
        isPlaying = true;
        canSelect = false;

        // 보물 들어올려 위치 보여주기
        yield return StartCoroutine(LiftCup(activeCups[targetIndex], true));
        yield return new WaitForSeconds(0.8f);
        yield return StartCoroutine(LiftCup(activeCups[targetIndex], false));
        yield return new WaitForSeconds(0.3f);

        // 섞기 루프
        for (int i = 0; i < currentSetting.shuffleCount; i++)
        {
            float progress = (float)i / (currentSetting.shuffleCount - 1);
            float currentDuration = Mathf.Lerp(currentSetting.startSpeed, currentSetting.endSpeed, progress);

            // ⭐ [핵심 2] 무작위 쌍 조합 생성 및 이동 코루틴 호출
            List<(int, int)> pairs = GenerateRandomPairs(activeCups.Count);
            yield return StartCoroutine(SwapPairsStep(pairs, currentDuration));
        }

        canSelect = true;
        Debug.Log("정답 컵을 선택하세요!");
    }

    // ⭐ [핵심 3] 스텝마다 1쌍 또는 2쌍을 무작위로 뽑아내는 함수
    private List<(int, int)> GenerateRandomPairs(int totalCount)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < totalCount; i++) indices.Add(i);

        // 인덱스 셔플
        for (int i = 0; i < indices.Count; i++)
        {
            int rand = Random.Range(i, indices.Count);
            int temp = indices[i];
            indices[i] = indices[rand];
            indices[rand] = temp;
        }

        int maxPairs = totalCount / 2; // 3개 ➔ 1쌍, 4~5개 ➔ 최대 2쌍
        int numPairs = Random.Range(1, maxPairs + 1); // 스텝마다 몇 쌍 움직일지 무작위 결정!

        List<(int, int)> pairs = new List<(int, int)>();
        for (int p = 0; p < numPairs; p++)
        {
            pairs.Add((indices[p * 2], indices[p * 2 + 1]));
        }
        return pairs;
    }

    // ⭐ [핵심 4] 한 스텝에서 선택된 N쌍의 컵을 동시에 교차 이동시키는 코루틴
    IEnumerator SwapPairsStep(List<(int, int)> pairs, float duration)
    {
        List<SwapPair> swapData = new List<SwapPair>();
        foreach (var p in pairs)
        {
            swapData.Add(new SwapPair
            {
                idx1 = p.Item1,
                idx2 = p.Item2,
                startPos1 = activeCups[p.Item1].anchoredPosition,
                startPos2 = activeCups[p.Item2].anchoredPosition
            });
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 선택된 모든 쌍 동시 스무스 보간 이동
            foreach (var sd in swapData)
            {
                activeCups[sd.idx1].anchoredPosition = Vector2.Lerp(sd.startPos1, sd.startPos2, smoothT);
                activeCups[sd.idx2].anchoredPosition = Vector2.Lerp(sd.startPos2, sd.startPos1, smoothT);
            }

            // 보물 실시간 추적
            if (currentTreasure != null)
                if (currentTreasure != null)
                    currentTreasure.anchoredPosition = activeCups[targetIndex].anchoredPosition + treasureOffset;

            yield return null;
        }

        // 이동 완료 후 위치 오차 고정 및 데이터/인덱스 갱신
        foreach (var sd in swapData)
        {
            activeCups[sd.idx1].anchoredPosition = sd.startPos2;
            activeCups[sd.idx2].anchoredPosition = sd.startPos1;

            // 리스트 내부 스왑
            RectTransform temp = activeCups[sd.idx1];
            activeCups[sd.idx1] = activeCups[sd.idx2];
            activeCups[sd.idx2] = temp;

            // 보물 targetIndex 추적
            if (targetIndex == sd.idx1) targetIndex = sd.idx2;
            else if (targetIndex == sd.idx2) targetIndex = sd.idx1;
        }

        // 코루틴 맨 끝 부분
        if (currentTreasure != null)
            currentTreasure.anchoredPosition = activeCups[targetIndex].anchoredPosition + treasureOffset;
    }

    public void OnCupClicked(RectTransform clickedRect)
    {
        if (!canSelect) return;

        int listIndex = activeCups.IndexOf(clickedRect);
        if (listIndex == -1) return;

        canSelect = false;
        StartCoroutine(RevealResult(listIndex));
    }

    IEnumerator RevealResult(int selectedListIndex)
    {
        yield return StartCoroutine(LiftCup(activeCups[selectedListIndex], true));

        bool isSuccess = (selectedListIndex == targetIndex);

        yield return new WaitForSeconds(1.0f);

        // 3초 뒤 남은 모든 컵 들기
        for (int i = 0; i < activeCups.Count; i++)
        {
            if (i != selectedListIndex)
                StartCoroutine(LiftCup(activeCups[i], true));
        }

        yield return new WaitForSeconds(2.0f);
        isPlaying = false;

        ClearAllElements();
        ShowResultPanel(isSuccess);
    }

    // 컵 위/아래 이동 애니메이션 (2행 위치도 완벽 대응)
    IEnumerator LiftCup(RectTransform cup, bool isUp)
    {
        Vector2 startPos = cup.anchoredPosition;
        float targetY = isUp ? startPos.y + cupLiftHeight : startPos.y - cupLiftHeight;
        Vector2 targetPos = new Vector2(startPos.x, targetY);

        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cup.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }
        cup.anchoredPosition = targetPos;
    }

    /// <summary>한 판이 끝나면 성공/실패 결과 패널을 띄운다.</summary>
    private void ShowResultPanel(bool isSuccess)
    {
        if (resultImage != null) resultImage.sprite = isSuccess ? successSprite : failSprite;

        string message;
        if (isSuccess)
        {
            int rewardKnowledge = currentSetting.rewardKnowledgePoint;
            long grantedKnowledge = GameManager.GrantReward(rewardGoldPerWin, rewardKnowledge);
            message = $"보상: {rewardGoldPerWin} 골드 / {grantedKnowledge} 지식 포인트";
            if (grantedKnowledge < rewardKnowledge) message += "\n(오늘 지식포인트 한도를 모두 채웠습니다)";
        }
        else
        {
            message = "아쉽네요, 다시 도전해 보세요.";
        }

        if (resultText != null) resultText.text = message;

        // 결과 화면에서는 우측 상단 나가기 대신 패널 안의 버튼을 쓴다.
        if (gameCloseButton != null) gameCloseButton.gameObject.SetActive(false);
        if (gameFinishPanel != null) gameFinishPanel.SetActive(true);
    }

    private void UpdateStatusText(string diff)
    {
        if (difficultyText != null) difficultyText.text = $"난이도: {diff}";
    }
}




