using UnityEngine;
using UnityEngine.UI;


public class BaseUI : MonoBehaviour
{
    //상점화면
    [SerializeField] 
    private GameObject store;
    //메인화면
    [SerializeField] 
    private GameObject main;
    [SerializeField]
    private Button Shopbutton;
    
    //설정화면
    void settingUI()
    {

    }
    public void ShowStoreButton()
    {
        Shopbutton.gameObject.SetActive(true);
    }
    public void CloseStoreButton()
    {
        Shopbutton.gameObject.SetActive(false);
    }
    //상점화면
    public void ShowStoreUI()
    {
        BuildingPopupUI.Instance?.Hide();
        store.SetActive(true);
        Shopbutton.gameObject.SetActive(false);
    }
    public void CloseStoreUI()
    {
        store.SetActive(false);
        Shopbutton.gameObject.SetActive(true);
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
