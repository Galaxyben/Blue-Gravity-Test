using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using UnityEngine;

public class BGSceneManager : MonoBehaviour
{
    const int k_EconomyPurchaseCostsNotMetStatusCode = 10504;

    public static BGSceneManager instance;
    
    public ShopController virtualShopSampleView;

    async void Start()
    {
        instance = this;
        try
        {
            await UnityServices.InitializeAsync();

            // Check that scene has not been unloaded while processing async wait to prevent throw.
            if (this == null) return;

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (this == null) return;
            }

            Debug.Log($"Player id:{AuthenticationService.Instance.PlayerId}");

            await EconomyManager.instance.RefreshEconomyConfiguration();

            EconomyManager.instance.InitializeVirtualPurchaseLookup();

            // Note: We want these methods to use the most up to date configuration data, so we will wait to
            // call them until the previous two methods (which update the configuration data) have completed.
            await Task.WhenAll(AddressablesManager.instance.PreloadAllEconomySprites(),
                RemoteConfigManager.instance.FetchConfigs(),
                EconomyManager.instance.RefreshCurrencyBalances());

            // Read all badge addressables
            // Note: must be done after Remote Config values have been read (above).
            await AddressablesManager.instance.PreloadAllShopBadgeSprites(
                RemoteConfigManager.instance.virtualShopConfig.categories);

            // Initialize all shops.
            // Note: must be done after all other initialization has completed (above).
            ShopManager.instance.Initialize();

            Debug.Log("Initialization and sign in complete.");

            EnablePurchases();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void EnablePurchases()
    {
        //virtualShopSampleView.SetInteractable();
    }

    public void OnCategoryButtonClicked(string categoryId)
    {
        /*var virtualShopCategory = VirtualShopManager.instance.virtualShopCategories[categoryId];
        virtualShopSampleView.ShowCategory(virtualShopCategory);*/
    }

    public async Task OnPurchaseClicked(VirtualShopItem virtualShopItem)
    {
        try
        {
            var result = await EconomyManager.instance.MakeVirtualPurchaseAsync(virtualShopItem.id);
            if (this == null) return;

            await EconomyManager.instance.RefreshCurrencyBalances();
            if (this == null) return;

            ShowRewardPopup(result.Rewards);
        }
        catch (EconomyException e)
            when (e.ErrorCode == k_EconomyPurchaseCostsNotMetStatusCode)
        {
            //virtualShopSampleView.ShowVirtualPurchaseFailedErrorPopup();
            
            print("Error while purchasing the item.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async void OnGainCurrencyDebugButtonClicked()
    {
        try
        {
            await EconomyManager.instance.GrantDebugCurrency("GEM", 30);
            if (this == null) return;

            await EconomyManager.instance.RefreshCurrencyBalances();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void ShowRewardPopup(Rewards rewards)
    {
        var addressablesManager = AddressablesManager.instance;

        /*var rewardDetails = new List<RewardDetail>();
        foreach (var inventoryReward in rewards.Inventory)
        {
            rewardDetails.Add(new RewardDetail
            {
                id = inventoryReward.Id,
                quantity = inventoryReward.Amount,
                sprite = addressablesManager.preloadedSpritesByEconomyId[inventoryReward.Id]
            });
        }

        foreach (var currencyReward in rewards.Currency)
        {
            rewardDetails.Add(new RewardDetail
            {
                id = currencyReward.Id,
                quantity = currencyReward.Amount,
                sprite = addressablesManager.preloadedSpritesByEconomyId[currencyReward.Id]
            });
        }

        virtualShopSampleView.ShowRewardPopup(rewardDetails);*/
    }
}
