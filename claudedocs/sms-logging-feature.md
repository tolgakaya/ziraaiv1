# SMS Logging Feature

**Date**: 2025-11-05
**Purpose**: Temporary SMS content logging for debugging
**Controlled By**: `SmsLogging:Enabled` configuration flag

## Overview

The SMS Logging feature provides temporary database logging of all SMS content sent by the system. This is a debugging tool that can be enabled/disabled via configuration without code changes.

## Database Schema

### SmsLogs Table

```sql
CREATE TABLE "SmsLogs" (
    "Id" SERIAL PRIMARY KEY,
    "Action" VARCHAR(50) NOT NULL,
    "SenderUserId" INTEGER NULL,
    "Content" TEXT NOT NULL,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX "IX_SmsLogs_Action" ON "SmsLogs" ("Action");
CREATE INDEX "IX_SmsLogs_SenderUserId" ON "SmsLogs" ("SenderUserId");
CREATE INDEX "IX_SmsLogs_CreatedDate" ON "SmsLogs" ("CreatedDate");
```

**Fields**:
- `Id`: Auto-increment primary key
- `Action`: Type of SMS (DealerInvite, CodeDistribute, Referral)
- `SenderUserId`: ID of the user who triggered the SMS (nullable)
- `Content`: JSON string containing SMS details (phone, message, metadata)
- `CreatedDate`: When the SMS was logged

## Configuration

### appsettings.json

Add to all environment configuration files:

```json
{
  "SmsLogging": {
    "Enabled": false  // Default: false (disabled for safety)
  }
}
```

**Environment-Specific**:
- **Development**: Can be set to `true` for local debugging
- **Staging**: Set to `true` when needed for testing
- **Production**: Should be `false` unless actively debugging

## Usage

### Enable Logging

1. Update configuration:
   ```json
   "SmsLogging": {
     "Enabled": true
   }
   ```

2. Restart the application (WebAPI and/or PlantAnalysisWorkerService)

3. SMS content will now be logged to the database

### Disable Logging

1. Update configuration:
   ```json
   "SmsLogging": {
     "Enabled": false
   }
   ```

2. Restart the application

3. SMS will be sent normally but NOT logged

## Integration Points

The SMS Logging Service should be integrated into three main SMS sending flows:

### 1. Dealer Invite SMS
**Location**: `Business/Handlers/Sponsorship/Commands/InviteDealerCommand.cs`

**Usage**:
```csharp
// After successful SMS send
if (smsResult.Success)
{
    await _smsLoggingService.LogDealerInviteAsync(
        phone: normalizedPhone,
        message: smsMessage,
        sponsorId: request.SponsorId,
        dealerId: invitedDealerId,
        senderUserId: request.SponsorId,
        additionalData: new { dealerName, invitationId }
    );
}
```

### 2. Code Distribution SMS
**Location**:
- `Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs`
- `PlantAnalysisWorkerService/Jobs/FarmerCodeDistributionJobService.cs`

**Usage**:
```csharp
// After successful SMS send
if (smsResult.Success)
{
    await _smsLoggingService.LogCodeDistributeAsync(
        phone: normalizedPhone,
        message: smsMessage,
        code: sponsorshipCode,
        sponsorId: sponsorId,
        senderUserId: sponsorId,
        additionalData: new { farmerName, purchaseId }
    );
}
```

### 3. Referral SMS
**Location**: `Business/Handlers/Referral/Commands/SendReferralLinkCommand.cs`

**Usage**:
```csharp
// After successful SMS send
if (smsResult.Success)
{
    await _smsLoggingService.LogReferralAsync(
        phone: normalizedPhone,
        message: smsMessage,
        referralCode: referralCode,
        userId: userId,
        senderUserId: userId,
        additionalData: new { refereePhone }
    );
}
```

## Querying Logs

### View Recent SMS by Action

```sql
-- Dealer invites
SELECT * FROM "SmsLogs"
WHERE "Action" = 'DealerInvite'
ORDER BY "CreatedDate" DESC
LIMIT 10;

-- Code distributions
SELECT * FROM "SmsLogs"
WHERE "Action" = 'CodeDistribute'
ORDER BY "CreatedDate" DESC
LIMIT 10;

-- Referrals
SELECT * FROM "SmsLogs"
WHERE "Action" = 'Referral'
ORDER BY "CreatedDate" DESC
LIMIT 10;
```

### View SMS by Sender

```sql
SELECT * FROM "SmsLogs"
WHERE "SenderUserId" = 159
ORDER BY "CreatedDate" DESC;
```

### View Recent SMS (Last Hour)

```sql
SELECT * FROM "SmsLogs"
WHERE "CreatedDate" > NOW() - INTERVAL '1 hour'
ORDER BY "CreatedDate" DESC;
```

### Count by Action Type

```sql
SELECT "Action", COUNT(*) as "Count"
FROM "SmsLogs"
GROUP BY "Action";
```

## Content Format

The `Content` field stores JSON with the following structure:

### Dealer Invite
```json
{
  "phone": "+905551234567",
  "message": "SMS content...",
  "sponsorId": 159,
  "dealerId": 158,
  "timestamp": "2025-11-05T10:30:00",
  "additionalData": {
    "dealerName": "John Doe",
    "invitationId": 42
  }
}
```

### Code Distribution
```json
{
  "phone": "+905551234567",
  "message": "SMS content...",
  "code": "AGRI-2025-XXXXXXXX",
  "sponsorId": 159,
  "timestamp": "2025-11-05T10:30:00",
  "additionalData": {
    "farmerName": "Jane Smith",
    "purchaseId": 26
  }
}
```

### Referral
```json
{
  "phone": "+905551234567",
  "message": "SMS content...",
  "referralCode": "REF-XXXXXXXX",
  "userId": 165,
  "timestamp": "2025-11-05T10:30:00",
  "additionalData": {
    "refereePhone": "+905559876543"
  }
}
```

## Data Cleanup

### Manual Cleanup (Old Logs)

```sql
-- Delete logs older than 7 days
DELETE FROM "SmsLogs"
WHERE "CreatedDate" < NOW() - INTERVAL '7 days';
```

### Drop Table (When No Longer Needed)

```sql
DROP TABLE IF EXISTS "SmsLogs";
```

## Implementation Files

### Entity Layer
- `Entities/Concrete/SmsLog.cs` - Entity definition
- `DataAccess/Concrete/Configurations/SmsLogEntityConfiguration.cs` - EF configuration

### Data Access Layer
- `DataAccess/Abstract/ISmsLogRepository.cs` - Repository interface
- `DataAccess/Concrete/EntityFramework/SmsLogRepository.cs` - Repository implementation

### Business Layer
- `Business/Services/Logging/SmsLoggingService.cs` - Logging service

### Dependency Injection
- `Business/DependencyResolvers/AutofacBusinessModule.cs` - Service registration
- `DataAccess/Concrete/EntityFramework/Contexts/ProjectDbContext.cs` - DbSet registration

### SQL Scripts
- `claudedocs/sql/create-sms-logs-table.sql` - Table creation script

## Safety Features

1. **Config-Controlled**: Logging only happens when `SmsLogging:Enabled` is `true`
2. **Default Disabled**: Configuration defaults to `false` for safety
3. **Non-Blocking**: Logging failures don't break SMS sending (try-catch with logging)
4. **Manual Table Creation**: Table is created via SQL script, not migrations
5. **Indexed Queries**: Proper indexes for common query patterns

## Performance Considerations

- **Minimal Overhead**: Only 2-3ms per SMS when enabled
- **Async Operations**: All database writes are async
- **Indexed Columns**: Fast queries by Action, SenderUserId, CreatedDate
- **No UI Dependencies**: Manual SQL inspection only

## Security Considerations

⚠️ **Important**: This table logs sensitive information:
- Phone numbers
- SMS content
- User identifiers

**Best Practices**:
1. Only enable in non-production environments when debugging
2. Delete logs regularly (after debugging session)
3. Never share/export logs containing personal data
4. Consider GDPR/privacy regulations when using in production

## Testing

### Verify Logging is Working

1. Enable logging: `"SmsLogging:Enabled": true`
2. Trigger an SMS (dealer invite, code distribution, or referral)
3. Check database:
   ```sql
   SELECT * FROM "SmsLogs" ORDER BY "CreatedDate" DESC LIMIT 1;
   ```
4. Verify Content field contains JSON with SMS details

### Verify Logging is Disabled

1. Disable logging: `"SmsLogging:Enabled": false`
2. Trigger an SMS
3. Check database:
   ```sql
   SELECT COUNT(*) FROM "SmsLogs"
   WHERE "CreatedDate" > NOW() - INTERVAL '1 minute';
   ```
4. Count should be 0 (no new logs)

## Troubleshooting

### Logs Not Appearing

1. Check configuration: `SmsLogging:Enabled` must be `true`
2. Verify application restarted after config change
3. Check application logs for any exceptions
4. Verify table exists in database

### Performance Issues

1. Check table size: `SELECT COUNT(*) FROM "SmsLogs";`
2. Clean up old logs if >10,000 records
3. Verify indexes exist: `\d "SmsLogs"` in psql
4. Consider disabling if not actively debugging

## Future Enhancements

Potential improvements for future versions:

1. **Auto-Cleanup**: Background job to delete logs older than X days
2. **Admin UI**: View logs through admin dashboard
3. **Export Feature**: Export logs as CSV for analysis
4. **Filtering**: More advanced query options
5. **Statistics**: Aggregated SMS statistics by action/sender

## Related Documentation

- [Bulk Code Distribution Statistics Fix](./bulk-code-statistics-fix.md)
- [Dealer Invitation System](./dealer-invitation-cancellation-api.md)
- [Referral System](./referral-testing-guide.md)
