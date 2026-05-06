#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcaneDecks.Core.Services;

namespace ArcaneDecks.DesktopGL;

public class DesktopIAPService : IIAPService
{
    public bool IsInitialized => true;
    public event Action<string>? PurchaseCompleted;
    public event Action<string, string>? PurchaseFailed;

    public void Initialize() { }

    public Task<IReadOnlyList<IAPProduct>> QueryProductsAsync(string[] productIds)
    {
        var products = new List<IAPProduct>();
        foreach (var id in productIds)
        {
            products.Add(new IAPProduct
            {
                Id = id,
                Name = id,
                Description = "Desktop test product",
                Price = "$0.99",
                Type = IAPProductType.Consumable
            });
        }
        return Task.FromResult<IReadOnlyList<IAPProduct>>(products);
    }

    public void Purchase(string productId)
    {
        PurchaseCompleted?.Invoke(productId);
        // Desktop has no real failure path; keep event alive for interface contract
        if (PurchaseCompleted == null)
            PurchaseFailed?.Invoke(productId, "No listener");
    }

    public bool IsPurchased(string productId) => false;
}
