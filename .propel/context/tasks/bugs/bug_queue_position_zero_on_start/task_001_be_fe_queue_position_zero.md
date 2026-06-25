---
post_title: "BUG-003 - Queue Row Position Shows 0 After Clicking Start (or Any Status Transition)"
author1: "AI Senior Developer"
post_slug: "bug-003-queue-position-zero-on-start"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Bug Fix"
tags: "BUG-003, EP-003, US-022, SCR-020, backend, frontend, queue, position"
ai_note: "Generated with AI assistance — identified during Queue Management screen testing"
summary: "Clicking Start (or any status transition button) on a queue entry causes the queue position number (# column) to display 0 for that row. Root cause: QueueService.UpdateStatusAsync and MarkArrivedAsync both hardcode position = 0 in the returned DTO; the frontend replaces the entry with the server response, overwriting the correct position."
post_date: "2026-06-23"
---

# Bug Fix Task - [BUG-003]

## Bug Report Reference
- Bug ID: BUG-003
- Source: Queue Management screen (SCR-020) — manual testing of Start button

## Bug Summary

### Issue Classification
- **Priority**: Medium
- **Severity**: Visual defect — queue position number renders as `0` after any status change; staff lose track of appointment order
- **Affected Version**: Current HEAD
- **Environment**: Web (all browsers), ASP.NET Core 8 backend + React frontend

### Steps to Reproduce
1. Log in as a staff user (`admin@upap.com` / `Admin@123`)
2. Navigate to Queue Management (`/staff/queue`)
3. Confirm queue entries show correct sequential position numbers in the `#` column (e.g., 1, 2, 3, 4, 5 …)
4. Click **Start** on any **Waiting** entry (transitions it to **In Progress**)
5. **Expected**: The `#` column for that row retains its original sequential position number
6. **Actual**: The `#` column for that row changes to **0**

The same defect occurs for all status-transition actions that go through the `PUT /api/scheduling/queue/{id}/status` endpoint:
- **Start** (Waiting → InProgress)
- **Complete** (InProgress → Completed)
- **Mark No-Show** (any → NoShow)
- **Mark Left** (any → Left/Cancelled)
- **Mark Arrival** (Scheduled → Waiting) via `POST /api/scheduling/queue/{id}/arrived`

**Screenshot evidence**: After clicking Start on rows 3 (Robert Kim) and 4 (Lisa Chen), both rows displayed `0` in the `#` column while rows for Waiting entries continued to show correct positions (5, 6, 7…).

**Error Output**:
```text
No runtime error or console error.
Visual defect only — position renders as 0.
```

### Root Cause Analysis

**Backend** — `QueueService.cs` hardcodes `position = 0` in two methods:

```csharp
// backend/src/Modules/Scheduling/Scheduling.Application/Services/QueueService.cs

// UpdateStatusAsync — line 181
var dto = new QueueEntryDto(
    appointment.Id,
    ...
    0, // position recalculated on full queue fetch   ← hardcoded 0
    Convert.ToBase64String(appointment.RowVersion));

// MarkArrivedAsync — line 264
var dto = new QueueEntryDto(
    appointment.Id,
    ...
    0, // position recalculated on full queue fetch   ← hardcoded 0
    Convert.ToBase64String(appointment.RowVersion));
```

The comment `// position recalculated on full queue fetch` correctly identifies the intent — position is only meaningful in the context of the full list — but neither method acts on that intent.

**Frontend** — `QueueManagementPage.tsx` overwrites the existing entry (which has the correct position from the last full fetch) with the server-returned DTO (which carries `position = 0`):

```tsx
// frontend/src/features/scheduling/pages/QueueManagementPage.tsx (handleTransition)
if (result.success) {
  // Replace with server-confirmed entry (updated rowVersion)
  setEntries((prev) =>
    prev.map((e) => (e.id === entryId ? result.data : e)),  // ← replaces position with 0
  );
}
```

### Impact Assessment
- **Affected Features**: All status-transition actions in Queue Management
- **User Impact**: After any status change, the row's position shows `0` instead of the correct queue number. Staff cannot rely on the `#` column to determine patient order. Positions only recover after a manual page refresh.
- **Data Integrity Risk**: No — positions are display-only and not stored; actual appointment data is unaffected
- **Security Implications**: None

## Fix Overview

### Option A — Frontend fix (minimal, recommended)
Preserve the existing `position` from the local state when applying the server-confirmed update. Since position is a display-computed value that doesn't change from a single-entry status update, the correct position is already in the optimistic state:

```tsx
// frontend/src/features/scheduling/pages/QueueManagementPage.tsx
if (result.success) {
  setEntries((prev) =>
    prev.map((e) =>
      e.id === entryId
        ? { ...result.data, position: e.position }  // preserve existing position
        : e,
    ),
  );
}
```

### Option B — Backend fix
Calculate the actual position inside `UpdateStatusAsync` and `MarkArrivedAsync` by querying the today's appointment order:

```csharp
// After saving, calculate correct position
var todayAppointments = await _dbContext.Appointments
    .Where(a => a.AppointmentDateTime >= today && a.AppointmentDateTime < tomorrow
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.Rescheduled
                && !a.IsDeleted)
    .OrderBy(a => a.AppointmentDateTime)
    .Select(a => a.Id)
    .ToListAsync(cancellationToken);
var position = todayAppointments.IndexOf(appointmentId) + 1;
```

This adds an extra DB query per status change.

> **Recommendation**: Option A — single-line frontend fix, no performance cost, no extra query.

## Fix Dependencies
- No other components need updating for Option A
- `QueuePatientRow.tsx` already reads `entry.position` and renders it in the `#` cell — no change needed there

## Impacted Components

### Frontend — React
| File | Change |
|------|--------|
| `frontend/src/features/scheduling/pages/QueueManagementPage.tsx` | MODIFY `handleTransition` — spread `result.data` but preserve `e.position` |

### Backend — C# (Option B only)
| File | Change |
|------|--------|
| `backend/src/Modules/Scheduling/Scheduling.Application/Services/QueueService.cs` | MODIFY `UpdateStatusAsync` (line 181) and `MarkArrivedAsync` (line 264) — compute real position |

## Verification Steps
1. Load `/staff/queue` — confirm rows show sequential positions (1, 2, 3, 4 …)
2. Click **Start** on a Waiting entry
3. Confirm the row's `#` column still shows its original sequential position (not 0)
4. Repeat for **Mark No-Show**, **Mark Left**, **Complete**, and **Mark Arrival**
5. Confirm positions remain correct without a page refresh
