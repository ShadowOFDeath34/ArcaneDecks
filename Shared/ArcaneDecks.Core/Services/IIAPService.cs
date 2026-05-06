#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArcaneDecks.Core.Services;

public enum IAPProductType
{
    Consumable,
    NonConsumable,
    Subscription
}

public class IAPProduct
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Price { get; init; } = "";
    public IAPProductType Type { get; init; }
}

public interface IIAPService
{
    bool IsInitialized { get; }
    void Initialize();
    Task<IReadOnlyList<IAPProduct>> QueryProductsAsync(string[] productIds);
    void Purchase(string productId);
    bool IsPurchased(string productId);
    event Action<string>? PurchaseCompleted;
    event Action<string, string>? PurchaseFailed;
}
