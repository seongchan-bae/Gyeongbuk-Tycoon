using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameLauncher : MonoBehaviour
{
    [SerializeField] private string silueteGameSceneName = "SilueteGameScene";

    public void LaunchSilueteGame()
    {
        SceneManager.LoadScene(silueteGameSceneName);
    }
}
