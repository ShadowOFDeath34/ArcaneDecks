#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcaneDecks.Core.Services;

namespace ArcaneDecks.iOS;

public class iOSIAPService : IIAPService
{
    public bool IsInitialized => true;
    public event Action<string>? PurchaseCompleted;
    public event Action<string, string>? PurchaseFailed;

    public void Initialize() { }

    public Task<IReadOnlyList<IAPProduct>> QueryProductsAsync(string[] productIds)
    {
        return Task.FromResult<IReadOnlyList<IAPProduct>>(new List<IAPProduct>());
    }

    public void Purchase(string productId)
    {
        PurchaseCompleted?.Invoke(productId);
        PurchaseFailed?.Invoke(productId, "Not implemented");
    }

    public bool IsPurchased(string productId) => false;
}
