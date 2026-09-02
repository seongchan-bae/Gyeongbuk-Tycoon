using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("카메라 이동 제한")]
    [SerializeField] private SpriteRenderer backgroundSprite;

    void Awake()
    {
        // backgroundSprite가 연결된 쪽이 Instance가 되도록 우선순위 부여
        if (Instance == null || (backgroundSprite != null && Instance.backgroundSprite == null))
        {
            Instance = this;
        }
        else if (backgroundSprite == null)
        {
            return;
        }
    }

    void LateUpdate()
    {
        // 매 프레임 후처리로 카메라가 어떤 이유로든 범위를 벗어나면 즉시 보정
        ClampCamera();
    }

    public void ClampCamera()
    {
        if (backgroundSprite == null) return;

        Bounds bg = backgroundSprite.bounds;
        float camH = Camera.main.orthographicSize;
        float camW = camH * Camera.main.aspect;

        float minX = bg.min.x + camW;
        float maxX = bg.max.x - camW;
        float minY = bg.min.y + camH;
        float maxY = bg.max.y - camH;

        Vector3 pos = Camera.main.transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        Camera.main.transform.position = pos;
    }
}
