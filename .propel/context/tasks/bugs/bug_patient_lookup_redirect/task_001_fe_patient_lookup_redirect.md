---
post_title: "BUG-001 - Patient Lookup Quick Action Routes to Wrong Page"
author1: "AI Senior Developer"
post_slug: "bug-001-patient-lookup-redirect"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Bug Fix"
tags: "BUG-001, EP-011, SCR-021, SCR-016, frontend, React, routing, staff-dashboard"
ai_note: "Generated with AI assistance — identified during SCR-021 wireframe review"
summary: "Patient Lookup quick action card on the Staff Dashboard navigates to /staff/queue instead of /staff/patient-view, sending staff to the queue management page rather than the 360° patient view page."
post_date: "2026-06-23"
---

# Bug Fix Task - [BUG-001]

## Bug Report Reference
- Bug ID: BUG-001
- Source: SCR-021 Staff Dashboard — wireframe review vs. implementation diff

## Bug Summary

### Issue Classification
- **Priority**: Medium
- **Severity**: Incorrect navigation — staff are routed to Queue Management instead of 360° Patient View when clicking "Patient Lookup"
- **Affected Version**: Current HEAD
- **Environment**: Web (all browsers), React SPA, React Router v6

### Steps to Reproduce
1. Log in as a staff user (e.g., admin@upap.com / Admin@123)
2. Navigate to the Staff Dashboard (`/staff/dashboard`)
3. Under **Quick Actions**, click the **Patient Lookup** card
4. **Expected**: Navigate to `/staff/patient-view` (SCR-016 — 360° Patient View)
5. **Actual**: Navigate to `/staff/queue` (SCR-020 — Queue Management)

**Error Output**:
```text
No runtime error. Incorrect route resolved silently.
```

### Root Cause Analysis
- **File**: `frontend/src/features/scheduling/pages/StaffDashboardPage.tsx`
- **Component**: `StaffDashboardPage`
- **Function**: `quickActions` array (lines ~175–190)
- **Cause**: The `to` property of the "Patient Lookup" quick action card is hardcoded as `'/staff/queue'` instead of `'/staff/patient-view'`. This causes React Router's `<Link>` to resolve the wrong route.

```tsx
// Current (incorrect)
{
  icon: UserSearch,
  title: 'Patient Lookup',
  description: 'Search and view complete patient records',
  to: '/staff/queue',   // ← wrong route
},

// Expected (correct — per SCR-021 wireframe)
{
  icon: UserSearch,
  title: 'Patient Lookup',
  description: 'Search and view complete patient records',
  to: '/staff/patient-view',
},
```

### Impact Assessment
- **Affected Features**: Staff Dashboard Quick Actions → Patient Lookup
- **User Impact**: Staff members clicking "Patient Lookup" land on the queue management page and must manually navigate to find a patient's 360° profile. Disrupts the primary lookup workflow defined in SCR-021.
- **Data Integrity Risk**: No
- **Security Implications**: None

## Fix Overview
Update the `to` field in the `quickActions` array for the "Patient Lookup" card from `'/staff/queue'` to `'/staff/patient-view'`.

## Fix Dependencies
- `/staff/patient-view` route must be registered in the React Router config — confirmed present in `frontend/src/App.tsx` (lines ~237–242).

## Impacted Components
### Frontend — React / React Router
- `frontend/src/features/scheduling/pages/StaffDashboardPage.tsx` — MODIFY `quickActions` array

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | `frontend/src/features/scheduling/pages/StaffDashboardPage.tsx` | Change `to: '/staff/queue'` → `to: '/staff/patient-view'` in the Patient Lookup quick action entry |

> No other files require modification. The route `/staff/patient-view` and its element (`PatientView360Page`) are already registered.

## Implementation Plan
1. Open `frontend/src/features/scheduling/pages/StaffDashboardPage.tsx`
2. Locate the `quickActions` array (approximately lines 175–190)
3. Find the object with `title: 'Patient Lookup'`
4. Change `to: '/staff/queue'` to `to: '/staff/patient-view'`
5. Verify the app navigates to the 360° patient view on click

## Regression Prevention Strategy
- [ ] Unit test: assert "Patient Lookup" card link `href` equals `/staff/patient-view`
- [ ] Navigation integration test: clicking "Patient Lookup" renders `PatientView360Page`
- [ ] Snapshot test update for `StaffDashboardPage` quick actions section

## Rollback Procedure
1. Revert the single-line change in `StaffDashboardPage.tsx`
2. Re-run frontend tests to confirm baseline is restored

## External References
- Wireframe SCR-021: `.propel/context/wireframes/Hi-Fi/wireframe-SCR-021-staff-dashboard.html` — "Patient Lookup" quick action links to `wireframe-SCR-016-360-degree-view.html`
- Target screen SCR-016: `.propel/context/wireframes/Hi-Fi/wireframe-SCR-016-360-degree-view.html`
- Route definition: `frontend/src/App.tsx` lines 237–242

## Build Commands
```bash
cd frontend
npm.cmd run dev       # verify fix in browser
npm.cmd run build     # ensure no TypeScript/build errors
npm.cmd run test      # run Vitest unit tests
```

## Implementation Validation Strategy
- [ ] Clicking "Patient Lookup" on `/staff/dashboard` navigates to `/staff/patient-view`
- [ ] No regression on other quick action links (Register Walk-In → `/staff/walk-in`, Manage Queue → `/staff/queue`)
- [ ] All existing `StaffDashboardPage` tests pass

## Implementation Checklist
- [x] Change `to: '/staff/queue'` to `to: '/staff/patient-view'` in `quickActions` array
- [ ] Run `npm.cmd run build` — confirm zero TypeScript errors
- [ ] Run `npm.cmd run test` — confirm all tests pass
- [ ] Manual browser verification: Patient Lookup routes to 360° patient view
