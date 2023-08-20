using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance { get; private set; }

    public Dictionary<string, VirtualShopCategory> virtualShopCategories { get; private set; }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void Initialize()
    {
        virtualShopCategories = new Dictionary<string, VirtualShopCategory>();

        foreach (var categoryConfig in RemoteConfigManager.instance.virtualShopConfig.categories)
        {
            var virtualShopCategory = new VirtualShopCategory(categoryConfig);
            virtualShopCategories[categoryConfig.id] = virtualShopCategory;
        }
    }
}
