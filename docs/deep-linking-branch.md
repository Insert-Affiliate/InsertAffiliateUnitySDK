# Branch.io Deep Linking Integration

This guide shows how to integrate Insert Affiliate Unity SDK with Branch.io for deep linking attribution.

## Prerequisites

- [Branch Unity SDK](https://help.branch.io/developers-hub/docs/unity-basic-integration) installed and configured
- Create a Branch deep link and provide it to affiliates via the [Insert Affiliate dashboard](https://app.insertaffiliate.com/affiliates)

## Platform Setup

Complete the deep linking setup for Branch by following their official documentation:
- [Branch Unity SDK Setup Guide](https://help.branch.io/developers-hub/docs/unity-basic-integration)

This covers:
- iOS: Info.plist configuration and universal links
- Android: AndroidManifest.xml intent filters and App Links

## Integration Examples

Choose the example that matches your IAP verification platform:

### Example with RevenueCat

```csharp
using UnityEngine;
using InsertAffiliate;
using BranchIO;
using RevenueCat;
using System.Collections.Generic;

public class BranchRevenueCatManager : MonoBehaviour
{
    void Start()
    {
        // Initialize Insert Affiliate SDK
        InsertAffiliateSDK.Initialize("your_company_code", verboseLogging: true);

        // Initialize RevenueCat
        Purchases.Configure("your_revenuecat_api_key");

        // Handle initial affiliate identifier
        HandleAffiliateIdentifier();

        // Initialize Branch and listen for deep links
        Branch.initSession(delegate(Dictionary<string, object> parameters, string error)
        {
            if (parameters.ContainsKey("~referring_link"))
            {
                string referringLink = parameters["~referring_link"] as string;

                // Process with Insert Affiliate
                InsertAffiliateSDK.SetInsertAffiliateIdentifier(referringLink, (shortCode) =>
                {
                    if (!string.IsNullOrEmpty(shortCode))
                    {
                        Debug.Log($"[Branch] Affiliate set: {shortCode}");
                        HandleAffiliateIdentifier();
                    }
                });
            }
        });
    }

    void HandleAffiliateIdentifier()
    {
        string affiliateId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();
        if (!string.IsNullOrEmpty(affiliateId))
        {
            var attributes = new Dictionary<string, string>
            {
                { "insert_affiliate", affiliateId }
            };
            Purchases.shared.SetAttributes(attributes);
            Debug.Log($"[RevenueCat] Attribution set: {affiliateId}");
        }
    }
}
```

### Example with Apphud

```csharp
using UnityEngine;
using InsertAffiliate;
using BranchIO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class BranchApphudManager : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _ApphudStart(string apiKey);

    [DllImport("__Internal")]
    private static extern void _ApphudSetUserProperty(string key, string value);
#endif

    void Start()
    {
        // Initialize Insert Affiliate SDK
        InsertAffiliateSDK.Initialize("your_company_code", verboseLogging: true);

        // Initialize Apphud
#if UNITY_IOS && !UNITY_EDITOR
        _ApphudStart("your_apphud_api_key");
#endif

        // Handle initial affiliate identifier
        HandleAffiliateIdentifier();

        // Initialize Branch and listen for deep links
        Branch.initSession(delegate(Dictionary<string, object> parameters, string error)
        {
            if (parameters.ContainsKey("~referring_link"))
            {
                string referringLink = parameters["~referring_link"] as string;

                InsertAffiliateSDK.SetInsertAffiliateIdentifier(referringLink, (shortCode) =>
                {
                    if (!string.IsNullOrEmpty(shortCode))
                    {
                        Debug.Log($"[Branch] Affiliate set: {shortCode}");
                        HandleAffiliateIdentifier();
                    }
                });
            }
        });
    }

    void HandleAffiliateIdentifier()
    {
        string affiliateId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();
        if (!string.IsNullOrEmpty(affiliateId))
        {
#if UNITY_IOS && !UNITY_EDITOR
            _ApphudSetUserProperty("insert_affiliate", affiliateId);
#endif
            Debug.Log($"[Apphud] Attribution set: {affiliateId}");
        }
    }
}
```

### Example with Iaptic

```csharp
using UnityEngine;
using InsertAffiliate;
using BranchIO;
using UnityEngine.Purchasing;
using System.Collections.Generic;

public class BranchIapticManager : MonoBehaviour, IStoreListener
{
    private IStoreController storeController;

    void Start()
    {
        // Initialize Insert Affiliate SDK
        InsertAffiliateSDK.Initialize("your_company_code", verboseLogging: true);

        // Initialize Unity IAP
        InitializePurchasing();

        // Initialize Branch and listen for deep links
        Branch.initSession(delegate(Dictionary<string, object> parameters, string error)
        {
            if (parameters.ContainsKey("~referring_link"))
            {
                string referringLink = parameters["~referring_link"] as string;

                InsertAffiliateSDK.SetInsertAffiliateIdentifier(referringLink, (shortCode) =>
                {
                    if (!string.IsNullOrEmpty(shortCode))
                    {
                        Debug.Log($"[Branch] Affiliate set: {shortCode}");
                    }
                });
            }
        });
    }

    void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct("monthly_subscription", ProductType.Subscription);
        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[IAP] Initialization failed: {error}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        // Get affiliate identifier for Iaptic validation
        string affiliateId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();

        // Validate with Iaptic, passing affiliate as applicationUsername
        ValidatePurchaseWithIaptic(
            args.purchasedProduct.receipt,
            affiliateId
        );

        return PurchaseProcessingResult.Complete;
    }

    void ValidatePurchaseWithIaptic(string receipt, string applicationUsername)
    {
        // Your Iaptic validation implementation
        // Pass applicationUsername to associate purchase with affiliate
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogError($"[IAP] Purchase failed: {product.definition.id}, {reason}");
    }
}
```

### Example with App Store / Google Play Direct

```csharp
using UnityEngine;
using InsertAffiliate;
using BranchIO;
using UnityEngine.Purchasing;
using System.Collections.Generic;

public class BranchStoreDirectManager : MonoBehaviour, IStoreListener
{
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;
    private string pendingAppAccountToken;

    void Start()
    {
        // Initialize Insert Affiliate SDK
        InsertAffiliateSDK.Initialize("your_company_code", verboseLogging: true);

        // Initialize Unity IAP
        InitializePurchasing();

        // Initialize Branch and listen for deep links
        Branch.initSession(delegate(Dictionary<string, object> parameters, string error)
        {
            if (parameters.ContainsKey("~referring_link"))
            {
                string referringLink = parameters["~referring_link"] as string;

                InsertAffiliateSDK.SetInsertAffiliateIdentifier(referringLink, (shortCode) =>
                {
                    if (!string.IsNullOrEmpty(shortCode))
                    {
                        Debug.Log($"[Branch] Affiliate set: {shortCode}");
                    }
                });
            }
        });
    }

    void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct("monthly_subscription", ProductType.Subscription);
        builder.AddProduct("consumable_gems", ProductType.Consumable);
        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[IAP] Initialization failed: {error}");
    }

    // iOS App Store Direct: Use appAccountToken
    public void PurchaseProduct(string productId)
    {
#if UNITY_IOS
        InsertAffiliateSDK.ReturnUserAccountTokenAndStoreExpectedTransaction((token) =>
        {
            pendingAppAccountToken = token;
            BuyProductWithToken(productId, token);
        });
#else
        storeController.InitiatePurchase(productId);
#endif
    }

    void BuyProductWithToken(string productId, string appAccountToken)
    {
        Product product = storeController.products.WithID(productId);
        if (product == null || !product.availableToPurchase) return;

#if UNITY_IOS
        if (!string.IsNullOrEmpty(appAccountToken))
        {
            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "applicationUsername", appAccountToken }
            };
            storeController.InitiatePurchase(product, payload);
        }
        else
        {
            storeController.InitiatePurchase(product);
        }
#else
        storeController.InitiatePurchase(product);
#endif
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Debug.Log($"[IAP] Purchase successful: {args.purchasedProduct.definition.id}");

#if UNITY_ANDROID
        // Google Play Direct: Store purchase token
        string purchaseToken = ExtractPurchaseToken(args.purchasedProduct.receipt);
        if (!string.IsNullOrEmpty(purchaseToken))
        {
            InsertAffiliateSDK.StoreExpectedStoreTransaction(purchaseToken);
        }
#endif

        pendingAppAccountToken = null;
        return PurchaseProcessingResult.Complete;
    }

    string ExtractPurchaseToken(string receipt)
    {
        // Parse receipt JSON to extract purchase token
        // Implementation depends on your JSON parsing approach
        return "";
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogError($"[IAP] Purchase failed: {product.definition.id}, {reason}");
        pendingAppAccountToken = null;
    }
}
```

## Testing

Test your Branch deep link integration:

```bash
# Android Emulator
adb shell am start -W -a android.intent.action.VIEW -d "https://your-app.app.link/abc123"

# iOS Simulator
xcrun simctl openurl booted "https://your-app.app.link/abc123"
```

## Troubleshooting

**Problem:** `~referring_link` is null
- **Solution:** Ensure Branch SDK is properly initialized before Insert Affiliate SDK
- Verify Branch link is properly configured with your app's URI scheme

**Problem:** Deep link opens browser instead of app
- **Solution:** Check Branch dashboard for associated domains configuration
- Verify your app's entitlements include the Branch link domain (iOS)
- Verify AndroidManifest.xml has correct intent filters (Android)

**Problem:** Deferred deep linking not working
- **Solution:** Make sure you're using `Branch.initSession()` correctly
- Test with a fresh app install (uninstall/reinstall)

## Next Steps

After completing Branch integration:
1. Test deep link attribution with a test affiliate link
2. Verify affiliate identifier is stored correctly
3. Make a test purchase to confirm tracking works end-to-end

[Back to Main README](../README.md)
