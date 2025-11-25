# Insert Affiliate Unity SDK

![Version](https://img.shields.io/badge/version-1.0.0-brightgreen) ![Unity](https://img.shields.io/badge/Unity-2019.4%2B-blue)

The **Insert Affiliate Unity SDK** provides seamless integration with the [Insert Affiliate platform](https://insertaffiliate.com) for Unity applications. Simplify affiliate marketing for Unity apps with in-app purchases, supporting both iOS and Android platforms.

## Table of Contents

- [🚀 Quick Start (5 Minutes)](#-quick-start-5-minutes)
- [⚙️ Essential Setup](#️-essential-setup)
  - [Choose Your IAP Verification Platform](#choose-your-iap-verification-platform)
  - [Choose Your Deep Linking Platform](#choose-your-deep-linking-platform)
- [✅ Verification Checklist](#-verification-checklist)
- [🔧 Advanced Features](#-advanced-features)
- [🔍 Troubleshooting](#-troubleshooting)
- [📚 API Reference](#-api-reference)

---

## 🚀 Quick Start (5 Minutes)

Get Insert Affiliate running in your Unity app with minimal configuration.

### 1. Install the SDK

**Option A: Unity Package Manager (Local)**
1. Open your Unity project
2. Go to `Window > Package Manager`
3. Click `+` > `Add package from disk`
4. Navigate to the SDK folder and select `package.json`

**Option B: Manual Installation**
1. Copy the `InsertAffiliateUnitySDK` folder to your project's `Packages` directory
2. Unity will automatically import the package

### 2. Initialize the SDK

Add this to your app's initialization code (e.g., in `Awake()` or `Start()` of your main script):

```csharp
using InsertAffiliate;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        InsertAffiliateSDK.Initialize(
            companyCode: "your_company_code_here",
            verboseLogging: true // Enable for debugging
        );
    }
}
```

Find your company code in your [Insert Affiliate dashboard settings](https://app.insertaffiliate.com/settings).

### 3. Test It Works

```csharp
// Check SDK is initialized
Debug.Log($"SDK Initialized: {InsertAffiliateSDK.IsInitialized()}");

// Test with a short code
InsertAffiliateSDK.SetShortCode("TEST123", isValid =>
{
    Debug.Log($"Short code valid: {isValid}");
});
```

**Next Steps:** Complete the [Essential Setup](#️-essential-setup) below to enable purchase tracking and deep linking.

---

## ⚙️ Essential Setup

### Choose Your IAP Verification Platform

Insert Affiliate requires a Receipt Verification platform to validate in-app purchases and attribute them to affiliates:

| Platform | Platforms | Best For |
|----------|-----------|----------|
| [RevenueCat](#option-1-revenuecat) | iOS, Android | Subscription-focused apps with cross-platform needs |
| [App Store Direct](#option-2-app-store-direct-ios) | iOS | Direct Apple integration without third-party services |
| [Google Play Direct](#option-3-google-play-direct-android) | Android | Direct Google integration without third-party services |
| [Apphud](#option-4-apphud) | iOS, Android | Subscription analytics and A/B testing |
| [Iaptic](#option-5-iaptic) | iOS, Android | Server-side receipt validation |

---

### Option 1: RevenueCat

<details>
<summary><strong>View RevenueCat Setup</strong></summary>

#### Code Setup

First, install the [RevenueCat Unity SDK](https://docs.revenuecat.com/docs/unity). Then set up attribution:

```csharp
using InsertAffiliate;
using RevenueCat;
using System.Collections.Generic;

public class IAPManager : MonoBehaviour
{
    void Start()
    {
        InsertAffiliateSDK.Initialize("your_company_code", verboseLogging: true);

        var purchases = GetComponent<Purchases>();
        purchases.revenueCatAPIKeyApple = "your_revenuecat_api_key";

        UpdateRevenueCatAttribution();

        InsertAffiliateSDK.SetInsertAffiliateIdentifierChangeCallback((identifier) =>
        {
            if (!string.IsNullOrEmpty(identifier))
            {
                UpdateRevenueCatAttribution();
            }
        });
    }

    void UpdateRevenueCatAttribution()
    {
        string affiliateId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();
        if (!string.IsNullOrEmpty(affiliateId))
        {
            var attributes = new Dictionary<string, string>
            {
                { "insert_affiliate", affiliateId }
            };
            Purchases.shared.SetAttributes(attributes);
            Debug.Log($"[IAP] RevenueCat attribution set: {affiliateId}");
        }
    }
}
```

#### Webhook Setup

1. Go to RevenueCat and [create a new webhook](https://app.revenuecat.com/settings/integrations/webhooks)
2. Configure the webhook:
   - **Webhook URL:** `https://api.insertaffiliate.com/v1/api/revenuecat-webhook`
   - **Authorization header:** (Get this from step 4)
   - **Event Type:** "All events"
3. In your [Insert Affiliate dashboard](https://app.insertaffiliate.com/settings), set IAP verification to **RevenueCat**
4. Copy the `RevenueCat Webhook Authentication Header` from Insert Affiliate and paste as the Authorization header in RevenueCat
5. Save the webhook

</details>

---

### Option 2: App Store Direct (iOS)

<details>
<summary><strong>View App Store Direct Setup</strong></summary>

Direct App Store integration allows iOS Unity apps to integrate with Apple's App Store without a third-party receipt verification platform.

#### 1. Server Notification Setup

Visit [our docs](https://docs.insertaffiliate.com/direct-store-purchase-integration#1-apple-app-store-server-notifications) to configure App Store Server Notifications.

#### 2. Implementation with Unity IAP

```csharp
using InsertAffiliate;
using UnityEngine;
using UnityEngine.Purchasing;
using System.Collections.Generic;

public class IAPManager : MonoBehaviour, IStoreListener
{
    private IStoreController storeController;
    private string pendingAppAccountToken;

    void Start()
    {
        InitializePurchasing();
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
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[IAP] Initialization failed: {error}");
    }

    public void PurchaseProduct(string productId)
    {
        InsertAffiliateSDK.ReturnUserAccountTokenAndStoreExpectedTransaction((token) =>
        {
            pendingAppAccountToken = token;
            BuyProductWithToken(productId, token);
        });
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
        pendingAppAccountToken = null;
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogError($"[IAP] Purchase failed: {product.definition.id}, {reason}");
        pendingAppAccountToken = null;
    }
}
```

#### 3. Testing with Override UUID

```csharp
InsertAffiliateSDK.OverrideUserAccountToken("12345678-1234-1234-1234-123456789012");
```

</details>

---

### Option 3: Google Play Direct (Android)

<details>
<summary><strong>View Google Play Direct Setup</strong></summary>

#### 1. Server Notification Setup

Visit [our docs](https://docs.insertaffiliate.com/direct-store-purchase-integration#google-play-real-time-developer-notifications) to configure Google Play Real-Time Developer Notifications.

#### 2. Implementation

```csharp
using InsertAffiliate;
using UnityEngine;
using UnityEngine.Purchasing;

public class GooglePlayIAPManager : MonoBehaviour, IStoreListener
{
    private IStoreController storeController;

    void Start()
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
#if UNITY_ANDROID
        string purchaseToken = ExtractPurchaseToken(args.purchasedProduct.receipt);
        if (!string.IsNullOrEmpty(purchaseToken))
        {
            InsertAffiliateSDK.StoreExpectedStoreTransaction(purchaseToken);
        }
#endif
        return PurchaseProcessingResult.Complete;
    }

    string ExtractPurchaseToken(string receipt)
    {
        // Parse the receipt JSON to extract purchase token
        // Implementation depends on your JSON parsing approach
        return "";
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogError($"[IAP] Purchase failed: {product.definition.id}, {reason}");
    }
}
```

</details>

---

### Option 4: Apphud

<details>
<summary><strong>View Apphud Setup</strong></summary>

[Apphud](https://apphud.com) is a subscription analytics platform that simplifies in-app purchase management.

**iOS Setup** (using native bridge):

1. **Add Apphud to CocoaPods** (`ios-build/Podfile`):
```ruby
pod 'ApphudSDK', '~> 3.4.0'
```

2. **Create Native Bridge** (`Assets/Plugins/iOS/ApphudBridge.m`):
```objc
#import <Foundation/Foundation.h>
#import <objc/runtime.h>
#import <objc/message.h>

extern "C" {
    void _ApphudStart(const char* apiKey) {
        @autoreleasepool {
            NSString *apiKeyStr = [NSString stringWithUTF8String:apiKey];
            Class apphudClass = NSClassFromString(@"ApphudSDK.Apphud") ?: NSClassFromString(@"Apphud");
            if (apphudClass) {
                ((void (*)(Class, SEL, NSString *))objc_msgSend)(apphudClass, NSSelectorFromString(@"startWithApiKey:"), apiKeyStr);
            }
        }
    }

    void _ApphudSetUserProperty(const char* key, const char* value) {
        @autoreleasepool {
            NSString *keyStr = [NSString stringWithUTF8String:key];
            NSString *valueStr = [NSString stringWithUTF8String:value];
            Class apphudClass = NSClassFromString(@"ApphudSDK.Apphud") ?: NSClassFromString(@"Apphud");
            if (apphudClass) {
                SEL selector = NSSelectorFromString(@"setUserPropertyWithKey:value:setOnce:");
                Class propertyKeyClass = NSClassFromString(@"ApphudSDK.ApphudUserPropertyKey") ?: NSClassFromString(@"ApphudUserPropertyKey");
                id propertyKey = ((id (*)(Class, SEL))objc_msgSend)(propertyKeyClass, NSSelectorFromString(@"alloc"));
                propertyKey = ((id (*)(id, SEL, NSString *))objc_msgSend)(propertyKey, NSSelectorFromString(@"initWithKey:"), keyStr);
                BOOL setOnce = NO;
                ((void (*)(Class, SEL, id, NSString *, BOOL))objc_msgSend)(apphudClass, selector, propertyKey, valueStr, setOnce);
            }
        }
    }
}
```

3. **Create Unity Manager** (`Assets/Scripts/ApphudManager.cs`):
```csharp
using System.Runtime.InteropServices;
using UnityEngine;

public class ApphudManager : MonoBehaviour
{
    private const string APPHUD_API_KEY = "your_apphud_api_key";

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _ApphudStart(string apiKey);

    [DllImport("__Internal")]
    private static extern void _ApphudSetUserProperty(string key, string value);
#endif

    void Start()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _ApphudStart(APPHUD_API_KEY);
#endif
    }

    public void SetInsertAffiliateAttribution(string shortCode)
    {
#if UNITY_IOS && !UNITY_EDITOR
        _ApphudSetUserProperty("insert_affiliate", shortCode);
#endif
    }
}
```

4. **Pass Affiliate to Apphud**:
```csharp
InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLinkUrl, (shortCode) =>
{
    if (!string.IsNullOrEmpty(shortCode))
    {
        ApphudManager apphudManager = FindObjectOfType<ApphudManager>();
        apphudManager?.SetInsertAffiliateAttribution(shortCode);
    }
});
```

</details>

---

### Option 5: Iaptic

<details>
<summary><strong>View Iaptic Setup</strong></summary>

[Iaptic](https://iaptic.com) provides server-side receipt validation for in-app purchases.

```csharp
using InsertAffiliate;

public class IapticManager : MonoBehaviour
{
    void Start()
    {
        string affiliateId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();
        InitializeIaptic(affiliateId);
    }

    void InitializeIaptic(string applicationUsername)
    {
        // Your Iaptic initialization code
        // Pass applicationUsername to Iaptic when making purchases
    }

    void OnPurchaseComplete(string receipt)
    {
        string affiliateId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();
        ValidatePurchaseWithIaptic(receipt, affiliateId);
    }

    void ValidatePurchaseWithIaptic(string receipt, string affiliateId)
    {
        // Include affiliateId in Iaptic purchase validation
    }
}
```

</details>

---

### Choose Your Deep Linking Platform

Insert Affiliate requires a Deep Linking platform to create links for your affiliates:

| Platform | Best For | Guide |
|----------|----------|-------|
| Branch.io | Full-featured deep linking with analytics | [View Guide](docs/deep-linking-branch.md) |
| AppsFlyer | Marketing attribution with deep linking | [View Guide](docs/deep-linking-appsflyer.md) |
| Other Providers | Custom deep linking solutions | [Basic Setup](#basic-deep-link-handling) |

> ⚠️ **Note:** Insert Links (our built-in deep linking solution) is not currently supported in the Unity SDK. Please use one of the third-party deep linking options above. Contact michael@insertaffiliate.com if you need Insert Links support for Unity.

### Basic Deep Link Handling

```csharp
using InsertAffiliate;

public class DeepLinkManager : MonoBehaviour
{
    void Start()
    {
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            HandleDeepLink(Application.absoluteURL);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !string.IsNullOrEmpty(Application.absoluteURL))
        {
            HandleDeepLink(Application.absoluteURL);
        }
    }

    void HandleDeepLink(string url)
    {
        InsertAffiliateSDK.SetInsertAffiliateIdentifier(url, (shortCode) =>
        {
            if (!string.IsNullOrEmpty(shortCode))
            {
                Debug.Log($"Affiliate set: {shortCode}");
                // Update your IAP provider attribution here
            }
        });
    }
}
```

---

## ✅ Verification Checklist

Before going live, verify your integration:

- [ ] SDK initializes without errors (`InsertAffiliateSDK.IsInitialized()` returns `true`)
- [ ] Deep links are captured and processed correctly
- [ ] Affiliate identifier is stored (`ReturnInsertAffiliateIdentifier()` returns value)
- [ ] IAP provider receives affiliate attribution
- [ ] Test purchase completes and appears in Insert Affiliate dashboard
- [ ] Short codes validate correctly (`SetShortCode` callback returns `true`)

---

## 🔧 Advanced Features

<details>
<summary><strong>Short Codes</strong></summary>

Short codes are unique, 3-25 character alphanumeric identifiers that affiliates can use to promote your app. Perfect for influencers sharing codes in videos, social posts, or streams.

```csharp
public class PromoCodeUI : MonoBehaviour
{
    public InputField codeInputField;
    public Text feedbackText;

    public void OnApplyCodeButtonClicked()
    {
        string enteredCode = codeInputField.text;

        if (string.IsNullOrEmpty(enteredCode))
        {
            ShowError("Please enter a promo code");
            return;
        }

        InsertAffiliateSDK.SetShortCode(enteredCode, isValid =>
        {
            if (isValid)
            {
                ShowSuccess("Promo code applied successfully!");

                string offerCode = InsertAffiliateSDK.OfferCode;
                if (!string.IsNullOrEmpty(offerCode))
                {
                    ShowSuccess($"You've unlocked a special offer: {offerCode}");
                }
            }
            else
            {
                ShowError("Invalid promo code. Please check and try again.");
            }
        });
    }

    void ShowError(string message) { /* Update UI */ }
    void ShowSuccess(string message) { /* Update UI */ }
}
```

**Requirements:**
- 3-25 characters
- Alphanumeric only (no special characters)
- Case insensitive

For more information, visit the [Insert Affiliate Short Codes Documentation](https://docs.insertaffiliate.com/short-codes).

</details>

<details>
<summary><strong>Getting Affiliate Details</strong></summary>

Retrieve detailed information about an affiliate:

```csharp
InsertAffiliateSDK.GetAffiliateDetails("PROMO123", details =>
{
    if (details != null)
    {
        Debug.Log($"Affiliate Name: {details.affiliateName}");
        Debug.Log($"Short Code: {details.affiliateShortCode}");
        Debug.Log($"Deep Link: {details.deeplinkUrl}");
    }
    else
    {
        Debug.Log("Affiliate not found");
    }
});
```

**Note:** This method only retrieves information—it does not store or set the affiliate identifier.

</details>

<details>
<summary><strong>Dynamic Offer Codes</strong></summary>

Load different product IDs based on affiliate offers. See the [complete guide](docs/dynamic-offer-codes.md).

```csharp
private const string BASE_PRODUCT_ID = "monthly_premium";

public string GetDynamicProductId()
{
    string offerCode = InsertAffiliateSDK.OfferCode;

    if (!string.IsNullOrEmpty(offerCode))
    {
        offerCode = offerCode.Trim().Trim('"', '\'');
        return BASE_PRODUCT_ID + offerCode;
    }

    return BASE_PRODUCT_ID;
}
```

</details>

<details>
<summary><strong>Event Tracking (Beta)</strong></summary>

Track custom events for affiliate attribution beyond just purchases.

**⚠️ Beta Feature:** This feature is currently in beta. While functional, we cannot guarantee it's fully resistant to tampering.

```csharp
InsertAffiliateSDK.TrackEvent("user_signup");
InsertAffiliateSDK.TrackEvent("level_5_completed");
InsertAffiliateSDK.TrackEvent("tutorial_finished");
InsertAffiliateSDK.TrackEvent("shared_on_social");
```

**Important:** You must set an affiliate identifier before tracking events.

</details>

<details>
<summary><strong>Attribution Timeout Control</strong></summary>

By default, affiliate attribution has no timeout. Configure a timeout to limit attribution windows:

```csharp
// 7 days timeout (recommended)
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    affiliateAttributionActiveTime: 604800f // 7 days in seconds
);

// Common values:
// 1 day:   86400f
// 7 days:  604800f
// 30 days: 2592000f
// 90 days: 7776000f
```

**Check attribution status:**
```csharp
bool isValid = InsertAffiliateSDK.IsAffiliateAttributionValid();
DateTime? storedDate = InsertAffiliateSDK.GetAffiliateStoredDate();

// Get identifier ignoring timeout (for debugging)
string rawIdentifier = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier(ignoreTimeout: true);
```

</details>

<details>
<summary><strong>Verbose Logging</strong></summary>

Enable detailed debug logging:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
bool verboseLogging = true;
#else
bool verboseLogging = false;
#endif

InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    verboseLogging: verboseLogging
);
```

**When enabled, you'll see:**
- Deep link processing confirmations
- API request/response details
- Affiliate identifier changes
- Offer code retrieval
- Attribution timeout validation
- Error details with context

</details>

---

## 🔍 Troubleshooting

<details>
<summary><strong>SDK not initializing</strong></summary>

- Verify your company code is correct
- Check Unity console for error messages
- Ensure SDK files are properly imported

</details>

<details>
<summary><strong>Deep links not working</strong></summary>

- iOS: Verify URL scheme in Info.plist and associated domains
- Android: Check intent-filter in AndroidManifest.xml
- Test with simulator/emulator commands:
  ```bash
  # iOS
  xcrun simctl openurl booted "ia-yourcompanycode://testshortcode"

  # Android
  adb shell am start -W -a android.intent.action.VIEW -d "ia-yourcompanycode://testshortcode"
  ```

</details>

<details>
<summary><strong>Affiliate identifier not persisting</strong></summary>

- Check if attribution timeout is configured and has expired
- Verify `SetInsertAffiliateIdentifier` callback received a valid short code
- Enable verbose logging to see storage operations

</details>

<details>
<summary><strong>Purchases not being attributed</strong></summary>

- Verify webhook is configured correctly in your IAP provider
- Check that affiliate identifier is set before purchase
- Confirm IAP provider is receiving the `insert_affiliate` attribute
- Test with verbose logging enabled

</details>

---

## 📚 API Reference

### Initialization

```csharp
InsertAffiliateSDK.Initialize(
    string companyCode,
    bool verboseLogging = false,
    bool insertLinksEnabled = false,
    float? affiliateAttributionActiveTime = null
)
```

### Attribution Methods

```csharp
// Set affiliate from referring link
InsertAffiliateSDK.SetInsertAffiliateIdentifier(string referringLink, Action<string> callback)

// Set short code with validation
InsertAffiliateSDK.SetShortCode(string shortCode, Action<bool> callback = null)

// Get affiliate details
InsertAffiliateSDK.GetAffiliateDetails(string affiliateCode, Action<AffiliateDetailsPublic> callback)

// Get current affiliate identifier
string identifier = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier(bool ignoreTimeout = false)

// Check if attribution is valid
bool isValid = InsertAffiliateSDK.IsAffiliateAttributionValid()

// Get stored date
DateTime? date = InsertAffiliateSDK.GetAffiliateStoredDate()
```

### Features

```csharp
// Track event (Beta)
InsertAffiliateSDK.TrackEvent(string eventName)

// Get offer code
string offerCode = InsertAffiliateSDK.OfferCode

// Check if initialized
bool isInit = InsertAffiliateSDK.IsInitialized()
```

### Events

```csharp
// Subscribe to affiliate identifier changes
InsertAffiliateSDK.OnAffiliateIdentifierChanged += (identifier) =>
{
    Debug.Log($"Affiliate changed: {identifier}");
};
```

---

## Platform-Specific Setup

### iOS

1. Configure URL scheme in Unity Build Settings > iOS > Other Settings
2. In Xcode after build:
   - Verify `Info.plist` has URL scheme
   - Add associated domains for universal links (if using)

### Android

Verify in `AndroidManifest.xml` after build:
```xml
<intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="ia-yourcompanycode" />
</intent-filter>
```

---

## Requirements

- Unity 2019.4 or later
- iOS 11.0+ (for iOS builds)
- Android API 22+ (for Android builds)

## Support

- Documentation: https://docs.insertaffiliate.com
- Dashboard: https://app.insertaffiliate.com
- Email: michael@insertaffiliate.com

## License

MIT License - see LICENSE file for details

## Related SDKs

- [Swift iOS SDK](https://github.com/Insert-Affiliate/InsertAffiliateSwiftSDK)
- [React Native SDK](../InsertAffiliateReactNativeSDK)
- [Flutter SDK](../insert_affiliate_flutter_sdk)
- [Android SDK](../InsertAffiliateAndroid)
