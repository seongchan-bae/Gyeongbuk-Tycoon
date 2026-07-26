using UnityEngine;
using TMPro;

public class Store : MonoBehaviour
{
    public BuildingData[] buildingLists; //상점에서 보여질 건물 리스트

    [Header("상점 UI 패널 및 슬롯 컨테이너")]
    public GameObject storeMainPanel;               // 상점 전체 팝업 패널
    [SerializeField] private GameObject storeSlotPrefab; // UI 카드 프리팹 (BuildingSlot)
    [SerializeField] private Transform slotContainer;    // ScrollView -> Viewport -> Content


    [Header("재화 UI 텍스트")]
    [SerializeField] private TextMeshProUGUI moneyText;

    void Start()
    {
        ///<summary>
        ///시작시 상점 UI창 꺼짐상태
        ///</summary>        
        if (storeMainPanel != null)
            storeMainPanel.SetActive(false);

        UpdateCurrencyUI();
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

        if (storeMainPanel != null)
            storeMainPanel.SetActive(true);

        UpdateCurrencyUI(); // 상점 켜질 때 재화 UI 갱신
        GenerateStoreSlots(); // 건물 정보를 가져와 슬롯 동적 생성
    }

    private void GenerateStoreSlots()
    {

        if (storeSlotPrefab == null || slotContainer == null) return;
        // 기존 슬롯 청소 (중복 생성 방지)
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        // buildingLists 배열의 모든 건물 개수만큼 UI 슬롯 생성
        foreach (BuildingData building in buildingLists)
        {
            if (building == null) continue;

            // 💡 세 번째 인자로 false (worldPositionStays = false) 추가!
            GameObject slotObj = Instantiate(storeSlotPrefab, slotContainer, false);

            StoreSlotUI slotUI = slotObj.GetComponent<StoreSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(building);
            }
        }
    }

    public void CloseStoreMainUI()
    {
        if (storeMainPanel != null)
            storeMainPanel.SetActive(false);
    }

    public void UpdateCurrencyUI()
    {
        Debug.Log(GameManager.Instance.UserMoney);
        if (moneyText != null && GameManager.Instance != null)
        {
            Debug.Log(GameManager.Instance.UserMoney);
            moneyText.text = $"{GameManager.Instance.UserMoney:N0} Gold";
        }
    }

    /// <summary>
    /// 퀘스트 등을 수행하지 않아 아직 잡겨 있는 건물의 lock을 풀어주는 함수
    /// </summary>
    void unlockBuilding()
    {

    }
}
