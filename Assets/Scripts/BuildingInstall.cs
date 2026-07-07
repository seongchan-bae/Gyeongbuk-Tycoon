using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
public class BuildingInstall : MonoBehaviour
{

    [SerializeField]
    //Grid 오브젝트
    private Grid baseGrid;
    [SerializeField]
    //타일맵을 얻어올 대상 변수
    private Tilemap baseTilemap;

    [SerializeField]
    //설치대상 건물
    private TileBase tileAsset;
    //인게임 마우스 포지션
    Vector3 mouseWorldPos;
    //인게임 마우스 포지션을 타일맵에 맞춰 격자좌표계로 변환한 함수
    Vector3Int cellPosition;

    GameManager gameManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        //현재 마우스 포지션을 인게임 좌표로 변환해 저장
        mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        //받아온 마우스 포지션을 타일맵 격자에 호환시킴(격자좌표계로 변환)
        cellPosition = baseGrid.WorldToCell(mouseWorldPos);

        //현재 건물 설치 기능이 활성화되어있는지 검사
        if (gameManager.installingActivation())
        {
            
            //좌클릭 감지시
            if (Input.GetMouseButtonDown(0))
            {
                buildingInstalling(cellPosition);
            }
        }
        //현재 삭제 기능이 활성화되어 있는지 검사
        if (gameManager.destroyingActivation())
        {
            
            //좌클릭 감지시
            if (Input.GetMouseButtonDown(0))
            {
                buildingInstalling(cellPosition);
            }
        }
            

        

        
    }



    
    /// <summary>
    ///건물을 설치하는 함수
    /// </summary>
    /// <param name="currentMousePos">
    /// 현재 마우스 포지션을 인자로 받음. 
    /// </param>
    void buildingInstalling(Vector3 currentMousePos){
        

        //건물을 설치하려는 대상 타일이 점유되어있을 경우
        if(isOccupiedArea()){
            //건물 근처를 빨갛게 빛낸다던지 해서 건물을 설치할 수 없다는 표시를 함.
        }
        //건물을 설치하려는 대상 타일이 점유되어있지 않을 경우
        else {
            baseTilemap.SetTile(cellPosition, tileAsset);
        }

    }
    

    
    /// <summary>
    /// 건물을 클릭하면 해당 건물을 하이라이트 처리한뒤 '건물을 삭제하시겠습니까?' 팝업을 띄우고 
    /// '아니오'를 클릭하면 그대로 동작종료. '예'를 클릭하면 건물을 삭제시키는 함수
    /// </summary>
    void buildingUninstalling(){
        //해당 칸의 타일을 제거
        baseTilemap.SetTile(cellPosition, null);
    }
    /// <summary>
    ///현재 타일이 점유된 상태인지를 loop를 통해 모두 검사. 설치된 건물의 타일(x * y)수만큼 반복.
    ///ex) 5*5 크기의 건물을 설치하려 한다면 현재 마우스 포지션은 그 건물의 중앙 타일을 가져오게되고, 그 위치 기준
    ///총 25개의 타일에 건물이 있는지 검사. 있으면 true 없으면 false    
    /// </summary>
    /// <returns>
    /// true: 건물이 이미 설치된 타일공간이다.
    /// false: 건물이 없는, 설치가 가능한 타일공간이다.
    /// </returns>
    
    bool isOccupiedArea(){
        //타일을 검사하며 타일이 설치가 불가능할 경우 true로 바뀌는 변수
        bool tileChecker = false;
        Building building = GetComponent<Building>();
        // 1. 가로(X축) 범위 계산
        int minX = -(building.tileWidth / 2);
        int maxX = (building.tileWidth - 1) / 2;

        // 2. 세로(Y축) 범위 계산
        int minY = -(building.tileHeight / 2);
        int maxY = (building.tileHeight - 1) / 2;
        //for문을 이용해 건물의 크기만큼 루프를 돌림. 루프를 돌리는 도중 
        //tileChecker가 검사 도중에 true가 될 경우 바로 루프 중단
        //이렇게 작성할 경우, 정확히 건물의 가로, 세로 타일의 영역만을 검사
        for(int width = minX; width < maxY; width++)
        {
            for(int height = minY; height < maxY ; height++)
            {
                if (baseTilemap.HasTile(cellPosition))
                {
                    tileChecker = true;
                    break;
                }
            }
            if(tileChecker) break;
        }
        
        return tileChecker;
    }
    
}
