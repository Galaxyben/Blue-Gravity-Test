using System;
using System.Collections.Generic;
using Unity.Services.Economy.Model;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    public static ShopController instance;
    
    [SerializeField] private GameObject categoryButtonPrefab;
    [SerializeField] private Transform categoryContainer;

    public VirtualShopItemView virtualShopItemPrefab;
    [SerializeField] private Transform itemContainer;
    
    List<CategoryButton> m_CategoryButtons = new List<CategoryButton>();

    private void Start()
    {
        instance = this;
    }

    public void Initialize(Dictionary<string, VirtualShopCategory> virtualShopCategories)
    {
        foreach (var kvp in virtualShopCategories)
        {
            var categoryButtonGameObject = Instantiate(categoryButtonPrefab.gameObject,
                categoryContainer.transform);
            var categoryButton = categoryButtonGameObject.GetComponent<CategoryButton>();
            categoryButton.Initialize(BGSceneManager.instance, kvp.Value.id);
            m_CategoryButtons.Add(categoryButton);
        }
    }

    public void Initialize(List<VirtualPurchaseDefinition> shopItems)
    {
        foreach (var shopItem in shopItems)
        {
            var itemButton = Instantiate(virtualShopItemPrefab, itemContainer);
            var shopItemComponent = itemButton.GetComponent<VirtualShopItemView>();
            var vsItem = new VirtualShopItem(shopItem);
            
            shopItemComponent.Initialize(BGSceneManager.instance, vsItem);
        }
    }
    
    public void ShowCategory(VirtualShopCategory virtualShopCategory)
    {
        ShowItems(virtualShopCategory);

        foreach (var categoryButton in m_CategoryButtons)
        {
            categoryButton.UpdateCategoryButtonUIState(virtualShopCategory.id);
        }
    }

    void ShowItems(VirtualShopCategory virtualShopCategory)
    {
        if (virtualShopItemPrefab is null)
        {
            throw new NullReferenceException("Shop Item Prefab was null.");
        }

        ClearContainer();

        foreach (var virtualShopItem in virtualShopCategory.virtualShopItems)
        {
            var virtualShopItemGameObject = Instantiate(virtualShopItemPrefab.gameObject,
                itemContainer);
            virtualShopItemGameObject.GetComponent<VirtualShopItemView>().Initialize(
                BGSceneManager.instance, virtualShopItem, AddressablesManager.instance);
        }
    }

    void ClearContainer()
    {
        var itemsContainerTransform = itemContainer;
        for (var i = itemsContainerTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsContainerTransform.GetChild(i).gameObject);
        }
    }
}
