---
post_title: "TASK_002 - Patient Arrival Marking API"
author1: "AI Senior Developer"
post_slug: "task-002-be-arrival-marking-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-003, US_023, backend, ASP.NET Core, arrival, staff-only, audit"
ai_note: "Generated with AI assistance from user story US_023"
summary: "Implement staff-only arrival marking API with date/status validation, SignalR broadcast, and audit logging."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_ARRIVAL_MARKING_API

## Requirement Reference

- User Story: us_023
- Story Location: .propel/context/tasks/EP-003/us_023/us_023.md
- Acceptance Criteria:
  - AC-1: Staff marks "Arrived" with timestamp
  - AC-2: Patient access → 403 Forbidden
  - AC-4: Queue and dashboard update via WebSocket
  - AC-5: Audit log records staff actor, appointment ID, arrival timestamp
- Edge Cases:
  - Cancelled appointment → reject with validation error
  - Future-date appointment → reject with "same-day only" validation

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

Build the arrival marking endpoint: PUT /api/scheduling/appointments/{id}/arrive with Staff-only authorization. Validates appointment is same-day and not cancelled. Sets status "Arrived" with timestamp, broadcasts via SignalR, and logs to audit trail.

## Dependent Tasks

- task_002_be_queue_management_api (US_022) — Queue infrastructure
- task_001_db_identity_scheduling_entities (US_008) — Requires Appointment entity
- task_001_db_clinical_ai_entities (US_009) — Requires AuditLog entity

## Impacted Components

- MODIFY: QueueController — Add arrival endpoint (or create ArrivalController)
- MODIFY: QueueService — Add arrival logic with validation
- MODIFY: AppointmentHub — Broadcast arrival event

## Implementation Plan

1. Create PUT /api/scheduling/appointments/{id}/arrive with Staff authorization
2. Validate appointment exists and is scheduled for today (not future-date)
3. Validate appointment is not cancelled or already arrived
4. Set status "Arrived" with UTC timestamp
5. Broadcast patient-arrived event via SignalR to staff group
6. Create immutable audit log entry with staff ID, appointment ID, timestamp
7. Return updated appointment status in response

## Current Project State

```
[PLACEHOLDER - Updated after US_022, US_008, US_009 tasks]
```

## Expected Changes

| Action | File Path                                             | Description              |
| ------ | ----------------------------------------------------- | ------------------------ |
| MODIFY | src/Modules/Scheduling/Controllers/QueueController.cs | Add arrival endpoint     |
| MODIFY | src/Modules/Scheduling/Services/QueueService.cs       | Arrival validation logic |
| MODIFY | src/Modules/Scheduling/Hubs/AppointmentHub.cs         | Broadcast arrival event  |

## External References

- N/A

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] PUT /arrive updates status to "Arrived" with timestamp
- [x] Patient access returns 403 Forbidden
- [x] Cancelled appointment returns 400 validation error
- [x] Future-date appointment returns 400 validation error
- [x] Audit log records staff actor and timestamp

## Implementation Checklist

- [x] Create PUT /api/scheduling/appointments/{id}/arrive endpoint
- [x] Enforce Staff-only authorization; return 403 for Patient role
- [x] Validate same-day appointment (reject future-date)
- [x] Validate not cancelled or already arrived
- [x] Set status "Arrived" with UTC timestamp
- [x] Broadcast patient-arrived event via SignalR
- [x] Create immutable audit log entry with staff ID and timestamp
