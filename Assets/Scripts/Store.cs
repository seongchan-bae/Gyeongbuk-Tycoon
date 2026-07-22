using UnityEngine;

public class Store : MonoBehaviour
{
    public Building[] buildingLists; //상점에서 보여질 건물 리스트
    public Building buildingSelect;  // 상점에서 선택한 건물, UI화면으로 옮겨질 것
    [Header("UI")]
    public GameObject storeMainPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ///<summary>
        ///시작시 상점 UI창 꺼짐상태
        ///</summary>        
        storeMainPanel.SetActive(false);
    }

    // // Update is called once per frame
    // void Update()
    // {
        
    // }

    /// <summary>
    /// 상점 초기화면(건물이나 여러 메뉴가 띄워져 있는 화면)을 팝업으로 출력해주는 함수
    /// </summary>
    public void ShowStoreMainUI()
    {
        
        ///<summary>
        ///시작시 상점 UI창 켜짐상태
        ///</summary>
        storeMainPanel.SetActive(true);
    }

    public void CloseStoreMainUI()
    {
        storeMainPanel.SetActive(false);
    }
    /// <summary>
    /// 퀘스트 등을 수행하지 않아 아직 잡겨 있는 건물의 lock을 풀어주는 함수
    /// </summary>
    void unlockBuilding()
    {
        
    }
}
