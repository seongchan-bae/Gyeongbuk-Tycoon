using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인화면(SampleScene)과 미니게임 씬(MiniGameScene)을 오가는 다리 역할.
///
/// 미니게임 UI는 갈래가 많아 SampleScene 안에 통째로 옮기는 대신 별도 씬으로 분리했다.
/// 재화(골드/지식포인트)는 SaveManager 가 DontDestroyOnLoad 로 살아남기 때문에
/// 씬을 오가도 유지된다.
///
/// 사용법
///  - SampleScene : 빈 오브젝트에 붙이고 [미니게임] 버튼 OnClick 에 OpenMiniGameScene() 연결
///  - MiniGameScene : 빈 오브젝트에 붙이고 openHubOnStart 체크,
///                    허브의 [닫기] 버튼 OnClick 에 ReturnToMainScene() 연결
/// </summary>
public class MiniGameSceneBridge : MonoBehaviour
{
    [Header("씬 이름 (Build Settings 에 등록되어 있어야 함)")]
    [SerializeField] private string mainSceneName = "SampleScene";
    [SerializeField] private string miniGameSceneName = "MiniGameScene";

    [Header("미니게임 씬에서만 체크")]
    [Tooltip("켜면 이 씬이 시작될 때 미니게임 선택 허브를 자동으로 연다.")]
    [SerializeField] private bool openHubOnStart = false;

    private IEnumerator Start()
    {
        if (!openHubOnStart) yield break;

        // MiniGameHubUI.Start() 가 허브 패널을 한 번 닫으므로 한 프레임 뒤에 연다.
        yield return null;

        if (MiniGameHubUI.Instance != null)
        {
            MiniGameHubUI.Instance.OpenHub();
        }
        else
        {
            Debug.LogWarning("[MiniGameSceneBridge] 씬에서 MiniGameHubUI를 찾지 못했습니다.");
        }
    }

    /// <summary>메인화면의 [미니게임] 버튼에서 호출.</summary>
    public void OpenMiniGameScene()
    {
        SaveBeforeSceneChange();
        SceneManager.LoadScene(miniGameSceneName);
    }

    /// <summary>미니게임 씬의 [닫기] / [메인으로] 버튼에서 호출.</summary>
    public void ReturnToMainScene()
    {
        SaveBeforeSceneChange();
        SceneManager.LoadScene(mainSceneName);
    }

    // 씬을 떠나기 전에 현재 재화를 SaveManager 에 밀어넣는다.
    // GameManager 는 씬마다 새로 생기지만 SaveManager 는 살아남으므로,
    // 이걸 거쳐야 미니게임에서 번 보상이 메인화면으로 넘어간다.
    private void SaveBeforeSceneChange()
    {
        if (SaveManager.Instance == null || GameManager.Instance == null) return;

        SaveManager.Instance.CurrentData.userMoney = GameManager.Instance.UserMoney;
        SaveManager.Instance.CurrentData.userKnowledgePoint = GameManager.Instance.UserKnowledgePoint;
    }
}
