using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 유저가 가지고 있는 돈(GameManager에서만 관리)
    private long userMoney = 0L;

    // 유저가 가지고 있는 지식포인트(GameManager에서만 관리)
    private long userKnowledgePoint = 0L;

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
    /// <summary>
    /// 건물 설치 기능 활성화 상태. 함수 호출 시 건물 설치 기능활성화
    /// </summary>
    /// <returns>
    /// 현재 활성화 상태면 true
    /// </returns>
    public bool installingActivation()
    {
        //dummy return value
        return false;
    }
    /// <summary>
    /// 건물 삭제 기능 활성화 상태. 함수 호출 시 건물 삭제 기능 활성화
    /// </summary>
    /// <returns>
    /// 현재 활성화 상태면 true
    /// </returns>
    public bool destroyingActivation()
    {
        //dummy return value
        return false;
    }


}
