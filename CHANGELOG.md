# Changelog

All notable changes to the Insert Affiliate Unity SDK will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2026-05-16

### Added
- **Insert Links** - Built-in deep linking solution, no third-party SDK required
  - iOS: Universal Links, custom URL schemes, clipboard UUID matching, fingerprint detection
  - Android: App Links, Google Play Install Referrer, `?insertAffiliate=` query param
  - Custom domain universal/app link support
  - Deferred deep linking via probabilistic device fingerprint matching
- **Native iOS plugin** for real iOS version detection and clipboard access
- **Native Android plugin** for Google Play Install Referrer capture
- **`insertLinksClipboardEnabled` parameter** in `Initialize()` for clipboard-based attribution
- **`HandleInsertLinks()` method** for routing deep link URLs through Insert Links
- **Country field** sent in detection payload for improved matching accuracy

### Fixed
- Custom URL scheme parsing for new format (`ia-companycode://insert-affiliate?code=SHORTCODE`)
- Universal link parsing for non-V1 format (`insertaffiliate.link/companycode/shortcode`)
- `DateTimeOffset` crash in `GetAffiliateExpiryTimestamp()` when DateTime.Kind is Local

## [1.3.1] - 2026-03-29

### Fixed
- **Offer code sanitization** - Fixed offer codes with dashes/underscores being stripped (e.g. `pro-v3-ext` was incorrectly becoming `prov3ext`)
  - Now checks for API error responses before cleaning the offer code
  - Sanitization regex updated to preserve dashes and underscores

## [1.1.0] - 2025-11-23

### Added
- **New `GetAffiliateDetails()` method** - Retrieve affiliate information without setting the identifier
  - Returns `AffiliateDetailsPublic` object with affiliateName, affiliateShortCode, and deeplinkUrl
  - Useful for displaying affiliate info or showing personalized content based on referrer
  - Automatically strips UUIDs from codes
  - Works with both short codes and deep link URLs

- **New `AffiliateDetailsPublic` class** - Public class for affiliate information
  - Contains affiliateName, affiliateShortCode, and deeplinkUrl properties

### Changed
- **BREAKING: `SetShortCode()` now includes validation callback** - Method signature changed from `void` to include `Action<bool>` callback parameter
  - Validates short codes against the Insert Affiliate API before storing
  - Callback receives `true` if code is valid and stored, `false` otherwise
  - Provides immediate feedback for user-facing validation
  - Callback parameter is optional (defaults to null) for backwards compatibility

### Fixed
- **Offer Code API** - Fixed to properly include company code and platform type in API requests
  - URL now correctly formatted as: `/v1/affiliateReturnOfferCode/{companyCode}/{affiliateLink}?platformType={platformType}`
  - Platform type defaults to "ios", with "android" on Android builds
  - Ensures offer codes are properly retrieved from the API

### Documentation
- Added comprehensive README documentation for `GetAffiliateDetails()`
- Updated `SetShortCode()` documentation with validation examples
- Added Unity code examples showing callback-based validation patterns
- Updated API reference section with new method signatures

## [1.0.0] - 2024-11-21

### Added
- Initial release of Insert Affiliate Unity SDK
- Affiliate tracking and attribution
- Short code support
- Deep linking integration
- Event tracking (Beta)
- Offer codes / Dynamic Product IDs
- Attribution timeout functionality
- Insert Links support
- App Store Direct Integration (iOS)
- RevenueCat integration support
