---
post_title: "BUG_005 - Past Appointments Display Active Status Badge"
author1: "AI Senior Developer"
post_slug: "bug-005-fix-past-appointment-status"
categories: "Healthcare, Bug Fix"
tags: "BUG-005, scheduling, frontend, React, PatientDashboardPage, badge, status"
ai_note: "Generated from bug report: past appointments show Scheduled/Confirmed status badge"
summary: "Appointments whose date has passed but whose DB status is still 'Scheduled' or 'Confirmed' display a blue active-state badge in the Past tab, misleading the patient into thinking the appointment is still active."
post_date: "2026-06-25"
---

# Bug Fix Task - BUG_005

## Bug Report Reference
- **Bug ID**: BUG-005
- **Source**: Manual observation — Past tab shows "Scheduled" / "Confirmed" badge on elapsed appointments
- **Reported**: 2026-06-25

---

## Bug Summary

### Issue Classification
- **Priority**: Medium
- **Severity**: UX / data accuracy — patients see misleading appointment status
- **Affected Version**: Current (frontend main branch)
- **Environment**: Web browser, all platforms, Development environment

### Steps to Reproduce
1. Log in as a patient who has one or more past appointments that were never explicitly marked Completed/Cancelled by staff
2. Navigate to the **Patient Dashboard** (`/dashboard`)
3. Click the **Past** tab in the Appointments section
4. Observe the **Status** badge on appointments whose `appointmentDateTime` is before now but whose API-returned `status` is still `"Scheduled"` or `"Confirmed"`
5. **Expected**: Past appointments should display a neutral "Completed" or "Past" badge indicating the appointment has elapsed
6. **Actual**: The badge reads "Scheduled" or "Confirmed" in the primary blue colour, identical to an upcoming active appointment

**Error Output**:
```text
No runtime error. Visual only — badge shows:
  Status: Scheduled   ← blue "default" variant, appointment date was yesterday
```

### Root Cause Analysis
- **File**: `frontend/src/features/scheduling/pages/PatientDashboardPage.tsx`
- **Component**: `PatientDashboardPage` — appointment table badge rendering
- **Functions**: `badgeVariant()` and the appointment table `<Badge>` render
- **Cause**:
  The `past` filter correctly moves elapsed ACTIVE_STATUS appointments to the Past tab:
  ```ts
  const past = appointments.filter(
    (a) => !ACTIVE_STATUSES.has(a.status) || new Date(a.appointmentDateTime) < now,
  );
  ```
  However, the displayed `<Badge>` still renders the raw API `status` string with `badgeVariant()`.
  For appointments in `ACTIVE_STATUSES` whose date has passed, `badgeVariant()` returns `'default'`
  (blue), and the badge text reads "Scheduled" or "Confirmed" — an active-looking label on a past appointment.

### Impact Assessment
- **Affected Features**: Patient Dashboard → Past appointments tab
- **User Impact**: Patients cannot tell at a glance whether a past appointment was completed, cancelled, or simply elapsed without a status update from staff
- **Data Integrity Risk**: None — display-only issue; underlying data is unchanged
- **Security Implications**: None

---

## Fix Overview

When rendering a badge for an appointment that lives in the **Past** tab and whose status is still in `ACTIVE_STATUSES`, display "Completed" with a neutral badge variant instead of the raw active status. No backend change is needed — the override is purely presentational.

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
| MODIFY | `frontend/src/features/scheduling/pages/PatientDashboardPage.tsx` | Derive a `displayStatus` for each row that maps elapsed ACTIVE_STATUS appointments to "Completed"; update `badgeVariant` to handle "completed" with a neutral style |

---

## Implementation Plan

1. **Add a `displayStatus` helper** (or inline logic) that returns `"Completed"` when the appointment is in the past tab and its status is an active status:
   ```ts
   function resolveDisplayStatus(appt: MyAppointment, isInPastTab: boolean): string {
     if (isInPastTab && ACTIVE_STATUSES.has(appt.status)) {
       return 'Completed';
     }
     return appt.status;
   }
   ```

2. **Update `badgeVariant`** to handle `"completed"`:
   ```ts
   case 'completed':
     return 'secondary';   // neutral grey
   ```

3. **Use `resolveDisplayStatus`** in the appointment table row:
   ```tsx
   const displayStatus = resolveDisplayStatus(appt, activeTab === 'past');
   <Badge variant={badgeVariant(displayStatus)}>
     {displayStatus}
   </Badge>
   ```

4. Verify the Past tab shows "Completed" (grey) for elapsed active appointments, and still shows "Cancelled" / "NoShow" (destructive) for explicitly terminated ones.

---

## Regression Prevention Strategy
- [ ] Unit test: `resolveDisplayStatus` returns "Completed" for `status="Scheduled"` when `isInPastTab=true`
- [ ] Unit test: `resolveDisplayStatus` returns original status for `status="Cancelled"` regardless of tab
- [ ] Unit test: `badgeVariant("completed")` returns `"secondary"`
- [ ] Visual check: Upcoming tab badge still shows "Scheduled"/"Confirmed" in blue

---

## Rollback Procedure
1. Revert `badgeVariant` and the badge render call to use `appt.status` directly
2. Remove `resolveDisplayStatus` helper

---

## External References
- `schedulingApi.ts` — `MyAppointment.status` is a raw string from the API
- `ACTIVE_STATUSES` set defined at top of `PatientDashboardPage.tsx`

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
- [x] Past tab: elapsed "Scheduled" appointments show grey "Completed" badge
- [x] Past tab: "Cancelled" and "NoShow" appointments still show red destructive badge
- [x] Upcoming tab: "Scheduled" / "Confirmed" appointments still show blue default badge
- [x] No regression on upcoming tab action buttons

---

## Implementation Checklist
- [x] Add `resolveDisplayStatus()` helper to `PatientDashboardPage.tsx`
- [x] Update `badgeVariant()` to handle `"completed"` → `"secondary"`
- [x] Replace `appt.status` with `resolveDisplayStatus(appt, activeTab === 'past')` in badge render
- [x] Verify Past tab visually shows correct badge colours
