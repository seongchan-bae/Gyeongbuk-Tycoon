using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using TMPro;

public class BuildingPopupUI : MonoBehaviour
{
    public static BuildingPopupUI Instance { get; private set; }

    [SerializeField] private RectTransform popupPanel;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button flipButton;
    [SerializeField] private TextMeshProUGUI buildingName;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BuildingData buildingData;
    

    [SerializeField] private BuildingInstall buildingInstall;

    [Header("TourAPI 설정")]
    [SerializeField] private string tourApiKey = "616315cd61c155564e9088acbc319ff980ccc75a67ed38601b3876602d23ee9d"; // data.go.kr 디코딩 키 입력
    [SerializeField] private GameObject infoPopupPanel;               // 관광 정보를 표시할 별도 패널
    [SerializeField] private TextMeshProUGUI infoText;                // 관광 정보 텍스트

    [Header("업그레이드 비용 텍스트")]
    [SerializeField] private TextMeshProUGUI upgradeCostText;

    [Header("건물 스탯 텍스트")]
    [SerializeField] private TextMeshProUGUI statGoldText;
    [SerializeField] private TextMeshProUGUI statTouristText;

    [Header("APIBoard 열릴 때 숨길 HUD")]
    [SerializeField] private GameObject goldUI;
    [SerializeField] private GameObject touristUI;
    [SerializeField] private GameObject knowledgeUI;
    

    private Building selectedBuilding;

    void Awake()
    {
        Instance = this;
        popupPanel.gameObject.SetActive(false);
        if (infoPopupPanel != null) infoPopupPanel.SetActive(false);

        // 팝업 배경이 클릭을 가로채지 않도록 Raycast Target 비활성화
        Image bg = popupPanel.GetComponent<Image>();
        if (bg != null) bg.raycastTarget = false;
    }

    void Update()
    {
        // 선택된 건물이 이동할 때 팝업 위치도 따라다님
        if (selectedBuilding != null && popupPanel.gameObject.activeSelf)
            UpdatePosition();

        // UI 버튼 외 클릭 시 팝업 닫기
        if (popupPanel.gameObject.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                WasHiddenThisClick = true;
                Hide();
            }
        }
        if (Input.GetMouseButtonUp(0))
            WasHiddenThisClick = false;
    }

    public void Show(Building building)
    {
        selectedBuilding = building;

        infoButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.RemoveAllListeners();
        deleteButton.onClick.RemoveAllListeners();
        if (flipButton != null) flipButton.onClick.RemoveAllListeners();

        infoButton.onClick.AddListener(OnInfoClicked);
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        deleteButton.onClick.AddListener(OnDeleteClicked);
        if (flipButton != null) flipButton.onClick.AddListener(OnFlipClicked);

        // upgradeTarget이 없으면 업그레이드 버튼/비용 텍스트 숨김
        bool canUpgrade = building != null && building.buildingData != null && building.buildingData.upgradeTarget != null;
        if (upgradeButton != null) upgradeButton.gameObject.SetActive(canUpgrade);
        if (upgradeCostText != null)
        {
            upgradeCostText.gameObject.SetActive(canUpgrade);
            if (canUpgrade) upgradeCostText.text = $"{building.buildingData.upgradeCost:N0} G";
        }

        popupPanel.gameObject.SetActive(true);
        if (buildingName != null)
        {
            // 인스펙터에 꽂힌 buildingData 는 특정 건물 한 개를 가리키고 있어서
            // 어떤 건물을 눌러도 같은 이름이 떴다. 실제로 누른 건물의 데이터를 먼저 쓴다.
            BuildingData data = building != null && building.buildingData != null
                ? building.buildingData
                : buildingData;
            buildingName.text = data != null ? data.buildingName : "";
        }
        UpdatePosition();
    }

    public static bool WasHiddenThisClick { get; private set; } = false;

    public void Hide()
    {
        popupPanel.gameObject.SetActive(false);
        selectedBuilding = null;
        if (infoPopupPanel != null)
        {
            infoPopupPanel.SetActive(false);
            SetHudVisible(true);
        }
    }

    void UpdatePosition()
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(selectedBuilding.transform.position);

        // 스크린 좌표 → Canvas 로컬 좌표 변환
        Canvas canvas = popupPanel.GetComponentInParent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos,
            canvas.worldCamera,
            out Vector2 localPos
        );
        popupPanel.localPosition = localPos + new Vector2(10f, -10f);
    }

    void OnInfoClicked()
    {
        string contentId = selectedBuilding?.buildingData?.contentId;

        // 액션 팝업만 먼저 닫기 (info 패널은 건드리지 않음)
        popupPanel.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(contentId))
        {
            selectedBuilding = null;
            StartCoroutine(FetchTourInfo(contentId));
        }
        else
        {
            string manual = selectedBuilding?.buildingData?.manualInfoText;
            // selectedBuilding을 null로 바꾸기 전에 ShowInfoText 호출
            ShowInfoText(!string.IsNullOrWhiteSpace(manual) ? manual : "");
            selectedBuilding = null;
        }
    }

    IEnumerator FetchTourInfo(string contentId)
    {
        // TourAPI - detailCommon1 (국문 공통정보) 엔드포인트
        string url =$"https://apis.data.go.kr/B551011/KorService2/detailCommon2" +
                    $"?serviceKey={tourApiKey}" +
                    $"&contentId={contentId}" +
                    $"&MobileOS=ETC" +
                    $"&MobileApp=GyeongbukTycoon" +
                    $"&_type=json" +
                    $"&numOfRows=10" +
                    $"&pageNo=1";
//numOfRows=10&pageNo=1
        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[TourAPI] 요청 실패: {req.error}");
            ShowInfoText("정보를 불러오지 못했습니다.");
            yield break;
        }

        string json = req.downloadHandler.text;
        string overview = FormatOverview(ParseOverview(json));

        if (string.IsNullOrEmpty(overview))
        {
            Debug.LogWarning("[TourAPI] overview 필드를 파싱하지 못했습니다.\n" + json);
            ShowInfoText("관광 정보가 없습니다.");
        }
        else
        {
            ShowInfoText(overview);
        }
    }
    string FormatOverview(string text){
    // 한자 필터링
    text = System.Text.RegularExpressions.Regex.Replace(text, @"\([\u4E00-\u9FFF]+\)", "");

    // 마침표 2개당 개행
    string[] sentences = text.Split(". ");
    System.Text.StringBuilder sb = new System.Text.StringBuilder();

    for (int i = 0; i < sentences.Length; i++){
        sb.Append(sentences[i]);
        if (i < sentences.Length - 1)
        {
            sb.Append(". ");
            if ((i + 1) % 2 == 0)
                sb.Append("\n");
        }
    }
        return sb.ToString();
    }
    // JsonUtility가 중첩 구조를 지원하지 않으므로 문자열 파싱으로 overview 추출
    string ParseOverview(string json)
    {
        const string key = "\"overview\":\"";
        int start = json.IndexOf(key);
        if (start < 0) return null;
        start += key.Length;
        int end = json.IndexOf("\"", start);
        if (end < 0) return null;
        return json.Substring(start, end - start)
                   .Replace("\\n", "\n")
                   .Replace("\\r", "")
                   .Replace("\\t", " ");
    }

    
    void PopulateStats()
    {
        BuildingData data = selectedBuilding?.buildingData;
        if (data == null) return;

        if (statGoldText      != null) statGoldText.text    = data.goldProductionRate.ToString("#,##0.##");
        if (statTouristText   != null) statTouristText.text = $"{data.touristIncrease:N0} / {data.maxTouristIncrease:N0}";
    }

    void SetHudVisible(bool visible)
    {
        if (goldUI     != null) goldUI.SetActive(visible);
        if (touristUI  != null) touristUI.SetActive(visible);
        if (knowledgeUI != null) knowledgeUI.SetActive(visible);
    }

    void ShowInfoText(string text)
    {
        if (infoPopupPanel != null)
        {
            infoPopupPanel.SetActive(true);
            SetHudVisible(false);
            PopulateStats();
            if (infoText != null)
            {
                infoText.text = "\n" + text;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(infoText.GetComponent<RectTransform>());
                RectTransform contentRect = infoText.transform.parent.GetComponent<RectTransform>();
                if (contentRect != null)
                    contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, infoText.preferredHeight + 50f);
            }
        }
        else
        {
            Debug.Log($"[TourAPI] {selectedBuilding?.buildingData?.buildingName}: {text}");
        }
    }

    void OnFlipClicked()
    {
        selectedBuilding?.ToggleFlip();
    }

    void OnUpgradeClicked()
    {
        BuildingData data = selectedBuilding?.buildingData;
        if (data == null || data.upgradeTarget == null) return;

        BuildingData target = data.upgradeTarget;

        if (!gameManager.SpendMoney(data.upgradeCost))
        {
            Debug.LogWarning($"[업그레이드] 골드 부족 (필요: {data.upgradeCost})");
            return;
        }

        // 현재 건물 위치 기록
        var install = buildingInstall != null ? buildingInstall : FindFirstObjectByType<BuildingInstall>();
        if (install == null) { Debug.LogError("[업그레이드] BuildingInstall을 찾을 수 없습니다."); return; }

        Vector3Int cellPos = install.BaseGrid.WorldToCell(selectedBuilding.transform.position);

        // 기존 건물 제거
        gameManager.RemoveTourists(0, data.maxTouristIncrease, data.touristIncrease);
        gameManager.UnregisterBuilding(data);
        install.FreeOccupiedCells(selectedBuilding.transform.position, data);

        Destroy(selectedBuilding.gameObject);
        selectedBuilding = null;
        Hide();

        // 새 건물 설치
        install.InstallBuildingAt(target, cellPos);

        Debug.Log($"[업그레이드] {data.buildingName} → {target.buildingName} (비용 {data.upgradeCost})");
    }

    void OnDeleteClicked()
    {
        BuildingData data = selectedBuilding.buildingData;

        gameManager.AddMoney(data.price / 2);
        gameManager.RemoveTourists(0, data.maxTouristIncrease, data.touristIncrease);
        gameManager.UnregisterBuilding(data);

        var buildingInstall = FindFirstObjectByType<BuildingInstall>();
        if (buildingInstall != null)
            buildingInstall.FreeOccupiedCells(selectedBuilding.transform.position, data);

#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = null;
#endif
        Destroy(selectedBuilding.gameObject);
        Hide();
        StartCoroutine(SaveAfterDestroy());
    }

    IEnumerator SaveAfterDestroy()
    {
        yield return null; // Destroy가 실제로 반영된 다음 프레임에 저장
        SaveManager.Instance?.SaveGameData();
    }
}
