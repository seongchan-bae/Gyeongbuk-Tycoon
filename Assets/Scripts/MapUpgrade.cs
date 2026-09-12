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
        if (SaveManager.Instance != null)
            currentStep = SaveManager.Instance.CurrentData.mapUpgradeStep;

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

    void UpdateButtons()
    {
        // 현재 단계 다음 버튼만 활성화, 나머지는 비활성화
        if (step2Button != null) step2Button.gameObject.SetActive(currentStep == 1);
        if (step3Button != null) step3Button.gameObject.SetActive(currentStep == 2);
        if (step4Button != null) step4Button.gameObject.SetActive(currentStep == 3);
    }

    void RestoreGrid()
    {
        if (currentStep == 1)
        {
            // 1단계 초기 그리드 (원본 주석 코드 활용)
            gridOverlay.DeleteGrid();
            gridOverlay.changeGridValue(17, 17);
            gridOverlay.changePositionValue(-9, 21);
            gridOverlay.DrawGrid();
        }
        else
        {
            UpgradingMap(currentStep);
        }
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
                deletedClouds.transform.Find("DeletedAt2Step").gameObject.SetActive(false);
                break;
            case 3:
                gridOverlay.changeGridValue(27,27);
                gridOverlay.changePositionValue(-14,16);
                deletedClouds.transform.Find("DeletedAt3Step").gameObject.SetActive(false);
                break;
            case 4:
                gridOverlay.changeGridValue(32,32);
                gridOverlay.changePositionValue(-17,13);
                deletedClouds.transform.Find("DeletedAt4Step").gameObject.SetActive(false);
                break;
        }
        gridOverlay.DrawGrid();
    }
}
