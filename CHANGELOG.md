# Changelog

All notable changes to the Insert Affiliate Unity SDK will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
