# Payment Data Flow Analysis - iyzico Integration

**Analysis Date**: 2025-11-26
**Purpose**: Document where each payment field originates and how data flows to iyzico payment gateway

---

## 🎯 Executive Summary

All buyer information sent to iyzico comes from **user's profile data in the database** (`Users` table), **NOT from payment forms**. There is NO payment form where users enter billing information manually.

### Key Finding:
- ✅ **Source**: `Users` table via `IUserRepository`
- ❌ **NOT from**: Payment forms, checkout flows, or manual user input
- ⚠️ **Hardcoded**: `identityNumber = "11111111111"` (TC Kimlik)

---

## 📊 Data Flow Diagram

```
User Registration/Profile
        ↓
   Users Table (Database)
        ↓
   IUserRepository.GetAsync(userId)
        ↓
   IyzicoPaymentService.InitializePaymentAsync()
        ↓
   Build iyzico API Request
        ↓
   iyzico Payment Gateway
```

---

## 🔍 Field-by-Field Analysis

### Alıcı Bilgileri (Buyer Information)

| iyzico Field | Source | Database Column | Notes |
|---|---|---|---|
| **buyer.id** | `userId.ToString()` | `Users.UserId` | Direct conversion |
| **buyer.name** | `user.FullName.Split(' ')[0]` | `Users.FullName` | First part before space |
| **buyer.surname** | `user.FullName.Split(' ').Skip(1)` | `Users.FullName` | Everything after first space |
| **buyer.email** | `user.Email` | `Users.Email` | Direct mapping |
| **buyer.gsmNumber** | `user.MobilePhones` (formatted) | `Users.MobilePhones` | Formatted to +90XXXXXXXXXX |
| **buyer.identityNumber** | `"11111111111"` | ❌ HARDCODED | **Test value, not from DB** |
| **buyer.registrationDate** | `user.RecordDate` | `Users.RecordDate` | Formatted: yyyy-MM-dd HH:mm:ss |
| **buyer.lastLoginDate** | `DateTime.Now` | ⏰ RUNTIME | Current timestamp |
| **buyer.registrationAddress** | `user.Address` | `Users.Address` | Fallback: "Istanbul, Turkey" |
| **buyer.city** | `"Istanbul"` | ❌ HARDCODED | Not from DB |
| **buyer.country** | `"Turkey"` | ❌ HARDCODED | Not from DB |
| **buyer.zipCode** | `"34732"` | ❌ HARDCODED | Not from DB |
| **buyer.ip** | `"127.0.0.1"` | ❌ HARDCODED | Should be real IP |

---

## 💻 Code Implementation Details

### Location
**File**: `Business/Services/Payment/IyzicoPaymentService.cs`
**Method**: `InitializePaymentAsync(int userId, PaymentInitializeRequestDto request)`
**Lines**: 195-279

### Step-by-Step Process

#### Step 1: Get User from Database
```csharp
// Line 78-82
var user = await _userRepository.GetAsync(u => u.UserId == userId);
if (user == null)
{
    return new ErrorDataResult<PaymentInitializeResponseDto>("User not found");
}
```

#### Step 2: Extract Buyer Name
```csharp
// Line 198-205
bool hasValidFullName = !string.IsNullOrEmpty(user.FullName)
    && user.FullName.Contains(' ')
    && !IsPlaceholderValue(user.FullName);

(string buyerFirstName, string buyerLastName) = hasValidFullName
    ? (user.FullName.Split(' ')[0], string.Join(" ", user.FullName.Split(' ').Skip(1)))
    : ExtractNameFromEmail(user.Email);
```

**Logic**:
- If `FullName` is valid and contains space → Split into first/last name
- If `FullName` is placeholder/invalid → Extract from email (e.g., `dort@dorttarim.com` → "Dort" + "Dorttarim")

#### Step 3: Format Phone Number
```csharp
// Line 207-214
string buyerPhone = "+905350000000"; // Default fallback
if (!string.IsNullOrEmpty(user.MobilePhones))
{
    buyerPhone = user.MobilePhones.StartsWith("+")
        ? user.MobilePhones
        : "+90" + user.MobilePhones.TrimStart('0');
}
```

**Logic**:
- Default: `+905350000000`
- If `MobilePhones` exists → Format to international (+90...)
- Removes leading 0, adds +90 prefix

#### Step 4: Validate Address
```csharp
// Line 216-219
string buyerAddress = IsPlaceholderValue(user.Address)
    ? "Istanbul, Turkey"
    : user.Address;
```

**Logic**:
- If `Address` is placeholder → Use "Istanbul, Turkey"
- Otherwise → Use database value

#### Step 5: Build iyzico Request
```csharp
// Line 221-279
var iyzicoRequest = new
{
    // ... payment details
    buyer = new
    {
        id = userId.ToString(),
        name = buyerFirstName,
        surname = buyerLastName,
        email = user.Email,
        gsmNumber = buyerPhone,
        identityNumber = "11111111111", // ⚠️ HARDCODED
        registrationDate = user.RecordDate.ToString("yyyy-MM-dd HH:mm:ss"),
        lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        registrationAddress = buyerAddress,
        city = "Istanbul", // ⚠️ HARDCODED
        country = "Turkey", // ⚠️ HARDCODED
        zipCode = "34732", // ⚠️ HARDCODED
        ip = "127.0.0.1" // ⚠️ HARDCODED
    },
    // ... shipping/billing addresses
};
```

---

## 🚨 Critical Issues & Hardcoded Values

### 1. TC Kimlik Numarası (Identity Number)
```csharp
identityNumber = "11111111111"
```
- ⚠️ **Hardcoded test value**
- ❌ Not fetched from database
- ❌ Not from user profile
- ❌ Not from payment form
- 💡 **Solution**: Should come from `Users.CitizenId` column

### 2. City/Country/ZipCode
```csharp
city = "Istanbul"
country = "Turkey"
zipCode = "34732"
```
- ⚠️ **All hardcoded**
- ❌ Not dynamic based on user location
- 💡 **Potential Issue**: Users in different cities show "Istanbul"

### 3. IP Address
```csharp
ip = "127.0.0.1"
```
- ⚠️ **Localhost hardcoded**
- ❌ Not real user IP
- 💡 **Should be**: `HttpContext.Connection.RemoteIpAddress` or from request headers

---

## 🎯 Your Specific Question Answered

**Q**: "Flow içinde bir yerlerde girdiğimiz tax number vb, bilgiler mi alınıyor yoksa doğrudan kullanıcının profil bilgileri mi alınıyor?"

**A**: **Doğrudan kullanıcının profil bilgileri alınıyor** (`Users` tablosundan).

### Verification:
1. ❌ Flow içinde **FORM YOK** → Kullanıcı ödeme sırasında bilgi girmiyor
2. ✅ **Database'den** → `_userRepository.GetAsync(u => u.UserId == userId)`
3. ❌ TC Kimlik **HARDCODED** → `"11111111111"` test değeri

### Ekran Görüntünüzdeki Veriler:
- **Ad**: Dort → `Users.FullName` (Split[0])
- **Soyad**: Dorttarim → `Users.FullName` (Split[1])
- **E-posta**: dort@dorttarim.com → `Users.Email`
- **Cep Telefonu**: +905411111114 → `Users.MobilePhones` (formatted)
- **TC Kimlik**: 11111111111 → ⚠️ **HARDCODED** (not from DB!)
- **Adres**: Istanbul, Turkey → `Users.Address` (or fallback)

---

## 🔄 Complete Data Flow

### Registration Flow
```
1. User Registers (Mobile/Web)
   ↓
2. Data Saved to Users Table
   - FullName: "Dort Dorttarim"
   - Email: "dort@dorttarim.com"
   - MobilePhones: "05411111114"
   - Address: "Istanbul, Turkey"
   - CitizenId: NOT USED ❌
   ↓
3. User Profile Created
```

### Payment Flow
```
1. User Initiates Payment
   ↓
2. Backend: PaymentController receives request
   - userId from JWT token
   - flowType, flowData from request body
   ↓
3. IyzicoPaymentService.InitializePaymentAsync()
   - Fetch user: _userRepository.GetAsync(userId)
   - Extract name from FullName
   - Format phone from MobilePhones
   - Use address from Address
   ↓
4. Build iyzico Request
   - buyer.name ← FullName (split)
   - buyer.email ← Email
   - buyer.gsmNumber ← MobilePhones (formatted)
   - buyer.identityNumber ← "11111111111" ⚠️ HARDCODED
   ↓
5. Send to iyzico API
   ↓
6. iyzico Shows Payment Page
   - Displays buyer info from request
```

---

## 📝 Database Schema Reference

### Users Table Relevant Columns
```sql
CREATE TABLE "Users" (
    "UserId" INTEGER PRIMARY KEY,
    "CitizenId" BIGINT NOT NULL,          -- ⚠️ TC Kimlik (NOT USED in payment)
    "FullName" VARCHAR(100) NOT NULL,     -- ✅ Used for buyer name/surname
    "Email" VARCHAR(50),                  -- ✅ Used for buyer email
    "MobilePhones" VARCHAR(30),           -- ✅ Used for buyer phone (formatted)
    "Address" VARCHAR(200),               -- ✅ Used for buyer address
    "RecordDate" TIMESTAMP,               -- ✅ Used for registrationDate
    "Status" BOOLEAN,
    -- ... other fields
);
```

### What's Used vs. What's NOT
| Column | Used in Payment? | How Used |
|---|---|---|
| `UserId` | ✅ Yes | buyer.id |
| `FullName` | ✅ Yes | Split into name/surname |
| `Email` | ✅ Yes | buyer.email |
| `MobilePhones` | ✅ Yes | Formatted to +90... |
| `Address` | ✅ Yes | buyer.registrationAddress |
| `RecordDate` | ✅ Yes | buyer.registrationDate |
| `CitizenId` | ❌ **NO** | Should be used for identityNumber! |

---

## 🐛 Potential Issues

### Issue 1: TC Kimlik Always "11111111111"
**Problem**: All payments show same test TC number
**Impact**:
- Fraud detection may flag
- Compliance issues for real transactions
- Cannot track by real citizen ID

**Solution**:
```csharp
// Replace line 240
identityNumber = user.CitizenId.ToString(), // Use real TC from database
```

### Issue 2: City/Country Hardcoded
**Problem**: All users show "Istanbul, Turkey"
**Impact**:
- Incorrect data for users in other cities
- May affect fraud detection

**Solution**: Add city column to Users table or parse from Address

### Issue 3: IP Address is Localhost
**Problem**: Shows 127.0.0.1 for all users
**Impact**:
- Security/fraud detection issues
- Cannot geolocate transactions

**Solution**:
```csharp
// Get real IP from HTTP context
var realIp = HttpContext.Connection.RemoteIpAddress?.ToString()
    ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
    ?? "127.0.0.1";
```

### Issue 4: Name Extraction from Email
**Problem**: If FullName is invalid, name extracted from email
**Example**: `dort@dorttarim.com` → "Dort" + "Dorttarim"
**Impact**: May not be user's real name

**Solution**: Require valid FullName during registration

---

## ✅ Recommendations

### Immediate Fixes (High Priority)
1. **Use Real TC Kimlik**: Replace hardcoded `"11111111111"` with `user.CitizenId`
2. **Capture Real IP**: Get from HttpContext instead of hardcoded localhost
3. **Add CitizenId Field**: Ensure `Users.CitizenId` is populated during registration

### Future Improvements (Medium Priority)
1. **Add City Column**: Store city separately for accurate location
2. **Validate FullName**: Require proper name format during registration
3. **Address Parsing**: Parse city/zipcode from address string

### Optional Enhancements (Low Priority)
1. **Payment Form**: Add optional billing address form for users to update
2. **Profile Completion**: Prompt users to complete profile before payment
3. **Address Validation**: Validate Turkish addresses with postal service API

---

## 🧪 Testing Verification

### Test Case 1: Profile Data Propagation
```sql
-- Update user profile
UPDATE "Users"
SET "FullName" = 'Test User',
    "Email" = 'test@example.com',
    "MobilePhones" = '05551234567',
    "Address" = 'Ankara, Turkey',
    "CitizenId" = 12345678901
WHERE "UserId" = 191;

-- Initiate payment
-- Verify iyzico shows:
-- ✅ Name: Test
-- ✅ Surname: User
-- ✅ Email: test@example.com
-- ✅ Phone: +905551234567
-- ❌ TC: Still 11111111111 (bug!)
```

### Test Case 2: Hardcoded Values
```csharp
// All users regardless of profile show:
identityNumber = "11111111111"  // ❌ Always same
city = "Istanbul"                // ❌ Always same
country = "Turkey"               // ✅ OK for TR users
zipCode = "34732"                // ❌ Always same
ip = "127.0.0.1"                 // ❌ Always same
```

---

## 📚 Related Files

- **Service**: `Business/Services/Payment/IyzicoPaymentService.cs`
- **Controller**: `WebAPI/Controllers/PaymentController.cs`
- **Repository**: `DataAccess/Abstract/IUserRepository.cs`
- **Entity**: `Core/Entities/Concrete/User.cs`
- **DTOs**: `Entities/Dtos/Payment/PaymentInitializeRequestDto.cs`

---

## 🎓 Summary

### Your Question:
> "Flow içinde bir yerlerde girdiğimiz tax number vb, bilgiler mi alınıyor yoksa doğrudan kullanıcının profil bilgileri mi alınıyor?"

### Answer:
**100% kullanıcının profil bilgileri** (`Users` tablosundan). **ANCAK**:
- ✅ Ad/Soyad → `FullName` (database)
- ✅ Email → `Email` (database)
- ✅ Telefon → `MobilePhones` (database)
- ✅ Adres → `Address` (database)
- ❌ **TC Kimlik → HARDCODED `"11111111111"`** (DATABASE'DEN DEĞİL!)

### Critical Finding:
**TC Kimlik Numarası** ödeme sağlayıcıya gönderiliyor **AMA** kullanıcıdan alınmıyor, hardcoded test değeri kullanılıyor!

```csharp
// Line 240 - IyzicoPaymentService.cs
identityNumber = "11111111111", // ⚠️ BUG: Should use user.CitizenId
```

**Çözüm**:
```csharp
identityNumber = user.CitizenId.ToString(), // ✅ Use real TC from database
```

---

**Document Version**: 1.0
**Created**: 2025-11-26
**Status**: Complete
**Next Steps**: Fix TC Kimlik hardcoded value
