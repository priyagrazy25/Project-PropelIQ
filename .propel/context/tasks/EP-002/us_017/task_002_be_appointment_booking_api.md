---
post_title: "TASK_002 - Appointment Booking API"
author1: "AI Senior Developer"
post_slug: "task-002-be-appointment-booking-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_017, backend, ASP.NET Core, booking, idempotent"
ai_note: "Generated with AI assistance from user story US_017"
summary: "Implement idempotent appointment booking API with slot reservation, conflict detection, and confirmation response."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_APPOINTMENT_BOOKING_API

## Requirement Reference
- User Story: us_017
- Story Location: .propel/context/tasks/EP-002/us_017/us_017.md
- Acceptance Criteria:
    - AC-1: Records appointment with unique ID, patient ID, provider, date, time, status "Confirmed"
    - AC-2: Idempotent — duplicate request returns existing appointment per NFR-019
    - AC-5: 409 Conflict when slot already taken
- Edge Cases:
    - DB transaction failure → rollback releases slot lock; 500 error; patient retries
    - Session expires during booking → handled by client-side token refresh

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
| Caching | Upstash Redis | 7.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the appointment booking API with idempotent POST endpoint. Use optimistic concurrency with row-level locking for slot reservation. Detect duplicate requests via idempotency key. Return 409 on slot conflicts. Broadcast slot-booked event via SignalR and invalidate Redis cache.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires Scheduling module
- task_001_db_identity_scheduling_entities (US_008) — Requires Appointment/AppointmentSlot entities
- task_002_be_provider_search_api (US_016) — Requires SignalR slot broadcast

## Impacted Components
- NEW: AppointmentBookingController
- NEW: IBookingService, BookingService
- NEW: BookAppointmentRequest, BookingConfirmationDto
- MODIFY: AppointmentHub — Broadcast slot-booked event

## Implementation Plan
1. Create POST /api/scheduling/appointments endpoint with BookAppointmentRequest
2. Implement idempotency key check (hash of patientId + slotId + timestamp) via Redis
3. Use serializable transaction with row-level lock on AppointmentSlot
4. Validate slot availability and return 409 Conflict if taken
5. Create Appointment entity with status "Confirmed" and unique ID
6. Update AppointmentSlot status to "Booked"
7. Broadcast slot-booked event via SignalR to connected search clients
8. Invalidate Redis search cache for affected provider/date

## Current Project State
```
[PLACEHOLDER - Updated after US_002, US_008, US_016 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Scheduling/Controllers/AppointmentBookingController.cs | Booking endpoint |
| CREATE | src/Modules/Scheduling/Services/IBookingService.cs | Service interface |
| CREATE | src/Modules/Scheduling/Services/BookingService.cs | Booking logic |
| CREATE | src/Modules/Scheduling/DTOs/BookAppointmentRequest.cs | Request DTO |
| CREATE | src/Modules/Scheduling/DTOs/BookingConfirmationDto.cs | Response DTO |
| MODIFY | src/Modules/Scheduling/Hubs/AppointmentHub.cs | Broadcast slot-booked |
| MODIFY | src/Modules/Scheduling/DependencyInjection.cs | Register booking service |

## External References
- EF Core concurrency tokens: https://learn.microsoft.com/ef/core/saving/concurrency
- Idempotency patterns: https://learn.microsoft.com/azure/architecture/patterns/idempotent-message-processing

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] POST /api/scheduling/appointments creates confirmed appointment
- [x] Duplicate request returns existing appointment (idempotent)
- [x] Concurrent booking of same slot returns 409 for loser
- [x] SignalR broadcasts slot-booked event
- [x] Redis cache invalidated on booking

## Implementation Checklist
- [x] Create AppointmentBookingController with POST endpoint
- [x] Implement idempotency key check via Redis cache
- [x] Use serializable transaction with row-level lock on slot
- [x] Validate slot availability and return 409 Conflict if taken
- [x] Create Appointment entity with unique ID and "Confirmed" status
- [x] Broadcast slot-booked event via SignalR AppointmentHub
- [x] Invalidate Redis search cache for affected provider/date
- [x] Return standardized ProblemDetails for 400, 409, 500 errors
