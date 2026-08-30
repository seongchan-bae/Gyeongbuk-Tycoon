using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingButton : MonoBehaviour
{
    [SerializeField]
    public Building building;
    [SerializeField]
    public Store store;
    public void OnClickPurchase()
    {
        Debug.Log("Purchase Click!");
        if (store == null || building == null || building.buildingData == null)
        {
            Debug.LogError("BuildingButton: store/building(buildingData)가 Inspector에 연결되지 않았습니다.");
            return;
        }
        store.BuyBuilding(building.buildingData);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
