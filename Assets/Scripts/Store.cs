using UnityEngine;

public class Store : MonoBehaviour
{
    [SerializeField]
    public Building[] buildingLists; //상점에서 보여질 건물 리스트 드래그해서 직접 추가하기
    public Building buildingSelect;  // 상점에서 선택한 건물, UI화면으로 옮겨질 것
    public BaseUI mainUI;
    public BuildingInstall bInstall;
    // void Update()
    // {    
    // }

    public void purchase(Building building)
    {
         Debug.Log("purchase");
        buildingSelect = building;
        mainUI.CloseStoreUI();
        bInstall.StartPlacement(building);
    } 
    /// <summary>
    /// 퀘스트 등을 수행하지 않아 아직 잡겨 있는 건물의 lock을 풀어주는 함수
    /// </summary>
    void unlockBuilding()
    {
        
    }
}
