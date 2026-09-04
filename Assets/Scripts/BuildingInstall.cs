using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BuildingInstall : MonoBehaviour
{
    public Grid BaseGrid => baseGrid;

    [Header("건물 데이터베이스")]
    [SerializeField] private BuildingData[] allBuildingDataList;

    [Header("타일맵 참조")]
    [SerializeField] private Grid baseGrid;
    [SerializeField] private Tilemap baseTilemap;

    [Header("설치 대상")]
    [SerializeField] private BuildingData currentBuildingData;
    [SerializeField] private TileBase tileAsset;

    [Header("게임 매니저 & 충돌 레이어")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LayerMask buildingLayer; // Building, Cloud 레이어 다중 선택
    [SerializeField] private GridOverlay gridOverlay; // 설치 가능 범위 판정용

    [Header("ShopButton 화면에 보이게/안보이게끔 조절")]
    [SerializeField]
    private BaseUI baseUI;

    private Vector3 mouseWorldPos;
    private Vector3Int cellPosition;
    private bool isCollidingWithBuilding = false;
    private bool isOutsideGrid = false;

    // 설치 불가 종합 판정 (충돌 OR 그리드 범위 밖)
    private bool CannotPlace => isCollidingWithBuilding || isOutsideGrid;

    // 건물 미리보기용 Ghost 스프라이트
    private SpriteRenderer ghostRenderer;
    private Rigidbody2D rb;

    [Header("타일 셀 하이라이트")]
    [SerializeField] private Color validColor   = new Color(0f, 1f, 0f, 0.6f);
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.6f);
    [SerializeField] private string highlightSortingLayer = "Default";
    [SerializeField] private int    highlightSortingOrder = 20;

    private List<SpriteRenderer> tileHighlights = new List<SpriteRenderer>();
    private Sprite   tileHighlightSprite;
    private GameObject highlightContainer;

    void Start()
    {
        SetupTriggerCollider();
        SetupGhost();
        SetupTileHighlight();
        rb = GetComponent<Rigidbody2D>();

        RestorePlacedBuildings();
    }

    public void RestorePlacedBuildings()
    {
        if (SaveManager.Instance == null || baseGrid == null) return;

        var savedBuildings = SaveManager.Instance.CurrentData.placedBuildings;
        if (savedBuildings == null || savedBuildings.Count == 0) return;

        foreach (var bSave in savedBuildings)
        {
            // 저장된 이름과 일치하는 설계도(BuildingData) 찾기
            BuildingData matchedData = System.Array.Find(allBuildingDataList, data => data.buildingName == bSave.buildingName);

            if (matchedData != null && matchedData.prefab != null)
            {
                Vector3Int cellPos = new Vector3Int(bSave.gridX, bSave.gridY, bSave.gridZ);

                // gridOverlay가 연결된 경우, 범위 밖 좌표의 건물은 복원 건너뜀
                if (gridOverlay != null && !gridOverlay.Contains(cellPos))
                {
                    Debug.LogWarning($"[RestorePlacedBuildings] {bSave.buildingName} 저장 좌표 {cellPos} 가 GridOverlay 범위 밖 → 복원 생략");
                    continue;
                }

                Vector3 spawnPos = baseGrid.GetCellCenterWorld(cellPos);

                GameObject installedBuilding = Instantiate(matchedData.prefab, spawnPos, Quaternion.identity);
                installedBuilding.transform.SetParent(baseGrid.transform);

                SetupBuildingCollider(installedBuilding);

                Building b = installedBuilding.GetComponent<Building>();
                if (b == null) b = installedBuilding.AddComponent<Building>();

                b.Initialize(gameManager);
                b.buildingData = matchedData;

                // 복원된 건물의 관광객 수치도 GameManager에 반영
                if (gameManager != null)
                    gameManager.AddTourists(matchedData.touristIncrease, matchedData.maxTouristIncrease);
            }
        }
    }


    // ===== 타일 셀 하이라이트 =====

    void SetupTileHighlight()
    {
        highlightContainer = new GameObject("TileHighlightContainer");
        highlightContainer.transform.SetParent(transform);
        tileHighlightSprite = CreateDiamondSprite();
    }

    // 2:1 아이소메트릭 마름모 스프라이트 생성 (에셋 없이 코드로)
    Sprite CreateDiamondSprite()
    {
        int texW = 128, texH = 64;
        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[texW * texH];

        for (int py = 0; py < texH; py++)
        {
            for (int px = 0; px < texW; px++)
            {
                float nx = (px + 0.5f) / texW;
                float ny = (py + 0.5f) / texH;
                float dist = Mathf.Abs(nx - 0.5f) * 2f + Mathf.Abs(ny - 0.5f) * 2f;

                pixels[py * texW + px] = dist > 1.0f ? Color.clear : Color.white;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        // PPU=128 → 1유닛 너비 / 0.5유닛 높이 (아이소메트릭 1셀 크기)
        return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), texW);
    }

    // 건물이 차지하는 셀 좌표 목록 반환
    List<Vector3Int> GetFootprintCells(Vector3Int center, BuildingData data)
    {
        var cells = new List<Vector3Int>();
        if (data == null) { cells.Add(center); return cells; }

        int w = data.tileWidth;
        int h = data.tileHeight;
        for (int x = -(w / 2); x < w - (w / 2); x++)
            for (int y = -(h / 2); y < h - (h / 2); y++)
                cells.Add(new Vector3Int(center.x + x, center.y + y, center.z));

        return cells;
    }

    void UpdateTileHighlights()
    {
        if (highlightContainer == null || tileHighlightSprite == null) return;

        bool show = gameManager != null && (gameManager.installingActivation || isBuildingMoving) && currentBuildingData != null;
        var cells = show ? GetFootprintCells(cellPosition, currentBuildingData) : new List<Vector3Int>();

        // 하이라이트가 건물/ghost 위에 온전히 보이려면 소팅이 ghost보다 높아야 함
        string targetLayer = ghostRenderer != null ? ghostRenderer.sortingLayerName : highlightSortingLayer;
        int    targetOrder = ghostRenderer != null ? ghostRenderer.sortingOrder + 1 : highlightSortingOrder;

        // 풀 확장 (부족할 때만)
        while (tileHighlights.Count < cells.Count)
        {
            var go = new GameObject("TileHighlight");
            go.transform.SetParent(highlightContainer.transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tileHighlightSprite;
            sr.sortingLayerName = targetLayer;
            sr.sortingOrder = targetOrder;
            tileHighlights.Add(sr);
        }

        // 기존 풀 오브젝트 소팅 동기화
        foreach (var sr in tileHighlights)
        {
            sr.sortingLayerName = targetLayer;
            sr.sortingOrder = targetOrder;
        }

        Color col = CannotPlace ? invalidColor : validColor;

        for (int i = 0; i < tileHighlights.Count; i++)
        {
            if (i < cells.Count && baseGrid != null)
            {
                tileHighlights[i].gameObject.SetActive(true);
                tileHighlights[i].transform.position = baseGrid.GetCellCenterWorld(cells[i]);
                tileHighlights[i].color = col;
            }
            else
            {
                tileHighlights[i].gameObject.SetActive(false);
            }
        }
    }

    void ClearTileHighlights()
    {
        foreach (var sr in tileHighlights)
            if (sr != null) sr.gameObject.SetActive(false);
    }

    // Escape 키로 설치 모드 취소
    void CancelInstalling()
    {
        if (ghostRenderer != null) ghostRenderer.gameObject.SetActive(false);
        ClearTileHighlights();
        isCollidingWithBuilding = false;
        if (gameManager != null)
        {
            gameManager.installingActivation = false;
            baseUI.ShowStoreButton();
        }
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

        // GridOverlay 범위 밖이면 설치 불가 처리
        isOutsideGrid = gridOverlay != null && !gridOverlay.Contains(cellPosition);

        // 3. Ghost 색상 업데이트 — 설치 불가: 빨간색 반투명 / 설치 가능: 초록색 반투명
        if (ghostRenderer != null && ghostRenderer.gameObject.activeSelf)
        {
            ghostRenderer.color = CannotPlace
                ? new Color(1f, 0f, 0f, 0.5f)
                : new Color(0f, 1f, 0f, 0.5f);
        }

        // 4. 타일 셀 하이라이트 갱신
        UpdateTileHighlights();

        // 5. 모드 변경 및 클릭 로직
        if (gameManager != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && gameManager.installingActivation)
                CancelInstalling();

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

    // 기존 건물 이동 모드
    private bool isBuildingMoving = false;
    private Vector3Int dragOriginalCell;

    void HandleBuildingInteraction()
    {
        // 두 손가락 확대/축소 중에는 드래그(건물 이동·카메라 이동)를 처리하지 않는다.
        if (Input.touchCount >= 2 || CameraController.IsPinching)
        {
            isDraggingBuilding = false;
            isCameraDragging = false;
            Building.AnyBuildingDragging = false;
            draggedBuilding = null;
            return;
        }

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
                if (Vector2.Distance(Input.mousePosition, mouseDownScreenPos) > 10f)
                {
                    // 처음 드래그 임계값 넘는 순간 이동 모드 시작
                    if (!isDraggingBuilding)
                        StartBuildingMove(draggedBuilding);

                    isDraggingBuilding = true;
                    Building.AnyBuildingDragging = true;

                    // 건물 시각 위치는 마우스 셀 중심으로 이동
                    draggedBuilding.transform.position = baseGrid.GetCellCenterWorld(cellPosition);
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
            if (draggedBuilding != null)
            {
                if (isDraggingBuilding && isBuildingMoving)
                    FinalizeBuildingMove(draggedBuilding);
                else if (!isDraggingBuilding && !BuildingPopupUI.WasHiddenThisClick)
                    BuildingPopupUI.Instance?.Show(draggedBuilding);
            }

            isDraggingBuilding = false;
            isCameraDragging = false;
            Building.AnyBuildingDragging = false;
            draggedBuilding = null;
        }
    }

    // 건물 이동 시작 — 드래그 임계값 넘는 순간 호출
    void StartBuildingMove(Building building)
    {
        isBuildingMoving = true;
        currentBuildingData = building.buildingData;
        dragOriginalCell = baseGrid.WorldToCell(building.transform.position);

        // 드래그 중인 건물의 콜라이더 비활성화 (자기 자신과 충돌 방지)
        var col = building.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 트리거 콜라이더를 이 건물 크기에 맞게 재설정
        SetupTriggerCollider();
    }

    // 건물 이동 확정 — 마우스 업 시 호출
    void FinalizeBuildingMove(Building building)
    {
        // 건물 콜라이더 복원
        var col = building.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        if (CannotPlace)
        {
            // 설치 불가 → 원래 위치로 복귀
            building.transform.position = baseGrid.GetCellCenterWorld(dragOriginalCell);
        }
        else
        {
            // 설치 가능 → 현재 위치 확정
            building.transform.position = baseGrid.GetCellCenterWorld(cellPosition);
        }

        isCollidingWithBuilding = false;
        isOutsideGrid = false;
        isBuildingMoving = false;
        currentBuildingData = null;
        ClearTileHighlights();
        SetupTriggerCollider(); // 콜라이더 크기 1x1 기본값으로 복원
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
            ghostRenderer.transform.localScale = data.prefab.transform.localScale;
            ghostRenderer.gameObject.SetActive(true);
        }

        if (gameManager != null)
        {
            gameManager.installingActivation = true;
            baseUI.CloseStoreButton();
        }
    }

    void buildingInstalling(Vector3Int currentCellPos)
    {
        if (CannotPlace)
        {
            Debug.LogWarning(isOutsideGrid ? "GridOverlay 범위 밖입니다. 설치할 수 없습니다!" : "여기는 건물이나 장애물이 있어 설치할 수 없습니다!");
            return;
        }

        // 관광객 초과 시 설치 불가
        int newCurrent = gameManager.CurrentTourists + currentBuildingData.touristIncrease;
        int newMax = gameManager.MaxTourists + currentBuildingData.maxTouristIncrease;
        if (newCurrent > newMax)
        {
            Debug.LogWarning("관광객 수용 한도를 초과하여 설치할 수 없습니다!");
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
        gameManager.AddTourists(currentBuildingData.touristIncrease, currentBuildingData.maxTouristIncrease);

        // Ghost·하이라이트 숨기고 설치 모드 종료
        if (ghostRenderer != null)
            ghostRenderer.gameObject.SetActive(false);
        ClearTileHighlights();

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

        Vector3 right = centerPos + new Vector3(horizontalRadius, 0, 0);
        Vector3 left = centerPos + new Vector3(-horizontalRadius, 0, 0);
        Vector3 top = centerPos + new Vector3(0, verticalRadius, 0);
        Vector3 bottom = centerPos + new Vector3(0, -verticalRadius, 0);

        Gizmos.DrawLine(top, right);
        Gizmos.DrawLine(right, bottom);
        Gizmos.DrawLine(bottom, left);
        Gizmos.DrawLine(left, top);
    }
}
