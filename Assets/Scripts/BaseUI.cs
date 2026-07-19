using UnityEngine;

public class BaseUI : MonoBehaviour
{
    public Store store;

    //설정화면
    void settingUI()
    {
        
    }
    //상점화면
    public void ShowStoreUI()
    {
        store.ShowStoreMainUI();
    }
    public void CloseStoreUI()
    {
        store.CloseStoreMainUI();
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
