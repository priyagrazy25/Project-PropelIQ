---
post_title: "TASK_002 - Preferred Slot Swap Engine API"
author1: "AI Senior Developer"
post_slug: "task-002-be-preferred-slot-swap-engine"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_018, backend, ASP.NET Core, swap, FIFO, automation"
ai_note: "Generated with AI assistance from user story US_018"
summary: "Implement FIFO-based preferred slot swap engine with automatic execution, notification dispatch, and calendar sync."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_PREFERRED_SLOT_SWAP_ENGINE

## Requirement Reference
- User Story: us_018
- Story Location: .propel/context/tasks/EP-002/us_018/us_018.md
- Acceptance Criteria:
    - AC-1: Register PreferredSlotSwap with FIFO priority
    - AC-2: FIFO processing — first-registered swap executes when slot becomes available
    - AC-3: Original slot released and waitlist notified; SMS/email confirmation sent
    - AC-4: Synced calendar event updated with new time
    - AC-5: Only first-registered patient gets swap; others retain original
- Edge Cases:
    - Preferred slot provider changes → remove swap request and notify patient
    - Original appointment cancelled → auto-expire swap request

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
| Email | SendGrid | Free tier |
| SMS | Twilio | Free trial |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the preferred slot swap engine as a background service that monitors slot releases (cancellation/reschedule events). When a preferred slot becomes available, process FIFO queue, execute the swap for the first-registered patient, release the original slot, notify via SMS/email, and update calendar events. Auto-expire swap requests when the base appointment is cancelled.

## Dependent Tasks
- task_002_be_appointment_booking_api (US_017) — Requires appointment creation/booking
- task_001_db_identity_scheduling_entities (US_008) — Requires PreferredSlotSwap entity

## Impacted Components
- NEW: PreferredSlotSwapController (registration endpoint)
- NEW: ISwapEngineService, SwapEngineService (FIFO processing)
- NEW: SwapBackgroundWorker (hosted service monitoring slot releases)
- MODIFY: BookingService — Trigger swap evaluation on cancel/reschedule

## Implementation Plan
1. Create POST /api/scheduling/swap-preferences endpoint to register swap
2. Implement SwapEngineService with FIFO queue processing logic
3. Create SwapBackgroundWorker as IHostedService listening to slot-released events
4. On swap execution: move appointment to preferred slot, release original
5. Send SMS and email notification via SendGrid/Twilio
6. Trigger calendar event update for synced patients
7. Auto-expire swap requests when base appointment is cancelled
8. Handle incompatible swap (provider change) with patient notification

## Current Project State
```
[PLACEHOLDER - Updated after US_017, US_008 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Scheduling/Controllers/PreferredSlotSwapController.cs | Swap registration |
| CREATE | src/Modules/Scheduling/Services/ISwapEngineService.cs | Swap engine interface |
| CREATE | src/Modules/Scheduling/Services/SwapEngineService.cs | FIFO swap processing |
| CREATE | src/Modules/Scheduling/Workers/SwapBackgroundWorker.cs | Hosted service |
| MODIFY | src/Modules/Scheduling/Services/BookingService.cs | Trigger swap eval |
| MODIFY | src/Modules/Scheduling/DependencyInjection.cs | Register services |

## External References
- IHostedService: https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services
- SendGrid .NET: https://docs.sendgrid.com/for-developers/sending-email/quickstart-csharp

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] POST /api/scheduling/swap-preferences registers with FIFO order
- [x] Slot release triggers swap for first-registered patient only
- [x] Original slot released and available in pool
- [x] SMS and email sent on swap execution
- [x] Swap auto-expires on base appointment cancellation

## Implementation Checklist
- [x] Create swap preference registration endpoint
- [x] Implement FIFO queue processing in SwapEngineService
- [x] Create SwapBackgroundWorker monitoring slot-released events
- [x] Execute swap: move appointment, release original slot
- [x] Send SMS (Twilio) and email (SendGrid) confirmation
- [x] Trigger calendar event update on swap execution
- [x] Auto-expire swap on base appointment cancellation
- [x] Handle provider-change incompatibility with patient notification
