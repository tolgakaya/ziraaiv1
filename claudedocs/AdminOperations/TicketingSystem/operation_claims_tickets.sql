-- Operation Claims Script: Ticketing System
-- Date: 2024
-- Description: Adds operation claims for ticketing system handlers
-- Last existing claim ID: 167 (from operation_claims.csv)

-- =============================================
-- Insert Operation Claims
-- =============================================

-- User Claims (Farmer and Sponsor)
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description") VALUES
(168, 'CreateTicketCommand', 'Destek Talebi Oluştur', 'Kullanıcı destek talebi oluşturabilir'),
(169, 'GetMyTicketsQuery', 'Kendi Taleplerimi Listele', 'Kullanıcı kendi destek taleplerini görebilir'),
(170, 'GetTicketDetailQuery', 'Talep Detayı Görüntüle', 'Kullanıcı kendi destek talebi detayını görebilir'),
(171, 'AddTicketMessageCommand', 'Talebe Mesaj Ekle', 'Kullanıcı kendi talebine mesaj ekleyebilir'),
(172, 'CloseTicketCommand', 'Talebi Kapat', 'Kullanıcı kendi talebini kapatabilir'),
(173, 'RateTicketResolutionCommand', 'Çözümü Puanla', 'Kullanıcı çözümü puanlayabilir');

-- Admin Claims
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description") VALUES
(174, 'GetAllTicketsAsAdminQuery', 'Tüm Talepleri Listele', 'Admin tüm destek taleplerini görebilir'),
(175, 'GetTicketDetailAsAdminQuery', 'Talep Detayı (Admin)', 'Admin herhangi bir talep detayını görebilir'),
(176, 'GetTicketStatsQuery', 'Talep İstatistikleri', 'Admin talep istatistiklerini görebilir'),
(177, 'AssignTicketCommand', 'Talep Ata', 'Admin talebi birine atayabilir'),
(178, 'AdminRespondTicketCommand', 'Talebe Yanıt Ver', 'Admin talebe yanıt verebilir'),
(179, 'UpdateTicketStatusCommand', 'Talep Durumunu Güncelle', 'Admin talep durumunu güncelleyebilir');

-- =============================================
-- Assign Claims to Groups
-- =============================================

-- GroupId: 1 = Admin, 2 = Farmer, 3 = Sponsor

-- Farmer Claims (GroupId = 2)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId") VALUES
(2, 168),  -- CreateTicketCommand
(2, 169),  -- GetMyTicketsQuery
(2, 170),  -- GetTicketDetailQuery
(2, 171),  -- AddTicketMessageCommand
(2, 172),  -- CloseTicketCommand
(2, 173);  -- RateTicketResolutionCommand

-- Sponsor Claims (GroupId = 3)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId") VALUES
(3, 168),  -- CreateTicketCommand
(3, 169),  -- GetMyTicketsQuery
(3, 170),  -- GetTicketDetailQuery
(3, 171),  -- AddTicketMessageCommand
(3, 172),  -- CloseTicketCommand
(3, 173);  -- RateTicketResolutionCommand

-- Admin Claims (GroupId = 1)
-- Admin gets all user claims + admin-specific claims
INSERT INTO "GroupClaims" ("GroupId", "ClaimId") VALUES
(1, 168),  -- CreateTicketCommand
(1, 169),  -- GetMyTicketsQuery
(1, 170),  -- GetTicketDetailQuery
(1, 171),  -- AddTicketMessageCommand
(1, 172),  -- CloseTicketCommand
(1, 173),  -- RateTicketResolutionCommand
(1, 174),  -- GetAllTicketsAsAdminQuery
(1, 175),  -- GetTicketDetailAsAdminQuery
(1, 176),  -- GetTicketStatsQuery
(1, 177),  -- AssignTicketCommand
(1, 178),  -- AdminRespondTicketCommand
(1, 179);  -- UpdateTicketStatusCommand

-- =============================================
-- Verification Queries
-- =============================================

-- Verify claims were inserted:
-- SELECT * FROM "OperationClaims" WHERE "Id" >= 168 ORDER BY "Id";

-- Verify group claims were assigned:
-- SELECT g."GroupId", oc."Name"
-- FROM "GroupClaims" g
-- JOIN "OperationClaims" oc ON g."ClaimId" = oc."Id"
-- WHERE oc."Id" >= 168
-- ORDER BY g."GroupId", oc."Id";

-- =============================================
-- Rollback Script (if needed)
-- =============================================

-- DELETE FROM "GroupClaims" WHERE "ClaimId" >= 168 AND "ClaimId" <= 179;
-- DELETE FROM "OperationClaims" WHERE "Id" >= 168 AND "Id" <= 179;
