using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

public class MapUpgrade : MonoBehaviour
{
    [SerializeField]
    GameObject deletedClouds;
    [SerializeField]
    GridOverlay gridOverlay;//gridOverlay를 입력받음

    [Header("단계별 업그레이드 버튼")]
    [SerializeField] private Button step2Button;
    [SerializeField] private Button step3Button;
    [SerializeField] private Button step4Button;

    [Header("단계별 잠금 이미지")]
    [SerializeField] private GameObject step2LockImage;
    [SerializeField] private GameObject step3LockImage;
    [SerializeField] private GameObject step4LockImage;

    [Header("단계별 비용 텍스트")]
    [SerializeField] private TextMeshProUGUI step2CostText;
    [SerializeField] private TextMeshProUGUI step3CostText;
    [SerializeField] private TextMeshProUGUI step4CostText;

    [Header("단계별 업그레이드 비용")]
    [SerializeField] private long step2Cost = 50000L;
    [SerializeField] private long step3Cost = 150000L;
    [SerializeField] private long step4Cost = 300000L;

    private int currentStep = 1;

    void Start()
    {
        currentStep = (SaveManager.Instance != null)
            ? SaveManager.Instance.CurrentData.mapUpgradeStep
            : 1;

        RestoreGrid();

        step2Button?.onClick.AddListener(() => TryUpgrade(2));
        step3Button?.onClick.AddListener(() => TryUpgrade(3));
        step4Button?.onClick.AddListener(() => TryUpgrade(4));

        if (step2CostText != null) step2CostText.text = $"{step2Cost:N0} G";
        if (step3CostText != null) step3CostText.text = $"{step3Cost:N0} G";
        if (step4CostText != null) step4CostText.text = $"{step4Cost:N0} G";

        UpdateButtons();
    }

    void TryUpgrade(int targetStep)
    {
        if (targetStep != currentStep + 1) return;

        long cost = targetStep == 2 ? step2Cost : targetStep == 3 ? step3Cost : step4Cost;
        if (!GameManager.Instance.SpendMoney(cost))
        {
            Debug.Log($"[MapUpgrade] 골드 부족 (필요: {cost:N0})");
            return;
        }

        currentStep = targetStep;
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentData.mapUpgradeStep = currentStep;
            SaveManager.Instance.SaveGameData();
        }
        GameManager.Instance?.SetStage(currentStep - 1);
        UpgradingMap(currentStep);
        UpdateButtons();
    }

    void OnEnable()
    {
        UpdateButtons();
    }

    void UpdateButtons()
    {
        SetButtonLock(step2Button, step2LockImage, currentStep != 1);
        SetButtonLock(step3Button, step3LockImage, currentStep != 2);
        SetButtonLock(step4Button, step4LockImage, currentStep != 3);
    }

    void SetButtonLock(Button btn, GameObject lockImage, bool locked)
    {
        if (btn != null)
        {
            btn.interactable = !locked;
            // CanvasGroup으로 raycast 자체 차단 (interactable만으로 안 막히는 경우 대비)
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = !locked;
            cg.blocksRaycasts = !locked;
        }
        if (lockImage != null) lockImage.SetActive(locked);
    }

    void RestoreGrid()
    {
        gridOverlay.DeleteGrid();

        if (currentStep == 1)
        {
            gridOverlay.changeGridValue(17, 17);
            gridOverlay.changePositionValue(-9, 21);
        }
        else
        {
            // 2단계부터 현재 단계까지 순회 — 구름 전부 숨기고 그리드는 마지막 값으로 확장
            for (int step = 2; step <= currentStep; step++)
            {
                switch (step)
                {
                    case 2:
                        gridOverlay.changeGridValue(22, 22);
                        gridOverlay.changePositionValue(-12, 18);
                        deletedClouds.transform.Find("DeletedAt2Step")?.gameObject.SetActive(false);
                        break;
                    case 3:
                        gridOverlay.changeGridValue(27, 27);
                        gridOverlay.changePositionValue(-14, 16);
                        deletedClouds.transform.Find("DeletedAt3Step")?.gameObject.SetActive(false);
                        break;
                    case 4:
                        gridOverlay.changeGridValue(32, 32);
                        gridOverlay.changePositionValue(-17, 13);
                        deletedClouds.transform.Find("DeletedAt4Step")?.gameObject.SetActive(false);
                        break;
                }
            }
        }

        gridOverlay.DrawGrid();
    }

    public void UpgradingMap(int step)
    {
       gridOverlay.DeleteGrid();
        switch (step)
        {
            // case 1:
            //     gridOverlay.changeGridValue(17,17);
            //     gridOverlay.changePositionValue(-9,21);
            //     deletedClouds.transform.Find("DeletedAt2Step").gameObject.SetActive(false);
            //     break;
            case 2:
                gridOverlay.changeGridValue(22,22);
                gridOverlay.changePositionValue(-12,18);
                deletedClouds.transform.Find("DeletedAt2Step")?.gameObject.SetActive(false);
                break;
            case 3:
                gridOverlay.changeGridValue(27,27);
                gridOverlay.changePositionValue(-14,16);
                deletedClouds.transform.Find("DeletedAt3Step")?.gameObject.SetActive(false);
                break;
            case 4:
                gridOverlay.changeGridValue(32,32);
                gridOverlay.changePositionValue(-17,13);
                deletedClouds.transform.Find("DeletedAt4Step")?.gameObject.SetActive(false);
                break;
        }
        gridOverlay.DrawGrid();
    }
}
