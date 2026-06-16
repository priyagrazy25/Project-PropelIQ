---
post_title: "TASK_002 - Patient Dashboard API"
author1: "AI Senior Developer"
post_slug: "task-002-be-patient-dashboard-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_050, backend, ASP.NET Core, dashboard, appointments"
ai_note: "Generated with AI assistance from user story US_050"
summary: "Implement GET /api/scheduling/appointments/my endpoint returning authenticated patient's appointments with provider details."
post_date: "2026-04-20"
---

# Task - TASK_002_BE_PATIENT_DASHBOARD_API

## Requirement Reference
- User Story: us_050
- Story Location: .propel/context/tasks/EP-002/us_050/us_050.md
- Acceptance Criteria:
    - AC-2: Returns appointments partitioned by status for Upcoming/Past tabs
    - AC-3: Each appointment includes provider name, specialty, location, date/time, status
- Edge Cases:
    - Patient with no appointments → returns empty array
    - Soft-deleted appointments excluded via global query filter

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

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Add a GET endpoint to AppointmentBookingController that returns all appointments for the authenticated patient. Implement GetMyAppointmentsQuery and GetMyAppointmentsQueryHandler following the existing CQRS handler pattern. The response includes provider name, specialty, location, date/time, duration, status, and type for each appointment. Results are ordered by appointment date descending. Register the handler in SchedulingModuleExtensions.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires Scheduling module and DbContext
- task_001_db_identity_scheduling_entities (US_008) — Requires Appointment and Provider entities

## Impacted Components
- NEW: GetMyAppointmentsQuery.cs — Query record with PatientId
- NEW: MyAppointmentResult.cs — Result DTO with appointment + provider details
- NEW: GetMyAppointmentsQueryHandler.cs — Handler querying appointments with provider include
- MODIFY: AppointmentBookingController.cs — Add GET "my" endpoint with auth
- MODIFY: SchedulingModuleExtensions.cs — Register GetMyAppointmentsQueryHandler

## Implementation Plan
1. Create GetMyAppointmentsQuery record in Scheduling.Application/Queries/GetMyAppointments/
2. Create MyAppointmentResult record with fields: AppointmentId, ProviderId, ProviderName, Specialty, Location, AppointmentDateTime, DurationMinutes, Status, Type
3. Create GetMyAppointmentsQueryHandler:
   a. Query Appointments filtered by PatientId and !IsDeleted
   b. Include Provider navigation for name/specialty/location
   c. Order by AppointmentDateTime descending
   d. Project to MyAppointmentResult via Select
   e. Return Result<IReadOnlyList<MyAppointmentResult>>
4. Add [HttpGet("my")] endpoint to AppointmentBookingController
5. Extract PatientId from JWT "sub" claim via existing GetPatientId()
6. Register handler in SchedulingModuleExtensions.cs

## Current Project State
```
backend/
  src/
    Modules/Scheduling/
      Scheduling.API/Controllers/AppointmentBookingController.cs  — POST booking, DELETE cancel, POST reschedule
      Scheduling.Application/
        Queries/SearchProviders/   — existing query handler pattern
        Queries/GetProviderSlots/  — existing query handler pattern
        Commands/BookAppointment/  — existing command handler pattern
      Scheduling.Infrastructure/SchedulingModuleExtensions.cs  — DI registration
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/Modules/Scheduling/Scheduling.Application/Queries/GetMyAppointments/GetMyAppointmentsQuery.cs | Query record |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Application/Queries/GetMyAppointments/MyAppointmentResult.cs | Result DTO |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Application/Queries/GetMyAppointments/GetMyAppointmentsQueryHandler.cs | Query handler |
| MODIFY | backend/src/Modules/Scheduling/Scheduling.API/Controllers/AppointmentBookingController.cs | Add GET "my" endpoint |
| MODIFY | backend/src/Modules/Scheduling/Scheduling.Infrastructure/SchedulingModuleExtensions.cs | Register handler |

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] GET /api/scheduling/appointments/my returns 200 with empty array for patient with no appointments
- [x] GET /api/scheduling/appointments/my returns appointments with provider details for patient with bookings
- [x] Soft-deleted appointments are excluded
- [x] Results ordered by AppointmentDateTime descending
- [x] 401 returned for unauthenticated requests
- [x] 400 returned when patient identity cannot be determined

## Implementation Checklist
- [x] Create GetMyAppointmentsQuery record with PatientId parameter
- [x] Create MyAppointmentResult record with all required fields
- [x] Create GetMyAppointmentsQueryHandler with EF Core query
- [x] Add [HttpGet("my")] endpoint to AppointmentBookingController
- [x] Add using for GetMyAppointments namespace in controller
- [x] Inject GetMyAppointmentsQueryHandler into controller constructor
- [x] Register GetMyAppointmentsQueryHandler in SchedulingModuleExtensions
- [x] Add using for GetMyAppointments namespace in SchedulingModuleExtensions
