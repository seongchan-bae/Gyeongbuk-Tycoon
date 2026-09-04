using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬을 바꿀 때 화면이 순간적으로 멎는 것을 가려주는 페이드 전환.
///
/// SceneManager.LoadScene 은 동기 호출이라 씬을 다 읽을 때까지 한 프레임이 통째로 멈춘다.
/// 미니게임 씬처럼 로드할 게 많으면 이 멈춤이 눈에 띄게 '끊김'으로 보인다.
/// 여기서는 LoadSceneAsync 로 백그라운드에서 읽고, 그 앞뒤를 검은 화면 페이드로 덮는다.
///
/// 인스펙터 연결이 필요 없도록 페이드용 캔버스는 코드로 만들고
/// DontDestroyOnLoad 로 씬 전환 내내 살려둔다.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    // 페이드 한쪽 방향에 걸리는 시간(초). 나가고 들어오는 데 각각 이만큼 쓴다.
    public const float FadeDuration = 0.25f;

    private static SceneTransition instance;
    private CanvasGroup group;
    private bool busy;

    /// <summary>페이드를 곁들여 씬을 전환한다. 이미 전환 중이면 무시한다.</summary>
    public static void LoadScene(string sceneName, System.Action beforeLoad = null)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        SceneTransition t = Ensure();
        if (t.busy) return;
        t.StartCoroutine(t.Run(sceneName, beforeLoad));
    }

    private static SceneTransition Ensure()
    {
        if (instance != null) return instance;

        GameObject root = new GameObject("SceneTransition");
        DontDestroyOnLoad(root);

        // 다른 어떤 UI보다도 위에 오도록 정렬 순서를 크게 잡는다.
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasGroup cg = root.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;   // 페이드 중이 아닐 땐 클릭을 가로채면 안 된다

        GameObject fill = new GameObject("Fade");
        fill.transform.SetParent(root.transform, false);
        Image img = fill.AddComponent<Image>();
        img.color = Color.black;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        instance = root.AddComponent<SceneTransition>();
        instance.group = cg;
        return instance;
    }

    private IEnumerator Run(string sceneName, System.Action beforeLoad)
    {
        busy = true;
        group.blocksRaycasts = true;    // 전환 중 두 번 눌리는 것을 막는다

        yield return Fade(0f, 1f);

        // 저장 등 씬을 떠나기 직전에 해야 할 일은 화면이 완전히 가려진 뒤에 처리한다.
        if (beforeLoad != null) beforeLoad();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // LoadSceneAsync 는 0.9 에서 멈춰 서서 활성화 허가를 기다린다.
        while (op.progress < 0.9f) yield return null;

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        // 새 씬의 Awake/Start 가 한 번 돌고 첫 프레임이 그려질 때까지 검은 화면을 유지한다.
        yield return null;
        yield return new WaitForEndOfFrame();

        yield return Fade(1f, 0f);

        group.blocksRaycasts = false;
        busy = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        group.alpha = from;
        while (t < FadeDuration)
        {
            // 씬 로드 중에는 Time.timeScale 이 어떻든 일정하게 흘러야 하므로 unscaled 를 쓴다.
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / FadeDuration);
            yield return null;
        }
        group.alpha = to;
    }
}
