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
- [🎁 In-App Referrals](#-in-app-referrals)
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
            verboseLogging: true,
            insertLinksEnabled: true,
            insertLinksClipboardEnabled: true
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

First, install the [RevenueCat Unity SDK](https://docs.revenuecat.com/docs/unity). Then set up attribution using RevenueCat Targeting:

```csharp
using InsertAffiliate;
using RevenueCat;
using System.Collections.Generic;

public class IAPManager : MonoBehaviour
{
    void Start()
    {
        // Initialize with 7-day timeout (604800 seconds)
        InsertAffiliateSDK.Initialize(
            companyCode: "your_company_code",
            verboseLogging: true,
            affiliateAttributionActiveTime: 604800f  // 7 days
        );

        var purchases = GetComponent<Purchases>();
        purchases.revenueCatAPIKeyApple = "your_revenuecat_api_key";

        // Set up callback for when affiliate changes
        InsertAffiliateSDK.SetInsertAffiliateIdentifierChangeCallback((identifier, offerCode) =>
        {
            if (!string.IsNullOrEmpty(identifier))
            {
                UpdateRevenueCatAttribution(identifier, offerCode);
            }
        });

        // Check for existing affiliate on startup
        string existingId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();
        if (!string.IsNullOrEmpty(existingId))
        {
            UpdateRevenueCatAttribution(existingId, InsertAffiliateSDK.OfferCode);
        }
    }

    async void UpdateRevenueCatAttribution(string affiliateId, string offerCode)
    {
        // OPTIONAL: Prevent attribution for existing subscribers
        // Uncomment to ensure affiliates only earn from users they actually brought:
        // var customerInfo = await Purchases.shared.GetCustomerInfo();
        // if (customerInfo.Entitlements.Active.Count > 0) return; // Already subscribed

        var attributes = new Dictionary<string, string>
        {
            { "insert_affiliate", affiliateId }
        };

        // Add expiry timestamp for RevenueCat targeting rules
        long? expiryTimestamp = InsertAffiliateSDK.GetAffiliateExpiryTimestamp();
        if (expiryTimestamp.HasValue)
        {
            attributes["insert_timedout"] = expiryTimestamp.Value.ToString();
        }

        // Add offer code for RevenueCat targeting (shows different offerings based on affiliate)
        if (!string.IsNullOrEmpty(offerCode))
        {
            attributes["affiliateOfferCode"] = offerCode;
        }

        Purchases.shared.SetAttributes(attributes);

        // IMPORTANT: Sync attributes AND reload offerings for targeting to work
        // Offerings must be fetched AFTER sync completes to get targeted offering
        await Purchases.shared.SyncAttributesAndOfferingsIfNeeded();

        // Now fetch offerings - targeting will return the correct offering based on affiliateOfferCode
        var offerings = await Purchases.shared.GetOfferings();
        if (offerings.Current != null)
        {
            Debug.Log($"[IAP] Current offering (with targeting): {offerings.Current.Identifier}");
            // Use offerings.Current.AvailablePackages to display products
        }

        Debug.Log($"[IAP] RevenueCat attribution set: {affiliateId}, offerCode: {offerCode}");
    }
}
```

#### Using RevenueCat Targeting

RevenueCat Targeting automatically shows different offerings based on user attributes. The flow is:

1. **Set attributes** (`affiliateOfferCode`, etc.)
2. **Call `SyncAttributesAndOfferingsIfNeeded()`** - syncs attributes to RevenueCat
3. **Call `GetOfferings()` AFTER sync completes** - returns the targeted offering

**Setup in RevenueCat Dashboard:**
1. Go to **Targeting** in RevenueCat dashboard
2. Create a rule: `affiliateOfferCode` is any of `["oneWeekFree"]`
3. Assign the offering (e.g., `oneMonthSubscription_oneWeekFree`)
4. Save

**Important:** Always fetch offerings AFTER `SyncAttributesAndOfferingsIfNeeded()` completes. If you fetch before sync, you'll get the default offering instead of the targeted one.

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

Insert Affiliate supports multiple deep linking options:

| Platform | Best For | Guide |
|----------|----------|-------|
| **Insert Links (Recommended)** | Built-in deep linking with automatic attribution | [Setup below](#insert-links-setup) |
| Branch.io | Full-featured deep linking with analytics | [View Guide](docs/deep-linking-branch.md) |
| AppsFlyer | Marketing attribution with deep linking | [View Guide](docs/deep-linking-appsflyer.md) |
| Other Providers | Custom deep linking solutions | [Basic Setup](#basic-deep-link-handling) |

### Insert Links Setup

Insert Links is our built-in deep linking solution. It supports Universal Links (iOS), App Links (Android), custom URL schemes, clipboard matching, fingerprint-based deferred deep linking, and Google Play Install Referrer.

#### 1. Enable Insert Links in SDK Initialization

```csharp
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code_here",
    verboseLogging: true,
    insertLinksEnabled: true,
    insertLinksClipboardEnabled: true,
    affiliateAttributionActiveTime: 604800f,
    preventAffiliateTransfer: true
);
```

#### 2. Handle Deep Links

```csharp
using InsertAffiliate;

public class DeepLinkManager : MonoBehaviour
{
    void Start()
    {
        Application.deepLinkActivated += OnDeepLinkActivated;

        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            OnDeepLinkActivated(Application.absoluteURL);
        }
    }

    void OnDeepLinkActivated(string url)
    {
        InsertAffiliateSDK.HandleInsertLinks(url);
    }

    void OnDestroy()
    {
        Application.deepLinkActivated -= OnDeepLinkActivated;
    }
}
```

#### 3. iOS Configuration

Add Associated Domains in your Xcode project (or via a post-build script):

- `applinks:insertaffiliate.link`
- `applinks:your-custom-domain.com` (if using a custom domain)

Add your URL scheme to Info.plist:

```xml
<key>CFBundleURLSchemes</key>
<array>
    <string>ia-YOUR_COMPANY_CODE</string>
</array>
```

#### 4. Android Configuration

Add intent filters to your `AndroidManifest.xml`:

```xml
<!-- App Links -->
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="https" android:host="insertaffiliate.link" />
</intent-filter>

<!-- Custom URL Scheme -->
<intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="ia-YOUR_COMPANY_CODE" />
</intent-filter>
```

Set `android:launchMode="singleTop"` on your main activity.

#### 5. How Insert Links Attribution Works

Insert Links provides multiple attribution methods, in order of confidence:

1. **Direct deep link** - Universal Links (iOS) / App Links (Android) open the app directly with the affiliate code
2. **Clipboard matching** - The redirect page copies a unique ID to the clipboard. On first app open, the SDK reads it and matches against the click (100% confidence, iOS only)
3. **Fingerprint matching** - On first app open, the SDK sends device info to the backend which matches it against recent link clicks using probabilistic scoring
4. **Install Referrer** - For Android Play Store installs, Google passes the affiliate code through the install referrer (100% confidence)
5. **Custom URL scheme** - Fallback deep link via `ia-companycode://shortcode`

### Basic Deep Link Handling

If using a third-party deep linking provider instead of Insert Links:

```csharp
using InsertAffiliate;

public class DeepLinkManager : MonoBehaviour
{
    void HandleDeepLink(string url)
    {
        InsertAffiliateSDK.SetInsertAffiliateIdentifier(url, (shortCode) =>
        {
            if (!string.IsNullOrEmpty(shortCode))
            {
                Debug.Log($"Affiliate set: {shortCode}");
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

// Get the Unix timestamp (ms) when attribution expires
long? expiryTimestamp = InsertAffiliateSDK.GetAffiliateExpiryTimestamp();

// Get identifier ignoring timeout (for debugging)
string rawIdentifier = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier(ignoreTimeout: true);
```

</details>

### Prevent Affiliate Transfer

By default, if a user clicks a new affiliate link, their attribution is updated to the new affiliate. Enable `preventAffiliateTransfer` to lock the first affiliate:

```csharp
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    preventAffiliateTransfer: true  // Lock first affiliate
);
```

**How it works:**
- When enabled, once a user is attributed to an affiliate, that attribution is locked
- New affiliate links will not overwrite the existing attribution
- The callback still fires with the existing affiliate data (not the new one)
- Useful for preventing "affiliate stealing" where users click competitor links

**Example scenario:**
1. User clicks Affiliate A's link → attributed to Affiliate A
2. User later clicks Affiliate B's link → still attributed to Affiliate A (blocked)
3. Affiliate A gets credit for any purchases

Learn more: [Prevent Affiliate Transfer Documentation](https://docs.insertaffiliate.com/prevent-affiliate-transfer)

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

## 🎁 In-App Referrals

Turn your own users into affiliates from inside your app, show them a drop-in "Refer a friend" screen, and read their referral stats so you can reward them.

A referrer is a normal affiliate: they take a seat, get the welcome email with a dashboard sign-in, and earn commission and/or the rewards you choose. Switch the program on (and pick what counts as a referral: install, event or purchase) in the Insert Affiliate dashboard first; until then enrolment returns `PROGRAM_DISABLED`.

### Drop-in "Refer a friend" panel

```csharp
using InsertAffiliate;

public void OnReferAFriendClicked()
{
    InsertAffiliateSDK.ShowReferAFriend(new ReferAFriendOptions
    {
        email = currentUser.Email,   // prefill (usually your logged-in user)
        name = currentUser.Name,
        onClose = () => Debug.Log("Refer a friend closed")
    });
}
```

The panel is built in code with uGUI (no prefab or assets to import) and handles every step:

1. **Not joined yet:** email and name fields (prefilled) and a "Get my link" button.
2. **Email code:** if the email is already an affiliate (reinstall, new phone, existing creator), a 6-digit code is emailed. The panel shows the code field, "Verify" and "Send a new code".
3. **Joined:** the user's code and link, "Copy" and "Share" buttons, their referral count and earnings, and "Open my dashboard". When the user has free premium time from rewards it shows "Free premium until {date}", and the reward codes this phone can redeem are listed under "Your rewards" with a "Redeem" button: App Store offer codes on iOS, Google Play promo codes on Android (every code in the editor and on other platforms).

**Options** (all optional):

| Option | Description |
|---|---|
| `email`, `name` | Prefill the join form |
| `appUserId`, `playPurchaseToken` | The user's RevenueCat / Adapty app user id and Google Play purchase token, for [automatic rewards](#automatic-rewards). Sent when the user joins, and saved with `SetReferrerAccount()` when the panel opens for a user who has already joined |
| `shareMessage` | Share message. May use `{link}` and `{code}` placeholders |
| `primaryColor` | Hex colour such as `"#6A0DAD"`. Order: this option, then the dashboard colour, then `#6A0DAD` |
| `headline`, `rewardText` | Override the copy set in the dashboard (default headline "Refer a friend") |
| `font` | Font for all text (defaults to Unity's built-in font) |
| `cornerRadius` | Corner radius of the card, buttons and fields (default `24`) |
| `sortingOrder` | Canvas sorting order so the panel draws above your UI (default `1000`) |
| `onClose` | Called when the user closes the panel |

Close it from code with `ReferAFriendPanel.Hide()`; `ReferAFriendPanel.IsShowing` tells you whether it is open.

**Notes:**
- The panel needs an `EventSystem` to receive taps. If your scene has none, it adds one with a `StandaloneInputModule` (removed again on close). Projects that use only the new Input System should keep their own `EventSystem` with an `InputSystemUIInputModule` in the scene.
- **Share** opens the system share sheet on iOS and Android. In the Editor and on desktop it copies the text to the clipboard and shows "Copied".
- The SDK never asks for Contacts access and nothing is gated behind sharing, in line with App Store and Google Play rules.

### Build your own UI

```csharp
// 1. Join (or reconnect) the current user
InsertAffiliateSDK.CreateAffiliateForUser(user.Email, user.Name, result =>
{
    if (result.IsConnected)
    {
        // status "created": new affiliate, this device is connected
        Debug.Log($"Share code: {result.affiliate.affiliateShortCode}");
    }
    else if (result.IsVerificationRequired)
    {
        // Already an affiliate: a 6-digit code was emailed. Ask for it, then:
        // InsertAffiliateSDK.VerifyAffiliateCode(user.Email, enteredCode, user.Name, OnVerified);
    }
    else
    {
        Debug.Log($"Could not join: {result.errorCode} {result.errorMessage}");
    }
});

// 2. Show stats (null when this device is not connected)
InsertAffiliateSDK.GetMyAffiliateDetails(details =>
{
    if (details == null) return;
    Debug.Log($"{details.referralCount} referrals, earned {details.totalEarned} {details.currency}");
});

// 3. Share
InsertAffiliateSDK.ShareReferralLink("Join me on MyApp: {link}");

// On app logout
InsertAffiliateSDK.SignOutAffiliate();
```

**Result statuses:** `created`, `connected` (reconnected with the email code), `verificationRequired`, or `error`. Error codes (`ReferralErrorCodes`): `PROGRAM_DISABLED`, `AFFILIATE_LIMIT_REACHED`, `INVALID_EMAIL`, `INVALID_CODE`, `TOO_MANY_CODES`, `RATE_LIMITED`, `COMPANY_NOT_FOUND`, `NETWORK_ERROR`, `SERVER_ERROR`, `NOT_INITIALIZED`.

**Share text:** with a link it is `"<message> <link>"` (default message `"Try {companyName}:"`). For Short Code Only programs it is `"Use my code {code} in {companyName}"`. A custom message may use the `{link}` and `{code}` placeholders; otherwise the link (or code) is appended.

**Using your own share plugin:** Unity has no built-in share sheet on every platform. If you already use a native share plugin, get the text and pass it on:

```csharp
InsertAffiliateSDK.GetReferralShareText(text =>
{
    if (text != null) MyNativeShare.Share(text);
});
```

**Storage:** connecting a device stores a private token in `PlayerPrefs` (one per company code). It only reads that user's own stats and is never logged. Uninstalling clears it; the user reconnects with the email code and their affiliate account, earnings and dashboard are unchanged.

### Rewarding referrers

`referralCount` only goes up, so you can compare it with what you have already rewarded and grant the difference. But values read on the device are for **display**: a modified device can fake them. Grant anything valuable (credits, premium time, currency) from your server instead:

- **`referral.created` webhook:** fires each time something you count as a referral happens. The payload includes `short_code`, `trigger` and `referral_count` (the running total), so rewarding up to that total is safe to repeat.
- **Public API:** `GET /public/v1/affiliates/:identifier` includes a `referrals` object with the same counts.

For free premium time, use Apple/Google offer codes or RevenueCat promotional entitlements rather than custom unlock codes, and never reward ratings or reviews.

#### Automatic rewards

Insert Affiliate can grant referrer rewards for you (set them up per verification method in the dashboard): RevenueCat or Adapty promotional access, App Store one-time offer codes, or a deferred Google Play renewal. To do that it needs the referrer's own accounts. Pass them when the user joins:

```csharp
var account = new ReferrerAccountOptions
{
    appUserId = Purchases.GetAppUserId(),   // RevenueCat app user id or Adapty customer user id
    playPurchaseToken = myPlayPurchaseToken // Android: the user's own Google Play subscription purchase token
};

InsertAffiliateSDK.CreateAffiliateForUser(user.Email, user.Name, account, OnResult);
InsertAffiliateSDK.VerifyAffiliateCode(user.Email, enteredCode, user.Name, account, OnResult);
```

Using the drop-in panel? Set `appUserId` and `playPurchaseToken` on `ReferAFriendOptions` instead; the panel sends them for you.

If the user subscribes or logs in after joining, save the accounts then. Rewards that were waiting for them are granted:

```csharp
InsertAffiliateSDK.SetReferrerAccount(new ReferrerAccountOptions { appUserId = appUserId }, saved =>
{
    Debug.Log($"Referrer account saved: {saved}");
});
```

The SDK also sends this device's id automatically, so a user can't refer themselves. `GetMyAffiliateDetails()` reports what was granted: `rewardsGranted`, `premiumUntil` (ISO date, or `null`) and `rewardCodes` (newest first, each with `code`, `redeemUrl`, `store` and `grantedAt`). `store` is `"app_store"` for an App Store offer code (iOS only) or `"google_play"` for a Google Play promo code (Android only, `redeemUrl` is `https://play.google.com/redeem?code=...`); codes from older servers have no store and are read as `"app_store"`. `IsAppStore` / `IsGooglePlay` check it, and `InsertAffiliateSDK.RewardCodesForPlatform(details.rewardCodes, Application.platform)` returns just the codes the phone can redeem. Open `redeemUrl` with `Application.OpenURL` to redeem a code.

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
    float? affiliateAttributionActiveTime = null,
    bool preventAffiliateTransfer = false
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

// Get expiry timestamp (ms) - returns null if no timeout configured
long? expiryTimestamp = InsertAffiliateSDK.GetAffiliateExpiryTimestamp()
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

### In-App Referrals

```csharp
// Join the current user as an affiliate (status: created | verificationRequired | error)
InsertAffiliateSDK.CreateAffiliateForUser(string email, string name, Action<ReferralResult> callback)
InsertAffiliateSDK.CreateAffiliateForUser(string email, string name, ReferrerAccountOptions options, Action<ReferralResult> callback)

// Finish reconnecting with the emailed 6-digit code (status: connected | created | error)
InsertAffiliateSDK.VerifyAffiliateCode(string email, string code, string name, Action<ReferralResult> callback)
InsertAffiliateSDK.VerifyAffiliateCode(string email, string code, string name, ReferrerAccountOptions options, Action<ReferralResult> callback)
InsertAffiliateSDK.VerifyAffiliateCode(string email, string code, Action<ReferralResult> callback)

// Save the referrer's app user id / Google Play purchase token after joining (true when saved)
InsertAffiliateSDK.SetReferrerAccount(ReferrerAccountOptions options, Action<bool> callback = null)

// The connected user's details and stats (null when not connected)
InsertAffiliateSDK.GetMyAffiliateDetails(Action<MyAffiliateDetails> callback)

// Is this device connected as an affiliate? (local, no network)
bool isReferrer = InsertAffiliateSDK.IsUserAnAffiliate()

// Disconnect this device (call on app logout)
InsertAffiliateSDK.SignOutAffiliate()

// Program settings from the dashboard (enabled, companyName, referralTrigger, headline, rewardText, primaryColor)
InsertAffiliateSDK.GetReferralProgramConfig(Action<ReferralProgramConfig> callback)

// Share sheet on iOS/Android, clipboard + "Copied" elsewhere
InsertAffiliateSDK.ShareReferralLink(string message = null, Action<bool> callback = null)

// Share text for your own share plugin
InsertAffiliateSDK.GetReferralShareText(Action<string> callback, string message = null)

// Drop-in "Refer a friend" panel
InsertAffiliateSDK.ShowReferAFriend(ReferAFriendOptions options = null)
```

### Events & Callbacks

```csharp
// Subscribe to affiliate identifier changes (includes offer code)
InsertAffiliateSDK.OnAffiliateIdentifierChanged += (identifier, offerCode) =>
{
    Debug.Log($"Affiliate changed: {identifier}, offer: {offerCode}");
};

// Set callback for affiliate changes (includes offer code)
InsertAffiliateSDK.SetInsertAffiliateIdentifierChangeCallback((identifier, offerCode) =>
{
    Debug.Log($"Callback: {identifier}, offer: {offerCode}");
});
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
