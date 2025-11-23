# Insert Affiliate Unity SDK

![Version](https://img.shields.io/badge/version-1.0.0-brightgreen) ![Unity](https://img.shields.io/badge/Unity-2019.4%2B-blue)

## Overview

The **Insert Affiliate Unity SDK** provides seamless integration with the [Insert Affiliate platform](https://insertaffiliate.com) for Unity applications. Simplify affiliate marketing for Unity apps with in-app purchases, supporting both iOS and Android platforms.

### Features

- **Unique Device ID**: Creates anonymous identifiers for attribution tracking
- **Affiliate Identifier Management**: Set and retrieve affiliate identifiers from deep links
- **Short Code Support**: Easy-to-share alphanumeric codes for affiliates
- **Deep Linking**: Handle affiliate deep links and universal links
- **Event Tracking**: Track custom events for affiliate attribution
- **Offer Codes**: Dynamic product modifiers for discounts and trials
- **Attribution Timeout**: Optional time-based attribution windows

## Installation

### Option 1: Unity Package Manager (Local)

1. Open your Unity project
2. Go to `Window > Package Manager`
3. Click `+` > `Add package from disk`
4. Navigate to the SDK folder and select `package.json`

### Option 2: Manual Installation

1. Copy the `InsertAffiliateUnitySDK` folder to your project's `Packages` directory
2. Unity will automatically import the package

## Quick Start

### 1. Initialize the SDK

Add this to your app's initialization code (e.g., in `Awake()` or `Start()` of your main script):

```csharp
using InsertAffiliate;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        // Initialize with your company code
        InsertAffiliateSDK.Initialize(
            companyCode: "your_company_code_here",
            verboseLogging: true // Enable for debugging
        );
    }
}
```

Find your company code in your [Insert Affiliate dashboard settings](https://app.insertaffiliate.com/settings).

## In-App Purchase Setup [Required]

Insert Affiliate requires a Receipt Verification platform to validate in-app purchases. You must choose one of our supported partners:

### Option 1: RevenueCat Integration

#### 1. Code Setup

First, install the [RevenueCat Unity SDK](https://docs.revenuecat.com/docs/unity). Then set up attribution:

```csharp
using InsertAffiliate;
using RevenueCat;
using System.Collections.Generic;

public class IAPManager : MonoBehaviour
{
    void Start()
    {
        // Initialize Insert Affiliate SDK
        InsertAffiliateSDK.Initialize("your_company_code", verboseLogging: true);

        // Initialize RevenueCat
        var purchases = GetComponent<Purchases>();
        purchases.revenueCatAPIKeyApple = "your_revenuecat_api_key";

        // Set initial attribution if exists
        UpdateRevenueCatAttribution();

        // Listen for affiliate identifier changes
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

Replace `your_revenuecat_api_key` with your **RevenueCat API Key** from the [RevenueCat dashboard](https://app.revenuecat.com/).

#### 2. Webhook Setup

1. Go to RevenueCat and [create a new webhook](https://app.revenuecat.com/settings/integrations/webhooks)

2. Configure the webhook with these settings:
   - **Webhook URL:** `https://api.insertaffiliate.com/v1/api/revenuecat-webhook`
   - **Authorization header:** (You'll get this value in step 4)
   - **Event Type:** "All events"

3. In your [Insert Affiliate dashboard settings](https://app.insertaffiliate.com/settings):
   - Navigate to **Verification Settings**
   - Set the in-app purchase verification method to **RevenueCat**

4. Back in your Insert Affiliate dashboard:
   - Locate the `RevenueCat Webhook Authentication Header` value
   - Copy this value
   - Paste it as the **Authorization header** value in your RevenueCat webhook configuration

5. Save the webhook in RevenueCat

### Option 2: App Store Direct Integration (iOS Only)

Direct App Store integration allows iOS Unity apps to integrate with Apple's App Store without using a receipt verification platform like RevenueCat.

#### 1. Apple App Store Notification Setup

Visit [our docs](https://docs.insertaffiliate.com/direct-store-purchase-integration#1-apple-app-store-server-notifications) and complete the required setup steps for App Store Server to Server Notifications.

#### 2. Implementing Purchases with Unity IAP

```csharp
using InsertAffiliate;
using UnityEngine;
using UnityEngine.Purchasing;
using System.Collections.Generic;

public class IAPManager : MonoBehaviour, IStoreListener
{
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;
    private string pendingAppAccountToken;

    void Start()
    {
        InitializePurchasing();
    }

    void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Add your products
        builder.AddProduct("monthly_subscription", ProductType.Subscription);
        builder.AddProduct("yearly_subscription", ProductType.Subscription);
        builder.AddProduct("consumable_gems", ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
        Debug.Log("[IAP] Unity IAP initialized");
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[IAP] Initialization failed: {error}");
    }

    // Call this when user wants to purchase
    public void PurchaseProduct(string productId)
    {
        // Get app account token before purchase
        InsertAffiliateSDK.ReturnUserAccountTokenAndStoreExpectedTransaction((token) =>
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[IAP] Failed to get app account token");
                BuyProductWithToken(productId, null);
                return;
            }

            Debug.Log($"[IAP] Got app account token: {token}");
            BuyProductWithToken(productId, token);
        });
    }

    void BuyProductWithToken(string productId, string appAccountToken)
    {
        if (storeController == null)
        {
            Debug.LogError("[IAP] Store not initialized");
            return;
        }

        Product product = storeController.products.WithID(productId);

        if (product == null || !product.availableToPurchase)
        {
            Debug.LogError($"[IAP] Product unavailable: {productId}");
            return;
        }

        pendingAppAccountToken = appAccountToken;

#if UNITY_IOS
        // Pass app account token to StoreKit via Unity IAP
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

For testing, use a specific UUID:

```csharp
// Set global override
InsertAffiliateSDK.OverrideUserAccountToken("12345678-1234-1234-1234-123456789012");

// Or pass in method call
InsertAffiliateSDK.ReturnUserAccountTokenAndStoreExpectedTransaction((token) =>
{
    Debug.Log($"Test token: {token}");
}, overrideUUID: "12345678-1234-1234-1234-123456789012");
```

### Option 3: Apphud Integration

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
using InsertAffiliate;

// When you get the affiliate identifier
InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLinkUrl, (shortCode) =>
{
    if (!string.IsNullOrEmpty(shortCode))
    {
        // Set user property in Apphud
        ApphudManager apphudManager = FindObjectOfType<ApphudManager>();
        apphudManager?.SetInsertAffiliateAttribution(shortCode);
    }
});
```

### Option 4: Iaptic Integration

[Iaptic](https://iaptic.com) provides server-side receipt validation for in-app purchases.

**Setup**:

1. **Initialize Iaptic** with application username:
```csharp
using InsertAffiliate;

public class IapticManager : MonoBehaviour
{
    void Start()
    {
        // Get affiliate identifier
        string affiliateId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();

        // Initialize Iaptic with affiliate as application username
        InitializeIaptic(affiliateId);
    }

    void InitializeIaptic(string applicationUsername)
    {
        // Your Iaptic initialization code
        // Pass applicationUsername to Iaptic when making purchases
        // This allows server-side tracking of affiliate-driven purchases
    }
}
```

2. **Pass Affiliate on Purchase**:
```csharp
// When processing a purchase
string affiliateId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();

// Include in Iaptic purchase validation
ValidatePurchaseWithIaptic(receipt, affiliateId);
```

**Note**: Both Apphud and Iaptic receive the affiliate identifier to track which purchases came from which affiliates.

## Deep Link Setup [Required]

Insert Affiliate requires a Deep Linking platform to create links for your affiliates. Choose one of the following options:

### Option 1: Branch.io Integration

Branch.io is a popular deep linking platform. Here's how to integrate:

#### Basic Setup

1. Install [Branch Unity SDK](https://help.branch.io/developers-hub/docs/unity-basic-integration)
2. Create deep links in Branch dashboard
3. Handle Branch deep links in your app:

```csharp
using InsertAffiliate;
using BranchIO;
using System.Collections.Generic;

public class BranchManager : MonoBehaviour
{
    void Start()
    {
        // Initialize Branch
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

                        // Update RevenueCat attribution
                        var attributes = new Dictionary<string, string>
                        {
                            { "insert_affiliate", shortCode }
                        };
                        Purchases.shared.SetAttributes(attributes);
                    }
                });
            }
        });
    }
}
```

### Option 2: AppsFlyer Integration

Install the [AppsFlyer Unity SDK](https://dev.appsflyer.com/hc/docs/unity) and implement the integration:

```csharp
using InsertAffiliate;
using AppsFlyerSDK;
using System.Collections.Generic;

public class AppsFlyerManager : MonoBehaviour, IAppsFlyerConversionData
{
    void Start()
    {
        // Initialize AppsFlyer
        AppsFlyer.initSDK("your_appsflyer_dev_key", "your_app_id");
        AppsFlyer.startSDK();
    }

    // AppsFlyer callback
    public void onConversionDataSuccess(string conversionData)
    {
        Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(conversionData);

        if (data.ContainsKey("af_dp") || data.ContainsKey("deep_link_value"))
        {
            string deepLink = (data.ContainsKey("af_dp")) ?
                data["af_dp"] as string : data["deep_link_value"] as string;

            // Process with Insert Affiliate
            InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLink, (shortCode) =>
            {
                if (!string.IsNullOrEmpty(shortCode))
                {
                    Debug.Log($"[AppsFlyer] Affiliate set: {shortCode}");

                    // Update RevenueCat
                    var attributes = new Dictionary<string, string>
                    {
                        { "insert_affiliate", shortCode }
                    };
                    Purchases.shared.SetAttributes(attributes);
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
        // Handle deep links when app is already installed (deferred deep linking)
        Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(attributionData);

        if (data.ContainsKey("af_dp") || data.ContainsKey("deep_link_value"))
        {
            string deepLink = (data.ContainsKey("af_dp")) ?
                data["af_dp"] as string : data["deep_link_value"] as string;

            // Process with Insert Affiliate
            InsertAffiliateSDK.SetInsertAffiliateIdentifier(deepLink, (shortCode) =>
            {
                if (!string.IsNullOrEmpty(shortCode))
                {
                    Debug.Log($"[AppsFlyer] App open affiliate set: {shortCode}");

                    // Update RevenueCat
                    var attributes = new Dictionary<string, string>
                    {
                        { "insert_affiliate", shortCode }
                    };
                    Purchases.shared.SetAttributes(attributes);
                }
            });
        }
    }

    public void onAppOpenAttributionFailure(string error)
    {
        Debug.LogError($"[AppsFlyer] Attribution failed: {error}");
    }
}
```

### Option 3: Other Deep Linking Platforms

Insert Affiliate works with any deep linking provider. General steps:
1. Generate a deep link using your provider
2. Pass the deep link to your dashboard when an affiliate signs up
3. Extract the link in your app and call `SetInsertAffiliateIdentifier`

---

### Note: Insert Links (Not Currently Supported)

> ⚠️ **Insert Links** (our built-in deep linking solution) is **not currently supported** in the Unity SDK.
>
> If you need Insert Links support for Unity, please contact **michael@insertaffiliate.com** to request this feature.
>
> For now, please use one of the third-party deep linking options above (Branch.io, AppsFlyer, or other providers).

### 3. Basic Deep Link Handling

```csharp
using InsertAffiliate;

public class DeepLinkManager : MonoBehaviour
{
    void Start()
    {
        // Handle deep link on app start
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

## Core Features

### Verbose Logging

By default, the SDK operates silently. Enable verbose logging to see detailed debug information:

```csharp
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    verboseLogging: true  // Enable detailed logs
);
```

**When verbose logging is enabled, you'll see:**
- Deep link processing confirmations
- API request/response details
- Affiliate identifier changes
- Offer code retrieval
- Attribution timeout validation
- Error details with context

**Best Practices:**
- ✅ Enable for development and testing builds
- ✅ Enable for TestFlight/beta releases for easier debugging
- ❌ Disable for production App Store/Play Store releases

**Conditional Logging Example:**
```csharp
void Start()
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool verboseLogging = true;
    #else
    bool verboseLogging = false;
    #endif

    InsertAffiliateSDK.Initialize(
        companyCode: "your_company_code",
        verboseLogging: verboseLogging
    );
}
```

### Short Codes

Short codes are unique, 3-25 character alphanumeric identifiers that affiliates can use to promote your app. They're perfect for influencers who want to share codes in videos, social posts, or streams.

**Example Use Case:** An influencer promotes your app with the code "STREAMER2024" in their Twitch stream description. When users enter this code in your app, the subscription is attributed to that influencer for commission payouts.

#### Setting a Short Code

#### Recommended Usage with Validation Feedback

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

        // SetShortCode now validates against the API and provides callback
        InsertAffiliateSDK.SetShortCode(enteredCode, isValid =>
        {
            if (isValid)
            {
                ShowSuccess($"Promo code applied successfully!");

                // Check if there's an associated offer
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

    void ShowError(string message)
    {
        Debug.LogError(message);
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = Color.red;
        }
    }

    void ShowSuccess(string message)
    {
        Debug.Log(message);
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = Color.green;
        }
    }
}
```

#### Basic Usage (Without Validation Feedback)

```csharp
// Simple usage without callback
InsertAffiliateSDK.SetShortCode("PROMO123");
```

**Important Notes:**
- `SetShortCode` now validates the short code against the Insert Affiliate API before storing it
- The callback parameter receives `true` if the code exists and was successfully validated/stored
- The callback receives `false` if the code doesn't exist or validation fails
- Validation checks both format (length, alphanumeric) and existence in your affiliate database
- Use the callback to provide immediate feedback to users about code validity

**Short Code Requirements:**
- Between **3 and 25 characters**
- **Alphanumeric only** (letters and numbers, no special characters)
- Case insensitive (automatically converted to uppercase)

For more information, visit the [Insert Affiliate Short Codes Documentation](https://docs.insertaffiliate.com/short-codes).

### Getting Affiliate Details

You can retrieve detailed information about an affiliate by their short code or deep link using the `GetAffiliateDetails` method. This is useful for displaying affiliate information to users or showing personalized content based on the referrer.

```csharp
public class AffiliateInfoDisplay : MonoBehaviour
{
    public Text affiliateNameText;

    void Start()
    {
        // Get affiliate details for a specific code
        InsertAffiliateSDK.GetAffiliateDetails("PROMO123", details =>
        {
            if (details != null)
            {
                Debug.Log($"Affiliate Name: {details.affiliateName}");
                Debug.Log($"Short Code: {details.affiliateShortCode}");
                Debug.Log($"Deep Link: {details.deeplinkUrl}");

                // Update UI with affiliate name
                if (affiliateNameText != null)
                {
                    affiliateNameText.text = $"Referred by: {details.affiliateName}";
                }
            }
            else
            {
                Debug.Log("Affiliate not found");
            }
        });
    }
}
```

**Return Value:**

The callback receives an `AffiliateDetailsPublic` object with:
- `affiliateName`: The name of the affiliate
- `affiliateShortCode`: The affiliate's short code
- `deeplinkUrl`: The affiliate's deep link URL

Returns `null` if:
- The affiliate code doesn't exist
- The company code is not initialized
- There's a network error or API issue

**Important Notes:**
- This method **does not store or set** the affiliate identifier - it only retrieves information
- Use `SetShortCode()` to actually associate an affiliate with a user
- The method automatically strips UUIDs from codes (e.g., "ABC123-uuid" becomes "ABC123")
- Works with both short codes and deep link URLs

### Event Tracking (Beta)

Track custom events for affiliate attribution beyond just purchases. This helps you:
- Understand user behavior from affiliate traffic
- Measure the effectiveness of different affiliates
- Incentivize affiliates for designated actions (signups, levels completed, etc.)

**⚠️ Beta Feature:** This feature is currently in beta. While functional, we cannot guarantee it's fully resistant to tampering or manipulation.

```csharp
// Track when user signs up
InsertAffiliateSDK.TrackEvent("user_signup");

// Track game progression
InsertAffiliateSDK.TrackEvent("level_5_completed");
InsertAffiliateSDK.TrackEvent("tutorial_finished");

// Track engagement
InsertAffiliateSDK.TrackEvent("shared_on_social");
InsertAffiliateSDK.TrackEvent("invited_friend");
```

**Important:** You must set an affiliate identifier before tracking events. Events won't be tracked if no affiliate is associated with the user.

### Offer Codes / Dynamic Product IDs

The SDK lets you dynamically load different product IDs based on whether a user came through an affiliate link with an offer. This allows you to provide trial periods or discounts to users referred by specific affiliates.

**How It Works:**
When someone clicks an affiliate link or enters a short code linked to an offer (configured in your Insert Affiliate Dashboard), the SDK fills `InsertAffiliateSDK.OfferCode` with the modifier (like `_oneWeekFree`). You then append this to your base product ID to load the correct subscription variant.

#### Dashboard Setup

1. Go to [app.insertaffiliate.com/affiliates](https://app.insertaffiliate.com/affiliates)
2. Select the affiliate you want to configure
3. Click **"View"** to access their settings
4. Assign an **iOS IAP Modifier** or **Android IAP Modifier** (e.g., `_oneWeekFree`, `_threeMonthsFree`)
5. Save the settings

Once configured, users clicking that affiliate's links will automatically receive the modifier.

#### Implementation with RevenueCat

```csharp
using InsertAffiliate;
using RevenueCat;

public class ProductManager : MonoBehaviour
{
    private const string BASE_PRODUCT_ID = "monthly_premium";

    public void LoadProducts()
    {
        // Get the dynamic product ID based on offer code
        string productId = GetDynamicProductId();

        Debug.Log($"Loading product: {productId}");

        // Load product from RevenueCat
        Purchases.shared.GetProducts(new[] { productId }, (products, error) =>
        {
            if (error != null)
            {
                Debug.LogError($"Failed to load products: {error.Message}");

                // Fallback: Try loading base product if offer product doesn't exist
                if (!string.IsNullOrEmpty(InsertAffiliateSDK.OfferCode))
                {
                    Debug.Log($"Falling back to base product: {BASE_PRODUCT_ID}");
                    Purchases.shared.GetProducts(new[] { BASE_PRODUCT_ID }, (baseProducts, baseError) =>
                    {
                        if (baseError == null && baseProducts.Count > 0)
                        {
                            DisplayProduct(baseProducts[0]);
                        }
                    });
                }
                return;
            }

            if (products.Count > 0)
            {
                DisplayProduct(products[0]);
            }
        });
    }

    string GetDynamicProductId()
    {
        string offerCode = InsertAffiliateSDK.OfferCode;

        if (!string.IsNullOrEmpty(offerCode))
        {
            // Clean the offer code (remove any quotes or whitespace)
            offerCode = offerCode.Trim().Trim('"', '\'');
            return BASE_PRODUCT_ID + offerCode;
        }

        return BASE_PRODUCT_ID;
    }

    void DisplayProduct(StoreProduct product)
    {
        Debug.Log($"Product loaded: {product.Identifier}");
        Debug.Log($"Price: {product.PriceString}");

        // Update your UI with product details
    }
}
```

#### Example Product Identifiers

Setup these product IDs in your App Store Connect / Google Play Console:
- Base product: `monthly_premium`
- With trial offer: `monthly_premium_oneWeekFree`
- With discount: `monthly_premium_50percentOff`
- With extended trial: `monthly_premium_threeMonthsFree`

#### Best Practices

- **Always implement fallback:** If the offer product doesn't exist, fall back to the base product
- **Call in purchase views:** Check for offer codes whenever showing purchase options
- **Handle both cases:** Ensure your app works whether an offer code is present or not
- **Test thoroughly:** Test with and without offer codes to ensure smooth experience

### Attribution Timeout Control

By default, affiliate attribution has **no timeout** - once set, the attribution remains valid indefinitely. However, you can configure an attribution timeout to limit how long after an affiliate link click that purchases can be attributed to that affiliate.

#### Enable Attribution Timeout

```csharp
using InsertAffiliate;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        // Set 7 days (604800 seconds) attribution timeout
        InsertAffiliateSDK.Initialize(
            companyCode: "your_company_code",
            affiliateAttributionActiveTime: 604800f // 7 days in seconds
        );
    }
}
```

#### Common Attribution Timeout Values

```csharp
// 1 day timeout
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    affiliateAttributionActiveTime: 86400f // 24 hours
);

// 7 days timeout (recommended for most apps)
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    affiliateAttributionActiveTime: 604800f // 7 days
);

// 30 days timeout
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    affiliateAttributionActiveTime: 2592000f // 30 days
);

// 90 days timeout
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    affiliateAttributionActiveTime: 7776000f // 90 days
);

// No timeout (default behavior)
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code"
    // affiliateAttributionActiveTime not specified = no timeout
);
```

#### Verbose Logging for Debugging Attribution Timeout

Enable verbose logging to see detailed attribution timeout information:

```csharp
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    verboseLogging: true, // Shows timeout validation details
    affiliateAttributionActiveTime: 604800f // 7 days
);
```

When enabled, you'll see console logs showing:
- When attribution is checked
- Time elapsed since attribution was stored
- Whether attribution has expired
- Remaining time until expiration

#### Additional Attribution Methods

```csharp
// Get affiliate identifier (respects timeout by default)
string identifier = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();
// Returns null if attribution has expired

// Get affiliate identifier ignoring timeout (for debugging)
string rawIdentifier = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier(ignoreTimeout: true);
// Always returns the stored identifier, even if expired

// Check if current attribution is still valid
bool isValid = InsertAffiliateSDK.IsAffiliateAttributionValid();
// Returns false if attribution has expired or doesn't exist

// Get when affiliate was stored
DateTime? storedDate = InsertAffiliateSDK.GetAffiliateStoredDate();
// Returns the exact datetime when the affiliate was stored

// Example: Show attribution status
if (storedDate.HasValue)
{
    TimeSpan elapsed = DateTime.UtcNow - storedDate.Value;
    Debug.Log($"Affiliate stored {elapsed.Days} days ago");

    if (isValid)
    {
        Debug.Log($"Attribution is active: {identifier}");
    }
    else
    {
        Debug.Log("Attribution has expired");
    }
}
```

## API Reference

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
// Track event
InsertAffiliateSDK.TrackEvent(string eventName)

// Get offer code
string offerCode = InsertAffiliateSDK.OfferCode

// Handle Insert Links deep link
bool handled = InsertAffiliateSDK.HandleInsertLinks(string url)

// Check if initialized
bool isInit = InsertAffiliateSDK.IsInitialized()
```

### Events

```csharp
// Subscribe to affiliate identifier changes
InsertAffiliateSDK.OnAffiliateIdentifierChanged += (identifier) =>
{
    Debug.Log($"Affiliate changed: {identifier}");
    // Update your IAP provider attribution
};
```

## Integration Examples

### RevenueCat Integration

```csharp
using InsertAffiliate;
using RevenueCat;

public class IAPManager : MonoBehaviour
{
    void Start()
    {
        // Initialize Insert Affiliate
        InsertAffiliateSDK.Initialize("your_company_code");

        // Initialize RevenueCat
        Purchases.Configure("your_revenuecat_api_key");

        // Set attribution
        UpdateRevenueCatAttribution();

        // Listen for changes
        InsertAffiliateSDK.OnAffiliateIdentifierChanged += (id) =>
        {
            UpdateRevenueCatAttribution();
        };
    }

    void UpdateRevenueCatAttribution()
    {
        string affiliateId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();
        if (!string.IsNullOrEmpty(affiliateId))
        {
            // Set RevenueCat attributes
            // (Method depends on RevenueCat Unity SDK version)
        }
    }
}
```

### Dynamic Product IDs with Offers

```csharp
using InsertAffiliate;

public class ProductManager : MonoBehaviour
{
    private const string BASE_PRODUCT_ID = "monthly_premium";

    public string GetProductId()
    {
        string offerCode = InsertAffiliateSDK.OfferCode;

        if (!string.IsNullOrEmpty(offerCode))
        {
            return BASE_PRODUCT_ID + offerCode;
        }

        return BASE_PRODUCT_ID;
    }

    public void LoadProduct()
    {
        string productId = GetProductId();
        Debug.Log($"Loading product: {productId}");
        // Load product from your IAP provider
    }
}
```

## Platform-Specific Setup

### iOS

1. Configure URL scheme in Unity:
   - Build Settings > iOS > Other Settings
   - Add your URL scheme (e.g., `ia-yourcompanycode`)

2. In Xcode after build:
   - Verify `Info.plist` has URL scheme
   - Add associated domains for universal links (if using)

### Android

1. Unity automatically adds deep link intent filters
2. Verify in `AndroidManifest.xml` after build:
   ```xml
   <intent-filter>
       <action android:name="android.intent.action.VIEW" />
       <category android:name="android.intent.category.DEFAULT" />
       <category android:name="android.intent.category.BROWSABLE" />
       <data android:scheme="ia-yourcompanycode" />
   </intent-filter>
   ```

## Testing

### Test Deep Links (iOS Simulator)

```bash
xcrun simctl openurl booted "ia-yourcompanycode://testshortcode"
```

### Test Deep Links (Android)

```bash
adb shell am start -W -a android.intent.action.VIEW -d "ia-yourcompanycode://testshortcode"
```

### Enable Verbose Logging

```csharp
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    verboseLogging: true  // See detailed logs in console
);
```

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
