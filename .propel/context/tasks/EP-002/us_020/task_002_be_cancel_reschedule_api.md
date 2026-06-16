---
post_title: "TASK_002 - Cancel & Reschedule API"
author1: "AI Senior Developer"
post_slug: "task-002-be-cancel-reschedule-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_020, backend, ASP.NET Core, cancel, reschedule, idempotent"
ai_note: "Generated with AI assistance from user story US_020"
summary: "Implement idempotent cancel and reschedule API with slot release, swap queue evaluation, waitlist notification, and calendar sync."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_CANCEL_RESCHEDULE_API

## Requirement Reference
- User Story: us_020
- Story Location: .propel/context/tasks/EP-002/us_020/us_020.md
- Acceptance Criteria:
    - AC-1: Cancel updates status to "Cancelled", releases slot, triggers waitlist
    - AC-2: Reschedule creates new appointment, cancels old, releases original slot
    - AC-3: Swap queue checked first, then waitlist notified on slot release
    - AC-4: Calendar event updated or removed on cancel/reschedule
    - AC-5: Idempotent cancel endpoint returns existing cancelled appointment
- Edge Cases:
    - Reschedule conflict (slot taken) → 409 Conflict
    - Walk-in cancellation → staff-only; removed from same-day queue

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Backend | ASP.NET Core | 8.0 LTS |
| ORM | Entity Framework Core | 8.0 |
| Database | SQL Server Express | 2022 |
| Real-Time | SignalR | 8.x |
| Calendar | Google Calendar API v3 / Microsoft Graph v1.0 | Latest |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build cancel and reschedule API endpoints. Cancel is idempotent: sets status "Cancelled", releases slot, evaluates swap queue (FIFO), notifies waitlist, and updates calendar. Reschedule creates new booking in a transaction, cancels old, and triggers the same cascade. Walk-in cancellations are staff-only.

## Dependent Tasks
- task_002_be_appointment_booking_api (US_017) — Requires booking infrastructure
- task_002_be_swap_engine (US_018) — Swap queue evaluation on slot release
- task_002_be_waitlist_api (US_019) — Waitlist notification on slot release

## Impacted Components
- NEW: AppointmentCancelController (cancel/reschedule endpoints)
- NEW: ICancelRescheduleService, CancelRescheduleService
- MODIFY: BookingService — Slot release triggers swap evaluation + waitlist

## Implementation Plan
1. Create PUT /api/scheduling/appointments/{id}/cancel endpoint (idempotent)
2. Create POST /api/scheduling/appointments/{id}/reschedule endpoint
3. Implement cancel: set status "Cancelled", release slot, emit slot-released event
4. Implement reschedule: book new slot (transaction), cancel old, release original
5. Slot-released event triggers swap queue evaluation (FIFO) first
6. If no swap matches, trigger waitlist notification
7. Update/delete Google Calendar and Microsoft Graph events
8. Enforce staff-only guard for walk-in appointment cancellation

## Current Project State
```
[PLACEHOLDER - Updated after US_017, US_018, US_019 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Scheduling/Controllers/AppointmentCancelController.cs | Cancel/reschedule |
| CREATE | src/Modules/Scheduling/Services/ICancelRescheduleService.cs | Service interface |
| CREATE | src/Modules/Scheduling/Services/CancelRescheduleService.cs | Cancel/reschedule logic |
| MODIFY | src/Modules/Scheduling/Services/BookingService.cs | Slot-released event |
| MODIFY | src/Modules/Scheduling/DependencyInjection.cs | Register services |

## External References
- Google Calendar API: https://developers.google.com/calendar/api/v3/reference
- Microsoft Graph Calendar: https://learn.microsoft.com/graph/api/resources/calendar

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] Cancel sets status "Cancelled" and releases slot
- [x] Duplicate cancel returns existing cancelled appointment
- [x] Reschedule creates new appointment and cancels old
- [x] Swap queue evaluated before waitlist on slot release
- [x] Calendar events updated/removed

## Implementation Checklist
- [x] Create idempotent cancel endpoint with slot release
- [x] Create reschedule endpoint with transactional book-new/cancel-old
- [x] Emit slot-released event triggering swap queue evaluation
- [x] Trigger waitlist notification if no swap match
- [x] Update Google Calendar / Microsoft Graph events on cancel/reschedule
- [x] Enforce staff-only authorization for walk-in cancellation
- [x] Return standardized ProblemDetails for 400, 404, 409 errors
- [x] Log audit trail for all cancel/reschedule mutations
