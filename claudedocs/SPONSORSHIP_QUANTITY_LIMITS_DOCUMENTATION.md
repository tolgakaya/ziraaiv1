# Sponsorship Purchase Quantity Limits Documentation

**Version:** 1.0.0
**Date:** 2025-10-11
**Status:** ✅ Implemented

---

## 📋 Overview

Sponsorship tiers now include configurable minimum and maximum purchase quantity limits to ensure proper package sizing and prevent both under-purchasing and over-purchasing scenarios.

---

## 🎯 Quantity Limits by Tier

| Tier | Min Quantity | Max Quantity | Recommended | Monthly Price |
|------|-------------|--------------|-------------|---------------|
| **Trial** | 1 | 10 | 5 | 0 TRY |
| **S (Small)** | 10 | 100 | 50 | 99.99 TRY |
| **M (Medium)** | 20 | 500 | 100 | 299.99 TRY |
| **L (Large)** | 50 | 2,000 | 500 | 599.99 TRY |
| **XL (Extra Large)** | 100 | 10,000 | 1,000 | 1,499.99 TRY |

---

## 🔧 Implementation Details

### Database Changes

**New Fields in `SubscriptionTiers` Table:**
```sql
MinPurchaseQuantity integer NOT NULL DEFAULT 10
MaxPurchaseQuantity integer NOT NULL DEFAULT 10000
RecommendedQuantity integer NOT NULL DEFAULT 100
```

**Constraints Added:**
- `CK_SubscriptionTiers_MinQuantity_Positive`: MinPurchaseQuantity > 0
- `CK_SubscriptionTiers_MaxQuantity_GreaterThanMin`: MaxPurchaseQuantity >= MinPurchaseQuantity
- `CK_SubscriptionTiers_RecommendedQuantity_InRange`: RecommendedQuantity between min and max

**Index Added:**
```sql
CREATE INDEX IX_SubscriptionTiers_Quantities
ON SubscriptionTiers (MinPurchaseQuantity, MaxPurchaseQuantity);
```

### Code Changes

**1. Entity Update** (`Entities/Concrete/SubscriptionTier.cs`)
```csharp
public int MinPurchaseQuantity { get; set; } = 10;
public int MaxPurchaseQuantity { get; set; } = 10000;
public int RecommendedQuantity { get; set; } = 100;
```

**2. DTO Update** (`Entities/Dtos/SubscriptionTierDto.cs`)
```csharp
public int MinPurchaseQuantity { get; set; }
public int MaxPurchaseQuantity { get; set; }
public int RecommendedQuantity { get; set; }
```

**3. Validation in SponsorshipService** (`Business/Services/Sponsorship/SponsorshipService.cs`)
```csharp
// Validate quantity limits
if (quantity < tier.MinPurchaseQuantity)
{
    return new ErrorDataResult<SponsorshipPurchaseResponseDto>(
        $"Quantity must be at least {tier.MinPurchaseQuantity} for {tier.DisplayName} tier");
}

if (quantity > tier.MaxPurchaseQuantity)
{
    return new ErrorDataResult<SponsorshipPurchaseResponseDto>(
        $"Quantity cannot exceed {tier.MaxPurchaseQuantity} for {tier.DisplayName} tier");
}
```

**4. Endpoint Update** (`WebAPI/Controllers/SubscriptionsController.cs`)

The `/api/v1/subscriptions/tiers` endpoint now returns quantity limits in the response:

```json
{
  "id": 2,
  "tierName": "S",
  "displayName": "Small",
  "minPurchaseQuantity": 10,
  "maxPurchaseQuantity": 100,
  "recommendedQuantity": 50,
  "monthlyPrice": 99.99,
  "currency": "TRY"
}
```

---

## 📱 Mobile Implementation Guide

### Displaying Quantity Limits

```dart
class TierSelectionWidget extends StatelessWidget {
  final SubscriptionTier tier;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Column(
        children: [
          Text(tier.displayName),
          Text('${tier.monthlyPrice} ${tier.currency}'),

          // Show quantity range
          Text(
            'Quantity: ${tier.minPurchaseQuantity} - ${tier.maxPurchaseQuantity}',
            style: TextStyle(color: Colors.grey),
          ),

          // Recommended badge
          Chip(
            label: Text('Recommended: ${tier.recommendedQuantity}'),
            backgroundColor: Colors.blue.shade100,
          ),
        ],
      ),
    );
  }
}
```

### Quantity Input Validation

```dart
class QuantityInput extends StatefulWidget {
  final SubscriptionTier tier;
  final Function(int) onQuantityChanged;

  @override
  _QuantityInputState createState() => _QuantityInputState();
}

class _QuantityInputState extends State<QuantityInput> {
  late TextEditingController _controller;
  String? _errorText;

  @override
  void initState() {
    super.initState();
    // Initialize with recommended quantity
    _controller = TextEditingController(
      text: widget.tier.recommendedQuantity.toString()
    );
  }

  void _validateQuantity(String value) {
    final quantity = int.tryParse(value);

    if (quantity == null) {
      setState(() => _errorText = 'Please enter a valid number');
      return;
    }

    if (quantity < widget.tier.minPurchaseQuantity) {
      setState(() => _errorText =
        'Minimum ${widget.tier.minPurchaseQuantity} required');
      return;
    }

    if (quantity > widget.tier.maxPurchaseQuantity) {
      setState(() => _errorText =
        'Maximum ${widget.tier.maxPurchaseQuantity} allowed');
      return;
    }

    setState(() => _errorText = null);
    widget.onQuantityChanged(quantity);
  }

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: _controller,
      keyboardType: TextInputType.number,
      decoration: InputDecoration(
        labelText: 'Quantity',
        hintText: 'Enter quantity (${widget.tier.minPurchaseQuantity}-${widget.tier.maxPurchaseQuantity})',
        errorText: _errorText,
        helperText: 'Recommended: ${widget.tier.recommendedQuantity}',
      ),
      onChanged: _validateQuantity,
    );
  }
}
```

### API Request Example

```dart
Future<void> purchaseBulkSponsorship({
  required int tierId,
  required int quantity,
}) async {
  // Validate before API call
  final tier = await getTierById(tierId);

  if (quantity < tier.minPurchaseQuantity ||
      quantity > tier.maxPurchaseQuantity) {
    throw ValidationException(
      'Quantity must be between ${tier.minPurchaseQuantity} and ${tier.maxPurchaseQuantity}'
    );
  }

  final response = await http.post(
    Uri.parse('$baseUrl/api/v1/sponsorship/purchase-bulk'),
    headers: {'Authorization': 'Bearer $accessToken'},
    body: jsonEncode({
      'subscriptionTierId': tierId,
      'quantity': quantity,
      'totalAmount': quantity * tier.monthlyPrice,
      'paymentMethod': 'CreditCard',
      'paymentReference': 'PAY-${DateTime.now().millisecondsSinceEpoch}',
    }),
  );

  if (response.statusCode != 200) {
    final error = jsonDecode(response.body);
    throw ApiException(error['message']);
  }
}
```

---

## 🧪 Testing Scenarios

### Valid Purchases

```http
POST /api/v1/sponsorship/purchase-bulk
Authorization: Bearer {token}
Content-Type: application/json

{
  "subscriptionTierId": 2,  // S tier
  "quantity": 50,            // ✅ Valid: 10 <= 50 <= 100
  "totalAmount": 4999.50,
  "paymentMethod": "CreditCard",
  "paymentReference": "PAY-123"
}
```

**Expected Response:** `200 OK` with generated codes

### Invalid Purchases - Below Minimum

```http
POST /api/v1/sponsorship/purchase-bulk

{
  "subscriptionTierId": 2,  // S tier
  "quantity": 5,             // ❌ Invalid: 5 < 10 (min)
  "totalAmount": 499.95,
  "paymentMethod": "CreditCard",
  "paymentReference": "PAY-124"
}
```

**Expected Response:** `400 Bad Request`
```json
{
  "success": false,
  "message": "Quantity must be at least 10 for Small tier"
}
```

### Invalid Purchases - Above Maximum

```http
POST /api/v1/sponsorship/purchase-bulk

{
  "subscriptionTierId": 2,  // S tier
  "quantity": 150,           // ❌ Invalid: 150 > 100 (max)
  "totalAmount": 14998.50,
  "paymentMethod": "CreditCard",
  "paymentReference": "PAY-125"
}
```

**Expected Response:** `400 Bad Request`
```json
{
  "success": false,
  "message": "Quantity cannot exceed 100 for Small tier"
}
```

---

## 📝 Migration Guide

### Step 1: Apply SQL Migration

Run the migration script located at: `claudedocs/AddQuantityLimitsToSubscriptionTiers.sql`

```bash
psql -U ziraai -d ziraai_db -f claudedocs/AddQuantityLimitsToSubscriptionTiers.sql
```

### Step 2: Verify Database Changes

```sql
SELECT
    "Id",
    "TierName",
    "DisplayName",
    "MinPurchaseQuantity",
    "MaxPurchaseQuantity",
    "RecommendedQuantity",
    "MonthlyPrice"
FROM "SubscriptionTiers"
ORDER BY "DisplayOrder";
```

Expected output should show all tiers with their quantity limits.

### Step 3: Update Mobile App

1. Update `SubscriptionTier` model to include new fields
2. Update tier selection UI to display quantity limits
3. Add client-side validation for quantity input
4. Update purchase flow to use recommended quantity as default

### Step 4: Test

1. Call `/api/v1/subscriptions/tiers` - verify new fields are returned
2. Attempt purchase below minimum - should fail with error message
3. Attempt purchase above maximum - should fail with error message
4. Attempt valid purchase within range - should succeed

---

## 🔄 Rollback Instructions

If you need to rollback these changes:

```sql
-- Drop constraints
ALTER TABLE "SubscriptionTiers" DROP CONSTRAINT IF EXISTS "CK_SubscriptionTiers_MinQuantity_Positive";
ALTER TABLE "SubscriptionTiers" DROP CONSTRAINT IF EXISTS "CK_SubscriptionTiers_MaxQuantity_GreaterThanMin";
ALTER TABLE "SubscriptionTiers" DROP CONSTRAINT IF EXISTS "CK_SubscriptionTiers_RecommendedQuantity_InRange";

-- Drop index
DROP INDEX IF EXISTS "IX_SubscriptionTiers_Quantities";

-- Drop columns
ALTER TABLE "SubscriptionTiers"
DROP COLUMN IF EXISTS "MinPurchaseQuantity",
DROP COLUMN IF EXISTS "MaxPurchaseQuantity",
DROP COLUMN IF EXISTS "RecommendedQuantity";
```

---

## 🤝 Support

**Implementation Location:**
- Entity: `Entities/Concrete/SubscriptionTier.cs`
- DTO: `Entities/Dtos/SubscriptionTierDto.cs`
- Validation: `Business/Services/Sponsorship/SponsorshipService.cs:45-58`
- Endpoint: `WebAPI/Controllers/SubscriptionsController.cs:42-68`
- Migration: `claudedocs/AddQuantityLimitsToSubscriptionTiers.sql`

**For Issues:**
- Verify migration was applied: Check table columns exist
- Check validation is working: Test with invalid quantities
- Verify endpoint returns limits: Check API response includes new fields

---

**End of Documentation**

*Last Updated: 2025-10-11 by Claude Code*
*Document Version: 1.0.0*
*Feature Status: ✅ Production Ready*
