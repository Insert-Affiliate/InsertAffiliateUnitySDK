using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InsertAffiliate
{
    /// <summary>
    /// Options for InsertAffiliateSDK.ShowReferAFriend. All optional.
    /// Headline, reward text and colour fall back to the dashboard settings, then to the defaults.
    /// </summary>
    public class ReferAFriendOptions
    {
        /// <summary>Prefills the email field (usually the app's logged-in user)</summary>
        public string email;
        /// <summary>Prefills the name field</summary>
        public string name;
        /// <summary>The user's RevenueCat app user id or Adapty customer user id, used to reward them automatically</summary>
        public string appUserId;
        /// <summary>The user's own Google Play subscription purchase token (Android), used to reward them automatically</summary>
        public string playPurchaseToken;
        /// <summary>Share message. May use {link} and {code} placeholders.</summary>
        public string shareMessage;
        /// <summary>Hex colour such as "#6A0DAD". Overrides the dashboard colour.</summary>
        public string primaryColor;
        /// <summary>Overrides the dashboard headline</summary>
        public string headline;
        /// <summary>Overrides the dashboard reward text</summary>
        public string rewardText;
        /// <summary>Font for all text. Defaults to Unity's built-in font.</summary>
        public Font font;
        /// <summary>Corner radius of the card, buttons and fields, in reference pixels (1080 x 1920 layout)</summary>
        public float cornerRadius = 24f;
        /// <summary>Canvas sorting order, so the panel draws above your UI</summary>
        public int sortingOrder = 1000;
        /// <summary>Called when the user closes the panel</summary>
        public Action onClose;
    }

    /// <summary>
    /// The drop-in "Refer a friend" screen, built in code with uGUI (no prefab or assets).
    /// Show it with InsertAffiliateSDK.ShowReferAFriend().
    /// </summary>
    public class ReferAFriendPanel : MonoBehaviour
    {
        private const string DEFAULT_PRIMARY_COLOR = "#6A0DAD";
        private const string DEFAULT_HEADLINE = "Refer a friend";
        private const float CARD_WIDTH = 960f;
        private const int TITLE_SIZE = 56;
        private const int BODY_SIZE = 38;
        private const int SMALL_SIZE = 32;
        private const float CONTROL_HEIGHT = 124f;
        // Space kept free above and below the card, in reference pixels
        private const float SCREEN_MARGIN = 64f;

        private static readonly Color CardColor = Color.white;
        private static readonly Color TextColor = new Color32(0x1F, 0x1F, 0x24, 0xFF);
        private static readonly Color MutedColor = new Color32(0x6B, 0x6B, 0x76, 0xFF);
        private static readonly Color FieldColor = new Color32(0xF2, 0xF2, 0xF5, 0xFF);
        private static readonly Color ErrorColor = new Color32(0xC6, 0x28, 0x28, 0xFF);
        private static readonly Color SuccessColor = new Color32(0x2E, 0x7D, 0x32, 0xFF);

        private static ReferAFriendPanel current;
        private static Sprite roundedSprite;

        private ReferAFriendOptions options;
        private ReferralProgramConfig config;
        private MyAffiliateDetails details;
        private Color primary;
        private Font font;
        private Text headlineText;
        private RectTransform card;
        private RectTransform header;
        private VerticalLayoutGroup cardLayout;
        private RectTransform body;
        private ScrollRect bodyScroll;
        private LayoutElement bodyScrollLayout;
        private bool bodyChanged;
        private Text statusText;
        private GameObject createdEventSystem;
        private string enteredEmail;
        private string enteredName;
        private bool referrerAccountSent;
        private bool closed;

        /// <summary>
        /// True while the panel is on screen
        /// </summary>
        public static bool IsShowing => current != null;

        /// <summary>
        /// Close the panel if it is showing
        /// </summary>
        public static void Hide()
        {
            if (current != null) current.Close();
        }

        internal static void Show(ReferAFriendOptions options)
        {
            // The panel already showing closes first (its onClose runs). The EventSystem it added
            // moves to the new panel, which removes it on close.
            GameObject eventSystem = null;
            if (current != null)
            {
                eventSystem = current.createdEventSystem;
                current.createdEventSystem = null;
                current.Close();
            }

            var root = new GameObject("InsertAffiliateReferAFriend");
            current = root.AddComponent<ReferAFriendPanel>();
            current.Build(options);
            if (eventSystem != null) current.createdEventSystem = eventSystem;
            current.Load();
        }

        /// <summary>
        /// A short message at the bottom of the screen (used when Share falls back to the clipboard).
        /// </summary>
        internal static void ShowToast(string message, Font font)
        {
            var root = new GameObject("InsertAffiliateToast");
            CreateCanvas(root, 1100);

            var pill = NewRect("Toast", root.transform);
            pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 0f);
            pill.pivot = new Vector2(0.5f, 0f);
            pill.anchoredPosition = new Vector2(0f, 220f);
            var image = pill.gameObject.AddComponent<Image>();
            ApplyRounded(image, 40f);
            image.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);
            image.raycastTarget = false;
            var layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 28, 28);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = pill.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var text = CreateText(pill, message, BODY_SIZE, Color.white, font ?? DefaultFont());
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            Destroy(root, 2f);
        }

        /// <summary>
        /// Close the panel
        /// </summary>
        public void Close()
        {
            // Runs once, so onClose fires once even when Close and the back button land in the same frame.
            if (closed) return;
            closed = true;

            if (current == this) current = null;
            if (createdEventSystem != null) Destroy(createdEventSystem);
            Destroy(gameObject);
            options?.onClose?.Invoke();
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private void Update()
        {
            // Android back button closes the panel.
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }
#endif

        // Layout

        private void Build(ReferAFriendOptions opts)
        {
            options = opts;
            font = opts.font != null ? opts.font : DefaultFont();
            primary = ResolvePrimaryColor();

            CreateCanvas(gameObject, opts.sortingOrder);
            createdEventSystem = EnsureEventSystem();

            // Dimmed backdrop that also swallows taps meant for the app behind.
            var backdrop = NewRect("Backdrop", transform);
            Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            card = NewRect("Card", transform);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(CARD_WIDTH, 0f);
            var cardImage = card.gameObject.AddComponent<Image>();
            ApplyRounded(cardImage, opts.cornerRadius);
            cardImage.color = CardColor;
            cardLayout = AddVerticalLayout(card.gameObject, 36f);
            cardLayout.padding = new RectOffset(64, 64, 56, 64);
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            header = NewRect("Header", card);
            var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childControlWidth = headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.spacing = 24f;

            headlineText = CreateText(header, ResolveHeadline(), TITLE_SIZE, TextColor, font, FontStyle.Bold, TextAnchor.MiddleLeft);
            headlineText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var closeButton = AddTextButton(header, "Close", MutedColor, Close);
            var closeLayout = closeButton.GetComponent<LayoutElement>();
            closeLayout.preferredWidth = 160f;
            closeLayout.flexibleWidth = 0f;

            // Everything below the header scrolls, so the card never grows past the screen
            // and Close stays reachable however much the body holds.
            var scroll = NewRect("BodyScroll", card);
            bodyScrollLayout = scroll.gameObject.AddComponent<LayoutElement>();
            bodyScrollLayout.flexibleHeight = 0f;

            var viewport = NewRect("Viewport", scroll);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            // Transparent image so drags anywhere in the body scroll it.
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            body = NewRect("Body", viewport);
            body.anchorMin = new Vector2(0f, 1f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 1f);
            body.sizeDelta = Vector2.zero;
            AddVerticalLayout(body.gameObject, 28f);
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            bodyScroll = scroll.gameObject.AddComponent<ScrollRect>();
            bodyScroll.viewport = viewport;
            bodyScroll.content = body;
            bodyScroll.horizontal = false;
            bodyScroll.vertical = true;
            bodyScroll.movementType = ScrollRect.MovementType.Clamped;
            bodyScroll.scrollSensitivity = 40f;
        }

        private void LateUpdate()
        {
            FitBodyToScreen();
        }

        // The body is as tall as its content, up to what the screen leaves after the margins and the
        // card's header and padding; beyond that it scrolls. Runs every frame, so rotation and new
        // content are picked up before the canvas draws.
        private void FitBodyToScreen()
        {
            if (body == null) return;
            if (bodyChanged)
            {
                bodyChanged = false;
                LayoutRebuilder.ForceRebuildLayoutImmediate(body);
                bodyScroll.verticalNormalizedPosition = 1f;
            }

            float canvasHeight = ((RectTransform)transform).rect.height;
            float chrome = cardLayout.padding.vertical + cardLayout.spacing + LayoutUtility.GetPreferredHeight(header);
            float maxHeight = Mathf.Max(0f, canvasHeight - 2f * SCREEN_MARGIN - chrome);
            float height = Mathf.Min(LayoutUtility.GetPreferredHeight(body), maxHeight);

            if (!Mathf.Approximately(bodyScrollLayout.preferredHeight, height))
            {
                bodyScrollLayout.minHeight = bodyScrollLayout.preferredHeight = height;
                LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            }
        }

        private void ClearBody()
        {
            for (int i = body.childCount - 1; i >= 0; i--)
            {
                var child = body.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            statusText = null;
            bodyChanged = true;
        }

        // States

        private void Load()
        {
            ShowLoading();
            InsertAffiliateSDK.FetchReferralProgramConfig((loaded, errorCode) =>
            {
                if (this == null) return;
                config = loaded;
                primary = ResolvePrimaryColor();
                headlineText.text = ResolveHeadline();

                if (InsertAffiliateSDK.IsUserAnAffiliate())
                {
                    SendReferrerAccountOnce();
                    LoadDetails();
                }
                else if (config == null)
                {
                    ShowError(MessageFor(errorCode ?? ReferralErrorCodes.NetworkError));
                }
                else if (!config.enabled)
                {
                    ShowNotice(MessageFor(ReferralErrorCodes.ProgramDisabled));
                }
                else
                {
                    ShowJoin();
                }
            });
        }

        // Already joined: save the accounts passed in the options so waiting rewards get granted.
        private void SendReferrerAccountOnce()
        {
            if (referrerAccountSent) return;
            if (string.IsNullOrEmpty(options.appUserId) && string.IsNullOrEmpty(options.playPurchaseToken)) return;
            referrerAccountSent = true;
            InsertAffiliateSDK.SetReferrerAccount(ReferrerAccount());
        }

        private ReferrerAccountOptions ReferrerAccount()
        {
            return new ReferrerAccountOptions { appUserId = options.appUserId, playPurchaseToken = options.playPurchaseToken };
        }

        private void LoadDetails()
        {
            ShowLoading();
            InsertAffiliateSDK.FetchMyAffiliateDetails((loaded, errorCode) =>
            {
                if (this == null) return;
                if (loaded != null)
                {
                    details = loaded;
                    ShowEnrolled();
                }
                else if (errorCode != null)
                {
                    ShowError(MessageFor(errorCode));
                }
                else if (config != null && !config.enabled)
                {
                    ShowNotice(MessageFor(ReferralErrorCodes.ProgramDisabled));
                }
                else
                {
                    // Not connected any more (the stored token was cleared): join again.
                    ShowJoin();
                }
            });
        }

        private void ShowLoading()
        {
            ClearBody();
            CreateText(body, "Loading...", BODY_SIZE, MutedColor, font);
        }

        private void ShowNotice(string message)
        {
            ClearBody();
            CreateText(body, message, BODY_SIZE, MutedColor, font);
        }

        private void ShowError(string message)
        {
            ClearBody();
            CreateText(body, message, BODY_SIZE, ErrorColor, font);
            AddButton(body, "Try again", primary, Color.white, Load);
        }

        private void ShowJoin()
        {
            ClearBody();

            string reward = ResolveRewardText();
            CreateText(body, string.IsNullOrEmpty(reward) ? "Get your own link to share with friends." : reward,
                BODY_SIZE, MutedColor, font);

            var emailField = AddInput(body, "Email", enteredEmail ?? options.email, InputField.ContentType.EmailAddress);
            var nameField = AddInput(body, "Name", enteredName ?? options.name, InputField.ContentType.Name);

            Button joinButton = null;
            joinButton = AddButton(body, "Get my link", primary, Color.white, () =>
            {
                string email = emailField.text.Trim();
                if (!LooksLikeEmail(email))
                {
                    SetStatus(MessageFor(ReferralErrorCodes.InvalidEmail), ErrorColor);
                    return;
                }
                enteredEmail = email;
                enteredName = nameField.text.Trim();

                SetBusy(joinButton, "Please wait...");
                InsertAffiliateSDK.CreateAffiliateForUser(enteredEmail, enteredName, ReferrerAccount(), result =>
                {
                    if (this == null) return;
                    if (result.IsConnected) LoadDetails();
                    else if (result.IsVerificationRequired) ShowCode();
                    else
                    {
                        SetIdle(joinButton, "Get my link");
                        SetStatus(MessageFor(result.errorCode), ErrorColor);
                    }
                });
            });

            statusText = CreateText(body, "", SMALL_SIZE, ErrorColor, font);
        }

        private void ShowCode()
        {
            ClearBody();

            CreateText(body, $"We emailed a 6-digit code to {enteredEmail}. Enter it below to connect this device.",
                BODY_SIZE, MutedColor, font);

            // Any characters are accepted, so a pasted "123 456" or digits from another script work;
            // the code is normalized to ASCII digits and Verify is enabled at exactly 6.
            var codeField = AddInput(body, "6-digit code", "", InputField.ContentType.Custom);
            codeField.lineType = InputField.LineType.SingleLine;
            codeField.inputType = InputField.InputType.Standard;
            codeField.keyboardType = TouchScreenKeyboardType.NumberPad;
            codeField.characterValidation = InputField.CharacterValidation.None;
            codeField.characterLimit = 32;

            Button verifyButton = null;
            bool verifying = false;
            verifyButton = AddButton(body, "Verify", primary, Color.white, () =>
            {
                string code = InsertAffiliateSDK.NormalizeVerificationCode(codeField.text);
                if (code.Length != 6)
                {
                    SetStatus("Enter the 6-digit code from the email.", ErrorColor);
                    return;
                }

                verifying = true;
                SetBusy(verifyButton, "Please wait...");
                InsertAffiliateSDK.VerifyAffiliateCode(enteredEmail, code, enteredName, ReferrerAccount(), result =>
                {
                    if (this == null) return;
                    if (result.IsConnected) LoadDetails();
                    else
                    {
                        verifying = false;
                        SetIdle(verifyButton, "Verify");
                        if (verifyButton != null) verifyButton.interactable = HasSixDigits(codeField.text);
                        SetStatus(MessageFor(result.errorCode), ErrorColor);
                    }
                });
            });
            verifyButton.interactable = false;
            codeField.onValueChanged.AddListener(value =>
            {
                if (!verifying) verifyButton.interactable = HasSixDigits(value);
            });

            Button resendButton = null;
            resendButton = AddTextButton(body, "Send a new code", primary, () =>
            {
                SetBusy(resendButton, "Sending...");
                InsertAffiliateSDK.CreateAffiliateForUser(enteredEmail, enteredName, ReferrerAccount(), result =>
                {
                    if (this == null) return;
                    SetIdle(resendButton, "Send a new code");
                    if (result.IsConnected) LoadDetails();
                    else if (result.IsVerificationRequired) SetStatus("A new code is on its way.", SuccessColor);
                    else SetStatus(MessageFor(result.errorCode), ErrorColor);
                });
            });

            AddTextButton(body, "Use a different email", MutedColor, ShowJoin);

            statusText = CreateText(body, "", SMALL_SIZE, ErrorColor, font);
        }

        private void ShowEnrolled()
        {
            ClearBody();

            string reward = ResolveRewardText();
            if (!string.IsNullOrEmpty(reward))
            {
                CreateText(body, reward, BODY_SIZE, MutedColor, font);
            }

            DateTime premiumUntil;
            if (TryParseDate(details.premiumUntil, out premiumUntil) && premiumUntil > DateTime.UtcNow)
            {
                string date = premiumUntil.ToLocalTime().ToString("D", CultureInfo.CurrentCulture);
                CreateText(body, $"Free premium until {date}", BODY_SIZE, SuccessColor, font, FontStyle.Bold);
            }

            string code = details.affiliateShortCode ?? "";
            string link = details.deeplinkurl ?? "";
            bool hasLink = link.StartsWith("http", StringComparison.OrdinalIgnoreCase);

            // Code box
            var codeBox = NewRect("CodeBox", body);
            var codeImage = codeBox.gameObject.AddComponent<Image>();
            ApplyRounded(codeImage, options.cornerRadius);
            codeImage.color = new Color(primary.r, primary.g, primary.b, 0.08f);
            AddVerticalLayout(codeBox.gameObject, 8f).padding = new RectOffset(32, 32, 28, 28);
            CreateText(codeBox, "Your code", SMALL_SIZE, MutedColor, font);
            CreateText(codeBox, code, 72, primary, font, FontStyle.Bold);
            if (hasLink)
            {
                CreateText(codeBox, link, SMALL_SIZE, TextColor, font);
            }

            // Copy + Share
            var actions = NewRect("Actions", body);
            var actionsLayout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 24f;
            actionsLayout.childControlWidth = actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = false;

            string copyValue = hasLink ? link : code;
            var copyButton = AddButton(actions, hasLink ? "Copy link" : "Copy code",
                new Color(primary.r, primary.g, primary.b, 0.12f), primary, () =>
                {
                    GUIUtility.systemCopyBuffer = copyValue;
                    SetStatus("Copied", SuccessColor);
                });
            copyButton.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var shareButton = AddButton(actions, "Share", primary, Color.white, () =>
            {
                string text = InsertAffiliateSDK.BuildReferralShareText(details.ToAffiliate(),
                    config != null ? config.companyName : InsertAffiliateSDK.ReferralCompanyName, options.shareMessage);
                if (string.IsNullOrEmpty(text)) return;
                if (!InsertAffiliateSDK.ShareText(text))
                {
                    SetStatus("Copied. Paste it anywhere to share.", SuccessColor);
                }
            });
            shareButton.GetComponent<LayoutElement>().flexibleWidth = 1f;

            // Stats
            var stats = NewRect("Stats", body);
            var statsLayout = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
            statsLayout.spacing = 24f;
            statsLayout.childControlWidth = statsLayout.childControlHeight = true;
            statsLayout.childForceExpandWidth = true;
            statsLayout.childForceExpandHeight = false;
            AddStat(stats, details.referralCount.ToString(CultureInfo.InvariantCulture), "Referrals");
            AddStat(stats, FormatMoney(details.totalEarned, details.currency), "Earned");

            // Only codes this phone's store can redeem (App Store on iOS, Google Play on Android).
            var rewardCodes = InsertAffiliateSDK.RewardCodesForPlatform(details.rewardCodes, Application.platform);
            if (rewardCodes.Length > 0)
            {
                AddRewardCodes(rewardCodes);
            }

            if (!string.IsNullOrEmpty(details.dashboardUrl))
            {
                string dashboardUrl = details.dashboardUrl;
                AddTextButton(body, "Open my dashboard", primary, () => Application.OpenURL(dashboardUrl));
            }

            statusText = CreateText(body, "", SMALL_SIZE, SuccessColor, font);
        }

        private void AddRewardCodes(ReferralRewardCode[] rewardCodes)
        {
            CreateText(body, "Your rewards", BODY_SIZE, TextColor, font, FontStyle.Bold, TextAnchor.MiddleLeft);

            foreach (var reward in rewardCodes)
            {
                if (reward == null || string.IsNullOrEmpty(reward.code)) continue;

                var row = NewRect("Reward", body);
                var image = row.gameObject.AddComponent<Image>();
                ApplyRounded(image, options.cornerRadius);
                image.color = FieldColor;
                var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                rowLayout.padding = new RectOffset(32, 16, 16, 16);
                rowLayout.spacing = 24f;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.childControlWidth = rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;

                var codeText = CreateText(row, reward.code, BODY_SIZE, TextColor, font, FontStyle.Bold, TextAnchor.MiddleLeft);
                codeText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                string redeemUrl = reward.redeemUrl;
                var redeemButton = AddButton(row, "Redeem", primary, Color.white, () =>
                {
                    if (!string.IsNullOrEmpty(redeemUrl)) Application.OpenURL(redeemUrl);
                });
                var redeemLayout = redeemButton.GetComponent<LayoutElement>();
                redeemLayout.preferredWidth = 260f;
                redeemLayout.flexibleWidth = 0f;
                redeemButton.interactable = !string.IsNullOrEmpty(redeemUrl);
            }
        }

        private static bool TryParseDate(string value, out DateTime date)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out date);
        }

        private void AddStat(Transform parent, string value, string label)
        {
            var box = NewRect("Stat", parent);
            var image = box.gameObject.AddComponent<Image>();
            ApplyRounded(image, options.cornerRadius);
            image.color = FieldColor;
            AddVerticalLayout(box.gameObject, 4f).padding = new RectOffset(16, 16, 24, 24);
            box.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            CreateText(box, value, 52, TextColor, font, FontStyle.Bold);
            CreateText(box, label, SMALL_SIZE, MutedColor, font);
        }

        // Helpers

        private void SetStatus(string message, Color color)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = color;
        }

        private static void SetBusy(Button button, string label)
        {
            button.interactable = false;
            button.GetComponentInChildren<Text>().text = label;
        }

        private static void SetIdle(Button button, string label)
        {
            if (button == null) return;
            button.interactable = true;
            button.GetComponentInChildren<Text>().text = label;
        }

        private Color ResolvePrimaryColor()
        {
            Color color;
            if (!string.IsNullOrEmpty(options.primaryColor) && ColorUtility.TryParseHtmlString(options.primaryColor, out color)) return color;
            if (config != null && !string.IsNullOrEmpty(config.primaryColor) && ColorUtility.TryParseHtmlString(config.primaryColor, out color)) return color;
            ColorUtility.TryParseHtmlString(DEFAULT_PRIMARY_COLOR, out color);
            return color;
        }

        private string ResolveHeadline()
        {
            if (!string.IsNullOrEmpty(options.headline)) return options.headline;
            if (config != null && !string.IsNullOrEmpty(config.headline)) return config.headline;
            return DEFAULT_HEADLINE;
        }

        private string ResolveRewardText()
        {
            if (!string.IsNullOrEmpty(options.rewardText)) return options.rewardText;
            return config != null ? config.rewardText : null;
        }

        private static string MessageFor(string errorCode)
        {
            switch (errorCode)
            {
                case ReferralErrorCodes.ProgramDisabled:
                    return "Referrals are not available in this app right now.";
                case ReferralErrorCodes.AffiliateLimitReached:
                    return "The referral program is full right now. Please try again later.";
                case ReferralErrorCodes.InvalidCode:
                    return "That code is wrong or has expired.";
                case ReferralErrorCodes.TooManyCodes:
                    return "Too many codes requested. Please wait a while and try again.";
                case ReferralErrorCodes.RateLimited:
                    return "Too many attempts. Please try again later.";
                case ReferralErrorCodes.InvalidEmail:
                    return "Please enter a valid email address.";
                case ReferralErrorCodes.NetworkError:
                    return "Could not connect. Check your connection and try again.";
                default:
                    return "Something went wrong. Please try again.";
            }
        }

        private static string FormatMoney(double amount, string currency)
        {
            string value = amount.ToString("0.00", CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(currency) || currency == "USD") return "$" + value;
            return value + " " + currency;
        }

        private static bool HasSixDigits(string value)
        {
            return InsertAffiliateSDK.NormalizeVerificationCode(value).Length == 6;
        }

        private static bool LooksLikeEmail(string email)
        {
            int at = email.IndexOf('@');
            return at > 0 && at < email.Length - 1 && email.IndexOf('.', at) > at;
        }

        // Taps need an EventSystem. Returns the one created here (removed on close), or null.
        private static GameObject EnsureEventSystem()
        {
            if (EventSystem.current != null) return null;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogWarning("[Insert Affiliate] No EventSystem in the scene. Add one with an InputSystemUIInputModule so the Refer a friend panel can receive taps.");
            return null;
#else
            return new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
        }

        private static void CreateCanvas(GameObject root, int sortingOrder)
        {
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            // Shrink keeps the whole 1080 x 1920 reference area on screen, in portrait and landscape.
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;

            root.AddComponent<GraphicRaycaster>();
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static VerticalLayoutGroup AddVerticalLayout(GameObject go, float spacing)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private static Text CreateText(Transform parent, string value, int size, Color color, Font font,
            FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var rect = NewRect("Text", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private Button AddButton(Transform parent, string label, Color background, Color foreground, Action onClick)
        {
            var rect = NewRect("Button", parent);
            var image = rect.gameObject.AddComponent<Image>();
            ApplyRounded(image, options.cornerRadius);
            image.color = background;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = CONTROL_HEIGHT;

            var text = CreateText(rect, label, BODY_SIZE, foreground, font, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        private Button AddTextButton(Transform parent, string label, Color color, Action onClick)
        {
            var rect = NewRect("TextButton", parent);
            // Transparent image so the whole row is tappable.
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 88f;

            var text = CreateText(rect, label, BODY_SIZE, color, font);
            Stretch(text.rectTransform);
            return button;
        }

        private InputField AddInput(Transform parent, string placeholder, string value, InputField.ContentType contentType)
        {
            var rect = NewRect("Input", parent);
            var image = rect.gameObject.AddComponent<Image>();
            ApplyRounded(image, options.cornerRadius);
            image.color = FieldColor;

            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = CONTROL_HEIGHT;

            var text = CreateText(rect, "", BODY_SIZE, TextColor, font, FontStyle.Normal, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Inset(text.rectTransform, 36f);

            var hint = CreateText(rect, placeholder, BODY_SIZE, MutedColor, font, FontStyle.Italic, TextAnchor.MiddleLeft);
            Inset(hint.rectTransform, 36f);

            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = text;
            input.placeholder = hint;
            input.contentType = contentType;
            input.text = value ?? "";
            return input;
        }

        private static void Inset(RectTransform rect, float horizontal)
        {
            Stretch(rect);
            rect.offsetMin = new Vector2(horizontal, 0f);
            rect.offsetMax = new Vector2(-horizontal, 0f);
        }

        // Rounded corners come from one generated 9-sliced sprite; the multiplier scales its corner to the radius.
        private static void ApplyRounded(Image image, float radius)
        {
            if (radius <= 0f) return;
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = RoundedSpriteRadius / radius;
        }

        private const int RoundedSpriteRadius = 64;

        private static Sprite RoundedSprite()
        {
            if (roundedSprite != null) return roundedSprite;

            int size = RoundedSpriteRadius * 2 + 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[size * size];
            float r = RoundedSpriteRadius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance from the nearest corner centre; inside the straight edges it is 0.
                    float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - r));
                    float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - r));
                    float alpha = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return roundedSprite;
        }

        internal static Font DefaultFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }
    }
}
