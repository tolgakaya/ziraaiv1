-- COMPLETE TIER FEATURE SYSTEM RESET
-- Date: 2025-10-27

SET session_replication_role = 'replica';
TRUNCATE TABLE "TierFeatures" RESTART IDENTITY CASCADE;
TRUNCATE TABLE "Features" RESTART IDENTITY CASCADE;
SET session_replication_role = 'origin';

INSERT INTO "Features" ("FeatureKey", "DisplayName", "Description", "RequiresConfiguration", "IsActive", "IsDeprecated", "CreatedDate")
VALUES
  ('messaging', 'Messaging', 'Text and attachments', false, true, false, NOW()),
  ('voice_messages', 'Voice Messages', 'Audio messages', false, true, false, NOW()),
  ('smart_links', 'Smart Links', 'Advanced analytics', false, true, false, NOW()),
  ('advanced_analytics', 'Advanced Analytics', 'Reports', false, true, false, NOW()),
  ('api_access', 'API Access', 'REST API', false, true, false, NOW()),
  ('sponsor_visibility', 'Sponsor Visibility', 'Logo and profile', true, true, false, NOW()),
  ('priority_support', 'Priority Support', 'Fast response', true, true, false, NOW());

INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson", "CreatedDate", "CreatedByUserId")
VALUES (2, 6, true, '{"showLogo": true, "showProfile": true}', NOW(), 1);

INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson", "CreatedDate", "CreatedByUserId")
VALUES
  (3, 1, true, NULL, NOW(), 1),
  (3, 2, true, NULL, NOW(), 1),
  (3, 4, true, NULL, NOW(), 1),
  (3, 6, true, '{"showLogo": true, "showProfile": true}', NOW(), 1),
  (3, 7, true, '{"responseTimeHours": 48}', NOW(), 1);

INSERT INTO "TierFeatures" ("SubscriptionTierId", "FeatureId", "IsEnabled", "ConfigurationJson", "CreatedDate", "CreatedByUserId")
VALUES
  (4, 1, true, NULL, NOW(), 1),
  (4, 2, true, NULL, NOW(), 1),
  (4, 3, true, NULL, NOW(), 1),
  (4, 4, true, NULL, NOW(), 1),
  (4, 5, true, NULL, NOW(), 1),
  (4, 6, true, '{"showLogo": true, "showProfile": true}', NOW(), 1),
  (4, 7, true, '{"responseTimeHours": 24}', NOW(), 1);
