using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화면을 덮는 창이 열려 있는 동안 이 오브젝트(HUD 버튼 줄)를 숨긴다.
///
/// 버튼 줄이 올라가 있는 캔버스는 정렬 순서가 높아서 상점처럼 전체 화면을 덮는
/// 창 위에도 그대로 보인다. 창을 여는 코드마다 숨김 처리를 넣으면 새 창이
/// 생길 때 빠뜨리기 쉬우므로, 감시할 창 목록을 두고 상태를 따라가게 했다.
///
/// 자기 자신을 끄면 이 스크립트도 멈추므로, 끄는 대상은 자식(버튼들을 담은
/// 컨테이너)이 아니라 이 오브젝트 아래의 <see cref="content"/> 로 지정한다.
/// </summary>
public class HudAutoHide : MonoBehaviour
{
    [Tooltip("실제로 숨길 대상. 비워두면 이 오브젝트의 모든 자식을 숨긴다.")]
    [SerializeField] private GameObject content;

    [Tooltip("이 중 하나라도 켜져 있으면 HUD 를 숨긴다.")]
    [SerializeField] private List<GameObject> hideWhileActive = new List<GameObject>();

    private bool lastShouldHide;
    private bool initialized;

    private void OnEnable()
    {
        initialized = false;
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void Apply()
    {
        bool shouldHide = false;
        for (int i = 0; i < hideWhileActive.Count; i++)
        {
            GameObject go = hideWhileActive[i];
            if (go != null && go.activeInHierarchy) { shouldHide = true; break; }
        }

        // 상태가 바뀔 때만 건드린다. 매 프레임 SetActive 를 호출하면
        // 자식들의 OnEnable 이 계속 다시 돌 수 있다.
        if (initialized && shouldHide == lastShouldHide) return;
        lastShouldHide = shouldHide;
        initialized = true;

        if (content != null)
        {
            content.SetActive(!shouldHide);
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(!shouldHide);
    }
}
