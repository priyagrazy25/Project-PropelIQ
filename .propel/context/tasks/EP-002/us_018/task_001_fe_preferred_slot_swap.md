---
post_title: "TASK_001 - Preferred Slot Swap UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-preferred-slot-swap"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_018, frontend, React, swap, preferred-slot"
ai_note: "Generated with AI assistance from user story US_018"
summary: "Implement preferred slot swap registration UI during booking flow with swap status display and notification."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_PREFERRED_SLOT_SWAP

## Requirement Reference
- User Story: us_018
- Story Location: .propel/context/tasks/EP-002/us_018/us_018.md
- Acceptance Criteria:
    - AC-1: During booking, select preferred unavailable slot alongside confirmed slot
    - AC-3: Notification of successful swap with updated appointment details
    - AC-4: Calendar event auto-updated after swap
- Edge Cases:
    - Preferred slot provider changes → notification that swap request removed

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-005-booking-confirmation.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-005 |
| **UXR Requirements** | UXR-504 |
| **Design Tokens** | .propel/context/docs/designsystem.md#badges, #cards |

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
Add preferred slot swap registration to the booking flow. Allow patients to select an unavailable slot they prefer alongside their confirmed booking. Display active swap preferences on the patient dashboard with status badges. Show swap notifications via toast when swap executes.

## Dependent Tasks
- task_001_fe_appointment_booking (US_017) — Requires booking confirmation flow
- task_001_fe_provider_search (US_016) — Requires slot display showing unavailable slots

## Impacted Components
- NEW: PreferredSlotPicker component
- NEW: SwapPreferenceCard for dashboard display
- MODIFY: BookingConfirmationPage — Add preferred slot section
- MODIFY: ProviderCard — Show unavailable slots as selectable preferences

## Implementation Plan
1. Modify ProviderCard to display unavailable slots as selectable preferred options
2. Create PreferredSlotPicker component for selecting preferred unavailable slot
3. Integrate preferred slot selection into booking confirmation flow
4. Add swap preference to booking mutation payload
5. Create SwapPreferenceCard on patient dashboard showing active swap status
6. Implement SignalR listener for swap-executed events
7. Show success toast with updated appointment details on swap execution
8. Handle swap removal notification when preferred slot becomes incompatible

## Current Project State
```
[PLACEHOLDER - Updated after US_016, US_017 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/scheduling/components/PreferredSlotPicker.tsx | Slot preference selector |
| CREATE | frontend/src/features/scheduling/components/SwapPreferenceCard.tsx | Dashboard swap status |
| MODIFY | frontend/src/features/scheduling/pages/BookingConfirmationPage.tsx | Add preferred slot section |
| MODIFY | frontend/src/features/scheduling/components/ProviderCard.tsx | Show unavailable slots |
| MODIFY | frontend/src/features/scheduling/api/schedulingApi.ts | Add swap endpoints |

## External References
- N/A

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Unavailable slots are selectable as preferred during booking
- [x] Booking request includes preferred slot ID
- [x] Dashboard shows active swap preferences with status
- [x] Swap execution notification appears via toast
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-005

## Implementation Checklist
- [x] Display unavailable slots as selectable preferences on ProviderCard
- [x] Create PreferredSlotPicker with slot info and selection action
- [x] Integrate preferred slot into booking confirmation flow
- [x] Create SwapPreferenceCard on patient dashboard with status badges
- [x] Implement SignalR listener for swap-executed notifications
- [x] Show swap execution success toast with new appointment details
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-005 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
