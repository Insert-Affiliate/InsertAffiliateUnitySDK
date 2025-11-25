# Dynamic Offer Codes Complete Guide

Automatically apply discounts or trials when users come from specific affiliates using offer code modifiers.

## How It Works

When someone clicks an affiliate link or enters a short code linked to an offer (set up in the Insert Affiliate Dashboard), the SDK fills in `InsertAffiliateSDK.OfferCode` with the right modifier (like `_oneWeekFree`). You can then add this to your regular product ID to load the correct version of the subscription in your app.

## Setup in Insert Affiliate Dashboard

1. Go to [app.insertaffiliate.com/affiliates](https://app.insertaffiliate.com/affiliates)
2. Select the affiliate you want to configure
3. Click "View" to access the affiliate's settings
4. Assign an **iOS IAP Modifier** to the affiliate (e.g., `_oneWeekFree`, `_threeMonthsFree`)
5. Assign an **Android IAP Modifier** to the affiliate (e.g., `-oneweekfree`, `-threemonthsfree`)
6. Save the settings

Once configured, when users click that affiliate's links or enter their short codes, your app will automatically receive the modifier and can load the appropriate discounted product.

## Setup in App Store Connect (iOS)

Create both a base and a promotional product:
- Base product: `oneMonthSubscription`
- Promo product: `oneMonthSubscription_oneWeekFree`

Ensure **both** products are approved and available for sale.

## Setup in Google Play Console (Android)

There are multiple ways you can configure your products:

1. **Multiple Products Approach**: Create both a base and a promotional product:
   - Base product: `oneMonthSubscription`
   - Promo product: `oneMonthSubscription-oneweekfree`

2. **Single Product with Multiple Base Plans**: Create one product with multiple base plans, one with an offer attached

3. **Developer Triggered Offers**: Have one base product and apply the offer through developer-triggered offers

4. **Base Product with Intro Offers**: Have one base product that includes an introductory offer

**If using the Multiple Products Approach:**
- Ensure **both** products are activated
- Generate a release to at least **Internal Testing** to make products available

## Basic Usage

### Access the Stored Offer Code

```csharp
using InsertAffiliate;

string offerCode = InsertAffiliateSDK.OfferCode;

if (!string.IsNullOrEmpty(offerCode))
{
    Debug.Log($"Offer code found: {offerCode}");
}
```

### Get Dynamic Product ID

```csharp
using InsertAffiliate;

public class ProductManager : MonoBehaviour
{
    private const string BASE_PRODUCT_ID = "monthly_premium";

    public string GetDynamicProductId()
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
}
```

## RevenueCat Integration Example

For apps using RevenueCat, dynamically load products based on offer codes:

```csharp
using UnityEngine;
using InsertAffiliate;
using RevenueCat;

public class RevenueCatOfferManager : MonoBehaviour
{
    private const string BASE_PRODUCT_ID = "monthly_premium";

    public void LoadProducts()
    {
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
        UpdateUI(product);
    }

    void UpdateUI(StoreProduct product)
    {
        // Show offer badge if using promotional product
        string offerCode = InsertAffiliateSDK.OfferCode;
        if (!string.IsNullOrEmpty(offerCode))
        {
            // Display "Special Offer Applied!" message
            Debug.Log("Special offer is active!");
        }
    }
}
```

## Unity IAP Integration Example

For apps using Unity's native In-App Purchasing:

```csharp
using UnityEngine;
using UnityEngine.Purchasing;
using InsertAffiliate;
using System.Collections.Generic;

public class UnityIAPOfferManager : MonoBehaviour, IStoreListener
{
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;

    private const string BASE_PRODUCT_ID = "monthly_premium";
    private string currentProductId;

    void Start()
    {
        InitializePurchasing();
    }

    void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Add base product
        builder.AddProduct(BASE_PRODUCT_ID, ProductType.Subscription);

        // Add promotional products
        builder.AddProduct(BASE_PRODUCT_ID + "_oneWeekFree", ProductType.Subscription);
        builder.AddProduct(BASE_PRODUCT_ID + "_threeMonthsFree", ProductType.Subscription);
        builder.AddProduct(BASE_PRODUCT_ID + "_50percentOff", ProductType.Subscription);

        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
        Debug.Log("[IAP] Unity IAP initialized");

        // Load the appropriate product
        LoadDynamicProduct();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[IAP] Initialization failed: {error}");
    }

    void LoadDynamicProduct()
    {
        currentProductId = GetDynamicProductId();
        Debug.Log($"[IAP] Loading product: {currentProductId}");

        Product product = storeController.products.WithID(currentProductId);

        if (product != null && product.availableToPurchase)
        {
            DisplayProduct(product);
        }
        else if (!string.IsNullOrEmpty(InsertAffiliateSDK.OfferCode))
        {
            // Fallback to base product
            Debug.Log($"[IAP] Promotional product not found, falling back to: {BASE_PRODUCT_ID}");
            currentProductId = BASE_PRODUCT_ID;
            product = storeController.products.WithID(currentProductId);

            if (product != null && product.availableToPurchase)
            {
                DisplayProduct(product);
            }
            else
            {
                Debug.LogError($"[IAP] Base product not found: {BASE_PRODUCT_ID}");
            }
        }
        else
        {
            Debug.LogError($"[IAP] Product not found: {currentProductId}");
        }
    }

    string GetDynamicProductId()
    {
        string offerCode = InsertAffiliateSDK.OfferCode;

        if (!string.IsNullOrEmpty(offerCode))
        {
            offerCode = offerCode.Trim().Trim('"', '\'');
            return BASE_PRODUCT_ID + offerCode;
        }

        return BASE_PRODUCT_ID;
    }

    void DisplayProduct(Product product)
    {
        Debug.Log($"[IAP] Displaying product: {product.definition.id}");
        Debug.Log($"[IAP] Price: {product.metadata.localizedPriceString}");
        Debug.Log($"[IAP] Title: {product.metadata.localizedTitle}");

        // Update your UI
        // priceText.text = product.metadata.localizedPriceString;
        // titleText.text = product.metadata.localizedTitle;

        // Show offer badge if using promotional product
        if (!string.IsNullOrEmpty(InsertAffiliateSDK.OfferCode))
        {
            Debug.Log("[IAP] Special offer is active!");
            // offerBadge.SetActive(true);
        }
    }

    public void PurchaseCurrentProduct()
    {
        if (string.IsNullOrEmpty(currentProductId))
        {
            Debug.LogError("[IAP] No product loaded");
            return;
        }

        Product product = storeController.products.WithID(currentProductId);

        if (product != null && product.availableToPurchase)
        {
            Debug.Log($"[IAP] Purchasing: {currentProductId}");
            storeController.InitiatePurchase(product);
        }
        else
        {
            Debug.LogError($"[IAP] Product not available: {currentProductId}");
        }
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Debug.Log($"[IAP] Purchase successful: {args.purchasedProduct.definition.id}");

        // Check if promotional product was purchased
        if (args.purchasedProduct.definition.id != BASE_PRODUCT_ID)
        {
            Debug.Log("[IAP] Promotional product purchased!");
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogError($"[IAP] Purchase failed: {product.definition.id}, {reason}");
    }
}
```

## UI Example with Offer Banner

```csharp
using UnityEngine;
using UnityEngine.UI;
using InsertAffiliate;

public class SubscriptionUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text priceText;
    public Text titleText;
    public GameObject offerBanner;
    public Text offerText;
    public Button purchaseButton;

    void Start()
    {
        // Hide offer banner by default
        if (offerBanner != null)
        {
            offerBanner.SetActive(false);
        }
    }

    public void DisplayProduct(string price, string title)
    {
        if (priceText != null) priceText.text = price;
        if (titleText != null) titleText.text = title;

        // Show offer banner if offer code is active
        string offerCode = InsertAffiliateSDK.OfferCode;
        if (!string.IsNullOrEmpty(offerCode) && offerBanner != null)
        {
            offerBanner.SetActive(true);

            if (offerText != null)
            {
                // Format the offer code for display
                string displayOffer = FormatOfferCode(offerCode);
                offerText.text = $"Special Offer: {displayOffer}";
            }
        }
    }

    string FormatOfferCode(string offerCode)
    {
        // Convert "_oneWeekFree" to "One Week Free"
        string formatted = offerCode.TrimStart('_', '-');
        // Add spaces before capital letters
        var result = new System.Text.StringBuilder();
        foreach (char c in formatted)
        {
            if (char.IsUpper(c) && result.Length > 0)
            {
                result.Append(' ');
            }
            result.Append(c);
        }
        return result.ToString();
    }
}
```

## Key Features

1. **Dynamic Product Loading**: Automatically constructs product IDs using the offer code modifier
2. **Fallback Strategy**: If the promotional product isn't found, falls back to the base product
3. **Visual Feedback**: Shows users when promotional pricing is applied
4. **Cross-Platform**: Works on both iOS and Android with appropriate product naming

## Example Product Identifiers

**iOS (App Store Connect):**
- Base product: `oneMonthSubscription`
- With introductory discount: `oneMonthSubscription_oneWeekFree`
- With different offer: `oneMonthSubscription_threeMonthsFree`

**Android (Google Play Console):**
- Base product: `onemonthsubscription`
- With introductory discount: `onemonthsubscription-oneweekfree`
- With different offer: `onemonthsubscription-threemonthsfree`

## Best Practices

1. **Product Setup**: Always create both base and promotional products in store consoles
2. **Naming Convention**: Use consistent naming patterns for offer code modifiers
3. **Fallback Logic**: Always implement fallback to base products if promotional ones aren't available
4. **User Experience**: Clearly indicate when special pricing is applied
5. **Testing**: Test both scenarios - with and without offer codes applied
6. **Call in purchase views**: Check for offer codes whenever showing purchase options

## Testing

1. **Set up test affiliate** with offer code modifier in Insert Affiliate dashboard
2. **Click test affiliate link** or enter short code
3. **Verify offer code** is stored:
   ```csharp
   string offerCode = InsertAffiliateSDK.OfferCode;
   Debug.Log($"Offer code: {offerCode}");
   ```
4. **Check dynamic product ID** is constructed correctly
5. **Complete test purchase** to verify correct product is purchased

### Test Deep Links

```bash
# iOS Simulator
xcrun simctl openurl booted "ia-yourcompanycode://testshortcode"

# Android Emulator
adb shell am start -W -a android.intent.action.VIEW -d "ia-yourcompanycode://testshortcode"
```

## Troubleshooting

**Problem:** Offer code is null
- **Solution:** Ensure affiliate has offer code modifier configured in dashboard
- Verify user clicked affiliate link or entered short code before checking

**Problem:** Promotional product not found
- **Solution:** Verify promotional product exists in App Store Connect / Google Play Console
- Check product ID matches exactly (including the modifier)
- Ensure product is published to at least TestFlight (iOS) or Internal Testing (Android)

**Problem:** Always showing base product instead of promotional
- **Solution:** Ensure offer code is retrieved before fetching products
- Check that `OfferCode` is not null/empty
- Verify the dynamic product identifier is correct

**Problem:** Offer code has extra characters
- **Solution:** Clean the offer code before using:
  ```csharp
  string offerCode = InsertAffiliateSDK.OfferCode?.Trim().Trim('"', '\'');
  ```

## Next Steps

- Configure offer code modifiers for high-value affiliates
- Create promotional products in App Store Connect and Google Play Console
- Test the complete flow from link click to purchase
- Monitor affiliate performance in Insert Affiliate dashboard

[Back to Main README](../README.md)
