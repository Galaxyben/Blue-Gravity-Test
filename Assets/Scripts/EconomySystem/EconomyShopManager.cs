using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Services.Economy.Model;

public class EconomyShopManager : MonoBehaviour
{
    public static EconomyShopManager instance { get; private set; }

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
        print("Shop Manager Initializing");
        virtualShopCategories = new Dictionary<string, VirtualShopCategory>();

        foreach (var categoryConfig in RemoteConfigManager.instance.virtualShopConfig.categories)
        {
            print("items: " + categoryConfig.items);
            var virtualShopCategory = new VirtualShopCategory(categoryConfig);
            virtualShopCategories[categoryConfig.id] = virtualShopCategory;
        }
    }
}

[Serializable]
public class VirtualShopCategory
{
    public string id { get; private set; }
    public bool enabledFlag { get; private set; }
    public List<VirtualShopItem> virtualShopItems { get; private set; }

    public VirtualShopCategory(RemoteConfigManager.CategoryConfig categoryConfig)
    {
        id = categoryConfig.id;
        enabledFlag = categoryConfig.enabledFlag;
        virtualShopItems = new List<VirtualShopItem>();

        foreach (var item in categoryConfig.items)
        {
            virtualShopItems.Add(new VirtualShopItem(item));
        }
    }

    public override string ToString()
    {
        return $"\"{id}\" enabled:{enabledFlag} items:{virtualShopItems?.Count}";
    }
}

[Serializable]
public class VirtualShopItem
{
    public string id { get; private set; }
    public string color { get; private set; }
    public string badgeIconAddress { get; private set; }
    public string badgeColor { get; private set; }
    public string badgeText { get; private set; }
    public string badgeTextColor { get; private set; }

    public List<ItemAndAmountSpec> costs { get; private set; }
    public List<ItemAndAmountSpec> rewards { get; private set; }

    public VirtualShopItem(RemoteConfigManager.ItemConfig itemConfig)
    {
        id = itemConfig.id;
        color = itemConfig.color;
        badgeIconAddress = itemConfig.badgeIconAddress;
        badgeColor = itemConfig.badgeColor;
        badgeText = itemConfig.badgeText;
        badgeTextColor = itemConfig.badgeTextColor;

        var transactionInfo = EconomyManager.instance.virtualPurchaseTransactions[id];
        costs = transactionInfo.costs;
        rewards = transactionInfo.rewards;
    }
    
    public VirtualShopItem(VirtualPurchaseDefinition itemDefinition)
    {
        id = itemDefinition.Id;
        color = "white";
        badgeIconAddress = "Adress";
        badgeColor = "white";
        badgeText = itemDefinition.Name;
        badgeTextColor = "white";

        var transactionInfo = EconomyManager.instance.virtualPurchaseTransactions[id];
        costs = transactionInfo.costs;
        rewards = transactionInfo.rewards;
    }

    public override string ToString()
    {
        var returnString = new StringBuilder($"\"{id}\"");

        returnString.Append($" costs:[{string.Join(", ", costs.Select(cost => cost.ToString()).ToArray())}]"
                            + $" rewards:[{string.Join(", ", rewards.Select(reward => reward.ToString()).ToArray())}]");

        returnString.Append($" color:{color}");

        if (!string.IsNullOrEmpty(badgeIconAddress))
        {
            returnString.Append($" badgeIconAddress:\"{badgeIconAddress}\"");
        }

        return returnString.ToString();
    }
}

[Serializable]
public struct ItemAndAmountSpec
{
    public string id;
    public int amount;

    public ItemAndAmountSpec(string id, int amount)
    {
        this.id = id;
        this.amount = amount;
    }

    public override string ToString()
    {
        return $"{id}:{amount}";
    }
}
