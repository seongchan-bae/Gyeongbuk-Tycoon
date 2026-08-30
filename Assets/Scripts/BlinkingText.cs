using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 텍스트의 투명도를 천천히 오르내리게 하여 부드럽게 깜빡이는 연출을 만듭니다.
/// "터치하여 시작하기"처럼 사용자의 입력을 기다리는 안내 문구에 사용합니다.
/// </summary>
[DisallowMultipleComponent]
public class BlinkingText : MonoBehaviour
{
    [Header("Blink Settings")]
    [SerializeField, Tooltip("한 번 깜빡이는 데 걸리는 시간(초). 값이 클수록 느리게 깜빡입니다.")]
    private float cycleDuration = 1.6f;

    [SerializeField, Range(0f, 1f), Tooltip("가장 흐려졌을 때의 투명도")]
    private float minAlpha = 0.2f;

    [SerializeField, Range(0f, 1f), Tooltip("가장 진해졌을 때의 투명도")]
    private float maxAlpha = 1f;

    [SerializeField, Tooltip("일시정지(timeScale = 0) 중에도 계속 깜빡일지 여부")]
    private bool ignoreTimeScale = true;

    private TMP_Text tmpText;       // TextMeshPro 텍스트 (있는 경우)
    private Graphic graphic;        // 일반 uGUI 그래픽 (Image, Text 등)
    private float originalAlpha = 1f;
    private float timer;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText == null) graphic = GetComponent<Graphic>();

        if (tmpText != null) originalAlpha = tmpText.alpha;
        else if (graphic != null) originalAlpha = graphic.color.a;
    }

    private void OnEnable()
    {
        // 활성화될 때마다 가장 진한 상태에서 시작
        timer = 0f;
        ApplyAlpha(maxAlpha);
    }

    private void OnDisable()
    {
        // 비활성화 시 원래 투명도로 되돌려, 꺼진 순간의 흐릿한 상태가 남지 않도록 함
        ApplyAlpha(originalAlpha);
    }

    private void Update()
    {
        if (cycleDuration <= 0f) return;

        timer += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

        // 코사인 곡선으로 1 -> 0 -> 1 을 반복 (선형보다 자연스럽게 잦아드는 느낌)
        float t = (Mathf.Cos(timer / cycleDuration * Mathf.PI * 2f) + 1f) * 0.5f;
        ApplyAlpha(Mathf.Lerp(minAlpha, maxAlpha, t));
    }

    private void ApplyAlpha(float alpha)
    {
        if (tmpText != null)
        {
            tmpText.alpha = alpha;
        }
        else if (graphic != null)
        {
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}
