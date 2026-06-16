---
post_title: "TASK_002 - Waitlist Management API"
author1: "AI Senior Developer"
post_slug: "task-002-be-waitlist-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_019, backend, ASP.NET Core, waitlist, notification"
ai_note: "Generated with AI assistance from user story US_019"
summary: "Implement waitlist enrollment API, auto-notification on slot availability, concurrent booking protection, and stale entry expiry."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_WAITLIST_API

## Requirement Reference
- User Story: us_019
- Story Location: .propel/context/tasks/EP-002/us_019/us_019.md
- Acceptance Criteria:
    - AC-1: Enroll on waitlist with preferred date range and provider
    - AC-2: Auto-notify via SMS and email when slot available in preferred window
    - AC-4: First to book gets slot; others see "Slot no longer available" per NFR-015
    - AC-5: Active waitlist entries visible with status and remove option
- Edge Cases:
    - Multiple waitlists per patient → independent entries
    - Expired date ranges → auto-expire stale entries

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
Build waitlist API: enrollment with date range and provider preference, automatic SMS/email notification when matching slot becomes available (triggered by cancel/reschedule/swap events), patient dashboard GET endpoint for active entries, DELETE for self-removal, and background expiry of stale entries.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires Scheduling module
- task_001_db_identity_scheduling_entities (US_008) — Requires Waitlist entity
- task_002_be_swap_engine (US_018) — Swap releases original slot → triggers waitlist

## Impacted Components
- NEW: WaitlistController (CRUD endpoints)
- NEW: IWaitlistService, WaitlistService
- NEW: WaitlistNotificationWorker (background service)
- MODIFY: BookingService — Trigger waitlist notification on cancel/reschedule

## Implementation Plan
1. Create POST /api/scheduling/waitlist endpoint to enroll with date range and provider
2. Create GET /api/scheduling/waitlist/my endpoint for patient's active entries
3. Create DELETE /api/scheduling/waitlist/{id} for self-removal
4. Implement WaitlistNotificationWorker that listens to slot-released events
5. Match released slots against waitlist preferred windows and provider
6. Send SMS (Twilio) and email (SendGrid) to matched waitlist patients
7. Broadcast waitlist-notification event via SignalR
8. Implement background job to auto-expire stale entries (date range passed)

## Current Project State
```
[PLACEHOLDER - Updated after US_002, US_008, US_018 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Scheduling/Controllers/WaitlistController.cs | Waitlist CRUD endpoints |
| CREATE | src/Modules/Scheduling/Services/IWaitlistService.cs | Service interface |
| CREATE | src/Modules/Scheduling/Services/WaitlistService.cs | Waitlist logic |
| CREATE | src/Modules/Scheduling/Workers/WaitlistNotificationWorker.cs | Background notifier |
| MODIFY | src/Modules/Scheduling/Services/BookingService.cs | Trigger waitlist on cancel |
| MODIFY | src/Modules/Scheduling/DependencyInjection.cs | Register services |

## External References
- Twilio .NET: https://www.twilio.com/docs/sms/quickstart/csharp-dotnet-core
- SendGrid .NET: https://docs.sendgrid.com/for-developers/sending-email/quickstart-csharp

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] POST /api/scheduling/waitlist enrolls with date range and provider
- [x] GET /my returns patient's active entries
- [x] Slot release triggers SMS/email to matched waitlist patients
- [x] Stale entries auto-expire when date range passes
- [x] Self-removal via DELETE works correctly

## Implementation Checklist
- [x] Create waitlist enrollment endpoint with date range and provider
- [x] Implement patient waitlist GET endpoint for active entries
- [x] Create DELETE endpoint for self-removal from waitlist
- [x] Build WaitlistNotificationWorker listening to slot-released events
- [x] Match released slots against waitlist preferences
- [x] Send SMS (Twilio) and email (SendGrid) notifications
- [x] Broadcast waitlist-notification via SignalR
- [x] Implement background expiry job for stale entries
