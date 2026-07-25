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
        store.purchase(building);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
