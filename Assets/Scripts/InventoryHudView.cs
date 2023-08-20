using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Economy.Model;

public class InventoryHudView : MonoBehaviour
{
    public GameObject inventoryItemPrefab;
    public Transform itemListParentTransform;

    public void Refresh(List<PlayersInventoryItem> playersInventoryItems)
    {
        // Check that scene has not been unloaded while processing async wait to prevent throw.
        if (inventoryItemPrefab == null || itemListParentTransform == null) return;

        RemoveAll();

        if (playersInventoryItems is null) return;

        foreach (var item in playersInventoryItems)
        {
            
            Debug.Log("<color=cyan>Inventory item: </color>" + item.GetItemDefinition().Name);
            var newInventoryItemGameObject = Instantiate(inventoryItemPrefab, itemListParentTransform);
            var inventoryItemView = newInventoryItemGameObject.GetComponent<InventoryItemView>();
            inventoryItemView.SetKey(item.InventoryItemId);
        }

        Debug.Log("Inventory items retrieved and updated. Total inventory item count: " +
                  $"{playersInventoryItems.Count}");
    }

    void RemoveAll()
    {
        while (itemListParentTransform.childCount > 0)
        {
            DestroyImmediate(itemListParentTransform.GetChild(0).gameObject);
        }
    }
}