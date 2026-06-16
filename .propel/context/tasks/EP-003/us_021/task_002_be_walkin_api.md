---
post_title: "TASK_002 - Walk-In Appointment API"
author1: "AI Senior Developer"
post_slug: "task-002-be-walkin-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-003, US_021, backend, ASP.NET Core, walk-in, queue"
ai_note: "Generated with AI assistance from user story US_021"
summary: "Implement staff-only walk-in booking API with patient search/creation, same-day slot assignment, and queue enrollment."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_WALKIN_API

## Requirement Reference

- User Story: us_021
- Story Location: .propel/context/tasks/EP-003/us_021/us_021.md
- Acceptance Criteria:
  - AC-1: Walk-in booking presents patient search and same-day slots
  - AC-2: Patient search by name or email returns existing records
  - AC-3: Create new patient during walk-in flow
  - AC-4: Appointment recorded status "Walk-In", added to same-day queue in arrival order
  - AC-5: Toast with queue position per UXR-504
- Edge Cases:
  - Minimal demographics capture (no full account)
  - No available same-day slots → add to wait queue with estimated time

## Design References (Frontend Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **UI Impact**  | No    |

## Applicable Technology Stack

| Layer     | Technology            | Version |
| --------- | --------------------- | ------- |
| Backend   | ASP.NET Core          | 8.0 LTS |
| ORM       | Entity Framework Core | 8.0     |
| Database  | SQL Server Express    | 2022    |
| Real-Time | SignalR               | 8.x     |

## AI References (AI Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **AI Impact**  | No    |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build staff-only walk-in API: patient search, inline patient creation, same-day slot assignment or queue enrollment, and queue position broadcast via SignalR. All endpoints require Staff role authorization.

## Dependent Tasks

- task_001_be_modular_monolith_setup (US_002) — Requires Scheduling module
- task_001_db_identity_scheduling_entities (US_008) — Requires Appointment/Patient entities
- task_002_be_registration_api (US_012) — Requires patient creation logic

## Impacted Components

- NEW: WalkInController — Walk-in booking endpoints
- NEW: IWalkInService, WalkInService
- NEW: WalkInBookingRequest, QueuePositionDto
- MODIFY: AppointmentHub — Broadcast queue-updated event

## Implementation Plan

1. Create POST /api/scheduling/walk-in endpoint with Staff authorization
2. Implement patient search GET /api/patients/search?q={query} (reuse or extend)
3. Implement inline patient creation for new walk-in patients
4. Assign same-day slot or enroll in wait queue with arrival order
5. Calculate queue position and estimated wait time
6. Broadcast queue-updated event via SignalR to staff clients
7. Return queue position in response for toast notification
8. Add audit log for walk-in registration

## Current Project State

```
Implemented - Walk-in API controller, service, DTOs, and SignalR broadcast created.
Build: PASS (0 errors) | Unit Tests: 267 passed | Integration Tests: 2 passed
```

## Expected Changes

| Action | File Path                                              | Description             |
| ------ | ------------------------------------------------------ | ----------------------- |
| CREATE | src/Modules/Scheduling/Controllers/WalkInController.cs | Walk-in endpoints       |
| CREATE | src/Modules/Scheduling/Services/IWalkInService.cs      | Service interface       |
| CREATE | src/Modules/Scheduling/Services/WalkInService.cs       | Walk-in logic           |
| CREATE | src/Modules/Scheduling/DTOs/WalkInBookingRequest.cs    | Request DTO             |
| CREATE | src/Modules/Scheduling/DTOs/QueuePositionDto.cs        | Queue response DTO      |
| MODIFY | src/Modules/Scheduling/Hubs/AppointmentHub.cs          | Broadcast queue updates |

## External References

- N/A

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] POST /api/scheduling/walk-in creates walk-in appointment
- [x] Patient search returns existing records
- [x] New patient created inline during walk-in
- [x] Queue position returned and broadcast via SignalR
- [x] Non-staff access returns 403

## Implementation Checklist

- [x] Create WalkInController with Staff role authorization
- [x] Implement walk-in booking with same-day slot or queue enrollment
- [x] Support inline patient creation with demographics
- [x] Calculate queue position and estimated wait time
- [x] Broadcast queue-updated event via SignalR
- [x] Support minimal demographics mode (no full account)
- [x] Add audit log for walk-in registration (via ILogger structured logging)
- [x] Return standardized ProblemDetails for validation errors
