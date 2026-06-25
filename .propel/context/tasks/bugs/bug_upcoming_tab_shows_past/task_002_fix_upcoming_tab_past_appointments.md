---
post_title: "BUG_002 - Past Appointments Showing in Upcoming Tab"
author1: "AI Senior Developer"
post_slug: "bug-002-fix-upcoming-tab-past-appointments"
categories: "Healthcare, Bug Fix"
tags: "BUG-002, scheduling, frontend, React, PatientDashboardPage"
ai_note: "Generated from bug report: past appointments visible in Upcoming tab of appointment grid"
summary: "The Upcoming tab in the patient appointment grid filters appointments by status only (Scheduled, Confirmed, Arrived, InProgress), with no check on appointmentDateTime. Appointments whose time has already passed but whose status was never updated still appear as Upcoming."
post_date: "2026-06-24"
---

# Bug Fix Task - BUG_002

## Bug Report Reference
- **Bug ID**: BUG-002
- **Source**: Manual observation — past appointments visible in Upcoming tab
- **Reported**: 2026-06-24

---

## Bug Summary

### Issue Classification
- **Priority**: High
- **Severity**: Incorrect data displayed to patients — causes confusion and erodes trust
- **Affected Version**: Current (frontend main branch)
- **Environment**: Web browser, all platforms, Development environment

### Steps to Reproduce
1. Log in as a patient who has appointments with status `Scheduled` or `Confirmed` that are dated in the past
2. Navigate to the Patient Dashboard
3. View the **Upcoming** tab in the appointments grid
4. **Expected**: Only appointments with a future `appointmentDateTime` should appear
5. **Actual**: Appointments with past `appointmentDateTime` but still-active statuses (`Scheduled`, `Confirmed`, etc.) appear alongside genuine upcoming appointments

**Error Output**:
```text
No runtime error. The Upcoming tab renders appointments whose appointmentDateTime
is already in the past, e.g.:
  Status: Scheduled
  Date:   June 20, 2026 · 9:00 AM   ← already passed
```

### Root Cause Analysis
- **File**: `frontend/src/features/scheduling/pages/PatientDashboardPage.tsx` — lines 117–124
- **Component**: Patient Dashboard / Appointment Tabs
- **Function**: `upcoming` and `past` `useMemo` filters
- **Cause**: Both memos classify appointments by `status` membership in `ACTIVE_STATUSES` alone:
  ```ts
  // Current (buggy)
  const upcoming = useMemo(
    () => appointments.filter((a) => ACTIVE_STATUSES.has(a.status)),
    [appointments],
  );

  const past = useMemo(
    () => appointments.filter((a) => !ACTIVE_STATUSES.has(a.status)),
    [appointments],
  );
  ```
  There is no check that `a.appointmentDateTime` is in the future. An appointment with
  status `Scheduled` whose date has already passed is still classified as "Upcoming".

### Impact Assessment
- **Affected Features**: Patient Dashboard → appointment grid → Upcoming tab
- **User Impact**: Patients see stale/past appointments in their Upcoming list; the count badge is inflated
- **Data Integrity Risk**: None — display-only issue, no data is mutated
- **Security Implications**: None

---

## Fix Overview

Add a `appointmentDateTime >= now` guard to the `upcoming` filter so only future
appointments are shown. The `past` filter should include any appointment that is either
non-active in status **or** whose time has already passed.

---

## Fix Dependencies
- No external dependencies
- No backend changes required (frontend-only fix)

---

## Impacted Components

### Frontend (React / TypeScript)
- `frontend/src/features/scheduling/pages/PatientDashboardPage.tsx` — MODIFY

---

## Expected Changes

| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | `frontend/src/features/scheduling/pages/PatientDashboardPage.tsx` | Add `new Date(a.appointmentDateTime) >= now` to `upcoming` filter; update `past` filter accordingly |

---

## Implementation Plan

**`PatientDashboardPage.tsx`** — update both `useMemo` filters:

```ts
// Before
const upcoming = useMemo(
  () => appointments.filter((a) => ACTIVE_STATUSES.has(a.status)),
  [appointments],
);

const past = useMemo(
  () => appointments.filter((a) => !ACTIVE_STATUSES.has(a.status)),
  [appointments],
);

// After
const upcoming = useMemo(() => {
  const now = new Date();
  return appointments.filter(
    (a) =>
      ACTIVE_STATUSES.has(a.status) &&
      new Date(a.appointmentDateTime) >= now,
  );
}, [appointments]);

const past = useMemo(() => {
  const now = new Date();
  return appointments.filter(
    (a) =>
      !ACTIVE_STATUSES.has(a.status) ||
      new Date(a.appointmentDateTime) < now,
  );
}, [appointments]);
```

> **Note on timezone**: `appointmentDateTime` is an ISO string. If the backend returns it
> without a `Z` suffix (Kind = Unspecified), `new Date(a.appointmentDateTime)` will be
> parsed as local time by the browser — consistent with how `formatDateTime` already
> renders it. No `+ 'Z'` correction is needed here since both sides (display and filter)
> should use the same interpretation.

---

## Regression Prevention Strategy
- [ ] Unit test: `upcoming` returns empty when all active-status appointments are in the past
- [ ] Unit test: `upcoming` returns only future active-status appointments when mixed data provided
- [ ] Unit test: `past` includes past-dated active-status appointments (e.g. forgotten Scheduled)
- [ ] Unit test: `past` includes non-active-status appointments regardless of date

---

## Rollback Procedure
1. Revert both `useMemo` filters in `PatientDashboardPage.tsx` to the status-only check
2. Verify appointments render again (regression restores the bug but unblocks the dashboard)

---

## External References
- `schedulingApi.ts` — `MyAppointment.appointmentDateTime` is an ISO string field (line 122)
- `ACTIVE_STATUSES` set — `Scheduled`, `Confirmed`, `Arrived`, `InProgress` (line 40–45)
- `formatDateTime` — renders `appointmentDateTime` as local time (line 44–53)

---

## Build Commands
```bash
# Frontend (from /frontend)
npm run dev        # start dev server
npm run build      # production build
npm run lint       # ESLint check
```

---

## Implementation Validation Strategy
- [x] Upcoming tab shows only appointments with a future `appointmentDateTime`
- [x] Past-dated `Scheduled`/`Confirmed` appointments appear in the Past tab
- [x] Upcoming count badge reflects the correct future-only count
- [x] Completed/Cancelled appointments remain in the Past tab

---

## Implementation Checklist
- [x] Modify `PatientDashboardPage.tsx` — add `new Date(a.appointmentDateTime) >= now` to `upcoming` filter
- [x] Modify `PatientDashboardPage.tsx` — update `past` filter to include past-dated active-status entries
- [x] Verify Upcoming count badge is correct after fix
- [x] Verify Past tab still shows completed/cancelled appointments
