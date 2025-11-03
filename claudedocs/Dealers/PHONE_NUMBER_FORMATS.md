# Phone Number Format Guide for Bulk Dealer Invitations

## Accepted Formats

The system now accepts **ALL** of the following Turkish phone number formats:

### ✅ International Format (with +90)
```
+90 506 946 86 93
+905069468693
+90-506-946-86-93
+90 (506) 946 86 93
```

### ✅ Local Format with Leading 0
```
0506 946 86 93
05069468693
0506-946-86-93
(0506) 946 86 93
```

### ✅ Local Format without Leading 0
```
506 946 86 93
5069468693
506-946-86-93
(506) 946 86 93
```

### ✅ 10-Digit Format (Most Common Excel Issue)
```
5069468693
```
**Note:** This is what Excel typically shows when you paste a phone number. System now handles this automatically!

## Automatic Normalization

All phone numbers are automatically normalized to **international format without spaces**:
- Input: `+90 506 946 86 93` → Output: `905069468693`
- Input: `0506 946 86 93` → Output: `905069468693`
- Input: `506 946 86 93` → Output: `905069468693`
- Input: `5069468693` → Output: `905069468693`

## Supported Formatting Characters

The system automatically removes these characters:
- **Spaces**: ` `
- **Dashes**: `-`
- **Parentheses**: `(` `)`
- **Dots**: `.`
- **Plus sign**: `+` (converted to country code)

## Excel Tips

### Problem: Excel Converts Phone to Number
When you paste `05069468693` into Excel, it might show as `5069468693` (removes leading zero).

### Solution 1: Format as Text (Recommended)
1. Select phone column in Excel
2. Right-click → Format Cells
3. Choose "Text" format
4. Re-enter phone numbers

### Solution 2: Add Apostrophe Prefix
Type: `'05069468693` (with apostrophe at start)
Excel will keep it as text and preserve the leading zero.

### Solution 3: Use Any Format (NEW!)
With the new normalization, you can now use **any format**:
- `5069468693` ✅ Works (10 digits)
- `05069468693` ✅ Works (11 digits)
- `905069468693` ✅ Works (12 digits)
- `+905069468693` ✅ Works (with plus)
- `0506 946 86 93` ✅ Works (with spaces)

All will be automatically normalized!

## Validation Rules

Phone numbers must meet these criteria after normalization:

1. **Length Check:**
   - 10 digits: `5xxxxxxxxx` (local format)
   - 11 digits: `05xxxxxxxxx` (local with zero)
   - 12 digits: `905xxxxxxxxx` (international)

2. **Mobile Number Check:**
   - Must start with `5` (after removing country/area codes)
   - Turkish mobile numbers: 5xx xxx xx xx

3. **Country Code:**
   - If present, must be `90` (Turkey)
   - Automatically added if missing

## Example Excel File

### CSV Format
```csv
Email,Phone,DealerName,CodeCount
dealer1@test.com,5551234567,Dealer 1,10
dealer2@test.com,0555 123 45 68,Dealer 2,5
dealer3@test.com,+90 555 123 45 69,Dealer 3,15
dealer4@test.com,905551234570,Dealer 4,8
```

### All of these are valid! ✅

## Error Messages

### Invalid Phone Format
```
Satır 3: Geçersiz telefon - 1234567890
```
**Reason:** Not a valid Turkish mobile format (doesn't start with 5)

### Invalid Length
```
Satır 5: Geçersiz telefon - 123
```
**Reason:** Too short (must be 10-12 digits after removing formatting)

## Common Issues Fixed

### Issue 1: Excel Removes Leading Zero ✅ FIXED
- **Before:** `05551234567` → Excel shows `5551234567` → ❌ Validation failed
- **After:** `5551234567` → Auto-normalized to `905551234567` → ✅ Valid

### Issue 2: Copy-Paste with Spaces ✅ FIXED
- **Before:** `0555 123 45 67` → ❌ Validation failed (spaces not allowed)
- **After:** `0555 123 45 67` → Auto-cleaned → `905551234567` → ✅ Valid

### Issue 3: Different Formats in Same File ✅ FIXED
- **Before:** Mixed formats required manual standardization
- **After:** All formats automatically normalized to same standard

## API Response

When phone numbers are normalized, you'll see them in the standard format in responses:

```json
{
  "dealerId": 123,
  "phone": "905069468693",
  "normalizedFormat": true
}
```

## Testing

### Test Case 1: 10-Digit Format
```csv
Email,Phone,DealerName,CodeCount
test@example.com,5551234567,Test,10
```
**Expected:** ✅ Normalized to `905551234567`

### Test Case 2: International Format
```csv
Email,Phone,DealerName,CodeCount
test@example.com,+90 555 123 45 67,Test,10
```
**Expected:** ✅ Normalized to `905551234567`

### Test Case 3: Local Format with Spaces
```csv
Email,Phone,DealerName,CodeCount
test@example.com,0555 123 45 67,Test,10
```
**Expected:** ✅ Normalized to `905551234567`

### Test Case 4: Invalid Format
```csv
Email,Phone,DealerName,CodeCount
test@example.com,1234567890,Test,10
```
**Expected:** ❌ Error - "Geçersiz telefon - 1234567890" (doesn't start with 5)

## Summary

✅ **What Changed:**
- All Turkish mobile phone formats now accepted
- Automatic normalization to `905xxxxxxxxx` format
- Formatting characters (spaces, dashes, etc.) automatically removed
- Leading zeros handled correctly
- Excel copy-paste issues resolved

✅ **User Benefits:**
- No need to worry about exact format
- Excel formatting won't break validation
- Copy-paste from any source works
- International and local formats both work
- Faster data entry

✅ **Backwards Compatible:**
- Old format (`905xxxxxxxxx`) still works
- No changes needed to existing Excel files
- API continues to return same format

---

**Last Updated:** 2025-11-03
**Feature:** Bulk Dealer Invitation Phone Normalization
**Status:** ✅ Deployed to Production
