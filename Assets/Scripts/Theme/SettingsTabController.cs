using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 환경설정 창의 탭 전환. 기본 설정(음량) / 테마 설정 패널을 서로 배타적으로 보여준다.
/// </summary>
public class SettingsTabController : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public Button button;
        public GameObject panel;
    }

    [SerializeField] private Tab[] tabs;
    [SerializeField] private Color activeTabColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(0.72f, 0.72f, 0.72f, 1f);

    private void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            if (tabs[i].button != null)
            {
                tabs[i].button.onClick.RemoveAllListeners();
                tabs[i].button.onClick.AddListener(() => SelectTab(index));
            }
        }
    }

    private void OnEnable()
    {
        SelectTab(0); // 창을 열 때는 항상 기본 설정부터
    }

    public void SelectTab(int index)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = (i == index);
            if (tabs[i].panel != null) tabs[i].panel.SetActive(active);

            Image img = tabs[i].button != null ? tabs[i].button.GetComponent<Image>() : null;
            if (img != null) img.color = active ? activeTabColor : inactiveTabColor;
        }
    }
}
