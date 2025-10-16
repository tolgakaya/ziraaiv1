# Sponsorship SMS Deep Linking Implementation Plan

## Implementation Date
2025-10-14

## Summary
Created comprehensive mobile integration guide for implementing SMS-based automatic sponsorship code redemption using deferred deep linking pattern from referral system.

## Key Documents Created
- **MOBILE_SPONSORSHIP_SMS_DEEP_LINKING_COMPLETE_GUIDE.md**: Complete 50+ page implementation guide

## Implementation Pattern
Following proven referral system SMS auto-fill pattern:
1. SMS contains visible code (AGRI-XXXXX format)
2. Mobile app SMS listener extracts code automatically
3. Deferred deep linking for app-not-installed scenarios
4. Auto-fill redemption screen with extracted code
5. 90-95% success rate (vs 40-50% current)

## Backend Changes (Minimal)
- Update SMS template to include visible code
- Add Play Store link parameter
- Configuration: MobileApp:PlayStorePackageName
- File: SendSponsorshipLinkCommand.cs

## Mobile Changes (2-3 days)
- SMS listener service (SponsorshipSmsListener)
- Deferred deep linking with SharedPreferences
- Post-login hook for pending codes
- Updated redemption screen with auto-fill
- Permissions: RECEIVE_SMS, READ_SMS

## API Endpoints (No Changes)
- POST /api/v1/sponsorship/redeem (already exists)
- GET /api/v1/sponsorship/validate/{code} (optional pre-check)

## Success Metrics
- SMS delivery rate: >95%
- Code detection rate: >90%
- Auto-fill success: >85%
- Overall redemption: >90% (vs 40-50% before)
- Time to redemption: <2 min (vs 5-10 min before)

## Testing Strategy
- Phase 1: SMS parsing tests (regex validation)
- Phase 2: App installed scenarios (logged in/out)
- Phase 3: Deferred deep linking (app not installed)
- Phase 4: API integration tests
- Phase 5: Permission tests

## Timeline
- Backend: 0.5 days (SMS template update)
- Mobile: 2-3 days (SMS listener + UI)
- Testing: 1 day (QA validation)
- Total: 3-4 days end-to-end

## Code Format
- Current: AGRI-XXXXX (no changes needed)
- Regex: `(AGRI-[A-Z0-9]+|SPONSOR-[A-Z0-9]+)`
- Backward compatible with existing codes

## Migration Strategy
- Week 1: Backend deployment (new SMS format)
- Week 2: Mobile beta (10% users)
- Week 3: Full rollout (100% users)
- Backward compatible: old apps still work

## Key Files for Mobile Team
1. SponsorshipSmsListener.dart - SMS detection service
2. SponsorshipRedeemScreen.dart - Redemption UI with auto-fill
3. AuthService.dart - Post-login pending code check
4. DeepLinkService.dart - Deep link handler
5. AndroidManifest.xml - Permissions and intent filters

## Environment Configuration
Development: com.ziraai.app.dev
Staging: com.ziraai.app.staging
Production: com.ziraai.app

## Status
✅ Documentation complete and ready for mobile team
✅ Backend changes identified and minimal
✅ Testing strategy defined
✅ Success metrics established
