---
post_title: "TASK_002 - Same-Day Queue Management API"
author1: "AI Senior Developer"
post_slug: "task-002-be-queue-management-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-003, US_022, backend, ASP.NET Core, queue, real-time, SignalR, audit"
ai_note: "Generated with AI assistance from user story US_022"
summary: "Implement same-day queue API with status transitions, real-time SignalR broadcast, and immutable audit logging."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_QUEUE_MANAGEMENT_API

## Requirement Reference

- User Story: us_022
- Story Location: .propel/context/tasks/EP-003/us_022/us_022.md
- Acceptance Criteria:
  - AC-1: Ordered list of today's patients with status indicators
  - AC-2: Status update persisted and broadcast via WebSocket within 500ms per UXR-503
  - AC-3: New walk-in appears in queue in real-time
  - AC-4: Each entry shows arrival time, appointment type, wait duration
  - AC-5: Immutable audit log for all status changes
- Edge Cases:
  - Concurrent update → optimistic concurrency, latest wins
  - Patient leaves → "Left"/"No-Show" status with audit trail

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
| Logging   | Serilog               | 3.x     |

## AI References (AI Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **AI Impact**  | No    |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build the queue management API: GET today's queue ordered by arrival, PUT status transitions with optimistic concurrency, SignalR broadcast on every mutation, and append-only audit log entries recording staff actor, action, and timestamp.

## Dependent Tasks

- task_002_be_walkin_api (US_021) — Walk-in adds to queue
- task_001_be_caching_realtime_setup (US_004) — Requires SignalR infrastructure
- task_001_db_clinical_ai_entities (US_009) — Requires AuditLog entity

## Impacted Components

- NEW: QueueController — Queue management endpoints
- NEW: IQueueService, QueueService
- NEW: QueueEntryDto, StatusUpdateRequest
- MODIFY: AppointmentHub — Broadcast queue-status-changed events

## Implementation Plan

1. Create GET /api/scheduling/queue/today endpoint returning ordered queue
2. Create PUT /api/scheduling/queue/{id}/status for status transitions
3. Implement status validation finite state machine (Waiting → In-Progress → Completed; Waiting → Left/No-Show)
4. Add optimistic concurrency via EF Core ConcurrencyToken on status column
5. Broadcast queue-status-changed via SignalR to staff group
6. Create immutable audit log entry for each status change
7. Include arrival time, type, and computed wait duration in response
8. Implement staff-only authorization on all queue endpoints

## Current Project State

```
[PLACEHOLDER - Updated after US_002, US_004, US_021 tasks]
```

## Expected Changes

| Action | File Path                                             | Description            |
| ------ | ----------------------------------------------------- | ---------------------- |
| CREATE | src/Modules/Scheduling/Controllers/QueueController.cs | Queue endpoints        |
| CREATE | src/Modules/Scheduling/Services/IQueueService.cs      | Service interface      |
| CREATE | src/Modules/Scheduling/Services/QueueService.cs       | Queue logic            |
| CREATE | src/Modules/Scheduling/DTOs/QueueEntryDto.cs          | Queue entry DTO        |
| CREATE | src/Modules/Scheduling/DTOs/StatusUpdateRequest.cs    | Status change DTO      |
| MODIFY | src/Modules/Scheduling/Hubs/AppointmentHub.cs         | Broadcast queue events |

## External References

- EF Core concurrency: https://learn.microsoft.com/ef/core/saving/concurrency

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] GET /queue/today returns ordered list with status indicators
- [x] PUT status transitions persist and broadcast within 500ms
- [x] Concurrent updates resolve via optimistic concurrency
- [x] Audit log records every status change with actor and timestamp
- [x] Non-staff access returns 403

## Implementation Checklist

- [x] Create GET /api/scheduling/queue/today with arrival-ordered results
- [x] Implement PUT /api/scheduling/queue/{id}/status with state machine
- [x] Add EF Core ConcurrencyToken for optimistic concurrency
- [x] Broadcast queue-status-changed via SignalR to staff group
- [x] Create immutable audit log entry per status change
- [x] Compute and return wait duration from arrival timestamp
- [x] Enforce staff-only authorization on all endpoints
- [x] Return ProblemDetails for invalid state transitions (400)
