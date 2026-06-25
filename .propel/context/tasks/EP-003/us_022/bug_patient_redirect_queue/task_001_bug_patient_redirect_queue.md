---
post_title: "BUG_002 - Clicking Patient in Queue Redirects to Patient View Instead of Queue"
author1: "AI QA Engineer"
post_slug: "bug-002-patient-redirect-patient-view-queue"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Bug Fix"
tags: "EP-003, US_022, bug, frontend, queue, navigation, staff-dashboard"
ai_note: "Bug report created from observed defect in QueuePatientRow navigation — patient name links to patient view instead of queue"
summary: "In the Staff Dashboard Queue view, clicking a patient name navigates to the PatientView360 page (/staff/patient-view/:patientId) instead of keeping staff in the queue context (/staff/queue)."
post_date: "2026-06-23"
---

# Bug Fix Task - [BUG_002]

## Bug Report Reference
- Bug ID: BUG_002
- Source: Staff Dashboard → Queue (`/staff/queue`) — `QueuePatientRow` component
- Related User Story: US_022
- Related Tasks: task_001_fe_queue_management.md, task_002_be_queue_management_api.md

## Bug Summary

### Issue Classification
- **Priority**: Medium
- **Severity**: UX / Navigation — patient name in queue is a styled link but the destination needs to match the wireframe (SCR-016: 360° health profile)
- **Affected Version**: Current (main branch)
- **Environment**: All environments; reproducible on localhost dev stack

### Steps to Reproduce
1. Log in as a Staff user (`admin@upap.com` / `Admin@123` in dev)
2. Navigate to **Staff Dashboard** (`/staff/dashboard`)
3. Click the **Queue / Waiting** summary card → lands on `/staff/queue`
4. Click on any **patient name** (shown as a blue link in the queue row)
5. **Expected**: Browser navigates to the patient's 360° health profile at `/staff/patient-view/:patientId` (SCR-016 per wireframe)
6. **Actual (was)**: Patient name rendered as plain non-navigable text after incorrect fix

### Error Output
```text
No JavaScript error — the navigation succeeds, but to the wrong destination.
Current destination:  /staff/patient-view/<patientId>  (PatientView360Page)
Expected destination: /staff/queue
```

### Root Cause Analysis
- **File**: `frontend/src/features/scheduling/components/QueuePatientRow.tsx:78–83`
- **Component**: `QueuePatientRow` — patient name cell
- **Cause**: The `NavLink` for the patient name uses the route `/staff/patient-view/${entry.patientId}` which resolves to `PatientView360Page`. This is a full 360° clinical record page intended for providers/admins, not for front-desk staff managing the queue. The correct navigation from a queue row should direct back to the queue management page, not the clinical patient view.

```tsx
// CURRENT (BUG):
<NavLink
  to={`/staff/patient-view/${entry.patientId}`}   // ← navigates away to patient view
  className="text-sm font-medium text-primary hover:underline no-underline"
>
  {entry.patientName}
</NavLink>

// EXPECTED FIX:
// Option A — make name non-navigable (plain text, actions via buttons only)
// Option B — navigate to /staff/queue (stay in queue context)
// Option C — navigate to queue filtered/anchored on this patient entry
```

### Impact Assessment
- **Affected Features**: Queue Management (US_022), Staff Dashboard queue widget
- **User Impact**: Front-desk staff are taken out of their queue workflow on every patient name click; must navigate back to queue each time. Breaks queue throughput for busy clinics.
- **Data Integrity Risk**: None
- **Security Implications**: `PatientView360Page` at `/staff/patient-view/:patientId` exposes clinical 360° data; this route should only be accessible to authorised roles. Accidental navigation may expose patient data beyond the staff member's intended scope.

## Fix Overview
Remove or correct the `NavLink` in `QueuePatientRow` so that clicking a patient name no longer navigates to `/staff/patient-view/:patientId`. The patient name should either be plain text (with queue actions accessed via the existing status-transition buttons) or link back to `/staff/queue`.

## Fix Dependencies
- No backend changes required
- Route `/staff/patient-view/:patientId` should remain intact for intended consumers (providers/admins)

## Impacted Components
### Frontend
- `frontend/src/features/scheduling/components/QueuePatientRow.tsx` — MODIFY patient name cell: replace `NavLink to="/staff/patient-view/:id"` with plain `<span>` or a link to `/staff/queue`

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | `frontend/src/features/scheduling/components/QueuePatientRow.tsx` | Replace `NavLink to={/staff/patient-view/${entry.patientId}}` with plain `<span>` or `<Link to="/staff/queue">` |

> Only list concrete, verifiable file operations.

## Implementation Plan
1. Open `frontend/src/features/scheduling/components/QueuePatientRow.tsx`
2. Locate the `NavLink` wrapping `{entry.patientName}` (lines ~78–83)
3. Replace with a non-navigating `<span>` styled identically (`text-sm font-medium text-foreground`) — patient selection actions are already available via the `StatusTransitionActions` column
4. Verify the queue page renders correctly and no broken links remain
5. Run existing frontend tests to confirm no regressions

## Regression Prevention Strategy
- [ ] Unit test: `QueuePatientRow` renders patient name without navigation link
- [ ] E2E test: Clicking patient name in queue does NOT navigate away from `/staff/queue`
- [ ] E2E test: Queue action buttons (Arrived, In Progress, Complete) remain functional after patient name change

## Rollback Procedure
1. Revert `QueuePatientRow.tsx` via `git revert` or restore the `NavLink` line
2. Navigation will revert to `/staff/patient-view/:patientId` — functional but incorrect UX

## External References
- Screen Spec: `.propel/context/docs/figma_spec.md#SCR-018` (Queue Management)
- User Story: `.propel/context/tasks/EP-003/us_022/us_022.md`
- Frontend Task: `.propel/context/tasks/EP-003/us_022/task_001_fe_queue_management.md`
- Backend Task: `.propel/context/tasks/EP-003/us_022/task_002_be_queue_management_api.md`
- Related Bug: `.propel/context/tasks/EP-003/us_021/bug_patient_search_walkin/task_001_bug_patient_search_walkin.md` (BUG_001)

## Build Commands
```bash
# Frontend dev server
cd frontend && npm.cmd run dev

# Run unit tests
cd frontend && npm.cmd run test:run -- src/features/scheduling/components/QueuePatientRow

# Backend dev server
cd backend/src/Host && dotnet run --configuration Release
```
