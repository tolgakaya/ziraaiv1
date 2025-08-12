-- Add AI Image Optimization Configuration Settings
-- These settings optimize images specifically for AI/OpenAI processing to avoid token limits

-- AI Image Max Size (100KB default for minimal token usage)
INSERT INTO "Configurations" ("Key", "Value", "Description", "Category", "DataType", "IsActive", "CreatedDate", "Status")
VALUES 
    ('AI_IMAGE_MAX_SIZE_MB', '0.1', 'Maximum image size in MB for AI processing (0.1MB = 100KB)', 'AI', 'Decimal', true, NOW(), true)
ON CONFLICT ("Key") DO UPDATE 
SET "Value" = EXCLUDED."Value", 
    "Description" = EXCLUDED."Description",
    "UpdatedDate" = NOW();

-- AI Image Optimization Enable
INSERT INTO "Configurations" ("Key", "Value", "Description", "Category", "DataType", "IsActive", "CreatedDate", "Status")
VALUES 
    ('AI_IMAGE_OPTIMIZATION', 'true', 'Enable aggressive image optimization for AI processing', 'AI', 'Boolean', true, NOW(), true)
ON CONFLICT ("Key") DO UPDATE 
SET "Value" = EXCLUDED."Value", 
    "Description" = EXCLUDED."Description",
    "UpdatedDate" = NOW();

-- AI Image Max Width
INSERT INTO "Configurations" ("Key", "Value", "Description", "Category", "DataType", "IsActive", "CreatedDate", "Status")
VALUES 
    ('AI_IMAGE_MAX_WIDTH', '800', 'Maximum image width in pixels for AI processing', 'AI', 'Integer', true, NOW(), true)
ON CONFLICT ("Key") DO UPDATE 
SET "Value" = EXCLUDED."Value", 
    "Description" = EXCLUDED."Description",
    "UpdatedDate" = NOW();

-- AI Image Max Height
INSERT INTO "Configurations" ("Key", "Value", "Description", "Category", "DataType", "IsActive", "CreatedDate", "Status")
VALUES 
    ('AI_IMAGE_MAX_HEIGHT', '600', 'Maximum image height in pixels for AI processing', 'AI', 'Integer', true, NOW(), true)
ON CONFLICT ("Key") DO UPDATE 
SET "Value" = EXCLUDED."Value", 
    "Description" = EXCLUDED."Description",
    "UpdatedDate" = NOW();

-- AI Image Quality
INSERT INTO "Configurations" ("Key", "Value", "Description", "Category", "DataType", "IsActive", "CreatedDate", "Status")
VALUES 
    ('AI_IMAGE_QUALITY', '70', 'JPEG quality for AI processing (1-100)', 'AI', 'Integer', true, NOW(), true)
ON CONFLICT ("Key") DO UPDATE 
SET "Value" = EXCLUDED."Value", 
    "Description" = EXCLUDED."Description",
    "UpdatedDate" = NOW();

-- API Base URL for image URL generation
INSERT INTO "Configurations" ("Key", "Value", "Description", "Category", "DataType", "IsActive", "CreatedDate", "Status")
VALUES 
    ('API_BASE_URL', 'https://localhost:5001', 'Base URL for generating image URLs', 'Application', 'String', true, NOW(), true)
ON CONFLICT ("Key") DO UPDATE 
SET "Value" = EXCLUDED."Value", 
    "Description" = EXCLUDED."Description",
    "UpdatedDate" = NOW();

-- Use URL for N8N
INSERT INTO "Configurations" ("Key", "Value", "Description", "Category", "DataType", "IsActive", "CreatedDate", "Status")
VALUES 
    ('N8N_USE_IMAGE_URL', 'true', 'Send image URLs to N8N instead of base64 to avoid token limits', 'N8N', 'Boolean', true, NOW(), true)
ON CONFLICT ("Key") DO UPDATE 
SET "Value" = EXCLUDED."Value", 
    "Description" = EXCLUDED."Description",
    "UpdatedDate" = NOW();

-- Query to verify configurations
SELECT "Key", "Value", "Description", "Category" 
FROM "Configurations" 
WHERE "Category" IN ('AI', 'N8N') 
ORDER BY "Category", "Key";