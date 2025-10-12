# Resend Expired Codes Feature

**Version:** 1.0
**Date:** 2025-10-12
**Target Audience:** Mobile Development Team
**Status:** ✅ Implemented

---

## 📋 Overview

Backend'e **expired kodları tekrar gönderme** özelliği eklendi. Sponsor'lar artık:

✅ Expire olmuş kodları tekrar gönderebilin
✅ Kod tekrar gönderildiğinde **expiry date otomatik yenilenir** (30 gün)
✅ Güvenli: Sadece kullanılmamış (IsUsed=false) kodlar tekrar gönderilebilir
✅ Geriye uyumlu: Mevcut davranış değişmedi, yeni parametre opsiyonel

---

## 🆕 What Changed

### Backend Changes

**SendSponsorshipLinkCommand** - Yeni parametre eklendi:

```csharp
public class SendSponsorshipLinkCommand
{
    public int SponsorId { get; set; }
    public List<LinkRecipient> Recipients { get; set; }
    public string Channel { get; set; } = "SMS";
    public string CustomMessage { get; set; }
    public bool AllowResendExpired { get; set; } = false;  // 🆕 NEW
}
```

### Business Logic

**Eski Davranış (AllowResendExpired = false):**
- ❌ Expired kod → Reddedilir
- ✅ Active kod → Gönderilir

**Yeni Davranış (AllowResendExpired = true):**
- ✅ Expired kod → ExpiryDate yenilenir (+ 30 gün) → Gönderilir
- ✅ Active kod → Olduğu gibi gönderilir

---

## 📱 Mobile Integration

### 1. Update API Service

**Dosya:** `lib/services/sponsorship_service.dart`

```dart
/// Send sponsorship links to farmers
///
/// [allowResendExpired] If true, allows resending expired codes with renewed expiry date
Future<BulkSendResult?> sendSponsorshipLinks({
  required List<LinkRecipient> recipients,
  String channel = 'SMS',
  String? customMessage,
  bool allowResendExpired = false,  // 🆕 NEW PARAMETER
}) async {
  try {
    final response = await _apiClient.post(
      '/sponsorship/send-link',
      data: {
        'recipients': recipients.map((r) => r.toJson()).toList(),
        'channel': channel,
        'customMessage': customMessage,
        'allowResendExpired': allowResendExpired,  // 🆕 SEND TO BACKEND
      },
    );

    return BulkSendResult.fromJson(response);
  } catch (e) {
    print('Error sending sponsorship links: $e');
    return null;
  }
}
```

### 2. Update UI - Send Codes Screen

**Scenario A: User selects expired codes**

```dart
class SendCodesScreen extends StatefulWidget {
  final List<SponsorshipCode> selectedCodes;

  @override
  _SendCodesScreenState createState() => _SendCodesScreenState();
}

class _SendCodesScreenState extends State<SendCodesScreen> {
  bool _hasExpiredCodes = false;
  bool _allowResendExpired = false;

  @override
  void initState() {
    super.initState();
    // Check if any selected codes are expired
    _hasExpiredCodes = widget.selectedCodes.any(
      (code) => code.expiryDate != null && code.expiryDate!.isBefore(DateTime.now())
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Send Codes')),
      body: Column(
        children: [
          // Selected codes list
          Expanded(
            child: ListView.builder(
              itemCount: widget.selectedCodes.length,
              itemBuilder: (context, index) {
                final code = widget.selectedCodes[index];
                final isExpired = code.expiryDate?.isBefore(DateTime.now()) ?? false;

                return ListTile(
                  title: Text(code.code),
                  subtitle: Text(isExpired ? '⚠️ Expired' : 'Active'),
                  trailing: Icon(
                    isExpired ? Icons.warning_amber : Icons.check_circle,
                    color: isExpired ? Colors.orange : Colors.green,
                  ),
                );
              },
            ),
          ),

          // 🆕 Warning banner for expired codes
          if (_hasExpiredCodes)
            Card(
              margin: EdgeInsets.all(16),
              color: Colors.orange.shade50,
              child: Padding(
                padding: EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Icon(Icons.warning_amber, color: Colors.orange),
                        SizedBox(width: 8),
                        Text(
                          'Expired Codes Detected',
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            color: Colors.orange.shade900,
                          ),
                        ),
                      ],
                    ),
                    SizedBox(height: 8),
                    Text(
                      'Some selected codes have expired. Would you like to renew and send them?',
                      style: TextStyle(color: Colors.orange.shade900),
                    ),
                    SizedBox(height: 12),
                    CheckboxListTile(
                      value: _allowResendExpired,
                      onChanged: (value) {
                        setState(() => _allowResendExpired = value ?? false);
                      },
                      title: Text(
                        'Renew expiry dates and send',
                        style: TextStyle(fontSize: 14),
                      ),
                      subtitle: Text(
                        'Codes will be valid for 30 more days',
                        style: TextStyle(fontSize: 12),
                      ),
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                    ),
                  ],
                ),
              ),
            ),

          // Send button
          Padding(
            padding: EdgeInsets.all(16),
            child: ElevatedButton(
              onPressed: _sendCodes,
              child: Text('Send ${widget.selectedCodes.length} Codes'),
              style: ElevatedButton.styleFrom(
                minimumSize: Size(double.infinity, 50),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _sendCodes() async {
    // Prepare recipients
    final recipients = widget.selectedCodes.map((code) => LinkRecipient(
      code: code.code,
      phone: '+905551234567',  // Get from user input
      name: 'Farmer Name',      // Get from user input
    )).toList();

    // Send with allowResendExpired flag
    final result = await _sponsorshipService.sendSponsorshipLinks(
      recipients: recipients,
      channel: 'SMS',
      allowResendExpired: _allowResendExpired,  // 🆕 PASS THE FLAG
    );

    if (result != null && result.successCount > 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('✅ ${result.successCount} codes sent successfully'),
          backgroundColor: Colors.green,
        ),
      );
      Navigator.pop(context);
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('❌ Failed to send codes'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }
}
```

**Scenario B: Auto-detect expired codes and prompt**

```dart
Future<void> _sendCodesWithAutoDetection(List<SponsorshipCode> codes) async {
  // Check for expired codes
  final expiredCount = codes.where(
    (c) => c.expiryDate != null && c.expiryDate!.isBefore(DateTime.now())
  ).length;

  bool allowResendExpired = false;

  if (expiredCount > 0) {
    // Show confirmation dialog
    final shouldRenew = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('⚠️ Expired Codes'),
        content: Text(
          '$expiredCount of ${codes.length} selected codes have expired.\n\n'
          'Would you like to renew their expiry dates and send them anyway?'
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text('Renew & Send'),
          ),
        ],
      ),
    );

    if (shouldRenew != true) {
      return; // User cancelled
    }

    allowResendExpired = true;
  }

  // Prepare recipients
  final recipients = codes.map((code) => LinkRecipient(
    code: code.code,
    phone: code.recipientPhone ?? '+905551234567',
    name: code.recipientName ?? 'Farmer',
  )).toList();

  // Send codes
  final result = await _sponsorshipService.sendSponsorshipLinks(
    recipients: recipients,
    channel: 'SMS',
    allowResendExpired: allowResendExpired,
  );

  // Handle result
  if (result != null && result.successCount > 0) {
    _showSuccessMessage('${result.successCount} codes sent');
  }
}
```

---

## 🔧 API Endpoint Details

### Endpoint
```
POST /api/v1/sponsorship/send-link
```

### Request Body

```json
{
  "recipients": [
    {
      "code": "AGRI-2025-ABC123",
      "phone": "+905551234567",
      "name": "Mehmet Yılmaz"
    }
  ],
  "channel": "SMS",
  "customMessage": "Optional custom message",
  "allowResendExpired": true
}
```

### Response

```json
{
  "success": true,
  "message": "📱 2 link başarıyla gönderildi via SMS",
  "data": {
    "totalSent": 2,
    "successCount": 2,
    "failureCount": 0,
    "results": [
      {
        "code": "AGRI-2025-ABC123",
        "phone": "+905551234567",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Sent"
      }
    ]
  }
}
```

---

## 🎯 Use Cases

### Use Case 1: Sponsor wants to resend expired codes
```dart
// User filtered to onlySentExpired=true
// User selects some expired codes
// User clicks "Send Again"
// Show checkbox: "Renew expiry dates (30 days)"
// If checked: allowResendExpired = true
```

### Use Case 2: Mixed selection (active + expired)
```dart
// User selects 10 codes (3 expired, 7 active)
// Show warning: "3 codes have expired"
// Show checkbox: "Renew and send expired codes too"
// If checked: All 10 codes sent (3 renewed automatically)
// If unchecked: Only 7 active codes sent
```

### Use Case 3: Bulk resend to non-responsive farmers
```dart
// Get codes sent 7 days ago but still unused
final codes = await service.getSponsorshipCodes(
  sentDaysAgo: 7,
  onlyUnused: true,
);

// Some might have expired in the meantime
// Auto-detect and prompt for renewal
await _sendCodesWithAutoDetection(codes);
```

---

## ⚡ Backend Behavior

### When AllowResendExpired = false (Default)
```
1. Validate codes: !IsUsed && ExpiryDate > NOW
2. Expired codes → Rejected with error
3. Active codes → Sent normally
```

### When AllowResendExpired = true
```
1. Validate codes: !IsUsed (no expiry check)
2. Find expired codes
3. Update expired codes: ExpiryDate = NOW + 30 days
4. Save changes
5. Send all codes (renewed + active)
6. Update DistributionDate for all sent codes
```

### Logs
```
📤 Sponsor 159 sending 5 sponsorship links via SMS
📋 Validated 5/5 codes (AllowResendExpired: true)
🔄 Renewing expiry date for 3 expired codes
✅ Renewed expiry dates successfully
📱 5 link başarıyla gönderildi via SMS
```

---

## 🧪 Testing Checklist

- [ ] Send active code (AllowResendExpired=false) → Should work
- [ ] Send expired code (AllowResendExpired=false) → Should fail
- [ ] Send expired code (AllowResendExpired=true) → Should renew & send
- [ ] Send mixed codes (AllowResendExpired=true) → All should send
- [ ] Verify expiry date updated in DB after resend
- [ ] Verify DistributionDate updated after resend
- [ ] Test with SMS channel
- [ ] Test with WhatsApp channel
- [ ] Test error handling (network errors)
- [ ] Test UI warning banner
- [ ] Test confirmation dialog

---

## 📞 Support

**Questions:** Backend Team
**Staging URL:** `https://ziraai-api-sit.up.railway.app`

---

## 🔄 Migration Timeline

| Phase | Task | Status |
|-------|------|--------|
| Phase 1 | Backend implementation | ✅ Complete |
| Phase 2 | Mobile API service update | 🔜 To Do |
| Phase 3 | Mobile UI implementation | 🔜 To Do |
| Phase 4 | Testing & QA | 🔜 To Do |
| Phase 5 | Production deployment | 🔜 To Do |

---

**End of Document**
