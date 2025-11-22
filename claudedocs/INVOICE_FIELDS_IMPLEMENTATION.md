# Invoice Fields Implementation Guide

**Date:** 2025-11-22  
**Change Type:** Backend + Mobile App Update  
**Priority:** 🟡 MEDIUM - Required for proper invoice record keeping

---

## Problem Statement

During the sponsor bulk purchase payment flow, three invoice-related fields were being saved correctly in **mock payment** but were **NULL** in the **real payment flow** (iyzico):

- `CompanyName` - Firma ismi
- `TaxNumber` - Vergi numarası
- `InvoiceAddress` - Fatura adresi

**Database Evidence:**

```sql
SELECT PurchaseId, CompanyName, TaxNumber, InvoiceAddress, PaymentMethod
FROM SponsorshipPurchase
WHERE SponsorId = 189;

-- Results showed:
-- Mock Payment: ✅ CompanyName, TaxNumber, InvoiceAddress populated
-- Real Payment: ❌ CompanyName, TaxNumber, InvoiceAddress = NULL
```

---

## Root Cause Analysis

### Code Comparison

#### ✅ Mock Payment (Correct)
**File:** `Business/Services/Sponsorship/SponsorshipService.cs` (Lines 114-116)

```csharp
var purchase = new SponsorshipPurchase
{
    // ... other fields ...
    CompanyName = finalCompanyName,        // ✅ Present
    InvoiceAddress = finalInvoiceAddress,  // ✅ Present
    TaxNumber = finalTaxNumber,            // ✅ Present
    // ...
};
```

#### ❌ Real Payment (Missing Fields)
**File:** `Business/Services/Payment/IyzicoPaymentService.cs` (Lines 713-733)

```csharp
var purchase = new SponsorshipPurchase
{
    SponsorId = transaction.UserId,
    SubscriptionTierId = flowData.SubscriptionTierId,
    Quantity = flowData.Quantity,
    // ... other fields ...
    // ❌ MISSING: CompanyName
    // ❌ MISSING: TaxNumber
    // ❌ MISSING: InvoiceAddress
    CodePrefix = "AGRI",
    // ...
};
```

#### ✅ Admin Purchase (Correct)
**File:** `Business/Handlers/AdminSponsorship/Commands/CreatePurchaseOnBehalfOfCommand.cs` (Lines 116-118)

```csharp
var purchase = new SponsorshipPurchase
{
    // ... other fields ...
    CompanyName = request.CompanyName,      // ✅ From request
    TaxNumber = request.TaxNumber,          // ✅ From request
    InvoiceAddress = request.InvoiceAddress, // ✅ From request
    // ...
};
```

### Why This Happened

The `SponsorBulkPurchaseFlowData` DTO (used for payment initialization) only contained:
- `SubscriptionTierId`
- `Quantity`

**It was missing:**
- `CompanyName`
- `TaxNumber`
- `InvoiceAddress`

These fields should be **collected from the user during the purchase flow** (not from User table), and sent to the backend in the payment initialization request.

---

## Solution Implementation

### Backend Changes (✅ COMPLETED)

#### 1. Updated Flow Data DTO
**File:** `Entities/Dtos/Payment/PaymentInitializeRequestDto.cs`

**BEFORE:**
```csharp
public class SponsorBulkPurchaseFlowData
{
    [Required]
    public int SubscriptionTierId { get; set; }

    [Required]
    [Range(1, 10000)]
    public int Quantity { get; set; }
}
```

**AFTER:**
```csharp
public class SponsorBulkPurchaseFlowData
{
    [Required]
    public int SubscriptionTierId { get; set; }

    [Required]
    [Range(1, 10000)]
    public int Quantity { get; set; }

    /// <summary>
    /// Company name for invoice (optional for personal purchases)
    /// </summary>
    public string CompanyName { get; set; }

    /// <summary>
    /// Tax number for invoice (optional for personal purchases)
    /// </summary>
    public string TaxNumber { get; set; }

    /// <summary>
    /// Invoice address (optional for personal purchases)
    /// </summary>
    public string InvoiceAddress { get; set; }
}
```

**Key Points:**
- ✅ Fields are **optional** (not `[Required]`) - allows backward compatibility
- ✅ Personal purchases can leave these blank
- ✅ Corporate purchases should fill these fields

#### 2. Updated Payment Processing
**File:** `Business/Services/Payment/IyzicoPaymentService.cs` (Line 713+)

**Added three lines:**
```csharp
var purchase = new SponsorshipPurchase
{
    SponsorId = transaction.UserId,
    SubscriptionTierId = flowData.SubscriptionTierId,
    Quantity = flowData.Quantity,
    UnitPrice = tier.MonthlyPrice,
    TotalAmount = transaction.Amount,
    Currency = transaction.Currency,
    PurchaseDate = DateTime.Now,
    PaymentMethod = "CreditCard",
    PaymentReference = transaction.IyzicoPaymentId,
    PaymentStatus = "Completed",
    PaymentCompletedDate = transaction.CompletedAt,
    PaymentTransactionId = transaction.Id,
    
    // ✅ NEW: Invoice fields from flow data
    CompanyName = flowData.CompanyName,
    TaxNumber = flowData.TaxNumber,
    InvoiceAddress = flowData.InvoiceAddress,
    
    CodePrefix = "AGRI",
    ValidityDays = 30,
    Status = "Active",
    CreatedDate = DateTime.Now,
    CodesGenerated = 0,
    CodesUsed = 0
};
```

---

## Mobile App Changes Required

### 1. Payment Initialization Screen

**Current Flow:**
```
1. User selects tier (S, M, L, XL)
2. User enters quantity (1-10000)
3. User clicks "Confirm Order"
4. App calls /api/v1/payments/initialize
```

**Required Addition:**
```
1. User selects tier (S, M, L, XL)
2. User enters quantity (1-10000)
3. ✅ NEW: User fills invoice information (optional)
   - Company Name (Firma İsmi)
   - Tax Number (Vergi Numarası)
   - Invoice Address (Fatura Adresi)
4. User clicks "Confirm Order"
5. App calls /api/v1/payments/initialize with invoice data
```

### 2. Updated Request Payload

#### Current Request (Missing Invoice Fields)
```dart
POST /api/v1/payments/initialize

{
  "flowType": "SponsorBulkPurchase",
  "flowData": {
    "subscriptionTierId": 1,
    "quantity": 50
  },
  "currency": "TRY"
}
```

#### ✅ New Request (With Invoice Fields)
```dart
POST /api/v1/payments/initialize

{
  "flowType": "SponsorBulkPurchase",
  "flowData": {
    "subscriptionTierId": 1,
    "quantity": 50,
    "companyName": "Ziraai Teknoloji A.Ş.",      // NEW
    "taxNumber": "1234567890",                    // NEW
    "invoiceAddress": "İstanbul, Türkiye"        // NEW
  },
  "currency": "TRY"
}
```

### 3. Mobile Implementation Example

**Dart Model Update:**
```dart
// File: lib/features/payment/data/models/payment_models.dart

class SponsorBulkPurchaseFlowData {
  final int subscriptionTierId;
  final int quantity;
  final String? companyName;      // NEW - nullable
  final String? taxNumber;        // NEW - nullable
  final String? invoiceAddress;   // NEW - nullable

  const SponsorBulkPurchaseFlowData({
    required this.subscriptionTierId,
    required this.quantity,
    this.companyName,
    this.taxNumber,
    this.invoiceAddress,
  });

  Map<String, dynamic> toJson() {
    return {
      'subscriptionTierId': subscriptionTierId,
      'quantity': quantity,
      if (companyName != null && companyName!.isNotEmpty) 
        'companyName': companyName,
      if (taxNumber != null && taxNumber!.isNotEmpty) 
        'taxNumber': taxNumber,
      if (invoiceAddress != null && invoiceAddress!.isNotEmpty) 
        'invoiceAddress': invoiceAddress,
    };
  }
}
```

**UI Implementation Example:**
```dart
// File: lib/features/payment/presentation/screens/sponsor_payment_screen.dart

class _SponsorPaymentScreenState extends State<SponsorPaymentScreen> {
  final _quantityController = TextEditingController();
  
  // NEW: Invoice form controllers
  final _companyNameController = TextEditingController();
  final _taxNumberController = TextEditingController();
  final _invoiceAddressController = TextEditingController();
  
  bool _needsInvoice = false; // Toggle for corporate purchase

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(
        children: [
          // Tier selection (existing)
          _buildTierSelector(),
          
          // Quantity input (existing)
          _buildQuantityInput(),
          
          // NEW: Invoice toggle
          SwitchListTile(
            title: Text('Kurumsal Fatura İstiyorum'),
            subtitle: Text('Şirket adına fatura kesmek için'),
            value: _needsInvoice,
            onChanged: (value) {
              setState(() {
                _needsInvoice = value;
              });
            },
          ),
          
          // NEW: Invoice form (shown only if toggle is ON)
          if (_needsInvoice) ...[
            Padding(
              padding: EdgeInsets.all(16),
              child: Column(
                children: [
                  TextField(
                    controller: _companyNameController,
                    decoration: InputDecoration(
                      labelText: 'Firma İsmi',
                      hintText: 'Örn: Ziraai Teknoloji A.Ş.',
                    ),
                  ),
                  SizedBox(height: 16),
                  TextField(
                    controller: _taxNumberController,
                    decoration: InputDecoration(
                      labelText: 'Vergi Numarası',
                      hintText: '10 haneli vergi numarası',
                    ),
                    keyboardType: TextInputType.number,
                    maxLength: 10,
                  ),
                  SizedBox(height: 16),
                  TextField(
                    controller: _invoiceAddressController,
                    decoration: InputDecoration(
                      labelText: 'Fatura Adresi',
                      hintText: 'Şehir, Ülke',
                    ),
                    maxLines: 2,
                  ),
                ],
              ),
            ),
          ],
          
          // Confirm button (existing, updated to include invoice data)
          ElevatedButton(
            onPressed: _initializePayment,
            child: Text('Ödemeye Geç'),
          ),
        ],
      ),
    );
  }

  Future<void> _initializePayment() async {
    final flowData = SponsorBulkPurchaseFlowData(
      subscriptionTierId: _selectedTierId,
      quantity: int.parse(_quantityController.text),
      // NEW: Include invoice data if toggle is ON
      companyName: _needsInvoice ? _companyNameController.text : null,
      taxNumber: _needsInvoice ? _taxNumberController.text : null,
      invoiceAddress: _needsInvoice ? _invoiceAddressController.text : null,
    );

    final request = PaymentInitializeRequest(
      flowType: 'SponsorBulkPurchase',
      flowData: flowData.toJson(),
      currency: 'TRY',
    );

    final result = await _paymentService.initializePayment(request);
    // Handle result...
  }
}
```

---

## Complete Payment Flow with Invoice Fields

### Step-by-Step Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ MOBILE APP - Sponsor Purchase Screen                           │
├─────────────────────────────────────────────────────────────────┤
│ 1. User selects tier: S (99.99 TRY)                           │
│ 2. User enters quantity: 50                                    │
│ 3. User toggles "Kurumsal Fatura İstiyorum": ON               │
│ 4. User fills invoice form:                                    │
│    - Firma İsmi: "Ziraai Teknoloji A.Ş."                      │
│    - Vergi No: "1234567890"                                    │
│    - Fatura Adresi: "İstanbul, Türkiye"                       │
│ 5. User clicks "Ödemeye Geç"                                   │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ REQUEST: POST /api/v1/payments/initialize                      │
├─────────────────────────────────────────────────────────────────┤
│ Headers:                                                        │
│   Authorization: Bearer {access_token}                          │
│   Content-Type: application/json                               │
│                                                                 │
│ Body:                                                           │
│ {                                                               │
│   "flowType": "SponsorBulkPurchase",                           │
│   "flowData": {                                                 │
│     "subscriptionTierId": 1,                                    │
│     "quantity": 50,                                             │
│     "companyName": "Ziraai Teknoloji A.Ş.",                    │
│     "taxNumber": "1234567890",                                  │
│     "invoiceAddress": "İstanbul, Türkiye"                      │
│   },                                                            │
│   "currency": "TRY"                                             │
│ }                                                               │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ BACKEND - IyzicoPaymentService.InitializePaymentAsync          │
├─────────────────────────────────────────────────────────────────┤
│ 1. Validates tier and calculates amount: 4999.50 TRY          │
│ 2. Creates PaymentTransaction record                           │
│ 3. Serializes flowData to JSON (includes invoice fields)       │
│ 4. Calls iyzico API with payment details                       │
│ 5. Returns paymentPageUrl to mobile                            │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ RESPONSE: Payment Initialized                                   │
├─────────────────────────────────────────────────────────────────┤
│ {                                                               │
│   "success": true,                                              │
│   "data": {                                                     │
│     "paymentToken": "abc-123-xyz",                             │
│     "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=...",│
│     "transactionId": 19,                                        │
│     "amount": 4999.50,                                          │
│     "currency": "TRY"                                           │
│   }                                                             │
│ }                                                               │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ MOBILE APP - Payment WebView                                   │
├─────────────────────────────────────────────────────────────────┤
│ 1. Opens WebView with paymentPageUrl                           │
│ 2. User fills card details in iyzico form                      │
│ 3. User clicks "Ödemeyi Tamamla"                               │
│ 4. 3D Secure authentication (SMS code: 123456)                 │
│ 5. iyzico processes payment                                    │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ CALLBACK: POST /api/v1/payments/callback                       │
├─────────────────────────────────────────────────────────────────┤
│ From: iyzico servers                                            │
│ Body: { "token": "abc-123-xyz", "status": "success" }          │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ BACKEND - ProcessSponsorBulkPurchaseAsync                      │
├─────────────────────────────────────────────────────────────────┤
│ 1. Retrieves PaymentTransaction by token                       │
│ 2. Deserializes flowData JSON to SponsorBulkPurchaseFlowData   │
│ 3. Creates SponsorshipPurchase record:                         │
│    {                                                            │
│      SponsorId: 189,                                            │
│      SubscriptionTierId: 1,                                     │
│      Quantity: 50,                                              │
│      TotalAmount: 4999.50,                                      │
│      PaymentMethod: "CreditCard",                               │
│      CompanyName: "Ziraai Teknoloji A.Ş.",    ✅ FROM FLOWDATA │
│      TaxNumber: "1234567890",                  ✅ FROM FLOWDATA │
│      InvoiceAddress: "İstanbul, Türkiye",     ✅ FROM FLOWDATA │
│      Status: "Active",                                          │
│      ...                                                        │
│    }                                                            │
│ 4. Generates 50 sponsorship codes                              │
│ 5. Invalidates dashboard cache                                 │
│ 6. Returns 302 Redirect to deep link                           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ DATABASE - SponsorshipPurchase Table                           │
├─────────────────────────────────────────────────────────────────┤
│ PurchaseId: 39                                                  │
│ SponsorId: 189                                                  │
│ TotalAmount: 4999.50                                            │
│ CompanyName: "Ziraai Teknoloji A.Ş."          ✅ SAVED         │
│ TaxNumber: "1234567890"                        ✅ SAVED         │
│ InvoiceAddress: "İstanbul, Türkiye"           ✅ SAVED         │
│ Status: Active                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Request/Response Examples

### Example 1: Personal Purchase (No Invoice)

**Request:**
```json
POST /api/v1/payments/initialize

{
  "flowType": "SponsorBulkPurchase",
  "flowData": {
    "subscriptionTierId": 1,
    "quantity": 10
  },
  "currency": "TRY"
}
```

**Result in Database:**
```sql
-- CompanyName: NULL
-- TaxNumber: NULL
-- InvoiceAddress: NULL
```

### Example 2: Corporate Purchase (With Invoice)

**Request:**
```json
POST /api/v1/payments/initialize

{
  "flowType": "SponsorBulkPurchase",
  "flowData": {
    "subscriptionTierId": 2,
    "quantity": 100,
    "companyName": "Tarım Teknolojileri Ltd.",
    "taxNumber": "9876543210",
    "invoiceAddress": "Ankara, Çankaya, Türkiye"
  },
  "currency": "TRY"
}
```

**Result in Database:**
```sql
-- CompanyName: "Tarım Teknolojileri Ltd."
-- TaxNumber: "9876543210"
-- InvoiceAddress: "Ankara, Çankaya, Türkiye"
```

---

## Testing Checklist

### Backend Testing (✅ Already Deployed)

- [x] Verify `SponsorBulkPurchaseFlowData` has 3 new fields
- [x] Verify `ProcessSponsorBulkPurchaseAsync` uses invoice fields from flowData
- [x] Deploy to Railway staging environment
- [ ] Test payment with invoice fields via Postman
- [ ] Verify database record has invoice fields populated

### Mobile App Testing (Required)

- [ ] Add invoice form UI to payment screen
- [ ] Add toggle for "Kurumsal Fatura"
- [ ] Update `SponsorBulkPurchaseFlowData` model
- [ ] Test personal purchase (invoice fields not sent)
- [ ] Test corporate purchase (invoice fields sent)
- [ ] Verify purchase completes successfully
- [ ] Check database for saved invoice fields

### End-to-End Test Scenarios

#### Scenario 1: Personal Purchase
```
1. Select tier: S
2. Enter quantity: 5
3. Toggle "Kurumsal Fatura": OFF
4. Complete payment
5. Expected: Purchase created with NULL invoice fields
```

#### Scenario 2: Corporate Purchase
```
1. Select tier: M
2. Enter quantity: 50
3. Toggle "Kurumsal Fatura": ON
4. Fill invoice form:
   - Company: "Test Şirketi A.Ş."
   - Tax No: "1234567890"
   - Address: "İstanbul"
5. Complete payment
6. Expected: Purchase created with invoice fields populated
```

---

## Deployment Instructions

### Backend Deployment (✅ COMPLETED)

```bash
# Changes already committed and deployed to Railway
git log --oneline -3
# Should show: "feat: Add invoice fields to sponsor bulk purchase flow"
```

### Mobile App Deployment (TODO)

```bash
# 1. Update Flutter model
# File: lib/features/payment/data/models/payment_models.dart
# Add: companyName, taxNumber, invoiceAddress fields

# 2. Update payment screen UI
# File: lib/features/payment/presentation/screens/sponsor_payment_screen.dart
# Add: Invoice toggle and form

# 3. Test locally
flutter run

# 4. Test on staging
# Use tier: S, quantity: 1
# Fill invoice fields
# Verify payment completes

# 5. Deploy to production
flutter build apk --release
```

---

## Configuration Notes

### Backend Configuration (No Changes Required)

The backend changes are **backward compatible**:
- ✅ Old mobile app versions (without invoice fields) will work
- ✅ New mobile app versions (with invoice fields) will work
- ✅ Invoice fields are **optional**, not required

### Railway Environment Variables

No environment variable changes required. The flow data is stored as JSON in the `PaymentTransaction` table.

---

## API Documentation Update

### Payment Initialize Endpoint

**Endpoint:** `POST /api/v1/payments/initialize`

**Request Body:**
```json
{
  "flowType": "SponsorBulkPurchase",
  "flowData": {
    "subscriptionTierId": 1,          // Required
    "quantity": 50,                    // Required (1-10000)
    "companyName": "string",           // Optional
    "taxNumber": "string",             // Optional
    "invoiceAddress": "string"         // Optional
  },
  "currency": "TRY"                    // Optional (defaults to TRY)
}
```

**Field Descriptions:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `flowType` | string | Yes | Must be "SponsorBulkPurchase" |
| `flowData.subscriptionTierId` | int | Yes | Tier ID (1=S, 2=M, 3=L, 4=XL) |
| `flowData.quantity` | int | Yes | Number of codes (1-10000) |
| `flowData.companyName` | string | No | Company name for invoice |
| `flowData.taxNumber` | string | No | Tax number for invoice |
| `flowData.invoiceAddress` | string | No | Invoice address |
| `currency` | string | No | Currency code (defaults to TRY) |

**Response:**
```json
{
  "success": true,
  "data": {
    "paymentToken": "abc-123-xyz",
    "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=...",
    "transactionId": 19,
    "amount": 4999.50,
    "currency": "TRY",
    "callbackUrl": "ziraai://payment-callback"
  }
}
```

---

## Summary

### What Changed
✅ Backend now accepts and saves invoice fields from payment flow  
✅ Invoice fields stored in `SponsorshipPurchase` table  
✅ Backward compatible with old mobile app versions

### What's Required
📱 Mobile app must collect and send invoice fields  
📱 Add invoice form to payment screen  
📱 Update payment initialization request

### Benefits
✅ Proper invoice record keeping for corporate purchases  
✅ Support for both personal and corporate purchases  
✅ Consistent invoice data across all purchase methods (mock, real, admin)

---

## Related Files

**Backend:**
- [Entities/Dtos/Payment/PaymentInitializeRequestDto.cs](../Entities/Dtos/Payment/PaymentInitializeRequestDto.cs) - Flow data DTO
- [Business/Services/Payment/IyzicoPaymentService.cs](../Business/Services/Payment/IyzicoPaymentService.cs) - Payment processing
- [Entities/Concrete/SponsorshipPurchase.cs](../Entities/Concrete/SponsorshipPurchase.cs) - Purchase entity

**Mobile:**
- `lib/features/payment/data/models/payment_models.dart` - Data models
- `lib/features/payment/presentation/screens/sponsor_payment_screen.dart` - Payment UI
- `lib/features/payment/services/payment_service.dart` - API service

---

**Document Version:** 1.0  
**Last Updated:** 2025-11-22  
**Status:** Backend ✅ Deployed | Mobile 📱 Pending Implementation
