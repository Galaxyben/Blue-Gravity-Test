using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using UnityEngine;
using System.Threading;
using TMPro;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EconomyManager : MonoBehaviour
{
    const int k_EconomyPurchaseCostsNotMetStatusCode = 10504;

    [SerializeField] private TMP_Text currencyText;
    
    //public CurrencyHudView currencyHudView;
    public InventoryHudView inventoryHudView;

    public static EconomyManager instance { get; private set; }

    // Dictionary of all Virtual Purchase transactions ids to lists of costs & rewards.
    public Dictionary<string, (List<ItemAndAmountSpec> costs, List<ItemAndAmountSpec> rewards)>
        virtualPurchaseTransactions
    {
        get;
        private set;
    }

    public List<CurrencyDefinition> currencyDefinitions { get; private set; }
    public List<InventoryItemDefinition> inventoryItemDefinitions { get; private set; }

    public List<VirtualPurchaseDefinition> virtualPurchaseDefinitions;

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

    public async Task RefreshEconomyConfiguration()
    {
        // Calling SyncConfigurationAsync(), will update the cached configuration list (the lists of Currency,
        // Inventory Item, and Purchase definitions) with any definitions that have been published or changed by
        // Economy or overriden by Game Overrides since the last time the player's configuration was cached. It also
        // ensures that other services like Cloud Code are working with the same configuration that has been cached.
        await EconomyService.Instance.Configuration.SyncConfigurationAsync();

        // Check that scene has not been unloaded while processing async wait to prevent throw.

        currencyDefinitions = EconomyService.Instance.Configuration.GetCurrencies();
        inventoryItemDefinitions = EconomyService.Instance.Configuration.GetInventoryItems();
        virtualPurchaseDefinitions = EconomyService.Instance.Configuration.GetVirtualPurchases();
    }

    public async void RefreshInventoryUI()
    {
        await RefreshInventory();
    }

    public async Task RefreshCurrencyBalances()
    {
        GetBalancesResult balanceResult = null;

        try
        {
            balanceResult = await GetEconomyBalances();
        }
        catch (EconomyRateLimitedException e)
        {
            balanceResult = await Utils.RetryEconomyFunction(GetEconomyBalances, e.RetryAfter);
        }
        catch (Exception e)
        {
            Debug.Log("Problem getting Economy currency balances:");
            Debug.LogException(e);
        }

        //currencyHudView.SetBalances(balanceResult);
        if (balanceResult != null) currencyText.text = balanceResult.Balances[0].Balance.ToString("C");
    }

    static Task<GetBalancesResult> GetEconomyBalances()
    {
        var options = new GetBalancesOptions { ItemsPerFetch = 100 };
        return EconomyService.Instance.PlayerBalances.GetBalancesAsync(options);
    }

    public async Task RefreshInventory()
    {
        GetInventoryResult inventoryResult = null;  

        // empty the inventory view first
        inventoryHudView.Refresh(default);

        try
        {
            inventoryResult = await GetEconomyPlayerInventory();
        }
        catch (EconomyRateLimitedException e)
        {
            inventoryResult = await Utils.RetryEconomyFunction(GetEconomyPlayerInventory, e.RetryAfter);
        }
        catch (Exception e)
        {
            Debug.Log("Problem getting Economy inventory items:");
            Debug.LogException(e);
        }

        if (inventoryResult != null) inventoryHudView.Refresh(inventoryResult.PlayersInventoryItems);
    }

    static Task<GetInventoryResult> GetEconomyPlayerInventory()
    {
        var options = new GetInventoryOptions { ItemsPerFetch = 100 };
        return EconomyService.Instance.PlayerInventory.GetInventoryAsync(options);
    }

    public void InitializeVirtualPurchaseLookup()
    {
        if (virtualPurchaseDefinitions == null)
        {
            Debug.Log("<color=red>Error: </color> Error getting virtual purchase definitions");
            return;
        }

        virtualPurchaseTransactions = new Dictionary<string,
            (List<ItemAndAmountSpec> costs, List<ItemAndAmountSpec> rewards)>();

        foreach (var virtualPurchaseDefinition in virtualPurchaseDefinitions)
        {
            Debug.Log("<color=blue>Shop Item Found: </color> " + virtualPurchaseDefinition.Name);
            var costs = ParseEconomyItems(virtualPurchaseDefinition.Costs);
            var rewards = ParseEconomyItems(virtualPurchaseDefinition.Rewards);

            virtualPurchaseTransactions[virtualPurchaseDefinition.Id] = (costs, rewards);
        }
        
        ShopController.instance.Initialize(virtualPurchaseDefinitions);
    }

    List<ItemAndAmountSpec> ParseEconomyItems(List<PurchaseItemQuantity> itemQuantities)
    {
        var itemsAndAmountsSpec = new List<ItemAndAmountSpec>();

        foreach (var itemQuantity in itemQuantities)
        {
            var id = itemQuantity.Item.GetReferencedConfigurationItem().Id;
            itemsAndAmountsSpec.Add(new ItemAndAmountSpec(id, itemQuantity.Amount));
        }

        return itemsAndAmountsSpec;
    }

    public async Task<MakeVirtualPurchaseResult> MakeVirtualPurchaseAsync(string virtualPurchaseId)
    {
        try
        {
            return await EconomyService.Instance.Purchases.MakeVirtualPurchaseAsync(virtualPurchaseId);
        }
        catch (EconomyException e)
            when (e.ErrorCode == k_EconomyPurchaseCostsNotMetStatusCode)
        {
            // Rethrow purchase-cost-not-met exception to be handled by shops manager.
            throw;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return default;
        }
    }

    // This method is used to help test this Use Case sample by giving some currency to permit
    // transactions to be completed.
    public async Task GrantDebugCurrency(string currencyId, int amount)
    {
        try
        {
            await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync(currencyId, amount);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}

public static class Utils
{
    public static async Task<T> RetryEconomyFunction<T>(Func<Task<T>> functionToRetry, int retryAfterSeconds)
    {
        if (retryAfterSeconds > 60)
        {
            Debug.Log("Economy returned a rate limit exception with an extended Retry After time " +
                $"of {retryAfterSeconds} seconds. Suggest manually retrying at a later time.");
            return default;
        }

        Debug.Log($"Economy returned a rate limit exception. Retrying after {retryAfterSeconds} seconds");

        try
        {
            // Using a CancellationToken allows us to ensure that the Task.Delay gets cancelled if we exit
            // playmode while it's waiting its delay time. Without it, it would continue trying to execute
            // the rest of this code, even outside of playmode.
            using (var cancellationTokenHelper = new CancellationTokenHelper())
            {
                var cancellationToken = cancellationTokenHelper.cancellationToken;

                await Task.Delay(retryAfterSeconds * 1000, cancellationToken);

                // Call the function that we passed in to this method after the retry after time period has passed.
                var result = await functionToRetry();

                if (cancellationToken.IsCancellationRequested)
                {
                    return default;
                }

                Debug.Log("Economy retry successfully completed");

                return result;
            }
        }
        catch (OperationCanceledException)
        {
            return default;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        return default;
    }

    public static string GetElapsedTimeRange(DateTime startTime)
    {
        var elapsedTime = DateTime.Now - startTime;
        var elapsedSeconds = elapsedTime.TotalSeconds;

        if (elapsedSeconds < 0)
        {
            return "N/A";
        }

        // BottomRange is the nearest divisible-by-10 number less than elapsedSeconds.
        // For instance, 47.85 seconds has a bottom range of 40.
        var bottomRange = (int)Math.Floor(elapsedSeconds / 10) * 10;

        // TopRange is the nearest divisible-by-10 number greater than elapsedSeconds.
        // For instance, 47.85 seconds has a top range of 50.
        var topRange = bottomRange + 10;

        // In the string being returned `[` represents inclusive and `)` represents exclusive. So a range of
        // [40, 50) includes numbers from 40.0 to 49.99999 etc.
        return $"[{bottomRange}, {topRange}) seconds";
    }
}

public class CancellationTokenHelper : IDisposable
{
    CancellationTokenSource m_CancellationTokenSource;
    bool m_Disposed;

    public CancellationToken cancellationToken => m_CancellationTokenSource.Token;

    public CancellationTokenHelper()
    {
        m_CancellationTokenSource = new CancellationTokenSource();
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
    {
        if (playModeStateChange == PlayModeStateChange.ExitingPlayMode)
        {
            m_CancellationTokenSource?.Cancel();
        }
    }
#endif

    // IDisposable related implementation modeled after
    // example code at https://learn.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-6.0
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool triggeredByUserCode)
    {
        if (m_Disposed)
        {
            return;
        }

        // If triggeredByUserCode equals true, dispose both managed and unmanaged resources.
        if (triggeredByUserCode)
        {
            // Dispose managed resources.
            m_CancellationTokenSource.Dispose();
            m_CancellationTokenSource = null;
        }

#if UNITY_EDITOR

        // Clean up unmanaged resources.
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif

        m_Disposed = true;
    }

    ~CancellationTokenHelper()
    {
        Dispose(false);
    }
}
