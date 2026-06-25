---
post_title: "TASK_001 - Cancel & Reschedule UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-cancel-reschedule"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_020, frontend, React, cancel, reschedule"
ai_note: "Generated with AI assistance from user story US_020"
summary: "Implement cancel and reschedule UI with confirmation dialogs, slot re-selection, and calendar event update feedback."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_CANCEL_RESCHEDULE

## Requirement Reference
- User Story: us_020
- Story Location: .propel/context/tasks/EP-002/us_020/us_020.md
- Acceptance Criteria:
    - AC-1: Cancel button updates status, shows confirmation dialog
    - AC-2: Reschedule selects new date/time, creates new appointment
    - AC-4: Calendar event updated or removed on cancel/reschedule
- Edge Cases:
    - Reschedule to slot that becomes unavailable → 409 Conflict → prompt new selection

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-008-reschedule-cancel.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-008 |
| **UXR Requirements** | UXR-601 |
| **Design Tokens** | .propel/context/docs/designsystem.md#modals, #buttons |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | React with TypeScript | 18.x |
| State Management | Redux Toolkit | 2.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build cancel and reschedule interfaces on the appointment detail view. Cancel shows confirmation dialog with reason selection, updates status, and shows success toast. Reschedule opens slot picker for new date/time, handles conflict, and displays updated confirmation.

## Dependent Tasks
- task_001_fe_appointment_booking (US_017) — Requires booking confirmation page as basis
- task_001_fe_provider_search (US_016) — Requires slot picker for rescheduling

## Impacted Components
- NEW: CancelAppointmentDialog — Confirmation with reason
- NEW: RescheduleAppointmentPage — Slot re-selection
- MODIFY: BookingConfirmationPage — Add cancel/reschedule action buttons
- MODIFY: schedulingApi — Add cancel/reschedule mutation endpoints

## Implementation Plan
1. Add Cancel and Reschedule action buttons to appointment detail view
2. Create CancelAppointmentDialog with reason selection and confirm
3. Implement cancel mutation with optimistic status update
4. Create RescheduleAppointmentPage reusing provider search slot picker
5. Handle 409 Conflict on reschedule with "Slot no longer available" prompt
6. Show success toast with updated appointment details after reschedule
7. Update dashboard appointment list after cancel/reschedule
8. Add calendar event update feedback (visual indicator)

## Current Project State
```
[PLACEHOLDER - Updated after US_016, US_017 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/scheduling/components/CancelAppointmentDialog.tsx | Cancel dialog |
| CREATE | frontend/src/features/scheduling/pages/RescheduleAppointmentPage.tsx | Reschedule page |
| MODIFY | frontend/src/features/scheduling/pages/BookingConfirmationPage.tsx | Add action buttons |
| MODIFY | frontend/src/features/scheduling/api/schedulingApi.ts | Add mutations |
| MODIFY | frontend/src/App.tsx | Add /reschedule route |

## External References
- N/A

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Cancel shows confirmation dialog and updates status
- [x] Reschedule opens slot picker and creates new appointment
- [x] 409 Conflict on reschedule shows user-friendly prompt
- [x] Dashboard updates after cancel/reschedule
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-008

## Implementation Checklist
- [x] Add Cancel and Reschedule action buttons to appointment detail
- [x] Create CancelAppointmentDialog with reason selection and confirmation
- [x] Implement cancel mutation with optimistic status update and toast
- [x] Create RescheduleAppointmentPage with slot re-selection flow
- [x] Handle 409 Conflict with "Slot no longer available" prompt
- [x] Update patient dashboard after cancel/reschedule actions
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-008 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
