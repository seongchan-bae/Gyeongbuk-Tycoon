using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class BuildingPopupUI : MonoBehaviour
{
    public static BuildingPopupUI Instance { get; private set; }

    [SerializeField] private RectTransform popupPanel;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TextMeshProUGUI buildingName;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BuildingData buildingData;
    

    [Header("TourAPI 설정")]
    [SerializeField] private string tourApiKey = "616315cd61c155564e9088acbc319ff980ccc75a67ed38601b3876602d23ee9d"; // data.go.kr 디코딩 키 입력
    [SerializeField] private GameObject infoPopupPanel;               // 관광 정보를 표시할 별도 패널
    [SerializeField] private TextMeshProUGUI infoText;                // 관광 정보 텍스트
    

    private Building selectedBuilding;

    void Awake()
    {
        Instance = this;
        popupPanel.gameObject.SetActive(false);
        if (infoPopupPanel != null) infoPopupPanel.SetActive(false);
    }

    void Update()
    {
        // 선택된 건물이 이동할 때 팝업 위치도 따라다님
        if (selectedBuilding != null && popupPanel.gameObject.activeSelf)
            UpdatePosition();
    }

    public void Show(Building building)
    {
        selectedBuilding = building;

        infoButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.RemoveAllListeners();
        deleteButton.onClick.RemoveAllListeners();

        infoButton.onClick.AddListener(OnInfoClicked);
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        deleteButton.onClick.AddListener(OnDeleteClicked);

        popupPanel.gameObject.SetActive(true);
        if(buildingName != null)
        {
            buildingName.text = buildingData.buildingName;
        }
        UpdatePosition();
    }

    public void Hide()
    {
        popupPanel.gameObject.SetActive(false);
        selectedBuilding = null;
        if (infoPopupPanel != null) infoPopupPanel.SetActive(false);
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
        if (string.IsNullOrEmpty(contentId))
        {
            Debug.LogWarning($"[Popup] {selectedBuilding?.buildingData?.buildingName}에 contentId가 설정되지 않았습니다.");
            return;
        }
        StartCoroutine(FetchTourInfo(contentId));
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

    
    void ShowInfoText(string text)
    {
        if (infoPopupPanel != null)
        {
            infoPopupPanel.SetActive(true);
            if (infoText != null)
                infoText.text = text;
        }
        else
        {
            Debug.Log($"[TourAPI] {selectedBuilding?.buildingData?.buildingName}: {text}");
        }
    }

    void OnUpgradeClicked()
    {
        Debug.Log($"[Popup] 업그레이드: {selectedBuilding?.buildingData?.buildingName}");
    }

    void OnDeleteClicked()
    {
        long sellMoney = buildingData.price/2;
        gameManager.AddMoney(sellMoney);
        Destroy(selectedBuilding.gameObject);
        Hide();
    }
}
