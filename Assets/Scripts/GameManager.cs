using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 유저가 가지고 있는 돈(GameManager에서만 관리)
    private long userMoney = 0L;

    // 유저가 가지고 있는 지식포인트(GameManager에서만 관리)
    private long userKnowledgePoint = 0L;

    [SerializeField]
    public bool installingActivation = true;
    [SerializeField]
    public bool destroyingActivation = false;
    [SerializeField] private GameObject puzzleUI;
    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject HintPopupUI;

    //유저머니 추가
    void addUserMoney(long money)
    {
        userMoney += money;    
    }
    //유저머니 차감
    void subUserMoney(long money)
    {
        userMoney -= money;
    }
    //유저 지식포인트 추가
    void addUserKnowledgePoint(long knowledgePoint)
    {
        userKnowledgePoint += knowledgePoint;    
    }
    //유저 지식포인트 차감
    void subUserknowledgePoint(long knowledgePoint)
    {
        userKnowledgePoint -= knowledgePoint;
    }
    /// <summary>
    /// 로딩 화면을 띄워주는 함수
    /// </summary>
    void showLoadingUI()
    {
        
    }
    
    


    

    // "미니게임" 버튼을 눌렀을 때 실행될 함수
    public void OpenPuzzleUI()
    {

        if (puzzleUI != null)
        {
            puzzleUI.SetActive(true); // PuzzleUI 활성화 (화면에 표시)
        }
        mainUI.SetActive(false);
    }

    // 퍼즐 UI 내의 "닫기(X)" 버튼 등에 연결할 함수
    public void ClosePuzzleUI()
    {
        if (puzzleUI != null)
        {
            puzzleUI.SetActive(false); // PuzzleUI 비활성화 (화면에서 숨김)
        }
        mainUI.SetActive(true);
    }

    public void OpenHintPopupUI()
    {

        if (HintPopupUI != null)
        {
            HintPopupUI.SetActive(true); 
        }
       
    }
    public void CloseHintPopupUI()
    {

        if (HintPopupUI != null)
        {
            HintPopupUI.SetActive(false); 
        }
       
    }
}


