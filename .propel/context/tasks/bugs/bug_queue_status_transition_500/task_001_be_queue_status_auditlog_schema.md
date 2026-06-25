---
post_title: "BUG-002 - Queue Status Transitions Fail with HTTP 500 (Start / Mark No-Show / Mark Left)"
author1: "AI Senior Developer"
post_slug: "bug-002-queue-status-transition-500"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Bug Fix"
tags: "BUG-002, EP-003, US-022, SCR-020, backend, EF Core, SQL, audit-log, queue"
ai_note: "Generated with AI assistance — identified during Queue Management screen testing"
summary: "Clicking Start, Mark No-Show, or Mark Left on any queue entry returns HTTP 500. Root cause is a schema mismatch: the AuditLog entity defines IsArchived and ArchivedAt columns that do not exist in the scheduling.AuditLogs and clinical.AuditLogs database tables."
post_date: "2026-06-23"
---

# Bug Fix Task - [BUG-002]

## Bug Report Reference
- Bug ID: BUG-002
- Source: Queue Management screen (SCR-020) — manual testing of status transition actions

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Blocker — all write operations on the queue are non-functional; staff cannot advance patient status
- **Affected Version**: Current HEAD
- **Environment**: Web (all browsers), ASP.NET Core 8 backend, SQL Server LocalDB

### Steps to Reproduce
1. Log in as a staff user (`admin@upap.com` / `Admin@123`)
2. Navigate to Queue Management (`/staff/queue`)
3. Perform any of the following actions on a **Waiting** entry:
   - Click the **Start** button (Waiting → InProgress)
   - Click **⋯ → Mark No-Show** (Waiting → NoShow)
   - Click **⋯ → Mark Left** (Waiting → Left / Cancelled)
4. **Expected**: Status updates successfully; row reflects new status in real time
5. **Actual**: HTTP 500 Internal Server Error is returned; toast shows an error message; status reverts due to optimistic update rollback

**Error Output**:
```text
HTTP 500 Internal Server Error

Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes.
 ---> Microsoft.Data.SqlClient.SqlException (0x80131904):
       Invalid column name 'ArchivedAt'.
       Invalid column name 'IsArchived'.
```

### Root Cause Analysis

The `AuditLog` entity (`SharedKernel/Domain/AuditLog.cs`) was extended with two new columns for audit retention (DR-011 compliance):

```csharp
// backend/src/Shared/SharedKernel/Domain/AuditLog.cs
public bool IsArchived { get; set; }          // line 24
public DateTime? ArchivedAt { get; set; }     // line 29
```

These properties were added to the entity but **no EF Core migration was run**, so the `AuditLogs` tables in the database were never updated. When any queue status change is saved, EF Core's `QueueService.UpdateStatusAsync` writes an `AuditLog` record, and the INSERT statement includes the two unmapped columns, causing SQL Server to reject it.

**Affected database tables** (both missing `IsArchived` and `ArchivedAt`):
- `scheduling.AuditLogs`
- `clinical.AuditLogs`

**Affected DbContexts**:
- `Scheduling.Infrastructure/Data/SchedulingDbContext.cs`
- `Clinical.Infrastructure/Data/ClinicalDbContext.cs`

**Code path (Start / Mark No-Show / Mark Left)**:
```
QueueController.UpdateStatus (PUT /api/scheduling/queue/{id}/status)
  └─ QueueService.UpdateStatusAsync
       └─ _dbContext.SaveChangesAsync     ← fails here
            └─ EF Core INSERT into scheduling.AuditLogs (includes IsArchived, ArchivedAt)
                 └─ SqlException: Invalid column name 'IsArchived' / 'ArchivedAt'
```

### Impact Assessment
- **Affected Features**:
  - Queue Management → Start (Waiting → InProgress)
  - Queue Management → Mark No-Show (`⋯` menu)
  - Queue Management → Mark Left (`⋯` menu)
  - Queue Management → Complete (InProgress → Completed) — same code path, also fails
  - Any clinical action that writes to `clinical.AuditLogs`
- **User Impact**: Staff cannot update any patient status in the queue. The entire queue workflow is blocked. All status-transition buttons silently fail and show a generic error toast.
- **Data Integrity Risk**: No data corruption — the transaction is rolled back on failure
- **Security Implications**: None

## Fix Overview

Two acceptable fixes:

### Option A — Add missing columns to the database (recommended, non-breaking)
Run ALTER TABLE on both `AuditLogs` tables to add the missing columns with defaults matching the entity:

```sql
-- scheduling schema
ALTER TABLE [scheduling].[AuditLogs]
  ADD [IsArchived] BIT NOT NULL DEFAULT 0,
      [ArchivedAt] DATETIME2 NULL;

-- clinical schema
ALTER TABLE [clinical].[AuditLogs]
  ADD [IsArchived] BIT NOT NULL DEFAULT 0,
      [ArchivedAt] DATETIME2 NULL;
```

### Option B — Remove properties from entity (if archival feature is not yet needed)
Remove `IsArchived` and `ArchivedAt` from `AuditLog.cs` (and `RetentionPolicyService.cs` references) until a proper migration is implemented.

> **Recommendation**: Option A — the columns are already referenced in `RetentionPolicyService.cs` (lines 119, 120, 156, 157) so the archival feature is partially implemented. Adding the columns is safer than removing entity properties.

## Fix Dependencies
- `RetentionPolicyService.cs` sets `log.IsArchived` and `log.ArchivedAt` — the columns must exist for that service to function
- No frontend changes required

## Impacted Components
### Backend — Database
- `scheduling.AuditLogs` — ADD columns `IsArchived BIT NOT NULL DEFAULT 0`, `ArchivedAt DATETIME2 NULL`
- `clinical.AuditLogs` — ADD columns `IsArchived BIT NOT NULL DEFAULT 0`, `ArchivedAt DATETIME2 NULL`

### Backend — EF Core (if Option B chosen)
- `backend/src/Shared/SharedKernel/Domain/AuditLog.cs` — REMOVE `IsArchived` and `ArchivedAt` properties
- `backend/src/Shared/SharedKernel/Services/RetentionPolicyService.cs` — REMOVE references to removed properties

## Expected Outcome After Fix
- `PUT /api/scheduling/queue/{id}/status` returns HTTP 200 with the updated queue entry
- Start, Mark No-Show, Mark Left, and Complete buttons all function correctly
- No HTTP 500 errors on any queue status transition
- `AuditLogs` table correctly records all status change events

## Verification Steps
```powershell
# Login and get a waiting entry
$login = Invoke-RestMethod -Uri "http://localhost:3000/api/auth/login" `
    -Method POST -ContentType "application/json" `
    -Body '{"email":"admin@upap.com","password":"Admin@123"}'
$token = $login.accessToken
$queue = Invoke-RestMethod -Uri "http://localhost:3000/api/scheduling/queue" `
    -Method GET -Headers @{Authorization="Bearer $token"}
$entry = $queue.entries | Where-Object { $_.status -eq "Waiting" } | Select-Object -First 1

# Attempt Start (Waiting -> InProgress) — should return HTTP 200
$body = @{ NewStatus = "InProgress"; RowVersion = $entry.rowVersion } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:3000/api/scheduling/queue/$($entry.id)/status" `
    -Method PUT -ContentType "application/json" -Body $body `
    -Headers @{Authorization="Bearer $token"}
```
