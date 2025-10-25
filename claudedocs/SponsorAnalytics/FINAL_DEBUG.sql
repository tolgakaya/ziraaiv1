-- =====================================================
-- Final Debug Query
-- =====================================================

-- Sponsor role'e atanmış TÜM analytics claim'leri göster (hem eski hem yeni)
SELECT oc."Id", oc."Name"
FROM "GroupClaims" gc
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 3
  AND (oc."Name" LIKE '%Analytics%' OR oc."Name" LIKE '%Statistics%')
ORDER BY oc."Id";
