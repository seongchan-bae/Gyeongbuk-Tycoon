using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject backCover;     // 카드 뒷면 (가림막)
    [SerializeField] private Image cardImage;         // 앞면 유적지 사진
    [SerializeField] private TextMeshProUGUI cardText; // 앞면 유적지 이름

    [Header("Flip Animation Setting")]
    [SerializeField] private float flipDuration = 0.25f; // 뒤집히는 시간

    public int CardID { get; private set; }
    public bool IsFlipped { get; private set; }
    public bool IsMatched { get; private set; }

    private CardMatchingGame gameController;
    private Coroutine flipCoroutine;
    private Button cardButton;
    private RectTransform rectTransform;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();

        // ⭐ 1. 정중앙(0.5, 0.5)을 기준으로 예쁘게 접히도록 피벗 강제 고정
        if (rectTransform != null)
        {
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    public void SetupCard(int id, Sprite sprite, string name, CardMatchingGame controller)
    {
        CardID = id;
        gameController = controller;
        IsFlipped = false;
        IsMatched = false;

        // 앞면 텍스트 세팅
        if (cardText != null)
        {
            cardText.text = name;
            cardText.gameObject.SetActive(true);
        }

        // 앞면 이미지 세팅
        if (cardImage != null)
        {
            if (sprite != null)
            {
                cardImage.sprite = sprite;
                cardImage.gameObject.SetActive(true);
            }
            else
            {
                cardImage.gameObject.SetActive(false);
            }
        }

        // 초기 상태: 뒷면 가림막 켜기
        if (backCover != null)
        {
            backCover.SetActive(true);
            backCover.transform.SetAsLastSibling(); // 뒷면을 맨 위로
        }

        SetFrontElementsActive(false);

        transform.localScale = Vector3.one;
        if (cardButton != null) cardButton.interactable = true;
    }

    public void OnCardClicked()
    {
        // 짝이 맞춰졌거나 이미 뒤집혔거나, 판정 진행 중이면 클릭 차단
        if (IsFlipped || IsMatched || (gameController != null && gameController.IsBusy)) return;

        FlipToFront();
        if (gameController != null)
        {
            gameController.OnCardSelected(this);
        }
    }

    public void FlipToFront()
    {
        if (flipCoroutine != null) StopCoroutine(flipCoroutine);
        flipCoroutine = StartCoroutine(AnimateFlip(true));
    }

    public void FlipToBack()
    {
        if (flipCoroutine != null) StopCoroutine(flipCoroutine);
        flipCoroutine = StartCoroutine(AnimateFlip(false));
    }

    // ⭐ 정중앙 기준 3D 뒤집기 코루틴
    private IEnumerator AnimateFlip(bool showFront)
    {
        IsFlipped = showFront;
        float halfDuration = flipDuration / 2f;

        // 1단계: 정중앙 축을 향해 납작해짐 (Scale X: 1 -> 0)
        float timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / halfDuration;
            float currentX = Mathf.Lerp(1f, 0f, progress);
            transform.localScale = new Vector3(currentX, 1f, 1f);
            yield return null;
        }
        transform.localScale = new Vector3(0f, 1f, 1f);

        // 2단계: 완전히 얇아진 순간 앞/뒷면 화면 스위칭
        if (showFront)
        {
            if (backCover != null) backCover.SetActive(false);
            SetFrontElementsActive(true);
        }
        else
        {
            if (backCover != null)
            {
                backCover.SetActive(true);
                backCover.transform.SetAsLastSibling();
            }
            SetFrontElementsActive(false);
        }

        // 3단계: 정중앙에서 다시 넓어짐 (Scale X: 0 -> 1)
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / halfDuration;
            float currentX = Mathf.Lerp(0f, 1f, progress);
            transform.localScale = new Vector3(currentX, 1f, 1f);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private void SetFrontElementsActive(bool isActive)
    {
        if (cardText != null)
        {
            cardText.gameObject.SetActive(isActive);
            if (isActive) cardText.transform.SetAsLastSibling(); // ⭐ 앞면 텍스트를 최상단 렌더링
        }

        if (cardImage != null && cardImage.sprite != null)
        {
            cardImage.gameObject.SetActive(isActive);
        }
    }

    public void SetMatched()
    {
        IsMatched = true;
        IsFlipped = true;
        if (backCover != null) backCover.SetActive(false);
        SetFrontElementsActive(true);
        if (cardButton != null) cardButton.interactable = false;
    }
}