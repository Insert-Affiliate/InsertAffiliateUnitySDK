using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace InsertAffiliate
{
    /// <summary>
    /// Insert Affiliate SDK for Unity
    /// Provides affiliate tracking, deep linking, and attribution functionality
    /// </summary>
    public static class InsertAffiliateSDK
    {
        // SDK Configuration
        private static string companyCode;
        private static bool isInitialized = false;
        private static bool verboseLogging = false;
        private static bool insertLinksEnabled = false;
        private static float? affiliateAttributionActiveTime = null;

        // Storage Keys
        private const string KEY_SHORT_UNIQUE_DEVICE_ID = "InsertAffiliate_ShortUniqueDeviceID";
        private const string KEY_INSERT_AFFILIATE_IDENTIFIER = "InsertAffiliate_Identifier";
        private const string KEY_AFFILIATE_STORED_DATE = "InsertAffiliate_StoredDate";
        private const string KEY_OFFER_CODE = "InsertAffiliate_OfferCode";
        private const string KEY_APP_ACCOUNT_TOKEN = "InsertAffiliate_AppAccountToken";
        private const string KEY_AFFILIATE_EMAIL = "InsertAffiliate_AffiliateEmail";
        private const string KEY_AFFILIATE_ID = "InsertAffiliate_AffiliateId";
        private const string KEY_COMPANY_NAME = "InsertAffiliate_CompanyName";
        private const string KEY_DEEP_LINK_DATA = "InsertAffiliate_DeepLinkData";

        // Override token for testing
        private static string overrideAppAccountToken = null;

        // API Endpoints
        private const string API_BASE_URL = "https://api.insertaffiliate.com";
        private const string API_CONVERT_LINK = "/V1/convert-deep-link-to-short-link";
        private const string API_OFFER_CODE = "/v1/affiliateReturnOfferCode";
        private const string API_TRACK_EVENT = "/v1/trackEvent";
        private const string API_EXPECTED_TRANSACTION = "/v1/api/app-store-webhook/create-expected-transaction";
        private const string API_CHECK_AFFILIATE = "/V1/checkAffiliateExists";

        // Events
        public static event Action<string> OnAffiliateIdentifierChanged;

        // Callback
        private static Action<string> insertAffiliateIdentifierChangeCallback;

        /// <summary>
        /// Initialize the Insert Affiliate SDK
        /// </summary>
        /// <param name="companyCode">Your Insert Affiliate company code</param>
        /// <param name="verboseLogging">Enable verbose logging for debugging</param>
        /// <param name="insertLinksEnabled">Enable Insert Links deep linking</param>
        /// <param name="affiliateAttributionActiveTime">Optional attribution timeout in seconds</param>
        public static void Initialize(
            string companyCode,
            bool verboseLogging = false,
            bool insertLinksEnabled = false,
            float? affiliateAttributionActiveTime = null)
        {
            if (isInitialized)
            {
                Debug.LogWarning("[Insert Affiliate] SDK is already initialized");
                return;
            }

            if (string.IsNullOrEmpty(companyCode))
            {
                Debug.LogError("[Insert Affiliate] Company code cannot be empty");
                return;
            }

            InsertAffiliateSDK.companyCode = companyCode;
            InsertAffiliateSDK.verboseLogging = verboseLogging;
            InsertAffiliateSDK.insertLinksEnabled = insertLinksEnabled;
            InsertAffiliateSDK.affiliateAttributionActiveTime = affiliateAttributionActiveTime;

            isInitialized = true;

            // Ensure we have a unique device ID
            GetOrCreateShortUniqueDeviceID();

            if (verboseLogging)
            {
                Debug.Log($"[Insert Affiliate] SDK initialized with company code: {companyCode}");
                Debug.Log($"[Insert Affiliate] Verbose logging: {verboseLogging}");
                Debug.Log($"[Insert Affiliate] Insert Links enabled: {insertLinksEnabled}");
                Debug.Log($"[Insert Affiliate] Attribution timeout: {(affiliateAttributionActiveTime.HasValue ? $"{affiliateAttributionActiveTime.Value}s" : "None")}");
            }
        }

        /// <summary>
        /// Check if the SDK is initialized
        /// </summary>
        public static bool IsInitialized()
        {
            return isInitialized;
        }

        /// <summary>
        /// Set a callback to be invoked when the affiliate identifier changes
        /// This is useful for updating your IAP provider attribution dynamically
        /// </summary>
        /// <param name="callback">Callback that receives the new affiliate identifier</param>
        public static void SetInsertAffiliateIdentifierChangeCallback(Action<string> callback)
        {
            insertAffiliateIdentifierChangeCallback = callback;

            if (verboseLogging)
            {
                Debug.Log("[Insert Affiliate] Affiliate identifier change callback registered");
            }
        }

        /// <summary>
        /// Get or create a user account token and store expected transaction for App Store Direct Integration
        /// This method is for iOS apps using StoreKit 2 without a receipt verification platform
        /// </summary>
        /// <param name="callback">Callback receives the UUID string to use as appAccountToken, or null on error</param>
        /// <param name="overrideUUID">Optional: Override UUID for testing purposes</param>
        public static void ReturnUserAccountTokenAndStoreExpectedTransaction(
            Action<string> callback,
            string overrideUUID = null)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[Insert Affiliate] SDK not initialized");
                callback?.Invoke(null);
                return;
            }

#if !UNITY_IOS
            Debug.LogWarning("[Insert Affiliate] App Store Direct Integration is only available on iOS");
            callback?.Invoke(null);
            return;
#endif

            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(
                ReturnUserAccountTokenAndStoreExpectedTransactionCoroutine(callback, overrideUUID));
        }

        private static IEnumerator ReturnUserAccountTokenAndStoreExpectedTransactionCoroutine(
            Action<string> callback,
            string overrideUUID)
        {
            // Get or create the app account token
            string token = GetOrCreateUserAccountToken(overrideUUID);

            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[Insert Affiliate] Failed to get user account token");
                callback?.Invoke(null);
                yield break;
            }

            // Store expected transaction on backend
            yield return StoreExpectedAppStoreTransactionCoroutine(token);

            if (verboseLogging)
            {
                Debug.Log($"[Insert Affiliate] App account token ready: {token}");
            }

            callback?.Invoke(token);
        }

        /// <summary>
        /// Override the user account token for testing
        /// </summary>
        /// <param name="uuid">UUID string to use instead of generating one</param>
        public static void OverrideUserAccountToken(string uuid)
        {
            overrideAppAccountToken = uuid;

            if (verboseLogging)
            {
                Debug.Log($"[Insert Affiliate] User account token overridden: {uuid}");
            }
        }

        /// <summary>
        /// Get or create the user account token (UUID)
        /// </summary>
        private static string GetOrCreateUserAccountToken(string overrideUUID = null)
        {
            // Use override if provided
            if (!string.IsNullOrEmpty(overrideUUID))
            {
                PlayerPrefs.SetString(KEY_APP_ACCOUNT_TOKEN, overrideUUID);
                PlayerPrefs.Save();
                return overrideUUID;
            }

            // Use static override if set
            if (!string.IsNullOrEmpty(overrideAppAccountToken))
            {
                PlayerPrefs.SetString(KEY_APP_ACCOUNT_TOKEN, overrideAppAccountToken);
                PlayerPrefs.Save();
                return overrideAppAccountToken;
            }

            // Check if token already exists
            string existingToken = PlayerPrefs.GetString(KEY_APP_ACCOUNT_TOKEN, string.Empty);

            if (!string.IsNullOrEmpty(existingToken))
            {
                if (verboseLogging)
                {
                    Debug.Log($"[Insert Affiliate] Using existing app account token: {existingToken}");
                }
                return existingToken;
            }

            // Generate new UUID
            string newToken = System.Guid.NewGuid().ToString().ToUpper();
            PlayerPrefs.SetString(KEY_APP_ACCOUNT_TOKEN, newToken);
            PlayerPrefs.Save();

            if (verboseLogging)
            {
                Debug.Log($"[Insert Affiliate] Generated new app account token: {newToken}");
            }

            return newToken;
        }

        /// <summary>
        /// Store expected App Store transaction on the backend
        /// </summary>
        private static IEnumerator StoreExpectedAppStoreTransactionCoroutine(string appAccountToken)
        {
            string affiliateIdentifier = ReturnInsertAffiliateIdentifier();

            if (string.IsNullOrEmpty(affiliateIdentifier))
            {
                if (verboseLogging)
                {
                    Debug.Log("[Insert Affiliate] No affiliate identifier - skipping expected transaction storage");
                }
                yield break;
            }

            // Prepare request payload
            var payload = new ExpectedTransactionPayload
            {
                companyId = companyCode,
                affiliateIdentifier = affiliateIdentifier,
                appAccountToken = appAccountToken
            };

            string jsonPayload = JsonUtility.ToJson(payload);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

            string url = $"{API_BASE_URL}{API_EXPECTED_TRANSACTION}";

            if (verboseLogging)
            {
                Debug.Log($"[Insert Affiliate] Storing expected transaction: {url}");
                Debug.Log($"[Insert Affiliate] Payload: {jsonPayload}");
            }

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (verboseLogging)
                    {
                        Debug.Log("[Insert Affiliate] Expected transaction stored successfully");
                        Debug.Log($"[Insert Affiliate] Response: {request.downloadHandler.text}");
                    }
                }
                else
                {
                    Debug.LogError($"[Insert Affiliate] Failed to store expected transaction: {request.error}");
                    Debug.LogError($"[Insert Affiliate] Response code: {request.responseCode}");

                    if (!string.IsNullOrEmpty(request.downloadHandler.text))
                    {
                        Debug.LogError($"[Insert Affiliate] Response: {request.downloadHandler.text}");
                    }
                }
            }
        }

        /// <summary>
        /// Get or create a unique device ID
        /// </summary>
        private static string GetOrCreateShortUniqueDeviceID()
        {
            if (PlayerPrefs.HasKey(KEY_SHORT_UNIQUE_DEVICE_ID))
            {
                return PlayerPrefs.GetString(KEY_SHORT_UNIQUE_DEVICE_ID);
            }

            // Generate a short unique device ID
            string guid = Guid.NewGuid().ToString();
            int hash = Math.Abs(guid.GetHashCode());
            string shortId = (hash % 0xFFFFFF).ToString("X6");

            PlayerPrefs.SetString(KEY_SHORT_UNIQUE_DEVICE_ID, shortId);
            PlayerPrefs.Save();

            if (verboseLogging)
            {
                Debug.Log($"[Insert Affiliate] Generated short unique device ID: {shortId}");
            }

            return shortId;
        }

        /// <summary>
        /// Set a short code for affiliate tracking
        /// </summary>
        public static void SetShortCode(string shortCode)
        {
            if (!isInitialized)
            {
                Debug.LogError("[Insert Affiliate] SDK not initialized. Call Initialize() first.");
                return;
            }

            if (string.IsNullOrEmpty(shortCode))
            {
                Debug.LogWarning("[Insert Affiliate] Short code cannot be empty");
                return;
            }

            string upperShortCode = shortCode.ToUpper();

            // Validate length (3-25 characters)
            if (upperShortCode.Length < 3 || upperShortCode.Length > 25)
            {
                Debug.LogError("[Insert Affiliate] Short code must be between 3 and 25 characters");
                return;
            }

            // Validate alphanumeric
            if (!IsAlphanumeric(upperShortCode))
            {
                Debug.LogError("[Insert Affiliate] Short code must contain only letters and numbers");
                return;
            }

            // Fetch affiliate details from API and store if valid
            FetchDeepLinkData(upperShortCode);

            if (verboseLogging)
            {
                Debug.Log($"[Insert Affiliate] Short code set: {upperShortCode}");
            }
        }

        /// <summary>
        /// Set affiliate identifier from a referring link
        /// </summary>
        public static void SetInsertAffiliateIdentifier(string referringLink, Action<string> callback = null)
        {
            if (!isInitialized)
            {
                Debug.LogError("[Insert Affiliate] SDK not initialized");
                callback?.Invoke(null);
                return;
            }

            if (string.IsNullOrEmpty(referringLink))
            {
                Debug.LogWarning("[Insert Affiliate] Referring link is empty");
                callback?.Invoke(null);
                return;
            }

            // Check if it's already a short code
            if (IsShortCode(referringLink))
            {
                if (verboseLogging)
                {
                    Debug.Log($"[Insert Affiliate] Referring link is already a short code: {referringLink}");
                }

                StoreInsertAffiliateIdentifier(referringLink);
                callback?.Invoke(referringLink);
                return;
            }

            // Convert deep link to short code via API
            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(
                ConvertDeepLinkToShortCodeCoroutine(referringLink, callback));
        }

        private static IEnumerator ConvertDeepLinkToShortCodeCoroutine(string referringLink, Action<string> callback)
        {
            string encodedLink = UnityWebRequest.EscapeURL(referringLink);
            string url = $"{API_BASE_URL}{API_CONVERT_LINK}?companyId={companyCode}&deepLinkUrl={encodedLink}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Insert Affiliate] Error converting link: {request.error}");
                    StoreInsertAffiliateIdentifier(referringLink);
                    callback?.Invoke(null);
                    yield break;
                }

                try
                {
                    string responseText = request.downloadHandler.text;

                    if (verboseLogging)
                    {
                        Debug.Log($"[Insert Affiliate] Raw API response: {responseText}");
                    }

                    // Handle different response formats
                    string shortLink = null;

                    // Try parsing as JSON object first
                    if (responseText.StartsWith("{"))
                    {
                        var response = JsonUtility.FromJson<ShortLinkResponse>(responseText);
                        shortLink = response?.shortLink;
                    }
                    // If it's a plain string (possibly quoted), use it directly
                    else
                    {
                        shortLink = responseText.Trim('"', ' ', '\n', '\r');
                    }

                    if (!string.IsNullOrEmpty(shortLink))
                    {
                        if (verboseLogging)
                        {
                            Debug.Log($"[Insert Affiliate] Short link received: {shortLink}");
                        }

                        StoreInsertAffiliateIdentifier(shortLink);
                        callback?.Invoke(shortLink);
                    }
                    else
                    {
                        Debug.LogWarning($"[Insert Affiliate] Empty response, storing original link");
                        StoreInsertAffiliateIdentifier(referringLink);
                        callback?.Invoke(null);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Insert Affiliate] Failed to parse response: {e.Message}");
                    Debug.LogError($"[Insert Affiliate] Response text: {request.downloadHandler.text}");
                    StoreInsertAffiliateIdentifier(referringLink);
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Store the affiliate identifier
        /// </summary>
        private static void StoreInsertAffiliateIdentifier(string referringLink)
        {
            string shortDeviceId = GetOrCreateShortUniqueDeviceID();
            string affiliateIdentifier = $"{referringLink}-{shortDeviceId}";

            // Check if same identifier is already stored
            string existingIdentifier = PlayerPrefs.GetString(KEY_INSERT_AFFILIATE_IDENTIFIER, string.Empty);

            if (existingIdentifier == affiliateIdentifier)
            {
                if (verboseLogging)
                {
                    Debug.Log($"[Insert Affiliate] Same affiliate identifier already stored: {affiliateIdentifier}");
                }
                return;
            }

            // Store new identifier
            PlayerPrefs.SetString(KEY_INSERT_AFFILIATE_IDENTIFIER, affiliateIdentifier);

            // Store timestamp
            string timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format
            PlayerPrefs.SetString(KEY_AFFILIATE_STORED_DATE, timestamp);
            PlayerPrefs.Save();

            if (verboseLogging)
            {
                if (!string.IsNullOrEmpty(existingIdentifier))
                {
                    Debug.Log($"[Insert Affiliate] Replaced identifier: {existingIdentifier} -> {affiliateIdentifier}");
                }
                else
                {
                    Debug.Log($"[Insert Affiliate] Stored new identifier: {affiliateIdentifier}");
                }
                Debug.Log($"[Insert Affiliate] Stored date: {timestamp}");
            }

            // Notify listeners
            OnAffiliateIdentifierChanged?.Invoke(affiliateIdentifier);

            // Notify callback
            insertAffiliateIdentifierChangeCallback?.Invoke(affiliateIdentifier);

            // Auto-fetch offer code for short codes
            if (IsShortCode(referringLink))
            {
                FetchOfferCode(referringLink);
            }
        }

        /// <summary>
        /// Get the current affiliate identifier (respects timeout)
        /// </summary>
        public static string ReturnInsertAffiliateIdentifier(bool ignoreTimeout = false)
        {
            string identifier = PlayerPrefs.GetString(KEY_INSERT_AFFILIATE_IDENTIFIER, string.Empty);

            if (string.IsNullOrEmpty(identifier))
            {
                return null;
            }

            // If ignoring timeout, return immediately
            if (ignoreTimeout)
            {
                return identifier;
            }

            // Check attribution timeout
            if (affiliateAttributionActiveTime.HasValue)
            {
                string storedDateStr = PlayerPrefs.GetString(KEY_AFFILIATE_STORED_DATE, string.Empty);

                if (!string.IsNullOrEmpty(storedDateStr))
                {
                    try
                    {
                        DateTime storedDate = DateTime.Parse(storedDateStr);
                        TimeSpan elapsed = DateTime.UtcNow - storedDate;

                        if (elapsed.TotalSeconds > affiliateAttributionActiveTime.Value)
                        {
                            if (verboseLogging)
                            {
                                Debug.Log($"[Insert Affiliate] Attribution expired ({elapsed.TotalSeconds}s > {affiliateAttributionActiveTime.Value}s)");
                            }
                            return null;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Insert Affiliate] Failed to parse stored date: {e.Message}");
                    }
                }
            }

            return identifier;
        }

        /// <summary>
        /// Check if attribution is still valid
        /// </summary>
        public static bool IsAffiliateAttributionValid()
        {
            return !string.IsNullOrEmpty(ReturnInsertAffiliateIdentifier());
        }

        /// <summary>
        /// Get the date when affiliate was stored
        /// </summary>
        public static DateTime? GetAffiliateStoredDate()
        {
            string storedDateStr = PlayerPrefs.GetString(KEY_AFFILIATE_STORED_DATE, string.Empty);

            if (string.IsNullOrEmpty(storedDateStr))
            {
                return null;
            }

            try
            {
                return DateTime.Parse(storedDateStr);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get stored affiliate email from deep link data
        /// </summary>
        public static string GetAffiliateEmail()
        {
            return PlayerPrefs.GetString(KEY_AFFILIATE_EMAIL, string.Empty);
        }

        /// <summary>
        /// Get stored affiliate ID from deep link data
        /// </summary>
        public static string GetAffiliateId()
        {
            return PlayerPrefs.GetString(KEY_AFFILIATE_ID, string.Empty);
        }

        /// <summary>
        /// Get stored company name from deep link data
        /// </summary>
        public static string GetCompanyName()
        {
            return PlayerPrefs.GetString(KEY_COMPANY_NAME, string.Empty);
        }

        /// <summary>
        /// Get the complete deep link data as JSON string
        /// </summary>
        public static string GetDeepLinkData()
        {
            return PlayerPrefs.GetString(KEY_DEEP_LINK_DATA, string.Empty);
        }

        /// <summary>
        /// Get the current offer code
        /// </summary>
        public static string OfferCode
        {
            get { return PlayerPrefs.GetString(KEY_OFFER_CODE, string.Empty); }
        }

        /// <summary>
        /// Fetch offer code for an affiliate link
        /// </summary>
        private static void FetchOfferCode(string affiliateLink)
        {
            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(FetchOfferCodeCoroutine(affiliateLink));
        }

        private static IEnumerator FetchOfferCodeCoroutine(string affiliateLink)
        {
            string encodedLink = UnityWebRequest.EscapeURL(affiliateLink);
            string url = $"{API_BASE_URL}{API_OFFER_CODE}/{encodedLink}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (verboseLogging)
                    {
                        Debug.Log($"[Insert Affiliate] No offer code found for: {affiliateLink}");
                    }
                    yield break;
                }

                string rawOfferCode = request.downloadHandler.text.Trim();
                string offerCode = RemoveSpecialCharacters(rawOfferCode);

                // Check for error responses
                if (offerCode.Contains("error") || offerCode.Contains("notfound") || offerCode.Contains("Routenotfound"))
                {
                    if (verboseLogging)
                    {
                        Debug.Log("[Insert Affiliate] Offer code not found");
                    }
                    yield break;
                }

                PlayerPrefs.SetString(KEY_OFFER_CODE, offerCode);
                PlayerPrefs.Save();

                if (verboseLogging)
                {
                    Debug.Log($"[Insert Affiliate] Offer code stored: {offerCode}");
                }
            }
        }

        /// <summary>
        /// Track a custom event
        /// </summary>
        public static void TrackEvent(string eventName)
        {
            if (!isInitialized)
            {
                Debug.LogError("[Insert Affiliate] SDK not initialized");
                return;
            }

            string identifier = ReturnInsertAffiliateIdentifier();
            if (string.IsNullOrEmpty(identifier))
            {
                Debug.LogWarning("[Insert Affiliate] No valid affiliate identifier found or attribution expired");
                return;
            }

            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(TrackEventCoroutine(eventName, identifier));
        }

        private static IEnumerator TrackEventCoroutine(string eventName, string deepLinkParam)
        {
            var payload = new EventPayload
            {
                eventName = eventName,
                deepLinkParam = deepLinkParam,
                companyId = companyCode
            };

            string jsonPayload = JsonUtility.ToJson(payload);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest($"{API_BASE_URL}{API_TRACK_EVENT}", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (verboseLogging)
                    {
                        Debug.Log($"[Insert Affiliate] Event tracked successfully: {eventName}");
                    }
                }
                else
                {
                    Debug.LogError($"[Insert Affiliate] Failed to track event: {request.error}");
                }
            }
        }

        /// <summary>
        /// Handle Insert Links deep link
        /// </summary>
        public static bool HandleInsertLinks(string url)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[Insert Affiliate] SDK not initialized");
                return false;
            }

            if (!insertLinksEnabled)
            {
                if (verboseLogging)
                {
                    Debug.Log("[Insert Affiliate] Insert Links is disabled");
                }
                return false;
            }

            if (verboseLogging)
            {
                Debug.Log($"[Insert Affiliate] Handling Insert Links URL: {url}");
            }

            // Parse and handle the URL
            if (url.StartsWith("ia-"))
            {
                // Custom URL scheme: ia-companycode://shortcode
                string shortCode = url.Substring(url.LastIndexOf("://") + 3).ToUpper();

                if (verboseLogging)
                {
                    Debug.Log($"[Insert Affiliate] Custom URL scheme detected - Short code: {shortCode}");
                }

                // Fetch deep link data for this short code
                FetchDeepLinkData(shortCode);
                return true;
            }
            else if (url.Contains("insertaffiliate.link"))
            {
                // Universal link: https://insertaffiliate.link/V1/companycode/shortcode
                string[] pathComponents = url.Split('/');

                if (pathComponents.Length >= 6 && pathComponents[3] == "V1")
                {
                    string linkCompanyCode = pathComponents[4];
                    string shortCode = pathComponents[5].ToUpper();

                    if (verboseLogging)
                    {
                        Debug.Log($"[Insert Affiliate] Universal link detected - Company: {linkCompanyCode}, Short code: {shortCode}");
                    }

                    // Validate company code matches
                    if (!string.IsNullOrEmpty(companyCode) && linkCompanyCode.ToLower() != companyCode.ToLower())
                    {
                        Debug.LogWarning($"[Insert Affiliate] URL company code ({linkCompanyCode}) doesn't match initialized company code ({companyCode})");
                    }

                    // Fetch deep link data for this short code
                    FetchDeepLinkData(shortCode);
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[Insert Affiliate] Invalid universal link format: {url}");
                }
            }

            return false;
        }

        /// <summary>
        /// Fetch deep link data from Insert Affiliate API
        /// </summary>
        private static void FetchDeepLinkData(string shortCode)
        {
            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(FetchDeepLinkDataCoroutine(shortCode));
        }

        private static IEnumerator FetchDeepLinkDataCoroutine(string shortCode)
        {
            string url = $"{API_BASE_URL}{API_CHECK_AFFILIATE}";

            // Prepare request payload
            var payload = new CheckAffiliatePayload
            {
                companyId = companyCode,
                affiliateCode = shortCode
            };

            string jsonPayload = JsonUtility.ToJson(payload);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

            if (verboseLogging)
            {
                Debug.Log($"[Insert Affiliate] Fetching affiliate details from: {url}");
                Debug.Log($"[Insert Affiliate] Payload: {jsonPayload}");
            }

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Insert Affiliate] Error fetching affiliate details: {request.error}");

                    if (request.responseCode == 404)
                    {
                        Debug.LogError($"[Insert Affiliate] Affiliate not found. Short code '{shortCode}' may not exist for company '{companyCode}'");
                    }

                    yield break;
                }

                try
                {
                    string responseText = request.downloadHandler.text;

                    if (verboseLogging)
                    {
                        Debug.Log($"[Insert Affiliate] Raw affiliate API response: {responseText}");
                    }

                    // Parse JSON response
                    AffiliateCheckResponse response = JsonUtility.FromJson<AffiliateCheckResponse>(responseText);

                    if (response != null && response.exists && response.affiliate != null)
                    {
                        if (verboseLogging)
                        {
                            Debug.Log($"[Insert Affiliate] Affiliate found: {response.affiliate.affiliateName}");
                            Debug.Log($"[Insert Affiliate] Short code: {response.affiliate.affiliateShortCode}");
                            Debug.Log($"[Insert Affiliate] Deep link: {response.affiliate.deeplinkurl}");
                        }

                        // Store affiliate metadata
                        PlayerPrefs.SetString(KEY_AFFILIATE_EMAIL, response.affiliate.affiliateName ?? "");
                        PlayerPrefs.SetString(KEY_AFFILIATE_ID, response.affiliate.affiliateShortCode ?? "");
                        PlayerPrefs.SetString(KEY_COMPANY_NAME, "");  // Not provided by this endpoint
                        PlayerPrefs.SetString(KEY_DEEP_LINK_DATA, responseText);
                        PlayerPrefs.Save();

                        // Store the affiliate identifier using the short code
                        StoreInsertAffiliateIdentifier(shortCode);
                    }
                    else
                    {
                        if (verboseLogging)
                        {
                            Debug.Log($"[Insert Affiliate] Affiliate not found or doesn't exist");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Insert Affiliate] Failed to parse affiliate data: {e.Message}");
                }
            }
        }

        // Helper Methods
        private static bool IsShortCode(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length < 3 || code.Length > 25)
            {
                return false;
            }

            return IsAlphanumeric(code);
        }

        private static bool IsAlphanumeric(string str)
        {
            foreach (char c in str)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        private static string RemoveSpecialCharacters(string str)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in str)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        // JSON Response Classes
        [Serializable]
        private class ShortLinkResponse
        {
            public string shortLink;
        }

        [Serializable]
        private class EventPayload
        {
            public string eventName;
            public string deepLinkParam;
            public string companyId;
        }

        [Serializable]
        private class CheckAffiliatePayload
        {
            public string companyId;
            public string affiliateCode;
        }

        [Serializable]
        private class AffiliateCheckResponse
        {
            public bool exists;
            public string searchType;
            public AffiliateDetails affiliate;
        }

        [Serializable]
        private class AffiliateDetails
        {
            public string affiliateName;
            public string affiliateShortCode;
            public string deeplinkurl;
        }

        [Serializable]
        private class ExpectedTransactionPayload
        {
            public string companyId;
            public string affiliateIdentifier;
            public string appAccountToken;
        }
    }
}
