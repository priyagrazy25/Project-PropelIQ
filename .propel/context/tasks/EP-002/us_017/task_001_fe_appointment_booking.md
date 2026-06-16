---
post_title: "TASK_001 - Appointment Booking UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-appointment-booking"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_017, frontend, React, booking, optimistic-ui"
ai_note: "Generated with AI assistance from user story US_017"
summary: "Implement appointment booking UI with slot selection, optimistic confirmation, 409 conflict rollback, and confirmation screen."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_APPOINTMENT_BOOKING

## Requirement Reference
- User Story: us_017
- Story Location: .propel/context/tasks/EP-002/us_017/us_017.md
- Acceptance Criteria:
    - AC-1: Confirm booking records appointment with unique ID, status "Confirmed"
    - AC-3: Optimistic UI shows slot as booked immediately; rollback on API error per UXR-502
    - AC-4: Confirmation screen displays provider, date, time, location, appointment ID
    - AC-5: 409 Conflict rolls back optimistic update; "Slot no longer available" prompt
- Edge Cases:
    - Session expires during booking → silent token refresh; timeout modal with context preservation

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-005-booking-confirmation.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-005 |
| **UXR Requirements** | UXR-502 |
| **Design Tokens** | .propel/context/docs/designsystem.md#buttons, #modals |

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
Build the appointment booking flow: slot selection from provider search results, confirm booking with optimistic UI, handle 409 conflict with rollback, and display confirmation screen with appointment details. Integrate with session management for token refresh during booking.

## Dependent Tasks
- task_001_fe_provider_search (US_016) — Requires provider search with slot selection
- task_001_fe_session_timeout_modal (US_014) — Requires session management

## Impacted Components
- NEW: BookingConfirmationPage
- NEW: SlotSelectionPanel, ConfirmBookingDialog, BookingConfirmationCard
- MODIFY: schedulingApi — Add booking mutation endpoint

## Implementation Plan
1. Create SlotSelectionPanel that receives selected provider and displays slot options
2. Build ConfirmBookingDialog with appointment summary and confirm button
3. Implement optimistic UI via RTK Query mutation with onQueryStarted cache update
4. Handle 409 Conflict response with rollback and "Slot no longer available" toast
5. Create BookingConfirmationPage showing appointment ID, provider, date, time, location
6. Integrate session token refresh for booking flow continuity
7. Add loading spinner on confirm button to prevent double-submit
8. Implement error boundary for unexpected booking failures

## Current Project State
```
[PLACEHOLDER - Updated after US_001, US_016 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/scheduling/pages/BookingConfirmationPage.tsx | Confirmation page |
| CREATE | frontend/src/features/scheduling/components/SlotSelectionPanel.tsx | Slot selection UI |
| CREATE | frontend/src/features/scheduling/components/ConfirmBookingDialog.tsx | Booking dialog |
| CREATE | frontend/src/features/scheduling/components/BookingConfirmationCard.tsx | Confirmation details |
| MODIFY | frontend/src/features/scheduling/api/schedulingApi.ts | Add booking mutation |
| MODIFY | frontend/src/App.tsx | Add /booking/confirmation route |

## External References
- RTK Query optimistic updates: https://redux-toolkit.js.org/rtk-query/usage/manual-cache-updates

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Slot selection triggers booking dialog
- [x] Optimistic UI shows slot as booked immediately
- [x] 409 Conflict rolls back and shows error toast
- [x] Confirmation page displays all appointment details
- [x] Double-submit prevented by button disable
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-005

## Implementation Checklist
- [x] Create SlotSelectionPanel with slot options from search results
- [x] Build ConfirmBookingDialog with summary and confirm button
- [x] Implement optimistic UI with RTK Query cache update and rollback
- [x] Handle 409 Conflict with user-friendly "Slot no longer available" prompt
- [x] Create BookingConfirmationPage with appointment details display
- [x] Add loading state on confirm to prevent double-submit
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-005 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
