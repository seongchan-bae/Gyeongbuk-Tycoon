using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class StoreSlotUI : MonoBehaviour
{
    [Header("슬롯 UI 연결")]
    [SerializeField] private Image buildingIconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button buyButton;

    [Header("해금 연출 컴포넌트")]
    [SerializeField] private CanvasGroup canvasGroup;  // 슬롯 전체 투명도 조절용

    private BuildingData currentBuilding;

    // 건물 데이터 세팅 함수
    public void Setup(BuildingData building)
    {
        currentBuilding = building;

        if (nameText != null) nameText.text = building.buildingName;

        if (buildingIconImage != null && building.buildingIcon != null)
        {
            buildingIconImage.sprite = building.buildingIcon;
        }

        SetLockState(building.isUnlocked);
    }

    // 미해금 시 흐리게 연출 및 구매 버튼 잠금
    public void SetLockState(bool isUnlocked)
    {
        // 미해금이면 슬롯 전체를 흐리게(0.5), 해금이면 선명하게(1.0)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isUnlocked ? 1.0f : 0.5f;
        }


        // 미해금 상태면 구매 버튼 터치 불가능
        if (buyButton != null)
            buyButton.interactable = isUnlocked;
    }

    public void OnClickUnlockTest()
    {
        if (currentBuilding == null) return;

        // 1. 현재 건물의 잠금 상태를 반전 (!isUnlocked)
        currentBuilding.isUnlocked = !currentBuilding.isUnlocked;

        // 2. 변경된 상태를 UI(투명도, 구매버튼 활성화)에 즉시 반영
        SetLockState(currentBuilding.isUnlocked);

        // 3. 디버그 확인 로그 (선택 사항)
        Debug.Log($"[{currentBuilding.buildingName}] 잠금 해제 상태: {currentBuilding.isUnlocked}");
    }

    public void OnClickBuyButton()
    {
        //
    }
}