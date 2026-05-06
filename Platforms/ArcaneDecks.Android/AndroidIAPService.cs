#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.BillingClient.Api;
using Microsoft.Xna.Framework;
using ArcaneDecks.Core.Services;

namespace ArcaneDecks.Android;

public class AndroidIAPService : IIAPService
{
    private BillingClient? _billingClient;
    private readonly Dictionary<string, ProductDetails> _productDetails = new();
    private readonly HashSet<string> _purchasedProducts = new();
    private readonly List<(string[] ProductIds, TaskCompletionSource<IReadOnlyList<IAPProduct>> Tcs)> _queryQueue = new();
    private TaskCompletionSource<bool>? _connectionTcs;
    private bool _isConnecting;

    public bool IsInitialized => _billingClient?.IsReady == true;

    public event Action<string>? PurchaseCompleted;
    public event Action<string, string>? PurchaseFailed;

    public void Initialize()
    {
        if (_billingClient != null) return;

        var activity = Game.Activity;
        if (activity == null) return;

        var purchaseListener = new PurchaseUpdateListener(this);

        _billingClient = BillingClient.NewBuilder(activity)
            .SetListener(purchaseListener)
            .EnablePendingPurchases(PendingPurchasesParams.NewBuilder().EnableOneTimeProducts().Build())
            .Build();

        Connect();
    }

    private void Connect()
    {
        if (_billingClient == null || _isConnecting) return;
        _isConnecting = true;
        var listener = new BillingConnectionListener(this);
        _billingClient.StartConnection(listener);
    }

    public Task<IReadOnlyList<IAPProduct>> QueryProductsAsync(string[] productIds)
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<IAPProduct>>();

        if (_billingClient == null || !_billingClient.IsReady)
        {
            _queryQueue.Add((productIds, tcs));
            Connect();
            return tcs.Task;
        }

        RunQuery(productIds, tcs);
        return tcs.Task;
    }

    private void RunQuery(string[] productIds, TaskCompletionSource<IReadOnlyList<IAPProduct>> tcs)
    {
        var products = productIds.Select(id => QueryProductDetailsParams.Product.NewBuilder()
            .SetProductId(id)
            .SetProductType(BillingClient.ProductType.Inapp)
            .Build()).ToList();

        var paramsBuilder = QueryProductDetailsParams.NewBuilder()
            .SetProductList(products)
            .Build();

        _billingClient?.QueryProductDetails(paramsBuilder, new ProductDetailsListener(tcs, this));
    }

    internal void SetProductDetails(string id, ProductDetails details)
    {
        _productDetails[id] = details;
    }

    public void Purchase(string productId)
    {
        var activity = Game.Activity;
        if (activity == null)
        {
            PurchaseFailed?.Invoke(productId, "Activity not available");
            return;
        }

        if (_billingClient == null || !_billingClient.IsReady)
        {
            PurchaseFailed?.Invoke(productId, "Billing client not ready");
            return;
        }

        if (!_productDetails.TryGetValue(productId, out var details))
        {
            PurchaseFailed?.Invoke(productId, "Product not loaded");
            return;
        }

        var productDetailsParams = BillingFlowParams.ProductDetailsParams.NewBuilder()
            .SetProductDetails(details)
            .Build();

        var flowParams = BillingFlowParams.NewBuilder()
            .SetProductDetailsParamsList(new List<BillingFlowParams.ProductDetailsParams> { productDetailsParams })
            .Build();

        _billingClient.LaunchBillingFlow(activity, flowParams);
    }

    public bool IsPurchased(string productId) => _purchasedProducts.Contains(productId);

    internal void OnBillingServiceDisconnected()
    {
        _isConnecting = false;
        Connect();
    }

    internal void OnBillingSetupFinished(BillingResult result)
    {
        _isConnecting = false;
        var success = result.ResponseCode == BillingResponseCode.Ok;

        _connectionTcs?.TrySetResult(success);
        _connectionTcs = null;

        if (success)
        {
            var pending = _queryQueue.ToList();
            _queryQueue.Clear();
            foreach (var (productIds, tcs) in pending)
            {
                RunQuery(productIds, tcs);
            }
        }
    }

    internal void OnPurchasesUpdated(BillingResult result, IList<Purchase>? purchases)
    {
        if (result.ResponseCode == BillingResponseCode.Ok && purchases != null)
        {
            foreach (var purchase in purchases)
            {
                if (purchase.PurchaseState == PurchaseState.Purchased)
                {
                    HandlePurchase(purchase);
                }
            }
        }
        else
        {
            var reason = result.ResponseCode.ToString();
            PurchaseFailed?.Invoke("", reason);
        }
    }

    private void HandlePurchase(Purchase purchase)
    {
        var productId = purchase.Products?.FirstOrDefault() ?? "";
        if (string.IsNullOrEmpty(productId)) return;

        _purchasedProducts.Add(productId);
        PurchaseCompleted?.Invoke(productId);

        if (!purchase.IsAcknowledged)
        {
            var acknowledgeParams = AcknowledgePurchaseParams.NewBuilder()
                .SetPurchaseToken(purchase.PurchaseToken)
                .Build();
            _billingClient?.AcknowledgePurchase(acknowledgeParams, new AcknowledgeListener(productId));
        }
    }

    private class BillingConnectionListener : Java.Lang.Object, IBillingClientStateListener
    {
        private readonly AndroidIAPService _service;

        public BillingConnectionListener(AndroidIAPService service)
        {
            _service = service;
        }

        public void OnBillingServiceDisconnected()
        {
            _service.OnBillingServiceDisconnected();
        }

        public void OnBillingSetupFinished(BillingResult result)
        {
            _service.OnBillingSetupFinished(result);
        }
    }

    private class PurchaseUpdateListener : Java.Lang.Object, IPurchasesUpdatedListener
    {
        private readonly AndroidIAPService _service;

        public PurchaseUpdateListener(AndroidIAPService service)
        {
            _service = service;
        }

        public void OnPurchasesUpdated(BillingResult billingResult, IList<Purchase>? purchases)
        {
            _service.OnPurchasesUpdated(billingResult, purchases);
        }
    }

    private class ProductDetailsListener : Java.Lang.Object, IProductDetailsResponseListener
    {
        private readonly TaskCompletionSource<IReadOnlyList<IAPProduct>> _tcs;
        private readonly AndroidIAPService _service;

        public ProductDetailsListener(TaskCompletionSource<IReadOnlyList<IAPProduct>> tcs, AndroidIAPService service)
        {
            _tcs = tcs;
            _service = service;
        }

        public void OnProductDetailsResponse(BillingResult result, QueryProductDetailsResult productDetailsResult)
        {
            if (result.ResponseCode != BillingResponseCode.Ok || productDetailsResult == null)
            {
                _tcs.TrySetResult(new List<IAPProduct>());
                return;
            }

            var products = new List<IAPProduct>();
            foreach (var details in productDetailsResult.ProductDetailsList ?? new List<ProductDetails>())
            {
                var id = details.ProductId;
                _service.SetProductDetails(id, details);

                var price = details.OneTimePurchaseOfferDetailsList?.FirstOrDefault()?.FormattedPrice ?? "???";

                var type = details.ProductType switch
                {
                    "inapp" => IAPProductType.Consumable,
                    "subs" => IAPProductType.Subscription,
                    _ => IAPProductType.NonConsumable
                };

                products.Add(new IAPProduct
                {
                    Id = id,
                    Name = details.Title,
                    Description = details.Description,
                    Price = price,
                    Type = type
                });
            }

            _tcs.TrySetResult(products);
        }
    }

    private class AcknowledgeListener : Java.Lang.Object, IAcknowledgePurchaseResponseListener
    {
        private readonly string _productId;

        public AcknowledgeListener(string productId)
        {
            _productId = productId;
        }

        public void OnAcknowledgePurchaseResponse(BillingResult result)
        {
            // Log or handle acknowledge failure
        }
    }
}
