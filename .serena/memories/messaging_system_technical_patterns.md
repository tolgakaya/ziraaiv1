# Messaging System: Technical Patterns & Learnings

## Pattern: Analysis-Scoped Messaging

### Concept
Messages are tied to specific `PlantAnalysisId`, creating contextual conversations around each analysis rather than open-ended sponsor-farmer chats.

### Implementation
```csharp
// Every message MUST have PlantAnalysisId
public class AnalysisMessage
{
    public int PlantAnalysisId { get; set; }
    // ... other properties
}

// Validation ensures sponsor owns the analysis
if (analysis.SponsorUserId != sponsorId)
    return (false, "You can only message farmers for analyses done using your sponsorship codes");
```

### Benefits
- Natural conversation context
- Prevents spam (must have sponsored analysis)
- Easy conversation retrieval
- Clear permission boundaries

---

## Pattern: Multi-Layer Validation Pipeline

### Concept
Sequential validation with early returns, each layer checking different authorization aspects.

### Implementation
```csharp
public async Task<(bool canSend, string errorMessage)> CanSendMessageForAnalysisAsync(...)
{
    // Layer 1: Tier Check
    if (!await CanSendMessageAsync(sponsorId))
        return (false, "Tier insufficient");
    
    // Layer 2: Ownership Check
    if (analysis.SponsorUserId != sponsorId)
        return (false, "Not your analysis");
    
    // Layer 3: Access Record
    var hasAccess = await _analysisAccessRepository.GetAsync(...);
    if (hasAccess == null)
        return (false, "No access record");
    
    // Layer 4: Block Check
    if (await _blockRepository.IsBlockedAsync(farmerId, sponsorId))
        return (false, "Blocked");
    
    // Layer 5: Rate Limit
    if (!await _rateLimitService.CanSendMessageToFarmerAsync(sponsorId, farmerId))
        return (false, "Rate limit");
    
    return (true, string.Empty);
}
```

### Benefits
- Clear error messages per layer
- Easy to debug (know exactly which check failed)
- Fail-fast performance
- Easy to add/remove checks

### When to Use
- Complex authorization scenarios
- Multiple independent checks required
- Need specific error messages per failure
- Performance-critical (early returns)

---

## Pattern: Rate Limiting with Date Ranges

### Concept
Use date arithmetic to define rolling windows without storing window state.

### Implementation
```csharp
public async Task<int> GetTodayMessageCountAsync(int sponsorId, int farmerId)
{
    var today = DateTime.Now.Date;
    var tomorrow = today.AddDays(1);
    
    var messages = await _messageRepository.GetListAsync(m =>
        m.FromUserId == sponsorId &&
        m.ToUserId == farmerId &&
        m.SenderRole == "Sponsor" &&
        m.SentDate >= today &&
        m.SentDate < tomorrow  // Important: < tomorrow, not <= today
    );
    
    return messages?.Count() ?? 0;
}
```

### Key Points
- `>= today` and `< tomorrow` ensures full 24-hour window
- `DateTime.Now.Date` resets time to 00:00:00
- No need to store "window start" - it's always CURRENT_DATE
- Database indexes on `SentDate` make this fast

### Pitfall to Avoid
```csharp
// WRONG - doesn't capture all of today
m.SentDate >= today && m.SentDate <= today

// CORRECT - captures full 24 hours
m.SentDate >= today && m.SentDate < tomorrow
```

---

## Pattern: Per-Relationship Limits (Not Per-Item)

### Concept
Rate limit is between two users (sponsor-farmer pair), not per analysis.

### Why
If limit was per-analysis:
- Sponsor could send 10 messages on Analysis A
- Then 10 more on Analysis B
- Then 10 more on Analysis C
- Total: 30 messages to same farmer (defeats purpose)

### Implementation
```csharp
// Rate limit check does NOT include PlantAnalysisId
var messages = await _messageRepository.GetListAsync(m =>
    m.FromUserId == sponsorId &&
    m.ToUserId == farmerId &&  // Per farmer, not per analysis
    m.SenderRole == "Sponsor" &&
    m.SentDate >= today
);
```

### Lesson
When designing rate limits, consider:
- What resource are we protecting? (Farmer's attention)
- What scope makes sense? (Sponsor → Farmer relationship)
- Can limit be bypassed? (Multiple analyses to same farmer)

---

## Pattern: Unique Constraints for Business Rules

### Concept
Use database unique constraints to enforce "one record per relationship" rules.

### Implementation
```csharp
// In FarmerSponsorBlockEntityConfiguration
builder.HasIndex(x => new { x.FarmerId, x.SponsorId }).IsUnique();
```

### Benefits
- Database-level enforcement (no race conditions)
- No need for manual "check if exists" logic
- Automatic error on duplicate attempts
- Self-documenting schema

### When to Use
- One-to-one relationships (user profile, settings)
- One record per pair (blocks, follows, friendships)
- Prevent duplicate entries (unique codes, slugs)

---

## Pattern: First-Time Detection

### Concept
Check if this is the first message in a conversation to apply special logic (approval required).

### Implementation
```csharp
private async Task<bool> IsFirstMessageAsync(int fromUserId, int toUserId, int plantAnalysisId)
{
    var existingMessages = await _messageRepository.GetListAsync(m =>
        m.FromUserId == fromUserId &&
        m.ToUserId == toUserId &&
        m.PlantAnalysisId == plantAnalysisId
    );
    
    return existingMessages == null || !existingMessages.Any();
}

// Usage
var isFirstMessage = await IsFirstMessageAsync(fromUserId, toUserId, plantAnalysisId);
var newMessage = new AnalysisMessage
{
    IsApproved = !isFirstMessage,  // First needs approval
    ApprovedDate = !isFirstMessage ? DateTime.Now : null
};
```

### Design Decision: Scope
First message is **per analysis**, not per sponsor-farmer pair globally.

**Rationale**:
- Each analysis is a separate conversation context
- Sponsor might have legitimate reason to message on new analysis
- Admin can review per-analysis first contact

**Alternative**: First message per sponsor-farmer pair (any analysis)
- Would be one approval for entire relationship
- Less admin overhead
- Trade-off: less context-aware moderation

---

## Pattern: Reversible Actions (Block/Unblock)

### Concept
Don't delete records on "undo" operations - use boolean flags.

### Implementation
```csharp
// Block
var existingBlock = await _blockRepository.GetBlockRecordAsync(farmerId, sponsorId);
if (existingBlock != null)
{
    existingBlock.IsBlocked = true;  // Reactivate
    _repository.Update(existingBlock);
}
else
{
    var newBlock = new FarmerSponsorBlock { IsBlocked = true };
    _repository.Add(newBlock);
}

// Unblock
existingBlock.IsBlocked = false;  // Deactivate, don't delete
_repository.Update(existingBlock);
```

### Benefits
- History preserved (when was it blocked/unblocked)
- Audit trail (who blocked whom, when, why)
- Can add "blocked_count" or "last_blocked_date" analytics
- No cascading delete issues

### When to Delete vs Flag
- **Use Flags**: User actions that might be reversed (block, mute, archive)
- **Delete**: True data removal (GDPR right-to-be-forgotten, spam accounts)
- **Soft Delete**: Records with foreign keys (users, products)

---

## Pattern: Farmer Protection (Asymmetric Permissions)

### Concept
Give recipients (farmers) control over who can contact them, not senders (sponsors).

### Implementation
```csharp
// Only farmers can block
[Authorize(Roles = "Farmer,Admin")]
[HttpPost("messages/block")]
public async Task<IActionResult> BlockSponsor([FromBody] BlockSponsorCommand command)

// Sponsors CANNOT block farmers
// (No endpoint exists)
```

### Rationale
- Farmers are receiving unsolicited messages
- Sponsors are business entities (expected to handle communication professionally)
- Prevents abuse (sponsor blocking farmer to avoid accountability)

### Similar Patterns
- Users can block advertisers (but not vice versa)
- Patients can block doctors (emergency exception)
- Customers can unsubscribe from businesses

---

## Pattern: DTO Projection with Computed Fields

### Concept
Return DTOs with additional computed data for client convenience.

### Implementation
```csharp
public class BlockedSponsorDto
{
    public int SponsorId { get; set; }
    public string SponsorName { get; set; }  // Joined from Users
    public bool IsBlocked { get; set; }
    public bool IsMuted { get; set; }
    public DateTime BlockedDate { get; set; }
    public string Reason { get; set; }
}

// In handler
var blockedSponsors = blocks.Select(b => new BlockedSponsorDto
{
    SponsorId = b.SponsorId,
    SponsorName = b.Sponsor?.FullName,  // EF navigation property
    IsBlocked = b.IsBlocked,
    IsMuted = b.IsMuted,
    BlockedDate = b.CreatedDate,
    Reason = b.Reason
}).ToList();
```

### Benefits
- Client gets all needed data in one request
- No need for client to make separate "get user by ID" calls
- Reduce mobile data usage and API calls

---

## Anti-Pattern Avoided: Hard-Coded User IDs in Requests

### Problem
```csharp
// BAD - User can fake their ID
public class BlockSponsorCommand
{
    public int FarmerId { get; set; }  // From request body - user could fake this
    public int SponsorId { get; set; }
}
```

### Solution
```csharp
// GOOD - Get user ID from auth token
[HttpPost("messages/block")]
public async Task<IActionResult> BlockSponsor([FromBody] BlockSponsorCommand command)
{
    var userId = GetUserId();  // From JWT claims
    command.FarmerId = userId.Value;  // Override whatever client sent
    // ... rest of logic
}
```

### Lesson
Never trust user-provided identity claims in request bodies. Always get identity from:
1. JWT token claims
2. Session cookies
3. Other server-side auth mechanisms

---

## Pattern: Tiered Feature Access

### Concept
Use simple numeric comparison for tier-based features rather than enum switches.

### Implementation
```csharp
// Simple comparison
public async Task<bool> CanSendMessageAsync(int sponsorId)
{
    var sponsor = await _sponsorRepository.GetAsync(s => s.UserId == sponsorId);
    return sponsor?.Tier >= 3;  // L=3, XL=4 both have messaging
}

// Alternative (more verbose)
public async Task<bool> CanSendMessageAsync(int sponsorId)
{
    var sponsor = await _sponsorRepository.GetAsync(s => s.UserId == sponsorId);
    return sponsor?.Tier == Tier.L || sponsor?.Tier == Tier.XL;
}
```

### Benefits of Numeric Tiers
- Easy to add higher tiers (Tier 5, Tier 6)
- Simple comparison (`>=`, `<`, `==`)
- Clear hierarchy in database

### Trade-offs
- Less explicit (what does "3" mean?)
- Must document tier meanings
- Consider enums if tier names matter more than order

---

## Performance Pattern: Indexed Date Range Queries

### Setup
```sql
CREATE INDEX IX_AnalysisMessages_FromUserId_ToUserId_SentDate 
ON AnalysisMessages(FromUserId, ToUserId, SentDate);
```

### Query
```csharp
var messages = await _messageRepository.GetListAsync(m =>
    m.FromUserId == sponsorId &&    // Index key 1
    m.ToUserId == farmerId &&        // Index key 2
    m.SentDate >= today &&           // Index key 3 (range start)
    m.SentDate < tomorrow            // Index key 3 (range end)
);
```

### Performance Impact
- Without index: Full table scan (slow as messages grow)
- With index: Index range scan (milliseconds even with millions of messages)

### Lesson
For "count messages in time window" queries:
1. Create composite index: (sender, receiver, date)
2. Use date ranges with `>=` and `<`
3. Verify with `EXPLAIN ANALYZE`

---

## Documentation Pattern: Progressive Detail

### Structure
1. **Executive Summary**: 1-2 paragraphs
2. **Quick Start**: Common use cases
3. **Detailed Guide**: Complete information
4. **API Reference**: Technical specs
5. **Examples**: Real-world scenarios
6. **Troubleshooting**: Common issues

### Example from Our Docs
- SPONSOR_FARMER_MESSAGING_SYSTEM.md:
  - Executive Summary (page 1)
  - Architecture Overview (page 2-5)
  - Business Rules (page 6-10)
  - API Reference (page 11-20)
  - Database Schema (page 21-25)
  - Examples (page 26-30)

### Benefits
- Executives can read first page only
- Developers can dive deep as needed
- Reference material easy to find
- Examples show "how to actually use it"

---

## Testing Pattern: Test Data Creation

### Structure
```sql
-- 1. Create test users
INSERT INTO Users ...

-- 2. Create relationships (sponsor profiles)
INSERT INTO SponsorProfiles ...

-- 3. Create test data (analyses)
INSERT INTO PlantAnalyses ...

-- 4. Verify test data
SELECT COUNT(*) FROM Users WHERE Email LIKE 'test%';
```

### Cleanup
```sql
-- Delete in reverse dependency order
DELETE FROM AnalysisMessages WHERE ...;
DELETE FROM PlantAnalyses WHERE ...;
DELETE FROM SponsorProfiles WHERE ...;
DELETE FROM Users WHERE Email LIKE 'test%';
```

### Lesson
- Create test data scripts BEFORE writing tests
- Include cleanup scripts to reset state
- Use identifiable prefixes (test_*, temp_*)
- Document test data in test docs

---

## Error Handling Pattern: Specific Error Messages

### Concept
Return specific error messages for each validation failure, not generic "forbidden".

### Implementation
```csharp
// GOOD - Specific errors
if (tier < 3) return "Messaging is only available for L and XL tier sponsors";
if (analysis.SponsorUserId != sponsorId) return "You can only message farmers for analyses done using your sponsorship codes";
if (isBlocked) return "This farmer has blocked messages from you";
if (rateLimitExceeded) return "Daily message limit reached (10 messages per day per farmer)";

// BAD - Generic error
if (!canSend) return "Permission denied";
```

### Benefits
- Client knows why request failed
- Easier debugging
- Better user experience (can explain to user)
- Faster issue resolution (no guessing)

### When to Be Generic
Security-sensitive operations:
```csharp
// Good - Don't reveal if email exists
return "Invalid email or password";  // Not "Email not found" or "Wrong password"
```

---

## Migration Pattern: Incremental Feature Rollout

### Phase 1: Database & Backend (This Session)
- ✅ Entity, repository, services
- ✅ API endpoints
- ✅ Validation logic
- ✅ Documentation

### Phase 2: Testing & Refinement (Next Session)
- Database migration
- API testing
- Bug fixes
- Admin approval endpoint

### Phase 3: Mobile Integration (Future)
- Flutter UI implementation
- WebSocket real-time delivery
- Push notifications
- Offline support

### Benefits
- Each phase is shippable
- Can test backend before mobile work
- Easier to debug (smaller changes)
- Team can parallelize work

---

## Lessons: Property Naming Consistency

### Issue We Hit
```csharp
// We wrote:
if (analysis.SponsorCompanyId != sponsorProfile.SponsorCompanyId)

// But actual property:
if (analysis.SponsorUserId != sponsorId)
```

### Prevention
1. **Read entity definitions first** before writing validation
2. **Use IDE autocomplete** to avoid typos
3. **Check similar code** for naming patterns
4. **Unit test** property access

### Pattern in This Codebase
- PlantAnalysis has `SponsorUserId` (direct reference to Users table)
- SponsorshipCode has `SponsorCompanyId` (reference to SponsorProfiles table)
- Different naming conventions for different relationships

---

## Summary: Key Takeaways

1. **Multi-layer validation** with specific error messages improves UX and debugging
2. **Rate limiting per relationship** (not per item) prevents bypass
3. **Database constraints** enforce business rules at lowest level
4. **Boolean flags** for reversible actions preserve history
5. **Asymmetric permissions** protect vulnerable party (farmers)
6. **Identity from auth tokens** never from request bodies
7. **Indexed date ranges** make time-window queries fast
8. **Progressive documentation** serves all audiences
9. **Incremental rollout** reduces risk and enables parallel work
10. **Property naming consistency** requires careful attention to existing patterns
