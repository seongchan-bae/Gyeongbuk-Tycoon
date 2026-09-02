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

    // 해당 셀이 그리드 범위 안에 있는지 여부
    public bool Contains(Vector3Int cell)
    {
        int halfW = mapWidth / 2;
        int halfH = mapHeight / 2;
        int minX = mapCenter.x - halfW+1;
        int maxX = mapCenter.x + (mapWidth - halfW)-2;  // 오른쪽 아래 1칸 보정
        int minY = mapCenter.y - halfH+1;
        int maxY = mapCenter.y + (mapHeight - halfH) - 2;  // 왼쪽 아래 1칸 보정
        return cell.x >= minX && cell.x <= maxX && cell.y >= minY && cell.y <= maxY;
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

    // Scene 뷰에서 격자 경계를 노란 마름모 윤곽으로 표시 — Inspector 값 조정 시 실시간 확인용
    private void OnDrawGizmos()
    {
        if (grid == null) return;

        int halfW = mapWidth / 2;
        int halfH = mapHeight / 2;
        int minX = mapCenter.x - halfW;
        int maxX = mapCenter.x + (mapWidth - halfW) - 1;
        int minY = mapCenter.y - halfH;
        int maxY = mapCenter.y + (mapHeight - halfH) - 1;

        // 셀 공간의 4 모서리를 월드 좌표로 변환
        Vector3 cornerBL = grid.GetCellCenterWorld(new Vector3Int(minX, minY, 0));
        Vector3 cornerBR = grid.GetCellCenterWorld(new Vector3Int(maxX, minY, 0));
        Vector3 cornerTR = grid.GetCellCenterWorld(new Vector3Int(maxX, maxY, 0));
        Vector3 cornerTL = grid.GetCellCenterWorld(new Vector3Int(minX, maxY, 0));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cornerBL, cornerBR);
        Gizmos.DrawLine(cornerBR, cornerTR);
        Gizmos.DrawLine(cornerTR, cornerTL);
        Gizmos.DrawLine(cornerTL, cornerBL);

        // 모서리 점 강조
        float dotSize = 0.15f;
        Gizmos.DrawSphere(cornerBL, dotSize);
        Gizmos.DrawSphere(cornerBR, dotSize);
        Gizmos.DrawSphere(cornerTR, dotSize);
        Gizmos.DrawSphere(cornerTL, dotSize);
    }
}
