using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("카메라 이동 제한")]
    [SerializeField] private SpriteRenderer backgroundSprite;

    void Awake()
    {
        Instance = this;
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
