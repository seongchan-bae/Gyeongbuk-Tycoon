using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapUpgrade : MonoBehaviour
{
    [SerializeField]
    GameObject deletedClouds;
    [SerializeField]
    GridOverlay gridOverlay;//gridOverlay를 입력받음
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
