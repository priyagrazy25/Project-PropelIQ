---
post_title: "TASK_001 - Walk-In Appointment Booking UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-walkin-booking"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-003, US_021, frontend, React, walk-in, staff, patient-creation"
ai_note: "Generated with AI assistance from user story US_021"
summary: "Implement staff-only walk-in booking UI with patient search, new patient creation modal, same-day slot selection, and queue enrollment."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_WALKIN_BOOKING

## Requirement Reference

- User Story: us_021
- Story Location: .propel/context/tasks/EP-003/us_021/us_021.md
- Acceptance Criteria:
  - AC-1: Walk-in option presents patient search and same-day slots
  - AC-2: Existing patient search by name or email
  - AC-3: "Create New Patient" modal with demographics
  - AC-4: Confirm walk-in records appointment status "Walk-In" and adds to queue
  - AC-5: Toast notification confirms booking with queue position per UXR-504
- Edge Cases:
  - Patient declines account → minimal demographic capture
  - No same-day slots → add to wait queue with estimated wait time

## Design References (Frontend Tasks Only)

| Reference Type         | Value                                                                  |
| ---------------------- | ---------------------------------------------------------------------- |
| **UI Impact**          | Yes                                                                    |
| **Figma URL**          | N/A                                                                    |
| **Wireframe Status**   | AVAILABLE                                                              |
| **Wireframe Type**     | HTML                                                                   |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-019-walkin-booking.html |
| **Screen Spec**        | .propel/context/docs/figma_spec.md#SCR-019                             |
| **UXR Requirements**   | UXR-504                                                                |
| **Design Tokens**      | .propel/context/docs/designsystem.md#forms, #modals                    |

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

Build the staff-only walk-in booking page with patient search, new patient creation modal, same-day slot or queue selection, and confirmation with queue position toast. Staff role guard prevents patient access.

## Dependent Tasks

- task_001_fe_react_scaffolding (US_001) — Requires React project
- task_001_fe_login_interface (US_013) — Requires role-based ProtectedRoute

## Impacted Components

- NEW: WalkInBookingPage — Staff-only walk-in flow
- NEW: PatientSearchBar, CreatePatientModal, SameDaySlotPicker
- MODIFY: App router — Add staff-protected walk-in route

## Implementation Plan

1. Create WalkInBookingPage with Staff role guard
2. Build PatientSearchBar with name/email search and result selection
3. Create CreatePatientModal for new patient demographics entry
4. Build SameDaySlotPicker showing available same-day slots
5. Implement walk-in confirmation with queue position toast per UXR-504
6. Handle no-slot scenario with wait queue enrollment and estimated wait time
7. Add minimal-demographics mode when patient declines full account creation
8. Add skeleton loading during patient search and slot fetch

## Current Project State

```
[PLACEHOLDER - Updated after US_001, US_013 tasks]
```

## Expected Changes

| Action | File Path                                                          | Description              |
| ------ | ------------------------------------------------------------------ | ------------------------ |
| CREATE | frontend/src/features/scheduling/pages/WalkInBookingPage.tsx       | Walk-in page             |
| CREATE | frontend/src/features/scheduling/components/PatientSearchBar.tsx   | Patient search           |
| CREATE | frontend/src/features/scheduling/components/CreatePatientModal.tsx | New patient modal        |
| CREATE | frontend/src/features/scheduling/components/SameDaySlotPicker.tsx  | Same-day slots           |
| MODIFY | frontend/src/features/scheduling/api/schedulingApi.ts              | Add walk-in endpoints    |
| MODIFY | frontend/src/App.tsx                                               | Add /staff/walk-in route |

## External References

- N/A

## Build Commands

- `npm run dev` — Start dev server

## Implementation Validation Strategy

- [x] Staff sees walk-in booking option; patient role is blocked
- [x] Patient search returns existing patients by name/email
- [x] Create Patient modal creates new account with demographics
- [x] Walk-in confirmation shows queue position toast
- [x] No-slot scenario adds to wait queue with estimated time
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-019

## Implementation Checklist

- [x] Create WalkInBookingPage with Staff role authorization guard
- [x] Build PatientSearchBar with debounced name/email search
- [x] Create CreatePatientModal with demographics form
- [x] Build SameDaySlotPicker for available same-day slots
- [x] Implement walk-in confirmation with queue position toast
- [x] Handle no-slot scenario with wait queue and estimated time
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-019 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
