using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingInstall : MonoBehaviour
{
    [Header("타일맵 참조")]
    [SerializeField] private Grid baseGrid;
    [SerializeField] private Tilemap baseTilemap;

    [Header("설치 대상")]
    [SerializeField] private GameObject BuildingPrefab;
    [SerializeField] private TileBase tileAsset;

    [Header("게임 매니저 & 충돌 레이어")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LayerMask buildingLayer; // Building, Cloud 레이어 다중 선택

    private Vector3 mouseWorldPos;
    private Vector3Int cellPosition;
    private bool isCollidingWithBuilding = false;

    void Start()
    {
        SetupTriggerCollider();
    }

    void SetupTriggerCollider()
    {
        // 2D 마름모 충돌을 감지할 PolygonCollider2D 추가/설정
        PolygonCollider2D col = GetComponent<PolygonCollider2D>();
        if (col == null) col = gameObject.AddComponent<PolygonCollider2D>();
        col.isTrigger = true;

        float w = 1f;
        float h = 1f;

        if (BuildingPrefab != null)
        {
            Building prefabBuilding = BuildingPrefab.GetComponent<Building>();
            if (prefabBuilding != null)
            {
                w = prefabBuilding.tileWidth;
                h = prefabBuilding.tileHeight;
            }
        }

        // 유니티 2D Isometric 타일 표준 마름모 비율 (2:1)
        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(-w * 0.5f, 0);       // 좌
        points[1] = new Vector2(0, h * 0.25f);       // 상
        points[2] = new Vector2(w * 0.5f, 0);        // 우
        points[3] = new Vector2(0, -h * 0.25f);      // 하
        col.points = points;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        // 1. 마우스의 월드 좌표 가져오기
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = -Camera.main.transform.position.z;
        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreen);
        mouseWorldPos.z = 0f;

        // 2. 마우스 좌표를 Grid 셀 단위로 변환 후, 이 감지 오브젝트의 위치를 셀 중심으로 이동
        if (baseGrid != null)
        {
            cellPosition = baseGrid.WorldToCell(mouseWorldPos);
            transform.position = baseGrid.GetCellCenterWorld(cellPosition);
        }

        // 3. 모드 변경 및 클릭 로직
        if (gameManager != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                gameManager.installingActivation = true;
                gameManager.destroyingActivation = false;
                Debug.Log("모드 변경: 건물 설치 모드");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                gameManager.destroyingActivation = true;
                gameManager.installingActivation = false;
                Debug.Log("모드 변경: 건물 삭제 모드");
            }

            // 설치 모드 클릭
            if (gameManager.installingActivation && Input.GetMouseButtonDown(0))
            {
                buildingInstalling(cellPosition);
            }

            // 삭제 모드 클릭
            if (gameManager.destroyingActivation && Input.GetMouseButtonDown(0))
            {
                buildingUninstalling();
            }
        }
    }

    // --- [충돌 감지 부분] ---
    private void OnTriggerStay2D(Collider2D other)
    {
        // 셀프 충돌 방지
        if (other.gameObject == this.gameObject || other.transform.IsChildOf(this.transform)) return;

        // 지정된 레이어(Building, Cloud)와 겹치면 감지
        if (((1 << other.gameObject.layer) & buildingLayer) != 0)
        {
            isCollidingWithBuilding = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & buildingLayer) != 0)
        {
            isCollidingWithBuilding = false;
        }
    }

    // 상점에서 건물 선택 시 호출 - 설치할 건물 프리팹 설정 및 설치 모드 활성화
    public void SetBuilding(GameObject prefab)
    {
        BuildingPrefab = prefab;
        SetupTriggerCollider();
        if (gameManager != null)
            gameManager.installingActivation = true;
    }

    void buildingInstalling(Vector3Int currentCellPos)
    {
        if (isCollidingWithBuilding)
        {
            Debug.LogWarning("여기는 건물이나 장애물이 있어 설치할 수 없습니다!");
            return;
        }

        // 현재 감지 마름모가 위치한 정확한 중심점에 건물 소환
        Vector3 spawnPos = baseGrid.GetCellCenterWorld(currentCellPos);
        GameObject installedBuilding = Instantiate(BuildingPrefab, spawnPos, Quaternion.identity);
        installedBuilding.transform.SetParent(baseGrid.transform);

        isCollidingWithBuilding = false;
        Debug.Log("건물 설치 완료!");
    }

    void buildingUninstalling()
    {
        if (baseTilemap != null)
        {
            baseTilemap.SetTile(cellPosition, null);
        }
    }

    // --- [씬 뷰 마름모 기즈모 시각화] ---
    private void OnDrawGizmos()
    {
        if (baseGrid == null) return;

        Vector3 centerPos = baseGrid.GetCellCenterWorld(cellPosition);

        float w = 1f;
        float h = 1f;

        if (BuildingPrefab != null)
        {
            Building prefabBuilding = BuildingPrefab.GetComponent<Building>();
            if (prefabBuilding != null)
            {
                w = prefabBuilding.tileWidth;
                h = prefabBuilding.tileHeight;
            }
        }

        // 충돌 중일 때는 빨간색, 설치 가능할 때는 초록색
        Gizmos.color = isCollidingWithBuilding ? Color.red : Color.green;

        float horizontalRadius = w * 0.5f;
        float verticalRadius = h * 0.25f;

        Vector3 right  = centerPos + new Vector3(horizontalRadius, 0, 0);
        Vector3 left   = centerPos + new Vector3(-horizontalRadius, 0, 0);
        Vector3 top    = centerPos + new Vector3(0, verticalRadius, 0);
        Vector3 bottom = centerPos + new Vector3(0, -verticalRadius, 0);

        Gizmos.DrawLine(top, right);
        Gizmos.DrawLine(right, bottom);
        Gizmos.DrawLine(bottom, left);
        Gizmos.DrawLine(left, top);
    }
}