using UnityEngine;

public class GridOverlay : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private int mapWidth = 20;       // 맵 가로 셀 수
    [SerializeField] private int mapHeight = 20;      // 맵 세로 셀 수
    [SerializeField] private Vector3Int mapCenter = Vector3Int.zero; // 맵 중심 셀 좌표
    [SerializeField] private float cellScale = 1f;    // 마름모 한 칸 크기 배율
    [SerializeField] private float lineWidth = 0.03f;
    [SerializeField] private int sortingOrder = 10;

    void Start()
    {
        DrawGrid();
    }

    void DrawGrid()
    {
        int halfW = mapWidth / 2;
        int halfH = mapHeight / 2;

        for (int x = -halfW; x < halfW; x++)
        {
            for (int y = -halfH; y < halfH; y++)
            {
                DrawCell(new Vector3Int(x + mapCenter.x, y + mapCenter.y, 0));
            }
        }
    }

    void DrawCell(Vector3Int cell)
    {
        Vector3 center = grid.GetCellCenterWorld(cell);
        Vector2 cellSize = grid.cellSize * cellScale;

        Vector3 top    = center + new Vector3(0,                   cellSize.y * 0.5f, 0);
        Vector3 right  = center + new Vector3( cellSize.x * 0.5f, 0,                 0);
        Vector3 bottom = center + new Vector3(0,                  -cellSize.y * 0.5f, 0);
        Vector3 left   = center + new Vector3(-cellSize.x * 0.5f, 0,                 0);

        CreateLine(top, right);
        CreateLine(right, bottom);
        CreateLine(bottom, left);
        CreateLine(left, top);
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject obj = new GameObject("GridLine");
        obj.transform.SetParent(transform);

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.black;
        lr.endColor = Color.black;
        lr.sortingLayerName = "Default";
        lr.sortingOrder = sortingOrder;
    }
}
