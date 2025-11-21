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

### 2. Set Up Attribution

#### With RevenueCat

```csharp
using InsertAffiliate;
using RevenueCat;

void Start()
{
    // Initialize RevenueCat
    Purchases.Configure("your_revenuecat_api_key");

    // Set affiliate attribution if exists
    string affiliateId = InsertAffiliateSDK.ReturnInsertAffiliateIdentifier();
    if (!string.IsNullOrEmpty(affiliateId))
    {
        // RevenueCat attribution
        var attributes = new Dictionary<string, string>
        {
            { "insert_affiliate", affiliateId }
        };
        // Note: Exact method depends on RevenueCat Unity SDK version
    }
}
```

### 3. Handle Deep Links

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

### Short Codes

Allow users to enter affiliate codes manually:

```csharp
public void OnUserEnteredCode(string code)
{
    InsertAffiliateSDK.SetShortCode(code);
}
```

### Event Tracking

Track custom events for affiliate attribution:

```csharp
// Track a custom event
InsertAffiliateSDK.TrackEvent("user_signup");
InsertAffiliateSDK.TrackEvent("level_completed");
InsertAffiliateSDK.TrackEvent("tutorial_finished");
```

### Offer Codes / Dynamic Product IDs

Get offer modifiers for discounted products:

```csharp
string baseProductId = "monthly_subscription";
string offerCode = InsertAffiliateSDK.OfferCode;

if (!string.IsNullOrEmpty(offerCode))
{
    // User came through an affiliate with an offer
    string discountedProduct = baseProductId + offerCode; // e.g., "monthly_subscription_oneWeekFree"
    // Load discounted product
}
else
{
    // Load regular product
}
```

### Attribution Timeout

Set an attribution window (time after click that purchases are attributed):

```csharp
// Set 7-day attribution window
InsertAffiliateSDK.Initialize(
    companyCode: "your_company_code",
    affiliateAttributionActiveTime: 604800 // 7 days in seconds
);

// Check if attribution is still valid
bool isValid = InsertAffiliateSDK.IsAffiliateAttributionValid();

// Get stored date
DateTime? storedDate = InsertAffiliateSDK.GetAffiliateStoredDate();
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

// Set short code
InsertAffiliateSDK.SetShortCode(string shortCode)

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
