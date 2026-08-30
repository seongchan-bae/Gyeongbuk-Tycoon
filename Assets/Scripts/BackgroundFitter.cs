using UnityEngine;

/// <summary>
/// 배경 SpriteRenderer를 카메라 시야에 항상 꽉 차게 맞춘다.
/// 직교 카메라의 세로 시야는 orthographicSize로 고정이지만 가로 시야는 화면비에 비례해
/// 늘어나므로, 폭이 넓은 기기(예: 20:9 폰)에서는 배경이 좌우를 못 덮고 여백이 생긴다.
/// 테마 교체로 스프라이트가 바뀌어도 자동으로 다시 맞춰진다.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundFitter : MonoBehaviour
{
    [Tooltip("기준이 될 카메라. 비워두면 Camera.main을 사용한다.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("카메라가 이동해도 항상 화면을 덮도록 배경을 따라 움직인다. " +
             "끄면 배경이 월드에 고정되어, 멀리 이동하면 가장자리가 보일 수 있다.")]
    [SerializeField] private bool followCamera = true;

    public enum FitMode
    {
        Cover,    // 화면을 빈틈없이 채운다. 이미지와 화면의 비율이 다르면 가장자리가 잘린다.
        Contain,  // 이미지 전체를 보여준다. 남는 영역은 카메라 배경색으로 채워진다.
        Stretch   // 가로/세로를 각각 늘려 정확히 채운다. 이미지가 일그러진다.
    }

    [Tooltip("Cover=잘림 감수하고 꽉 채움 / Contain=전체 표시(여백 생김) / Stretch=늘려서 채움(일그러짐)")]
    [SerializeField] private FitMode fitMode = FitMode.Cover;

    [Tooltip("가시 영역보다 이 비율만큼 여유를 두고 덮는다. 1.0 = 여유 없음.")]
    [SerializeField] private float paddingRatio = 1f;

    private SpriteRenderer spriteRenderer;
    private Sprite lastSprite;
    private float lastAspect;
    private float lastOrthoSize;

    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Fit(force: true);
    }

    private void LateUpdate()
    {
        Fit(force: false);
    }

    private Camera ResolveCamera()
    {
        if (targetCamera != null) return targetCamera;
        return Camera.main;
    }

    private void Fit(bool force)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null) return;

        Camera cam = ResolveCamera();
        if (cam == null || !cam.orthographic) return;

        // 매 프레임 계산할 필요는 없다. 바뀐 게 있을 때만 다시 맞춘다.
        bool changed = force
            || sprite != lastSprite
            || !Mathf.Approximately(cam.aspect, lastAspect)
            || !Mathf.Approximately(cam.orthographicSize, lastOrthoSize);

        if (changed)
        {
            float visibleHeight = cam.orthographicSize * 2f;
            float visibleWidth = visibleHeight * cam.aspect;

            // 스케일 1일 때의 스프라이트 월드 크기
            float baseWidth = sprite.rect.width / sprite.pixelsPerUnit;
            float baseHeight = sprite.rect.height / sprite.pixelsPerUnit;
            if (baseWidth <= 0f || baseHeight <= 0f) return;

            float scaleX = visibleWidth / baseWidth;
            float scaleY = visibleHeight / baseHeight;

            switch (fitMode)
            {
                case FitMode.Stretch:
                    transform.localScale = new Vector3(scaleX * paddingRatio, scaleY * paddingRatio, 1f);
                    break;
                case FitMode.Contain:
                    // 둘 중 작은 배율 -> 이미지 전체가 들어온다 (대신 여백이 생긴다)
                    float contain = Mathf.Min(scaleX, scaleY) * paddingRatio;
                    transform.localScale = new Vector3(contain, contain, 1f);
                    break;
                default:
                    // 둘 중 큰 배율 -> 빈 곳 없이 덮인다 (대신 가장자리가 잘린다)
                    float cover = Mathf.Max(scaleX, scaleY) * paddingRatio;
                    transform.localScale = new Vector3(cover, cover, 1f);
                    break;
            }

            lastSprite = sprite;
            lastAspect = cam.aspect;
            lastOrthoSize = cam.orthographicSize;
        }

        if (followCamera)
        {
            Vector3 camPos = cam.transform.position;
            transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }
    }
}
