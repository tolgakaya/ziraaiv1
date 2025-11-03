# Bulk Dealer Invitation - Excel File Formats

## Overview
The bulk dealer invitation system supports two modes for Excel uploads:

1. **Tier-Specific Mode**: Specify PackageTier for each dealer (per-tier validation)
2. **Auto-Allocation Mode**: No tier specification - system automatically allocates codes from available tiers

## Excel Column Requirements

### Required Columns (All Modes)
| Column Name | Description | Example |
|------------|-------------|---------|
| Email | Dealer email address | dealer@example.com |
| Phone | Dealer phone number (Turkish format) | 905551234567 |
| CodeCount | Number of codes to allocate | 10 |

### Optional Columns
| Column Name | Description | Example |
|------------|-------------|---------|
| DealerName | Display name for dealer | Ahmet Tarım Ltd |
| PackageTier | Subscription tier (S, M, L, XL) | M |

## Mode 1: Auto-Allocation Mode (Recommended)

**When to use:** You want the system to automatically select codes from any available tier

**Excel Structure:**
```
Email               | Phone         | DealerName        | CodeCount
dealer1@example.com | 905551234567 | Ahmet Tarım       | 10
dealer2@example.com | 905551234568 | Mehmet Gıda       | 15
dealer3@example.com | 905551234569 | Ayşe Bahçecilik   | 20
```

**Behavior:**
- System automatically allocates codes from ANY available tier
- Codes are selected based on expiry date (expiring soon first)
- When one tier runs out, automatically moves to next tier
- Same behavior as single dealer invitations
- **Validation:** Checks total available codes across all tiers

**Example:**
If sponsor has:
- S tier: 5 codes
- M tier: 20 codes
- L tier: 30 codes

For a dealer requesting 10 codes:
- System will select 5 from S tier (expiring soonest)
- Then 5 from M tier
- Result: Dealer gets 10 codes across multiple tiers

## Mode 2: Tier-Specific Mode

**When to use:** You want to control which tier codes are allocated from

**Excel Structure:**
```
Email               | Phone         | DealerName        | PackageTier | CodeCount
dealer1@example.com | 905551234567 | Ahmet Tarım       | M           | 10
dealer2@example.com | 905551234568 | Mehmet Gıda       | L           | 15
dealer3@example.com | 905551234569 | Ayşe Bahçecilik   | S           | 5
```

**Behavior:**
- System ONLY allocates codes from specified tier
- Each dealer can have different tier
- **Validation:** Checks per-tier code availability
- **IMPORTANT:** ALL rows must specify PackageTier (mixed mode not supported)

**Valid Tier Values:**
- `S` - Small tier
- `M` - Medium tier
- `L` - Large tier
- `XL` - Extra Large tier

(Case insensitive - `s`, `S`, `m`, `M` all work)

## Phone Number Format

### Valid Formats (Turkish)
```
+905551234567  (international format)
 905551234567  (without +)
 05551234567   (local format)
```

All spaces, dashes, parentheses are automatically removed during validation.

## Email Validation
- Must be valid email format
- Must not be duplicate within the file
- System checks for existing dealer accounts

## File Constraints
- **Maximum file size:** 5 MB
- **Maximum rows:** 2000 dealers
- **Supported formats:** .xlsx, .xls
- **Header row:** Row 1 must contain column names
- **Data rows:** Row 2 onwards

## Header-Based Parsing

The system uses **flexible header-based parsing**:
- Columns can be in ANY order
- Only specified columns are required
- Column names are case-insensitive
- Extra columns are ignored

### Example - Valid Custom Order:
```
Phone         | DealerName        | Email               | CodeCount
905551234567 | Ahmet Tarım       | dealer1@example.com | 10
```

## Code Availability Validation

### Auto-Allocation Mode Validation
```
Total Required Codes = SUM(all CodeCount values)
Total Available Codes = Count of all available codes across all tiers

IF Total Available Codes >= Total Required Codes:
  ✅ Validation passes
ELSE:
  ❌ Error: "Yetersiz kod. Gerekli: X, Mevcut: Y (tüm tier'lar)"
```

### Tier-Specific Mode Validation
```
FOR EACH tier in Excel:
  Required Codes (tier) = SUM(CodeCount WHERE PackageTier = tier)
  Available Codes (tier) = Count of available codes for that tier
  
  IF Available Codes < Required Codes:
    ❌ Error: "{tier} tier: {available} kod mevcut, {required} kod gerekli"
```

## Common Validation Errors

### File Validation Errors
```
❌ "Dosya yüklenmedi" 
   → No file uploaded

❌ "Dosya boyutu çok büyük. Maksimum: 5 MB"
   → File exceeds size limit

❌ "Geçersiz dosya formatı. Sadece .xlsx ve .xls desteklenir"
   → Wrong file format
```

### Excel Structure Errors
```
❌ "Excel'de 'Email' sütunu zorunludur"
   → Missing Email column header

❌ "Excel'de 'Phone' sütunu zorunludur"
   → Missing Phone column header

❌ "Excel'de 'CodeCount' sütunu zorunludur"
   → Missing CodeCount column header

❌ "Excel dosyasında geçerli satır bulunamadı"
   → No valid data rows (all empty)

❌ "Maksimum 2000 dealer kaydı yüklenebilir. Dosyanızda {count} kayıt var"
   → Too many rows
```

### Row Validation Errors
```
❌ "Satır 5: Geçersiz email - abc123"
   → Invalid email format

❌ "Satır 7: Geçersiz telefon - 123"
   → Invalid phone format

❌ "Satır 10: Duplicate email - dealer@example.com"
   → Duplicate email within file

❌ "Satır 12: CodeCount geçersiz veya boş - 'abc'"
   → Invalid or missing code count

❌ "Satır 15: PackageTier geçersiz - 'Z'. S, M, L veya XL olmalı"
   → Invalid tier value (when using tier-specific mode)
```

### Code Availability Errors
```
❌ "Yetersiz kod. Gerekli: 100, Mevcut: 50 (tüm tier'lar)"
   → Not enough codes in auto-allocation mode

❌ "M tier: 10 kod mevcut, 20 kod gerekli (Eksik: 10)"
   → Insufficient codes for specific tier

❌ "Karma mod desteklenmiyor. Tüm satırlar tier belirtmeli veya hiçbiri belirtmemeli. 5 satırda tier eksik"
   → Mixed mode (some rows with tier, some without)
```

## API Request Example

### Endpoint
```
POST /api/v1/sponsorship/dealer/bulk-invite
```

### Request (multipart/form-data)
```
SponsorId: 123
ExcelFile: [file upload]
InvitationType: "Invite"  // or "AutoCreate"
SendSms: true
```

**Note:** No `DefaultTier` or `DefaultCodeCount` parameters needed - the system uses Excel data only.

### Response
```json
{
  "success": true,
  "data": {
    "jobId": 45,
    "totalDealers": 50,
    "status": "Processing",
    "createdDate": "2025-01-25T10:00:00Z",
    "statusCheckUrl": "/api/v1/sponsorship/dealer/bulk-status/45"
  },
  "message": "Toplu davet işlemi başlatıldı. 50 dealer kuyruğa eklendi."
}
```

## Best Practices

### 1. Use Auto-Allocation Mode
✅ **Recommended** for most use cases:
- Simplifies Excel structure
- Maximizes code utilization across tiers
- Prevents tier-specific shortages
- Same behavior as single dealer flow

### 2. Use Tier-Specific Mode Only When
- You have business requirements to assign specific tiers
- You want to control tier distribution precisely
- You need to preserve specific tier features for dealers

### 3. Prepare Excel Files
- Start with template (download from system if available)
- Verify phone numbers are in correct format
- Check for duplicate emails before upload
- Keep row count under 2000 for optimal processing

### 4. Monitor Progress
- Use StatusCheckUrl to track processing
- Subscribe to SignalR notifications for real-time updates
- Check final status: Completed, PartialSuccess, or Failed

## Example Excel Files

### Example 1: Auto-Allocation (Simple)
```csv
Email,Phone,DealerName,CodeCount
dealer1@test.com,905551234567,Test Dealer 1,10
dealer2@test.com,905551234568,Test Dealer 2,15
dealer3@test.com,905551234569,Test Dealer 3,20
```

### Example 2: Tier-Specific (Advanced)
```csv
Email,Phone,DealerName,PackageTier,CodeCount
premium@test.com,905551234567,Premium Dealer,XL,50
standard@test.com,905551234568,Standard Dealer,M,20
basic@test.com,905551234569,Basic Dealer,S,5
```

## Technical Notes

### Code Selection Algorithm (Auto-Allocation)
```csharp
// Pseudo-code for auto-allocation
SELECT TOP(CodeCount) FROM SponsorshipCodes
WHERE 
  SponsorId = @sponsorId AND
  IsUsed = false AND
  DealerId = null AND
  ReservedForInvitationId = null AND
  ExpiryDate > NOW()
ORDER BY 
  ExpiryDate ASC,    // Expiring soon first
  CreatedDate ASC    // Oldest first (FIFO)
```

### Processing Flow
1. Upload Excel → Validation (file, structure, rows, codes)
2. Create BulkInvitationJob entity → Save to database
3. Publish N messages to RabbitMQ (one per dealer)
4. Worker service consumes messages → Process in parallel
5. SignalR notifications → Real-time progress updates
6. Final status → Completed/PartialSuccess/Failed
