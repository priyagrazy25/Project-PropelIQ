---
post_title: "TASK_002 - Manual Intake API"
author1: "AI Senior Developer"
post_slug: "task-002-be-manual-intake-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-004, US_025, backend, ASP.NET Core, intake, autosave, validation"
ai_note: "Generated with AI assistance from user story US_025"
summary: "Implement manual intake API with autosave endpoint, server-side validation, and IntakeRecord persistence."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_MANUAL_INTAKE_API

## Requirement Reference

- User Story: us_025
- Story Location: .propel/context/tasks/EP-004/us_025/us_025.md
- Acceptance Criteria:
  - AC-1: Structured fields for history, symptoms, allergies, medications
  - AC-2: Autosave every 30s with server persistence per UXR-505
  - AC-3: Server-side validation for required fields
  - AC-4: Field-level editing after submission per FR-017

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

## AI References (AI Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **AI Impact**  | No    |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build manual intake API: PUT autosave endpoint for periodic form state persistence, POST submit with full FluentValidation, PATCH field-level edit after submission, and GET to retrieve current intake state.

## Dependent Tasks

- task_001_be_modular_monolith_setup (US_002) — Requires Clinical module
- task_001_db_identity_scheduling_entities (US_008) — Requires IntakeRecord entity

## Impacted Components

- MODIFY: IntakeController — Add manual intake endpoints
- NEW: IManualIntakeService, ManualIntakeService
- NEW: ManualIntakeRequest, IntakeFieldUpdateRequest DTOs
- NEW: ManualIntakeValidator — FluentValidation rules

## Implementation Plan

1. Create PUT /api/clinical/intake/{appointmentId}/autosave for partial save
2. Create POST /api/clinical/intake/{appointmentId}/submit for final submission
3. Create PATCH /api/clinical/intake/{appointmentId}/fields for field-level edits
4. Create GET /api/clinical/intake/{appointmentId} to retrieve current state
5. Implement FluentValidation for required fields on submit
6. Store draft state as JSON column in IntakeRecord
7. Allow field-level editing without staff assistance (patient-accessible)

## Current Project State

```
[PLACEHOLDER - Updated after US_002, US_008 tasks]
```

## Expected Changes

| Action | File Path                                                | Description         |
| ------ | -------------------------------------------------------- | ------------------- |
| MODIFY | src/Modules/Clinical/Controllers/IntakeController.cs     | Manual endpoints    |
| CREATE | src/Modules/Clinical/Services/IManualIntakeService.cs    | Service interface   |
| CREATE | src/Modules/Clinical/Services/ManualIntakeService.cs     | Manual intake logic |
| CREATE | src/Modules/Clinical/DTOs/ManualIntakeRequest.cs         | Request DTO         |
| CREATE | src/Modules/Clinical/DTOs/IntakeFieldUpdateRequest.cs    | Field update DTO    |
| CREATE | src/Modules/Clinical/Validators/ManualIntakeValidator.cs | Validation rules    |

## External References

- FluentValidation: https://docs.fluentvalidation.net/en/latest/aspnet.html

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] PUT autosave persists partial form state
- [x] POST submit validates required fields
- [x] PATCH fields updates individual fields
- [x] GET retrieves current intake state including drafts
- [x] FluentValidation rejects invalid submissions

## Implementation Checklist

- [x] Create PUT autosave endpoint for periodic form persistence
- [x] Create POST submit endpoint with FluentValidation
- [x] Create PATCH field-level edit endpoint (patient-accessible)
- [x] Create GET endpoint to retrieve current intake state
- [x] Store draft state as JSON column in IntakeRecord
- [x] Implement FluentValidation for history, symptoms, allergies, medications
- [x] Return ProblemDetails for validation failures
