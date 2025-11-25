# AppsFlyer Deep Linking Integration

This guide shows how to integrate Insert Affiliate Unity SDK with AppsFlyer for deep linking attribution.

## Prerequisites

- [AppsFlyer Unity SDK](https://dev.appsflyer.com/hc/docs/unity) installed and configured
- Create an AppsFlyer OneLink and provide it to affiliates via the [Insert Affiliate dashboard](https://app.insertaffiliate.com/affiliates)
- AppsFlyer Dev Key from your AppsFlyer dashboard
- iOS App ID and Android package name configured in AppsFlyer

## Platform Configuration

### Android Setup

Add to `Assets/Plugins/Android/AndroidManifest.xml`:

```xml
<!-- Permissions -->
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<uses-permission android:name="com.android.vending.INSTALL_REFERRER" />

<activity android:name="com.unity3d.player.UnityPlayerActivity" android:exported="true">
    <!-- OneLink deep linking -->
    <intent-filter android:autoVerify="true">
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data android:scheme="https" android:host="YOUR_SUBDOMAIN.onelink.me" />
    </intent-filter>
</activity>

<!-- AppsFlyer metadata -->
<application>
    <meta-data android:name="com.appsflyer.ApiKey" android:value="YOUR_APPSFLYER_DEV_KEY" />
</application>
```

### iOS Setup

Add to `Info.plist` (via Unity iOS Build Settings or post-build script):

```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLSchemes</key>
        <array><string>YOUR_CUSTOM_SCHEME</string></array>
    </dict>
</array>
<key>com.apple.developer.associated-domains</key>
<array>
    <string>applinks:YOUR_SUBDOMAIN.onelink.me</string>
</array>
```

## Integration Examples

Choose the example that matches your IAP verification platform:

### Example with RevenueCat

```csharp
using UnityEngine;
using InsertAffiliate;
using AppsFlyerSDK;
using RevenueCat;
using System.Collections.Generic;

public class AppsFlyerRevenueCatManager : MonoBehaviour, IAppsFlyerConversionData
{
    void Start()
    {
        // Initialize Insert Affiliate SDK
        InsertAffiliateSDK.Initialize("your_company_code", verboseLogging: true);

        // Initialize RevenueCat
        Purchases.Configure("your_revenuecat_api_key");

        // Handle initial affiliate identifier
        HandleAffiliateIdentifier();

        // Initialize AppsFlyer
        AppsFlyer.initSDK("your_appsflyer_dev_key", "your_ios_app_id", this);
        AppsFlyer.startSDK();
    }

    // AppsFlyer callback - install attribution
    public void onConversionDataSuccess(string conversionData)
    {
        Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(conversionData);

        if (data.ContainsKey("af_dp") || data.ContainsKey("deep_link_value"))
        {
            string deepLink = data.ContainsKey("af_dp") ?
                data["af_dp"] as string : data["deep_link_value"] as string;

            InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLink, (shortCode) =>
            {
                if (!string.IsNullOrEmpty(shortCode))
                {
                    Debug.Log($"[AppsFlyer] Affiliate set: {shortCode}");
                    HandleAffiliateIdentifier();
                }
            });
        }
    }

    public void onConversionDataFail(string error)
    {
        Debug.LogError($"[AppsFlyer] Conversion data failed: {error}");
    }

    // AppsFlyer callback - deep link when app already installed
    public void onAppOpenAttribution(string attributionData)
    {
        Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(attributionData);

        if (data.ContainsKey("af_dp") || data.ContainsKey("deep_link_value"))
        {
            string deepLink = data.ContainsKey("af_dp") ?
                data["af_dp"] as string : data["deep_link_value"] as string;

            InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLink, (shortCode) =>
            {
                if (!string.IsNullOrEmpty(shortCode))
                {
                    Debug.Log($"[AppsFlyer] App open affiliate set: {shortCode}");
                    HandleAffiliateIdentifier();
                }
            });
        }
    }

    public void onAppOpenAttributionFailure(string error)
    {
        Debug.LogError($"[AppsFlyer] Attribution failed: {error}");
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
using AppsFlyerSDK;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class AppsFlyerApphudManager : MonoBehaviour, IAppsFlyerConversionData
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

        // Initialize AppsFlyer
        AppsFlyer.initSDK("your_appsflyer_dev_key", "your_ios_app_id", this);
        AppsFlyer.startSDK();
    }

    public void onConversionDataSuccess(string conversionData)
    {
        Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(conversionData);

        if (data.ContainsKey("af_dp") || data.ContainsKey("deep_link_value"))
        {
            string deepLink = data.ContainsKey("af_dp") ?
                data["af_dp"] as string : data["deep_link_value"] as string;

            InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLink, (shortCode) =>
            {
                if (!string.IsNullOrEmpty(shortCode))
                {
                    Debug.Log($"[AppsFlyer] Affiliate set: {shortCode}");
                    HandleAffiliateIdentifier();
                }
            });
        }
    }

    public void onConversionDataFail(string error)
    {
        Debug.LogError($"[AppsFlyer] Conversion data failed: {error}");
    }

    public void onAppOpenAttribution(string attributionData)
    {
        Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(attributionData);

        if (data.ContainsKey("af_dp") || data.ContainsKey("deep_link_value"))
        {
            string deepLink = data.ContainsKey("af_dp") ?
                data["af_dp"] as string : data["deep_link_value"] as string;

            InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLink, (shortCode) =>
            {
                if (!string.IsNullOrEmpty(shortCode))
                {
                    Debug.Log($"[AppsFlyer] App open affiliate set: {shortCode}");
                    HandleAffiliateIdentifier();
                }
            });
        }
    }

    public void onAppOpenAttributionFailure(string error)
    {
        Debug.LogError($"[AppsFlyer] Attribution failed: {error}");
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
using AppsFlyerSDK;
using UnityEngine.Purchasing;
using System.Collections.Generic;

public class AppsFlyerIapticManager : MonoBehaviour, IAppsFlyerConversionData, IStoreListener
{
    private IStoreController storeController;

    void Start()
    {
        // Initialize Insert Affiliate SDK
        InsertAffiliateSDK.Initialize("your_company_code", verboseLogging: true);

        // Initialize Unity IAP
        InitializePurchasing();

        // Initialize AppsFlyer
        AppsFlyer.initSDK("your_appsflyer_dev_key", "your_ios_app_id", this);
        AppsFlyer.startSDK();
    }

    public void onConversionDataSuccess(string conversionData)
    {
        Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(conversionData);

        if (data.ContainsKey("af_dp") || data.ContainsKey("deep_link_value"))
        {
            string deepLink = data.ContainsKey("af_dp") ?
                data["af_dp"] as string : data["deep_link_value"] as string;

            InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLink, (shortCode) =>
            {
                if (!string.IsNullOrEmpty(shortCode))
                {
                    Debug.Log($"[AppsFlyer] Affiliate set: {shortCode}");
                }
            });
        }
    }

    public void onConversionDataFail(string error)
    {
        Debug.LogError($"[AppsFlyer] Conversion data failed: {error}");
    }

    public void onAppOpenAttribution(string attributionData)
    {
        Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(attributionData);

        if (data.ContainsKey("af_dp") || data.ContainsKey("deep_link_value"))
        {
            string deepLink = data.ContainsKey("af_dp") ?
                data["af_dp"] as string : data["deep_link_value"] as string;

            InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLink, (shortCode) =>
            {
                if (!string.IsNullOrEmpty(shortCode))
                {
                    Debug.Log($"[AppsFlyer] App open affiliate set: {shortCode}");
                }
            });
        }
    }

    public void onAppOpenAttributionFailure(string error)
    {
        Debug.LogError($"[AppsFlyer] Attribution failed: {error}");
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
using AppsFlyerSDK;
using UnityEngine.Purchasing;
using System.Collections.Generic;

public class AppsFlyerStoreDirectManager : MonoBehaviour, IAppsFlyerConversionData, IStoreListener
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

        // Initialize AppsFlyer
        AppsFlyer.initSDK("your_appsflyer_dev_key", "your_ios_app_id", this);
        AppsFlyer.startSDK();
    }

    public void onConversionDataSuccess(string conversionData)
    {
        Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(conversionData);

        if (data.ContainsKey("af_dp") || data.ContainsKey("deep_link_value"))
        {
            string deepLink = data.ContainsKey("af_dp") ?
                data["af_dp"] as string : data["deep_link_value"] as string;

            InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLink, (shortCode) =>
            {
                if (!string.IsNullOrEmpty(shortCode))
                {
                    Debug.Log($"[AppsFlyer] Affiliate set: {shortCode}");
                }
            });
        }
    }

    public void onConversionDataFail(string error)
    {
        Debug.LogError($"[AppsFlyer] Conversion data failed: {error}");
    }

    public void onAppOpenAttribution(string attributionData)
    {
        Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(attributionData);

        if (data.ContainsKey("af_dp") || data.ContainsKey("deep_link_value"))
        {
            string deepLink = data.ContainsKey("af_dp") ?
                data["af_dp"] as string : data["deep_link_value"] as string;

            InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLink, (shortCode) =>
            {
                if (!string.IsNullOrEmpty(shortCode))
                {
                    Debug.Log($"[AppsFlyer] App open affiliate set: {shortCode}");
                }
            });
        }
    }

    public void onAppOpenAttributionFailure(string error)
    {
        Debug.LogError($"[AppsFlyer] Attribution failed: {error}");
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

## Deep Link Callback Types

AppsFlyer provides two types of deep link callbacks:

| Callback | When It Fires | Use Case |
|----------|---------------|----------|
| `onConversionDataSuccess` | First app launch after install | Deferred deep linking (install attribution) |
| `onAppOpenAttribution` | App opened via deep link (app installed) | Direct attribution |

For comprehensive affiliate tracking, implement both callbacks as shown in the examples.

## Configuration Placeholders

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `YOUR_APPSFLYER_DEV_KEY` | Your AppsFlyer Dev Key | From AppsFlyer dashboard |
| `YOUR_IOS_APP_ID` | iOS App ID (numbers only) | `123456789` |
| `YOUR_SUBDOMAIN` | OneLink subdomain | `yourapp` (from yourapp.onelink.me) |
| `YOUR_CUSTOM_SCHEME` | Custom URL scheme | `myapp` |

## Testing

Test your AppsFlyer deep link integration:

```bash
# Android Emulator
adb shell am start -W -a android.intent.action.VIEW -d "https://YOUR_SUBDOMAIN.onelink.me/LINK_ID/test"

# iOS Simulator
xcrun simctl openurl booted "https://YOUR_SUBDOMAIN.onelink.me/LINK_ID/test"
```

Check logs in Unity Console for:
- `[AppsFlyer] Affiliate set:`
- `[RevenueCat] Attribution set:` (or your IAP provider)

## Troubleshooting

**Problem:** App opens store instead of app
- **Solution:** Verify package name/bundle ID and certificate fingerprints in AppsFlyer OneLink settings

**Problem:** No attribution data
- **Solution:** Ensure AppsFlyer SDK initialization occurs before setting up callbacks

**Problem:** Deep links not working
- **Solution:** Check intent-filter/URL scheme configuration in manifest/plist

**Problem:** `deep_link_value` is null
- **Solution:** Log the full conversion data to see available fields; the data structure may vary

## Next Steps

After completing AppsFlyer integration:
1. Test deep link attribution with a test affiliate link
2. Verify affiliate identifier is stored correctly
3. Make a test purchase to confirm tracking works end-to-end

[Back to Main README](../README.md)
