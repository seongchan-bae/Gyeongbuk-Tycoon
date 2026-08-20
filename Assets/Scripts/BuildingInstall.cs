using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

public class BuildingInstall : MonoBehaviour
{
    [Header("타일맵 참조")]
    [SerializeField] private Grid baseGrid;
    [SerializeField] private Tilemap baseTilemap;

    [Header("설치 대상")]
    [SerializeField] private BuildingData currentBuildingData;
    [SerializeField] private TileBase tileAsset;

    [Header("게임 매니저 & 충돌 레이어")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LayerMask buildingLayer; // Building, Cloud 레이어 다중 선택

    [Header("ShopButton 화면에 보이게/안보이게끔 조절")]
    [SerializeField]
    private BaseUI baseUI;

    private Vector3 mouseWorldPos;
    private Vector3Int cellPosition;
    private bool isCollidingWithBuilding = false;

    // 건물 미리보기용 Ghost 스프라이트
    private SpriteRenderer ghostRenderer;
    private Rigidbody2D rb;

    void Start()
    {
        SetupTriggerCollider();
        SetupGhost();
        rb = GetComponent<Rigidbody2D>();

    }

    // Ghost 오브젝트를 자식으로 자동 생성 — Inspector 작업 불필요
    void SetupGhost()
    {
        GameObject ghostObj = new GameObject("Ghost");
        ghostObj.transform.SetParent(transform);
        ghostObj.transform.localPosition = Vector3.zero;
        ghostRenderer = ghostObj.AddComponent<SpriteRenderer>();
        ghostObj.SetActive(false);
    }

    void SetupTriggerCollider()
    {
        // 2D 마름모 충돌을 감지할 PolygonCollider2D 추가/설정
        PolygonCollider2D col = GetComponent<PolygonCollider2D>();
        if (col == null) col = gameObject.AddComponent<PolygonCollider2D>();
        col.isTrigger = true;

        float w = 1f;
        float h = 1f;

        if (currentBuildingData != null)
        {
            w = currentBuildingData.tileWidth;
            h = currentBuildingData.tileHeight;
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
        // rb.MovePosition 사용 — transform 직접 조작 시 Kinematic RB가 물리 이동을 인식 못해 트리거 미발동
        if (baseGrid != null)
        {
            cellPosition = baseGrid.WorldToCell(mouseWorldPos);
            Vector3 targetPos = baseGrid.GetCellCenterWorld(cellPosition);
            if (rb != null)
                rb.MovePosition(targetPos);
            else
                transform.position = targetPos;
        }

        // 3. Ghost 색상 업데이트 — 설치 불가: 빨간색 반투명 / 설치 가능: 흰색 반투명
        if (ghostRenderer != null && ghostRenderer.gameObject.activeSelf)
        {
            ghostRenderer.color = isCollidingWithBuilding
                ? new Color(1f, 0f, 0f, 0.5f)
                : new Color(0f, 1f, 0f, 0.5f);
        }

        // 4. 모드 변경 및 클릭 로직
        if (gameManager != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                gameManager.destroyingActivation = true;
                gameManager.installingActivation = false;
                Debug.Log("모드 변경: 건물 삭제 모드");
            }

                if (!EventSystem.current.IsPointerOverGameObject())
            {
                bool clickHandled = false;

                // 설치/삭제 모드일 때
                if (Input.GetMouseButtonDown(0) && !Building.AnyBuildingDragging)
                {
                    if (gameManager.installingActivation)
                    {
                        buildingInstalling(cellPosition);
                        clickHandled = true;
                        clickConsumedByAction = true;
                    }
                    if (gameManager.destroyingActivation)
                    {
                        buildingUninstalling();
                        clickHandled = true;
                        clickConsumedByAction = true;
                    }
                }

                // 버튼을 떼면 소비 플래그 해제
                if (Input.GetMouseButtonUp(0))
                    clickConsumedByAction = false;

                // 일반 모드일 때 건물 클릭/드래그 처리 (설치/삭제 클릭과 같은 프레임엔 실행 안 함)
                if (!gameManager.installingActivation && !gameManager.destroyingActivation && !clickHandled && !clickConsumedByAction)
                    HandleBuildingInteraction();
            }
        }
    }

    // --- [건물 클릭/드래그 + 카메라 이동 처리] ---
    private Building draggedBuilding = null;
    private Vector3 mouseDownScreenPos;
    private bool isDraggingBuilding = false;

    // 카메라 드래그용
    private bool isCameraDragging = false;
    private Vector3 cameraDragStartWorld;

    // 설치/삭제 클릭으로 소비된 버튼 — 손 뗄 때까지 HandleBuildingInteraction 진입 차단
    private bool clickConsumedByAction = false;

    void HandleBuildingInteraction()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownScreenPos = Input.mousePosition;
            isDraggingBuilding = false;
            isCameraDragging = false;
            draggedBuilding = null;

            // 스프라이트 경계 기반 건물 클릭 감지
            Vector3 clickPos = mouseWorldPos;
            foreach (Building b in FindObjectsByType<Building>(FindObjectsSortMode.None))
            {
                SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
                if (sr == null) sr = b.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.bounds.Contains(new Vector3(clickPos.x, clickPos.y, sr.bounds.center.z)))
                {
                    draggedBuilding = b;
                    break;
                }
            }

            if (draggedBuilding != null)
            {
                BuildingPopupUI.Instance?.Hide();
            }
            else
            {
                BuildingPopupUI.Instance?.Hide();
                // 빈 화면 클릭 → 카메라 드래그 준비
                cameraDragStartWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (draggedBuilding != null)
            {
                // 건물 드래그 이동
                if (Vector2.Distance(Input.mousePosition, mouseDownScreenPos) > 10f)
                {
                    isDraggingBuilding = true;
                    Building.AnyBuildingDragging = true;

                    Vector3Int cell = baseGrid.WorldToCell(mouseWorldPos);
                    draggedBuilding.transform.position = baseGrid.GetCellCenterWorld(cell);
                }
            }
            else
            {
                // 카메라 드래그 이동
                if (Vector2.Distance(Input.mousePosition, mouseDownScreenPos) > 5f)
                {
                    isCameraDragging = true;
                    Vector3 currentWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector3 delta = cameraDragStartWorld - currentWorld;
                    delta.z = 0f;
                    Camera.main.transform.position += delta;
                    // 다음 프레임 기준점 갱신 (누적 방지)
                    cameraDragStartWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (draggedBuilding != null && !isDraggingBuilding)
                BuildingPopupUI.Instance?.Show(draggedBuilding);

            isDraggingBuilding = false;
            isCameraDragging = false;
            Building.AnyBuildingDragging = false;
            draggedBuilding = null;
        }
    }

    // --- [충돌 감지 부분] ---
    private bool IsTilemapCollider(Collider2D other)
    {
        // TilemapCollider2D 또는 CompositeCollider2D(타일맵이 합성할 때 생성) 모두 제외
        return other.GetComponentInParent<Tilemap>() != null;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 셀프·자식 오브젝트 충돌 방지
        if (other.gameObject == this.gameObject || other.transform.IsChildOf(this.transform)) return;
        // 타일맵 계열 Collider 제외
        if (IsTilemapCollider(other)) return;

        // buildingLayer 미설정 시 모든 Collider2D 감지, 설정 시 해당 레이어만 감지
        bool layerMatch = buildingLayer.value == 0 || ((1 << other.gameObject.layer) & buildingLayer) != 0;
        if (layerMatch)
            isCollidingWithBuilding = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == this.gameObject || other.transform.IsChildOf(this.transform)) return;
        if (IsTilemapCollider(other)) return;

        bool layerMatch = buildingLayer.value == 0 || ((1 << other.gameObject.layer) & buildingLayer) != 0;
        if (layerMatch)
            isCollidingWithBuilding = false;
    }

    // 상점 건물 카드에서 호출 — BuildingData 교체 후 Ghost 표시 + 설치 모드 ON
    public void SelectBuilding(BuildingData data)
    {
        currentBuildingData = data;
        SetupTriggerCollider();

        // 프리팹의 SpriteRenderer에서 스프라이트를 가져와 Ghost에 적용
        SpriteRenderer prefabSprite = data.prefab.GetComponent<SpriteRenderer>();
        if (prefabSprite != null && ghostRenderer != null)
        {
            ghostRenderer.sprite = prefabSprite.sprite;
            ghostRenderer.sortingLayerName = prefabSprite.sortingLayerName;
            ghostRenderer.sortingOrder = prefabSprite.sortingOrder + 1;
            ghostRenderer.gameObject.SetActive(true);
        }

        if (gameManager != null){
            gameManager.installingActivation = true;
            baseUI.CloseStoreButton();
        }
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
        GameObject installedBuilding = Instantiate(currentBuildingData.prefab, spawnPos, Quaternion.identity);
        installedBuilding.transform.SetParent(baseGrid.transform);

        // 설치된 건물에 2D 충돌체 자동 설정 (트리거 겹침 감지용)
        SetupBuildingCollider(installedBuilding);

        // Building 컴포넌트가 없으면 코드에서 직접 추가
        Building building = installedBuilding.GetComponent<Building>();
        if (building == null)
            building = installedBuilding.AddComponent<Building>();

        building.Initialize(gameManager);
        building.buildingData = currentBuildingData;

        // Ghost 숨기고 설치 모드 종료
        if (ghostRenderer != null)
            ghostRenderer.gameObject.SetActive(false);

        isCollidingWithBuilding = false;
        gameManager.installingActivation = false; // 설치 완료 후 모드 자동 종료
        baseUI.ShowStoreButton();
        Debug.Log("건물 설치 완료!");
    }

    void SetupBuildingCollider(GameObject building)
    {
        // 이미 Collider2D가 있으면 레이어만 설정
        if (building.GetComponent<Collider2D>() == null)
        {
            float w = currentBuildingData != null ? currentBuildingData.tileWidth : 1f;
            float h = currentBuildingData != null ? currentBuildingData.tileHeight : 1f;

            PolygonCollider2D col = building.AddComponent<PolygonCollider2D>();
            col.points = new Vector2[]
            {
                new Vector2(-w * 0.5f, 0),
                new Vector2(0,  h * 0.25f),
                new Vector2( w * 0.5f, 0),
                new Vector2(0, -h * 0.25f)
            };
            Debug.Log($"[BuildingInstall] {building.name} → PolygonCollider2D 추가됨 (w={w}, h={h})");
        }

        // buildingLayer 마스크에서 첫 번째 레이어를 꺼내 건물 레이어로 설정
        if (buildingLayer.value != 0)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((buildingLayer.value & (1 << i)) != 0)
                {
                    building.layer = i;
                    Debug.Log($"[BuildingInstall] {building.name} → layer = {i} ({LayerMask.LayerToName(i)})");
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning("[BuildingInstall] buildingLayer가 Inspector에 설정되지 않았습니다. 충돌 감지가 작동하지 않을 수 있습니다.");
        }

        // Rigidbody2D(Static)이 없으면 추가 — 없으면 트리거 이벤트가 발생하지 않음
        if (building.GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D buildingRb = building.AddComponent<Rigidbody2D>();
            buildingRb.bodyType = RigidbodyType2D.Static;
        }
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

        if (currentBuildingData != null)
        {
            w = currentBuildingData.tileWidth;
            h = currentBuildingData.tileHeight;
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
