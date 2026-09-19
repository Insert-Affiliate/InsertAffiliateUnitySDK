using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace InsertAffiliate
{
    /// <summary>
    /// In-app referrals: turn the app's own users into affiliates, read their
    /// referral stats and share their link.
    ///
    /// Enrolling returns a device token that is the only way to read the user's
    /// own stats (the company code is public). It is stored in PlayerPrefs, one
    /// per company code, and never logged.
    /// </summary>
    public static partial class InsertAffiliateSDK
    {
        private const string API_SDK_AFFILIATE = "/V1/sdk/affiliate";
        private const string KEY_REFERRER_TOKEN_PREFIX = "InsertAffiliate_ReferrerToken_";
        private const string TOKEN_HEADER = "X-Insert-Affiliate-Token";
        private const string REFERRAL_PLATFORM = "unity";
        private const int REFERRAL_REQUEST_TIMEOUT = 30;

        // Company name from the last config fetch, used for share text.
        private static string referralCompanyName;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _InsertAffiliate_ShareText(string text);
#endif

        /// <summary>
        /// Make the app's current user an affiliate (a "referrer").
        /// New email: the affiliate is created and the device is connected straight away (status "created").
        /// Email that is already an affiliate: a 6-digit code is emailed (status "verificationRequired");
        /// pass it to VerifyAffiliateCode to connect this device.
        /// </summary>
        /// <param name="email">The user's email (usually the app's logged-in user)</param>
        /// <param name="name">Display name for the affiliate (optional)</param>
        /// <param name="callback">Receives the result; status is "created", "verificationRequired" or "error"</param>
        public static void CreateAffiliateForUser(string email, string name, Action<ReferralResult> callback)
        {
            CreateAffiliateForUser(email, name, null, callback);
        }

        /// <summary>
        /// Make the app's current user an affiliate, with the accounts used to reward them automatically.
        /// </summary>
        /// <param name="email">The user's email (usually the app's logged-in user)</param>
        /// <param name="name">Display name for the affiliate (optional)</param>
        /// <param name="options">The user's RevenueCat / Adapty app user id and Google Play purchase token (optional)</param>
        /// <param name="callback">Receives the result; status is "created", "verificationRequired" or "error"</param>
        public static void CreateAffiliateForUser(string email, string name, ReferrerAccountOptions options, Action<ReferralResult> callback)
        {
            if (!CanCallReferralApi(callback)) return;

            var payload = new ReferralEnrolPayload
            {
                companyId = companyCode,
                email = (email ?? "").Trim(),
                name = name ?? "",
                platform = REFERRAL_PLATFORM,
                deviceId = GetOrCreateShortUniqueDeviceID(),
                appUserId = options?.appUserId ?? "",
                playPurchaseToken = options?.playPurchaseToken ?? ""
            };

            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(
                ReferralEnrolCoroutine("/enrol", ReferralRequestJson(payload), callback));
        }

        /// <summary>
        /// Finish connecting an existing affiliate with the 6-digit code emailed by CreateAffiliateForUser.
        /// </summary>
        /// <param name="email">The same email passed to CreateAffiliateForUser</param>
        /// <param name="code">The 6-digit code from the email</param>
        /// <param name="name">Display name, used if the company requires a code for new referrers too (optional)</param>
        /// <param name="callback">Receives the result; status is "connected", "created" or "error"</param>
        public static void VerifyAffiliateCode(string email, string code, string name, Action<ReferralResult> callback)
        {
            VerifyAffiliateCode(email, code, name, null, callback);
        }

        /// <summary>
        /// Finish connecting an existing affiliate, with the accounts used to reward them automatically.
        /// </summary>
        /// <param name="email">The same email passed to CreateAffiliateForUser</param>
        /// <param name="code">The 6-digit code from the email</param>
        /// <param name="name">Display name, used if the company requires a code for new referrers too (optional)</param>
        /// <param name="options">The user's RevenueCat / Adapty app user id and Google Play purchase token (optional)</param>
        /// <param name="callback">Receives the result; status is "connected", "created" or "error"</param>
        public static void VerifyAffiliateCode(string email, string code, string name, ReferrerAccountOptions options, Action<ReferralResult> callback)
        {
            if (!CanCallReferralApi(callback)) return;

            var payload = new ReferralVerifyPayload
            {
                companyId = companyCode,
                email = (email ?? "").Trim(),
                code = (code ?? "").Trim(),
                name = name ?? "",
                platform = REFERRAL_PLATFORM,
                deviceId = GetOrCreateShortUniqueDeviceID(),
                appUserId = options?.appUserId ?? "",
                playPurchaseToken = options?.playPurchaseToken ?? ""
            };

            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(
                ReferralEnrolCoroutine("/verify", ReferralRequestJson(payload), callback));
        }

        /// <summary>
        /// Finish connecting an existing affiliate with the 6-digit code emailed by CreateAffiliateForUser.
        /// </summary>
        public static void VerifyAffiliateCode(string email, string code, Action<ReferralResult> callback)
        {
            VerifyAffiliateCode(email, code, null, callback);
        }

        /// <summary>
        /// Get the connected user's affiliate details and referral stats.
        /// Receives null when this device is not connected, or when the connection is no longer
        /// valid (the stored token is then cleared). Values are for display; grant anything
        /// valuable from your server (referral.created webhook or Public API).
        /// </summary>
        /// <param name="callback">Receives the details, or null</param>
        public static void GetMyAffiliateDetails(Action<MyAffiliateDetails> callback)
        {
            if (!isInitialized)
            {
                Debug.LogError("[Insert Affiliate] SDK not initialized. Call Initialize() first.");
                callback?.Invoke(null);
                return;
            }

            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(
                FetchMyAffiliateDetailsCoroutine((details, errorCode) => callback?.Invoke(details)));
        }

        /// <summary>
        /// Save the connected referrer's RevenueCat / Adapty app user id or Google Play purchase token.
        /// Call it when the user subscribes or logs in after joining; rewards that were waiting for
        /// these accounts are then granted.
        /// </summary>
        /// <param name="options">The accounts to save</param>
        /// <param name="callback">Receives true when saved; false when this device is not connected or the request failed</param>
        public static void SetReferrerAccount(ReferrerAccountOptions options, Action<bool> callback = null)
        {
            if (!isInitialized)
            {
                Debug.LogError("[Insert Affiliate] SDK not initialized. Call Initialize() first.");
                callback?.Invoke(false);
                return;
            }

            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(SetReferrerAccountCoroutine(options, callback));
        }

        /// <summary>
        /// True when this device is connected as an affiliate for this company (local check, no network).
        /// </summary>
        public static bool IsUserAnAffiliate()
        {
            return !string.IsNullOrEmpty(GetReferrerToken());
        }

        /// <summary>
        /// Disconnect this device from the user's affiliate account (call on app logout).
        /// The affiliate, their earnings and dashboard are not affected.
        /// </summary>
        public static void SignOutAffiliate()
        {
            ClearReferrerToken();

            if (verboseLogging)
            {
                Debug.Log("[Insert Affiliate] Referrer signed out on this device");
            }
        }

        /// <summary>
        /// Get the company's referral program settings (on/off, trigger, drop-in UI copy and colour).
        /// </summary>
        /// <param name="callback">Receives the config, or null on error</param>
        public static void GetReferralProgramConfig(Action<ReferralProgramConfig> callback)
        {
            if (!isInitialized)
            {
                Debug.LogError("[Insert Affiliate] SDK not initialized. Call Initialize() first.");
                callback?.Invoke(null);
                return;
            }

            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(
                FetchReferralProgramConfigCoroutine((config, errorCode) => callback?.Invoke(config)));
        }

        /// <summary>
        /// Build the text a referrer shares: their link, or their short code when the company has no links.
        /// Pass it to your own native share plugin if you use one.
        /// </summary>
        /// <param name="callback">Receives the share text, or null when this device is not connected</param>
        /// <param name="message">Optional message. May use {link} and {code} placeholders; otherwise the link or code is appended.</param>
        public static void GetReferralShareText(Action<string> callback, string message = null)
        {
            if (!isInitialized)
            {
                Debug.LogError("[Insert Affiliate] SDK not initialized. Call Initialize() first.");
                callback?.Invoke(null);
                return;
            }

            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(GetReferralShareTextCoroutine(message, callback));
        }

        /// <summary>
        /// Share the referrer's link. Opens the system share sheet on iOS and Android; elsewhere
        /// (Editor, desktop) the text is copied to the clipboard and a "Copied" message is shown.
        /// </summary>
        /// <param name="message">Optional message. May use {link} and {code} placeholders.</param>
        /// <param name="callback">Receives true when there was something to share</param>
        public static void ShareReferralLink(string message = null, Action<bool> callback = null)
        {
            GetReferralShareText(text =>
            {
                if (string.IsNullOrEmpty(text))
                {
                    if (verboseLogging)
                    {
                        Debug.Log("[Insert Affiliate] Nothing to share: this device is not connected as an affiliate");
                    }
                    callback?.Invoke(false);
                    return;
                }

                bool opened = ShareText(text);
                if (!opened)
                {
                    ReferAFriendPanel.ShowToast("Copied to clipboard", null);
                }
                callback?.Invoke(true);
            }, message);
        }

        /// <summary>
        /// Show the drop-in "Refer a friend" screen: joins the user up, handles the email code step,
        /// then shows their code, link, Copy and Share buttons and their referral stats.
        /// </summary>
        /// <param name="options">Prefill, copy and theme options (all optional)</param>
        public static void ShowReferAFriend(ReferAFriendOptions options = null)
        {
            if (!isInitialized)
            {
                Debug.LogError("[Insert Affiliate] SDK not initialized. Call Initialize() first.");
                return;
            }

            ReferAFriendPanel.Show(options ?? new ReferAFriendOptions());
        }

        /// <summary>
        /// The share text for the given affiliate. Used by GetReferralShareText and the drop-in panel.
        /// </summary>
        public static string BuildReferralShareText(ReferrerAffiliate affiliate, string companyName, string message = null)
        {
            if (affiliate == null) return null;

            string link = affiliate.deeplinkurl ?? "";
            string code = affiliate.affiliateShortCode ?? "";
            bool hasLink = link.StartsWith("http", StringComparison.OrdinalIgnoreCase);
            string appName = !string.IsNullOrEmpty(companyName) ? companyName : Application.productName;
            string shared = hasLink ? link : code;

            if (string.IsNullOrEmpty(shared)) return null;

            if (!string.IsNullOrEmpty(message))
            {
                if (message.Contains("{link}") || message.Contains("{code}"))
                {
                    return message.Replace("{link}", shared).Replace("{code}", code);
                }
                return $"{message} {shared}";
            }

            return hasLink
                ? $"Try {appName}: {link}"
                : $"Use my code {code} in {appName}";
        }

        // Internal API used by the drop-in panel

        internal static string ReferralCompanyName => referralCompanyName;

        internal static void FetchMyAffiliateDetails(Action<MyAffiliateDetails, string> callback)
        {
            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(FetchMyAffiliateDetailsCoroutine(callback));
        }

        internal static void FetchReferralProgramConfig(Action<ReferralProgramConfig, string> callback)
        {
            InsertAffiliateCoroutineRunner.Instance.StartCoroutine(FetchReferralProgramConfigCoroutine(callback));
        }

        /// <summary>
        /// Opens the native share sheet. Returns false where there is none; the text is then on the clipboard.
        /// </summary>
        internal static bool ShareText(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                using (var intent = new AndroidJavaObject("android.content.Intent"))
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                    intent.Call<AndroidJavaObject>("setType", "text/plain");
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share"))
                    {
                        activity.Call("startActivity", chooser);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                if (verboseLogging) Debug.Log($"[Insert Affiliate] Android share failed: {e.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                _InsertAffiliate_ShareText(text);
                return true;
            }
            catch (Exception e)
            {
                if (verboseLogging) Debug.Log($"[Insert Affiliate] iOS share failed: {e.Message}");
            }
#endif
            GUIUtility.systemCopyBuffer = text;
            return false;
        }

        // Token storage

        private static string ReferrerTokenKey()
        {
            return KEY_REFERRER_TOKEN_PREFIX + companyCode;
        }

        private static string GetReferrerToken()
        {
            if (string.IsNullOrEmpty(companyCode)) return null;
            string token = PlayerPrefs.GetString(ReferrerTokenKey(), string.Empty);
            return string.IsNullOrEmpty(token) ? null : token;
        }

        private static void StoreReferrerToken(string token)
        {
            PlayerPrefs.SetString(ReferrerTokenKey(), token);
            PlayerPrefs.Save();
        }

        private static void ClearReferrerToken()
        {
            if (string.IsNullOrEmpty(companyCode)) return;
            PlayerPrefs.DeleteKey(ReferrerTokenKey());
            PlayerPrefs.Save();
        }

        // Requests

        private static bool CanCallReferralApi(Action<ReferralResult> callback)
        {
            if (isInitialized && !string.IsNullOrEmpty(companyCode)) return true;

            Debug.LogError("[Insert Affiliate] SDK not initialized. Call Initialize() first.");
            callback?.Invoke(ReferralResult.Error(ReferralErrorCodes.NotInitialized, "SDK not initialized. Call Initialize() first."));
            return false;
        }

        private static IEnumerator ReferralEnrolCoroutine(string path, string jsonPayload, Action<ReferralResult> callback)
        {
            string url = $"{API_BASE_URL}{API_SDK_AFFILIATE}{path}";

            if (verboseLogging)
            {
                Debug.Log($"[Insert Affiliate] Referral request: POST {url}");
            }

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonPayload));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = REFERRAL_REQUEST_TIMEOUT;

                yield return request.SendWebRequest();

                ReferralResult result;
                ReferralResponse response = ParseReferralJson<ReferralResponse>(request.downloadHandler.text);

                if (request.result == UnityWebRequest.Result.Success && response != null)
                {
                    if (response.status == ReferralStatus.VerificationRequired)
                    {
                        result = new ReferralResult { status = ReferralStatus.VerificationRequired };
                    }
                    else if (!string.IsNullOrEmpty(response.token) &&
                             (response.status == ReferralStatus.Created || response.status == ReferralStatus.Connected))
                    {
                        StoreReferrerToken(response.token);
                        result = new ReferralResult { status = response.status, affiliate = response.affiliate };
                    }
                    else
                    {
                        result = ReferralResult.Error(ReferralErrorCodes.ServerError, "Unexpected response from the server.");
                    }
                }
                else
                {
                    result = ErrorResultFor(request, response);
                }

                // Never log the response body here: it holds the device token.
                if (verboseLogging)
                {
                    Debug.Log($"[Insert Affiliate] Referral {path.TrimStart('/')} result: {result.status}" +
                              (result.IsError ? $" ({result.errorCode})" : ""));
                }

                callback?.Invoke(result);
            }
        }

        private static IEnumerator FetchMyAffiliateDetailsCoroutine(Action<MyAffiliateDetails, string> callback)
        {
            string token = GetReferrerToken();
            if (string.IsNullOrEmpty(token))
            {
                if (verboseLogging)
                {
                    Debug.Log("[Insert Affiliate] Not connected as an affiliate on this device");
                }
                callback?.Invoke(null, null);
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get($"{API_BASE_URL}{API_SDK_AFFILIATE}/me"))
            {
                request.SetRequestHeader(TOKEN_HEADER, token);
                request.timeout = REFERRAL_REQUEST_TIMEOUT;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    MyAffiliateDetails details = ParseReferralJson<MyAffiliateDetails>(request.downloadHandler.text);
                    if (details != null)
                    {
                        // JsonUtility reads a missing or null string as "" and may leave a missing array null.
                        if (string.IsNullOrEmpty(details.premiumUntil)) details.premiumUntil = null;
                        if (details.rewardCodes == null) details.rewardCodes = new ReferralRewardCode[0];

                        if (verboseLogging)
                        {
                            Debug.Log($"[Insert Affiliate] Affiliate details loaded: {details.affiliateShortCode}, referrals: {details.referralCount}");
                        }
                        callback?.Invoke(details, null);
                        yield break;
                    }
                    callback?.Invoke(null, ReferralErrorCodes.ServerError);
                    yield break;
                }

                ReferralResult error = ErrorResultFor(request, ParseReferralJson<ReferralResponse>(request.downloadHandler.text));

                // The token no longer works (revoked, or the affiliate was deleted): forget it.
                if (request.responseCode == 401 || request.responseCode == 404)
                {
                    ClearReferrerToken();
                    if (verboseLogging)
                    {
                        Debug.Log($"[Insert Affiliate] Affiliate connection no longer valid ({error.errorCode}), cleared");
                    }
                    callback?.Invoke(null, null);
                    yield break;
                }

                if (verboseLogging)
                {
                    Debug.Log($"[Insert Affiliate] Failed to load affiliate details: {error.errorCode} ({request.responseCode})");
                }
                callback?.Invoke(null, error.errorCode);
            }
        }

        private static IEnumerator SetReferrerAccountCoroutine(ReferrerAccountOptions options, Action<bool> callback)
        {
            string token = GetReferrerToken();
            if (string.IsNullOrEmpty(token))
            {
                if (verboseLogging)
                {
                    Debug.Log("[Insert Affiliate] Not connected as an affiliate on this device");
                }
                callback?.Invoke(false);
                yield break;
            }

            var payload = new ReferrerIdentityPayload
            {
                appUserId = options?.appUserId ?? "",
                playPurchaseToken = options?.playPurchaseToken ?? "",
                deviceId = GetOrCreateShortUniqueDeviceID()
            };

            using (UnityWebRequest request = new UnityWebRequest($"{API_BASE_URL}{API_SDK_AFFILIATE}/me/identity", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(ReferralRequestJson(payload)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader(TOKEN_HEADER, token);
                request.timeout = REFERRAL_REQUEST_TIMEOUT;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ReferrerIdentityResponse response = ParseReferralJson<ReferrerIdentityResponse>(request.downloadHandler.text);
                    bool saved = response != null && response.saved;
                    if (verboseLogging)
                    {
                        Debug.Log($"[Insert Affiliate] Referrer account saved: {saved}");
                    }
                    callback?.Invoke(saved);
                    yield break;
                }

                ReferralResult error = ErrorResultFor(request, ParseReferralJson<ReferralResponse>(request.downloadHandler.text));

                // The token no longer works (revoked, or the affiliate was deleted): forget it.
                if (request.responseCode == 401 || request.responseCode == 404)
                {
                    ClearReferrerToken();
                    if (verboseLogging)
                    {
                        Debug.Log($"[Insert Affiliate] Affiliate connection no longer valid ({error.errorCode}), cleared");
                    }
                    callback?.Invoke(false);
                    yield break;
                }

                if (verboseLogging)
                {
                    Debug.Log($"[Insert Affiliate] Failed to save referrer account: {error.errorCode} ({request.responseCode})");
                }
                callback?.Invoke(false);
            }
        }

        private static IEnumerator FetchReferralProgramConfigCoroutine(Action<ReferralProgramConfig, string> callback)
        {
            string url = $"{API_BASE_URL}{API_SDK_AFFILIATE}/config/{UnityWebRequest.EscapeURL(companyCode)}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = REFERRAL_REQUEST_TIMEOUT;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ReferralProgramConfig config = ParseReferralJson<ReferralProgramConfig>(request.downloadHandler.text);
                    if (config != null)
                    {
                        referralCompanyName = config.companyName;
                        if (verboseLogging)
                        {
                            Debug.Log($"[Insert Affiliate] Referral program config: enabled={config.enabled}, trigger={config.referralTrigger}");
                        }
                        callback?.Invoke(config, null);
                        yield break;
                    }
                    callback?.Invoke(null, ReferralErrorCodes.ServerError);
                    yield break;
                }

                ReferralResult error = ErrorResultFor(request, ParseReferralJson<ReferralResponse>(request.downloadHandler.text));
                if (verboseLogging)
                {
                    Debug.Log($"[Insert Affiliate] Failed to load referral program config: {error.errorCode} ({request.responseCode})");
                }
                callback?.Invoke(null, error.errorCode);
            }
        }

        private static IEnumerator GetReferralShareTextCoroutine(string message, Action<string> callback)
        {
            MyAffiliateDetails details = null;
            bool done = false;
            yield return FetchMyAffiliateDetailsCoroutine((d, e) => { details = d; done = true; });

            if (!done || details == null)
            {
                callback?.Invoke(null);
                yield break;
            }

            if (string.IsNullOrEmpty(referralCompanyName))
            {
                yield return FetchReferralProgramConfigCoroutine((c, e) => { });
            }

            callback?.Invoke(BuildReferralShareText(details.ToAffiliate(), referralCompanyName, message));
        }

        private static ReferralResult ErrorResultFor(UnityWebRequest request, ReferralResponse response)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return ReferralResult.Error(ReferralErrorCodes.NetworkError, request.error ?? "Could not connect.");
            }

            string code = response != null && !string.IsNullOrEmpty(response.code)
                ? response.code
                : ReferralErrorCodes.ServerError;
            string message = response != null && !string.IsNullOrEmpty(response.error)
                ? response.error
                : (request.error ?? "Request failed.");
            return ReferralResult.Error(code, message);
        }

        // Request body for enrol, verify and /me/identity, with the phone's OS added.
        private static string ReferralRequestJson(object payload)
        {
            return WithReferralOs(JsonUtility.ToJson(payload), Application.platform);
        }

        // The phone's OS, so the server can pick the referrer's reward store (App Store or Google Play).
        // Null in the editor and on other platforms.
        private static string ReferralOs(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                case RuntimePlatform.Android:
                    return "android";
                default:
                    return null;
            }
        }

        // JsonUtility cannot leave out an empty field, so "os" is appended to the JSON object only when known.
        private static string WithReferralOs(string json, RuntimePlatform platform)
        {
            string os = ReferralOs(platform);
            if (os == null || string.IsNullOrEmpty(json) || !json.EndsWith("}")) return json;
            string separator = json.TrimEnd('}').Trim() == "{" ? "" : ",";
            return json.Substring(0, json.Length - 1) + separator + "\"os\":\"" + os + "\"}";
        }

        private static T ParseReferralJson<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json) || !json.TrimStart().StartsWith("{")) return null;
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        [Serializable]
        private class ReferralEnrolPayload
        {
            public string companyId;
            public string email;
            public string name;
            public string platform;
            public string deviceId;
            public string appUserId;
            public string playPurchaseToken;
        }

        [Serializable]
        private class ReferralVerifyPayload
        {
            public string companyId;
            public string email;
            public string code;
            public string name;
            public string platform;
            public string deviceId;
            public string appUserId;
            public string playPurchaseToken;
        }

        [Serializable]
        private class ReferrerIdentityPayload
        {
            public string appUserId;
            public string playPurchaseToken;
            public string deviceId;
        }

        [Serializable]
        private class ReferrerIdentityResponse
        {
            public bool saved;
        }

        // Success ({ status, token, affiliate }) and error ({ error, code }) bodies share this shape.
        [Serializable]
        private class ReferralResponse
        {
            public string status;
            public string token;
            public ReferrerAffiliate affiliate;
            public string error;
            public string code;
        }
    }

    /// <summary>
    /// Status values for ReferralResult.status
    /// </summary>
    public static class ReferralStatus
    {
        public const string Created = "created";
        public const string Connected = "connected";
        public const string VerificationRequired = "verificationRequired";
        public const string Error = "error";
    }

    /// <summary>
    /// Error codes for ReferralResult.errorCode. Server codes are passed through as-is.
    /// </summary>
    public static class ReferralErrorCodes
    {
        public const string InvalidEmail = "INVALID_EMAIL";
        public const string InvalidCompanyId = "INVALID_COMPANY_ID";
        public const string InvalidCode = "INVALID_CODE";
        public const string ProgramDisabled = "PROGRAM_DISABLED";
        public const string AffiliateLimitReached = "AFFILIATE_LIMIT_REACHED";
        public const string CompanyNotFound = "COMPANY_NOT_FOUND";
        public const string TooManyCodes = "TOO_MANY_CODES";
        public const string RateLimited = "RATE_LIMITED";
        public const string NetworkError = "NETWORK_ERROR";
        public const string ServerError = "SERVER_ERROR";
        public const string NotInitialized = "NOT_INITIALIZED";
    }

    /// <summary>
    /// Result of CreateAffiliateForUser and VerifyAffiliateCode
    /// </summary>
    [Serializable]
    public class ReferralResult
    {
        /// <summary>"created", "connected", "verificationRequired" or "error" (see ReferralStatus)</summary>
        public string status;
        /// <summary>The affiliate, when status is "created" or "connected"</summary>
        public ReferrerAffiliate affiliate;
        /// <summary>Set when status is "error" (see ReferralErrorCodes)</summary>
        public string errorCode;
        public string errorMessage;

        public bool IsConnected => status == ReferralStatus.Created || status == ReferralStatus.Connected;
        public bool IsVerificationRequired => status == ReferralStatus.VerificationRequired;
        public bool IsError => status == ReferralStatus.Error;

        internal static ReferralResult Error(string code, string message)
        {
            return new ReferralResult { status = ReferralStatus.Error, errorCode = code, errorMessage = message };
        }
    }

    /// <summary>
    /// The referrer's own accounts, used to reward them automatically. All optional.
    /// </summary>
    [Serializable]
    public class ReferrerAccountOptions
    {
        /// <summary>The user's RevenueCat app user id or Adapty customer user id</summary>
        public string appUserId;
        /// <summary>The user's own Google Play subscription purchase token (Android)</summary>
        public string playPurchaseToken;
    }

    /// <summary>
    /// An App Store one-time offer code granted to the referrer as a reward
    /// </summary>
    [Serializable]
    public class ReferralRewardCode
    {
        public string code;
        /// <summary>Opens the App Store to redeem the code</summary>
        public string redeemUrl;
        /// <summary>ISO 8601 date and time</summary>
        public string grantedAt;
    }

    /// <summary>
    /// A referrer's affiliate identity
    /// </summary>
    [Serializable]
    public class ReferrerAffiliate
    {
        public string affiliateName;
        public string affiliateShortCode;
        /// <summary>The referral link. For Short Code Only companies this is the short code itself; it can be empty until a link is assigned.</summary>
        public string deeplinkurl;
    }

    /// <summary>
    /// The connected user's affiliate details and referral stats.
    /// For display: a modified device can fake these, so grant anything valuable from your server.
    /// </summary>
    [Serializable]
    public class MyAffiliateDetails
    {
        public string affiliateName;
        public string affiliateShortCode;
        public string deeplinkurl;
        /// <summary>What counts as a referral for this company: "install", "event" or "purchase"</summary>
        public string referralTrigger;
        /// <summary>Referrals for the configured trigger. Only goes up.</summary>
        public int referralCount;
        public int installCount;
        public int eventCount;
        public int purchaseCount;
        public double totalEarned;
        public double totalPaid;
        public double totalUnpaid;
        public string currency;
        public string dashboardUrl;
        /// <summary>Rewards granted to this referrer so far</summary>
        public int rewardsGranted;
        /// <summary>ISO 8601 date the referrer's free premium runs until, or null</summary>
        public string premiumUntil;
        /// <summary>App Store one-time offer codes granted as rewards, newest first. Never null.</summary>
        public ReferralRewardCode[] rewardCodes;

        public ReferrerAffiliate ToAffiliate()
        {
            return new ReferrerAffiliate
            {
                affiliateName = affiliateName,
                affiliateShortCode = affiliateShortCode,
                deeplinkurl = deeplinkurl
            };
        }
    }

    /// <summary>
    /// The company's referral program settings, set in the Insert Affiliate dashboard
    /// </summary>
    [Serializable]
    public class ReferralProgramConfig
    {
        public bool enabled;
        public string companyName;
        public string referralTrigger;
        /// <summary>Empty when not set in the dashboard</summary>
        public string headline;
        /// <summary>Empty when not set in the dashboard</summary>
        public string rewardText;
        /// <summary>Hex colour such as "#6A0DAD", or empty when not set</summary>
        public string primaryColor;
    }
}
