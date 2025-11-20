# Code Distribution vs Redemption Pattern

## Critical Design Pattern

The sponsorship code system follows a **two-phase pattern** that must be maintained consistently:

### Phase 1: Code Distribution (Delivery)
**Purpose**: Deliver the code to a recipient via SMS/Email  
**User Requirement**: None - recipient may or may not be registered  
**Code State Change**: Available → Distributed (NOT consumed)

### Phase 2: Code Redemption (Activation)
**Purpose**: User activates the code and receives subscription benefits  
**User Requirement**: User must be registered (or register during redemption)  
**Code State Change**: Distributed → Redeemed (consumed)

## Implementation Reference: SendSponsorshipLinkCommand

This is the **authoritative pattern** that all distribution implementations must follow:

```csharp
// ✅ CORRECT PATTERN - Single Code Distribution
public class SendSponsorshipLinkCommand
{
    public async Task<IResult> Handle()
    {
        // 1. Get available code
        var code = await GetAvailableCode(request.PurchaseId);
        
        // 2. Send SMS to recipient (NO user lookup required)
        var formattedPhone = FormatPhoneNumber(recipient.Phone);
        var smsResult = await SendSms(formattedPhone, code.Code);
        
        if (smsResult.Success)
        {
            // 3. Update ONLY distribution fields
            code.RedemptionLink = deepLink;
            code.RecipientPhone = formattedPhone;
            code.RecipientName = recipient.Name;
            code.LinkSentDate = DateTime.Now;
            code.LinkSentVia = "SMS";
            code.LinkDelivered = true;
            code.DistributionChannel = "SMS";
            code.DistributionDate = DateTime.Now;
            code.DistributedTo = $"{recipient.Name} ({formattedPhone})";
            
            _codeRepository.Update(code);
            
            // ❌ NEVER SET THESE DURING DISTRIBUTION:
            // code.IsUsed = true;
            // code.UsedByUserId = userId;
            // code.UsedDate = DateTime.Now;
        }
    }
}
```

## Common Anti-Patterns (AVOID)

### ❌ Anti-Pattern 1: User Lookup During Distribution
```csharp
// WRONG - Distribution should work without user existing
var user = await _userRepository.GetAsync(u => u.Email == message.Email);
if (user == null)
{
    return Error("User not found");
}
```

**Why Wrong**: Recipients may not be registered yet. They'll register when redeeming the code.

### ❌ Anti-Pattern 2: Marking Code as Used During Distribution
```csharp
// WRONG - This consumes the code prematurely
code.IsUsed = true;
code.UsedByUserId = user.UserId;
code.UsedDate = DateTime.Now;
```

**Why Wrong**: Code is only DISTRIBUTED, not REDEEMED. User hasn't activated it yet.

### ❌ Anti-Pattern 3: Explicitly Setting DealerId to Null
```csharp
// WRONG - Don't touch DealerId during distribution
code.DealerId = null;
```

**Why Wrong**: If code was transferred from sponsor to dealer, DealerId tracks this relationship.

## Correct Implementation Checklist

When implementing ANY code distribution feature (single, bulk, automated):

- [ ] ✅ No user lookup required (`IUserRepository` not needed)
- [ ] ✅ Send message directly to provided phone/email
- [ ] ✅ Update ONLY distribution-related fields
- [ ] ✅ Never set `IsUsed`, `UsedByUserId`, `UsedDate`
- [ ] ✅ Never modify `DealerId`
- [ ] ✅ Use `FormatPhoneNumber()` for phone normalization (returns `+905321234567`)
- [ ] ✅ Handle non-existent recipients gracefully

## SponsorshipCode Entity State Diagram

```
┌─────────────┐
│  Available  │ Initial state after purchase
└──────┬──────┘
       │ Distribution (Phase 1)
       ▼
┌─────────────┐
│ Distributed │ SMS sent, but not activated yet
└──────┬──────┘
       │ Redemption (Phase 2)
       ▼
┌─────────────┐
│  Redeemed   │ User activated, subscription granted
└─────────────┘
```

### State Indicators:

| State | DistributionDate | IsUsed | UsedByUserId | UsedDate |
|-------|------------------|--------|--------------|----------|
| Available | null | false | null | null |
| Distributed | set | false | null | null |
| Redeemed | set | true | set | set |

## Phone Number Formatting Standard

All implementations MUST use `FormatPhoneNumber()`:

```csharp
private string FormatPhoneNumber(string phone)
{
    // Remove all non-numeric characters
    var cleaned = new string(phone.Where(char.IsDigit).ToArray());

    // Add Turkey country code if not present
    if (!cleaned.StartsWith("90") && cleaned.Length == 10)
    {
        cleaned = "90" + cleaned;
    }

    // Add + prefix
    if (!cleaned.StartsWith("+"))
    {
        cleaned = "+" + cleaned;
    }

    return cleaned; // Returns "+905321234567"
}
```

**Examples**:
- `05321234567` → `+905321234567`
- `5321234567` → `+905321234567`
- `905321234567` → `+905321234567`
- `+905321234567` → `+905321234567`
- `0532 123 45 67` → `+905321234567`
- `(0532) 123-45-67` → `+905321234567`

## Bulk Distribution Worker Fix (Case Study)

### Problem Identified
The `FarmerCodeDistributionJobService` was implementing a different pattern than `SendSponsorshipLinkCommand`, causing failures for non-registered users.

### Original (WRONG) Implementation
```csharp
// ❌ WRONG: Required user to exist
var user = await _userRepository.GetAsync(u => u.Email == message.Email);
if (user == null)
{
    _logger.LogWarning("User not found: {Email}", message.Email);
    success = false;
}
else
{
    // ❌ WRONG: Consumed code immediately
    code.IsUsed = true;
    code.UsedByUserId = user.UserId;
    code.UsedDate = DateTime.Now;
    code.DealerId = null; // ❌ WRONG: Explicitly set to null
    
    // ❌ WRONG: Different phone format
    var phone = NormalizePhoneNumber(user.MobilePhones); // Returns "05321234567"
}
```

**Errors Seen in Logs**:
```
warn: [FARMER_CODE_DISTRIBUTION_USER_NOT_FOUND] User not found with email: mehmet.demir@hotmail.com
warn: [FARMER_CODE_DISTRIBUTION_USER_NOT_FOUND] User not found with email: elif.yildiz@outlook.com
warn: [FARMER_CODE_DISTRIBUTION_USER_NOT_FOUND] User not found with email: ali.ozturk@gmail.com
```

### Fixed (CORRECT) Implementation
```csharp
public class FarmerCodeDistributionJobService : IFarmerCodeDistributionJobService
{
    // ✅ REMOVED: IUserRepository, IUserSubscriptionRepository
    private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
    private readonly ISponsorProfileRepository _sponsorProfileRepository;
    private readonly IMessagingServiceFactory _messagingFactory;
    // ... other dependencies

    public async Task ProcessFarmerCodeDistributionAsync(FarmerCodeDistributionQueueMessage message)
    {
        // Step 1: Get available code (SAME AS SendSponsorshipLinkCommand)
        var code = await GetAvailableCode(message.PurchaseId);
        
        if (code != null && message.SendSms && !string.IsNullOrEmpty(message.Phone))
        {
            // ✅ CORRECT: Use phone from message directly
            var normalizedPhone = FormatPhoneNumber(message.Phone);
            
            // ✅ CORRECT: Send SMS without user lookup
            var smsResult = await SendSms(normalizedPhone, code.Code);
            
            if (smsResult.Success)
            {
                // ✅ CORRECT: Only distribution fields
                code.RedemptionLink = deepLink;
                code.RecipientPhone = normalizedPhone;
                code.RecipientName = message.FarmerName ?? "Değerli Üyemiz";
                code.LinkSentDate = DateTime.Now;
                code.LinkSentVia = "SMS";
                code.LinkDelivered = true;
                code.DistributionChannel = "SMS";
                code.DistributionDate = DateTime.Now;
                code.DistributedTo = $"{code.RecipientName} ({normalizedPhone})";
                
                _sponsorshipCodeRepository.Update(code);
                
                success = true;
            }
        }
    }
}
```

## Testing Verification

### Distribution Test (Should Pass)
```bash
# Test: Distribute code to non-registered phone
POST /api/v1/sponsorship/send-link
{
  "purchaseId": 26,
  "recipients": [
    {
      "phone": "05329999999",  // Not registered
      "name": "Test User"
    }
  ],
  "channel": "SMS"
}

# Expected Result:
# - SMS sent successfully
# - Code.DistributionDate = now
# - Code.IsUsed = false (NOT consumed)
# - Code.UsedByUserId = null
```

### Redemption Test (Should Pass)
```bash
# Test: Non-registered user redeems distributed code
POST /api/v1/sponsorship/use-code
{
  "code": "AGRI-2025-XXXXXXXX"
}

# Expected Result:
# - User registration flow triggered (if not registered)
# - Subscription created/updated
# - Code.IsUsed = true
# - Code.UsedByUserId = {userId}
# - Code.UsedDate = now
```

## Key Takeaways

1. **Distribution ≠ Redemption**: These are separate phases with different responsibilities
2. **No User Dependency**: Distribution works without user existence check
3. **State Management**: Only update distribution fields during distribution
4. **Phone Formatting**: Always use `FormatPhoneNumber()` for consistency
5. **Follow Reference**: `SendSponsorshipLinkCommand` is the authoritative pattern
6. **DealerId Preservation**: Never modify DealerId during distribution

## Related Files

- `Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs` - Reference implementation
- `PlantAnalysisWorkerService/Jobs/FarmerCodeDistributionJobService.cs` - Bulk worker (fixed)
- `Business/Handlers/Sponsorship/Commands/UseCodeCommand.cs` - Redemption logic
- `Entities/Concrete/SponsorshipCode.cs` - Entity definition
