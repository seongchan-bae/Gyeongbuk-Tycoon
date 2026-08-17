using UnityEngine;

public class BaseUI : MonoBehaviour
{
    //상점화면
    [SerializeField]
    private GameObject store;
    //메인화면
    [SerializeField]
    private GameObject main;

    [SerializeField] private CardMatchingGame cardMatchingGameController; // CardGamePanel 오브젝트 연결
    [SerializeField] private YabawiGameUI YabawiGameController;

    //설정화면
    void settingUI()
    {

    }
    //상점화면
    public void ShowStoreUI()
    {
        store.SetActive(true);
        main.SetActive(false);
    }
    public void CloseStoreUI()
    {
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

    public void OpenCardMatchingGame()
    {

        if (cardMatchingGameController != null)
        {
            cardMatchingGameController.OpenGamePanel();
        }
    }

    public void OpenYabawiGame()
    {

        if (YabawiGameController != null)
        {
            YabawiGameController.OpenPanel();
        }
    }
}
