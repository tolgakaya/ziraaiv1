-- =====================================================
-- Verify Code Transfer to Dealer
-- =====================================================
-- Purpose: Check if codes 932-936 were actually assigned to dealer 158
-- Date: 2025-10-26
-- =====================================================

-- Check transferred codes
SELECT
    sc."Id" as "CodeId",
    sc."Code",
    sc."SponsorId",
    sc."DealerId",
    sc."IsUsed",
    sc."IsActive",
    sc."DistributionDate",
    sc."TransferredAt",
    sc."ExpiryDate"
FROM public."SponsorshipCodes" sc
WHERE sc."Id" IN (932, 933, 934, 935, 936)
ORDER BY sc."Id";

-- Expected Result:
-- CodeId | Code     | SponsorId | DealerId | IsUsed | IsActive | DistributionDate | TransferredAt           | ExpiryDate
-- -------|----------|-----------|----------|--------|----------|------------------|-------------------------|------------
-- 932    | XXXX-... | 159       | 158      | false  | true     | NULL             | 2025-10-26 16:19:26     | ...
-- 933    | XXXX-... | 159       | 158      | false  | true     | NULL             | 2025-10-26 16:19:26     | ...
-- ...

-- If DealerId is NULL:
-- Transfer did not update database correctly

-- If DealerId is 158 but codes still empty in API:
-- Repository query has wrong filter (missing DealerId check)
