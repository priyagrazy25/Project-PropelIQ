---
post_title: "TASK_001 - Same-Day Queue Management UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-queue-management"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-003, US_022, frontend, React, queue, real-time, SignalR"
ai_note: "Generated with AI assistance from user story US_022"
summary: "Implement real-time same-day queue management UI with status transitions, WebSocket updates, and arrival time display."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_QUEUE_MANAGEMENT

## Requirement Reference

- User Story: us_022
- Story Location: .propel/context/tasks/EP-003/us_022/us_022.md
- Acceptance Criteria:
  - AC-1: Ordered list with status indicators (Waiting/In-Progress/Completed) and skeleton loading per UXR-501
  - AC-2: Status change broadcast within 500ms via WebSocket per UXR-503
  - AC-3: New walk-in appears in real-time without refresh
  - AC-4: Show arrival time, appointment type, current wait duration
- Edge Cases:
  - Concurrent status update → optimistic concurrency; latest wins
  - Patient leaves without being seen → mark "Left"/"No-Show"

## Design References (Frontend Tasks Only)

| Reference Type         | Value                                                                    |
| ---------------------- | ------------------------------------------------------------------------ |
| **UI Impact**          | Yes                                                                      |
| **Figma URL**          | N/A                                                                      |
| **Wireframe Status**   | AVAILABLE                                                                |
| **Wireframe Type**     | HTML                                                                     |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-020-queue-management.html |
| **Screen Spec**        | .propel/context/docs/figma_spec.md#SCR-020                               |
| **UXR Requirements**   | UXR-501, UXR-503                                                         |
| **Design Tokens**      | .propel/context/docs/designsystem.md#status-indicators, #tables          |

## Applicable Technology Stack

| Layer            | Technology            | Version |
| ---------------- | --------------------- | ------- |
| Frontend         | React with TypeScript | 18.x    |
| State Management | Redux Toolkit         | 2.x     |
| Real-Time        | @microsoft/signalr    | 8.x     |

## AI References (AI Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **AI Impact**  | No    |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build the same-day queue management view for staff with an ordered patient list, color-coded status badges (Waiting/In-Progress/Completed/Left/No-Show), status transition actions, real-time updates via SignalR, and wait duration timer.

## Dependent Tasks

- task_001_fe_walkin_booking (US_021) — Walk-in adds to queue
- task_001_be_caching_realtime_setup (US_004) — Requires SignalR infrastructure

## Impacted Components

- NEW: QueueManagementPage — Staff queue view
- NEW: QueuePatientRow, StatusBadge, StatusTransitionActions
- NEW: useQueueSignalR hook for real-time queue updates

## Implementation Plan

1. Create QueueManagementPage with ordered patient list
2. Build QueuePatientRow with arrival time, type, wait duration, and status badge
3. Create StatusTransitionActions for Waiting → In-Progress → Completed transitions
4. Add "Left" and "No-Show" status actions for drop-outs
5. Create useQueueSignalR hook to receive real-time queue updates
6. Implement optimistic status update with concurrency resolution
7. Add skeleton loading state during initial data fetch per UXR-501
8. Implement live wait duration timer using arrival timestamp

## Current Project State

```
[PLACEHOLDER - Updated after US_001, US_004, US_021 tasks]
```

## Expected Changes

| Action | File Path                                                               | Description            |
| ------ | ----------------------------------------------------------------------- | ---------------------- |
| CREATE | frontend/src/features/scheduling/pages/QueueManagementPage.tsx          | Queue page             |
| CREATE | frontend/src/features/scheduling/components/QueuePatientRow.tsx         | Queue row              |
| CREATE | frontend/src/features/scheduling/components/StatusBadge.tsx             | Status badge           |
| CREATE | frontend/src/features/scheduling/components/StatusTransitionActions.tsx | Actions                |
| CREATE | frontend/src/features/scheduling/hooks/useQueueSignalR.ts               | Real-time hook         |
| MODIFY | frontend/src/App.tsx                                                    | Add /staff/queue route |

## External References

- N/A

## Build Commands

- `npm run dev` — Start dev server

## Implementation Validation Strategy

- [x] Queue displays ordered patients with status badges
- [x] Status transitions update within 500ms via WebSocket
- [x] New walk-ins appear in real-time
- [x] Wait duration timer updates live
- [x] Skeleton loading during initial fetch
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-020

## Implementation Checklist

- [x] Create QueueManagementPage with ordered patient list
- [x] Build QueuePatientRow with arrival time, type, and status badge
- [x] Implement status transitions (Waiting → In-Progress → Completed)
- [x] Add "Left" and "No-Show" status actions
- [x] Create useQueueSignalR hook for real-time updates within 500ms
- [x] Implement live wait duration timer from arrival timestamp
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-020 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
