---
post_title: "TASK_001 - Patient Arrival Marking UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-arrival-marking"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-003, US_023, frontend, React, arrival, staff-only"
ai_note: "Generated with AI assistance from user story US_023"
summary: "Implement staff-only 'Mark Arrived' action on scheduled appointments with real-time queue update and self-check-in prevention."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_ARRIVAL_MARKING

## Requirement Reference

- User Story: us_023
- Story Location: .propel/context/tasks/EP-003/us_023/us_023.md
- Acceptance Criteria:
  - AC-1: "Mark Arrived" button on scheduled appointments for Staff
  - AC-2: Patient attempting arrival → 403 (no self-check-in)
  - AC-3: No check-in button, QR code, or self-arrival on patient portal
  - AC-4: Queue and dashboard reflect arrival in real-time via WebSocket
- Edge Cases:
  - Mark arrival for cancelled appointment → validation error
  - Mark arrival for future-date appointment → validation error

## Design References (Frontend Tasks Only)

| Reference Type         | Value                                                                    |
| ---------------------- | ------------------------------------------------------------------------ |
| **UI Impact**          | Yes                                                                      |
| **Figma URL**          | N/A                                                                      |
| **Wireframe Status**   | AVAILABLE                                                                |
| **Wireframe Type**     | HTML                                                                     |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-020-queue-management.html |
| **Screen Spec**        | .propel/context/docs/figma_spec.md#SCR-020                               |
| **UXR Requirements**   | N/A                                                                      |
| **Design Tokens**      | .propel/context/docs/designsystem.md#buttons, #status-indicators         |

## Applicable Technology Stack

| Layer            | Technology            | Version |
| ---------------- | --------------------- | ------- |
| Frontend         | React with TypeScript | 18.x    |
| State Management | Redux Toolkit         | 2.x     |

## AI References (AI Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **AI Impact**  | No    |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Add "Mark Arrived" action button to staff-facing appointment list/queue for scheduled same-day appointments. Ensure no self-check-in UI exists on patient portal. Arrival updates queue in real-time.

## Dependent Tasks

- task_001_fe_queue_management (US_022) — Integrates into queue management page
- task_001_fe_login_interface (US_013) — Requires role-based access

## Impacted Components

- MODIFY: QueuePatientRow — Add "Mark Arrived" action for scheduled appointments
- MODIFY: StatusTransitionActions — Include arrival state
- AUDIT: Patient portal pages — Verify no self-check-in UI exists

## Implementation Plan

1. Add "Mark Arrived" button to QueuePatientRow for scheduled (non-arrived) appointments
2. Implement arrival mutation via RTK Query calling arrival API endpoint
3. Update queue status in real-time after arrival confirmation
4. Show validation error toast for cancelled or future-date appointments
5. Audit all patient-facing pages to ensure no check-in/self-arrival UI exists
6. Add arrival timestamp display on queue row after marking

## Current Project State

```
[PLACEHOLDER - Updated after US_022 tasks]
```

## Expected Changes

| Action | File Path                                                               | Description           |
| ------ | ----------------------------------------------------------------------- | --------------------- |
| MODIFY | frontend/src/features/scheduling/components/QueuePatientRow.tsx         | Add arrival action    |
| MODIFY | frontend/src/features/scheduling/components/StatusTransitionActions.tsx | Include arrival state |
| MODIFY | frontend/src/features/scheduling/api/schedulingApi.ts                   | Add arrival endpoint  |

## External References

- N/A

## Build Commands

- `npm run dev` — Start dev server

## Implementation Validation Strategy

- [x] "Mark Arrived" button appears for scheduled same-day appointments
- [x] Arrival mutation updates queue in real-time
- [x] Invalid arrival (cancelled/future) shows error toast
- [x] No self-check-in UI exists on patient portal
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-020

## Implementation Checklist

- [x] Add "Mark Arrived" button to QueuePatientRow for scheduled appointments
- [x] Implement arrival mutation via RTK Query
- [x] Show validation error for cancelled or future-date appointments
- [x] Display arrival timestamp after marking arrived
- [x] Audit patient portal: no check-in button, QR, or self-arrival option
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-020 during implementation
