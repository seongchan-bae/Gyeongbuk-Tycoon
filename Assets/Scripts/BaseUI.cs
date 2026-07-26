using UnityEngine;
using TMPro;

public class BaseUI : MonoBehaviour
{
    //상점화면
    [SerializeField]
    private GameObject store;
    //메인화면
    [SerializeField]
    private GameObject main;

    [SerializeField] private Store storeScript;  // Store.cs 스크립트 참조

    [Header("메인 화면 재화 UI")]
    [SerializeField] private GameObject mainCurrencyPanel;   // MainCurrencyPanel (필요 시 켜고 끌 용도)
    [SerializeField] private TextMeshProUGUI mainMoneyText;  // MainMoneyText (실제 돈 글자)

    private void Start()
    {
        ShowMainUI();
    }

    /// <summary>
    /// 메인 화면 켜기 및 돈 갱신
    /// </summary>
    public void ShowMainUI()
    {
        if (main != null) main.SetActive(true);
        if (store != null) store.SetActive(false);

        UpdateMainMoneyUI();
    }

    /// <summary>
    /// 메인 화면 재화 텍스트 실시간 업데이트
    /// </summary>
    public void UpdateMainMoneyUI()
    {
        if (mainMoneyText == null) return;

        if (GameManager.Instance != null)
        {
            mainMoneyText.text = $"{GameManager.Instance.UserMoney:N0} Gold";
        }
    }

    //설정화면
    void settingUI()
    {

    }
    //상점화면
    public void ShowStoreUI()
    {
        store.SetActive(true);
        main.SetActive(false);

        if (storeScript != null)
        {
            storeScript.ShowStoreMainUI();
        }
    }
    public void CloseStoreUI()
    {

        if (storeScript != null)
        {
            storeScript.CloseStoreMainUI();
        }

        store.SetActive(false);
        main.SetActive(true);
    }
    //연구소화면
    void laboratoryUI()
    {

    }
    //유저정보화면(레벨(진척도), 현재 골드량, 해금건물 개수 등등)
    void userInformationUI()
    {

    }
}
