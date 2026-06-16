---
post_title: "TASK_002 - Intake Mode Switch & Summary API"
author1: "AI Senior Developer"
post_slug: "task-002-be-intake-mode-switch-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-004, US_026, backend, ASP.NET Core, intake, mode-switch, summary"
ai_note: "Generated with AI assistance from user story US_026"
summary: "Implement API endpoints for intake mode switching with data preservation and summary retrieval."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_INTAKE_MODE_SWITCH_API

## Requirement Reference

- User Story: us_026
- Story Location: .propel/context/tasks/EP-004/us_026/us_026.md
- Acceptance Criteria:
  - AC-1: AI→manual data preservation on server side
  - AC-2: Manual→AI passes data as context
  - AC-3: Summary endpoint returns categorized intake data
  - AC-4: AI health check for circuit breaker status per NFR-013

## Design References (Frontend Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **UI Impact**  | No    |

## Applicable Technology Stack

| Layer    | Technology            | Version |
| -------- | --------------------- | ------- |
| Backend  | ASP.NET Core          | 8.0 LTS |
| ORM      | Entity Framework Core | 8.0     |
| Database | SQL Server Express    | 2022    |
| Caching  | Upstash Redis         | 7.x     |

## AI References (AI Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **AI Impact**  | No    |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build mode switch API endpoints: POST to switch mode with data snapshot, GET summary with categorized fields, and GET health endpoint for AI availability. Merge data from both modes preserving latest edits.

## Dependent Tasks

- task_002_be_intake_api (US_024) — Requires intake session infrastructure
- task_002_be_manual_intake_api (US_025) — Requires manual intake persistence

## Impacted Components

- MODIFY: IntakeController — Add mode switch and summary endpoints
- MODIFY: IntakeSessionService — Add mode switch logic and data merge

## Implementation Plan

1. Create POST /api/clinical/intake/{appointmentId}/switch-mode endpoint
2. Snapshot current mode data and preserve in Redis session
3. Create GET /api/clinical/intake/{appointmentId}/summary for categorized review
4. Merge AI-parsed and manual data, preferring latest edits
5. Create GET /api/clinical/ai/health for circuit breaker status check
6. Return categorized fields with confidence scores (null for manual edits)
7. Support final confirmation POST that locks the intake record

## Current Project State

```
[PLACEHOLDER - Updated after US_024, US_025 tasks]
```

## Expected Changes

| Action | File Path                                             | Description           |
| ------ | ----------------------------------------------------- | --------------------- |
| MODIFY | src/Modules/Clinical/Controllers/IntakeController.cs  | Mode switch endpoints |
| MODIFY | src/Modules/Clinical/Services/IntakeSessionService.cs | Mode switch logic     |
| CREATE | src/Modules/Clinical/DTOs/IntakeSummaryDto.cs         | Summary response DTO  |
| CREATE | src/Modules/Clinical/DTOs/ModeSwitchRequest.cs        | Switch request DTO    |

## External References

- N/A

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] POST switch-mode preserves data from current mode
- [x] GET summary returns categorized fields with confidence scores
- [x] Data merge prefers latest edits from either mode
- [x] GET ai/health returns circuit breaker status
- [x] Final confirmation locks intake record

## Implementation Checklist

- [x] Create POST switch-mode endpoint with data snapshot
- [x] Implement data merge logic preferring latest edits
- [x] Create GET summary endpoint with categorized fields
- [x] Add confidence scores (null for manual edits) in summary
- [x] Create GET ai/health for AI availability check
- [x] Support final confirmation POST locking intake record
- [x] Return ProblemDetails for session not found errors
