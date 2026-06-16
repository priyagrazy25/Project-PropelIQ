---
post_title: "TASK_002 - Insurance Pre-Check API"
author1: "AI Senior Developer"
post_slug: "task-002-be-insurance-precheck-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-004, US_027, backend, ASP.NET Core, insurance, validation"
ai_note: "Generated with AI assistance from user story US_027"
summary: "Implement insurance pre-check API with name/ID matching against seeded records and tri-state validation response."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_INSURANCE_PRECHECK_API

## Requirement Reference

- User Story: us_027
- Story Location: .propel/context/tasks/EP-004/us_027/us_027.md
- Acceptance Criteria:
  - AC-1: Check against predefined dummy records inline per UXR-105
  - AC-2: Full match → "Verified"
  - AC-3: Partial match (name matches, ID mismatch) → "Unverified - Member ID mismatch"
  - AC-4: No match → "Unverified - Insurance not recognized" flagged for staff
  - AC-5: Staff can view verification status on dashboard

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

Build insurance validation endpoint: POST with insurance name and member ID, query seeded insurance records with case-insensitive matching, return tri-state result (Verified/PartialMatch/Unrecognized), persist verification status linked to appointment for staff dashboard.

## Dependent Tasks

- task_001_be_modular_monolith_setup (US_002) — Requires Clinical module
- task_001_db_backup_seed_data (US_011) — Requires seeded insurance records

## Impacted Components

- NEW: InsuranceController — Pre-check endpoint
- NEW: IInsuranceValidationService, InsuranceValidationService
- NEW: InsuranceCheckRequest, InsuranceCheckResult DTOs

## Implementation Plan

1. Create POST /api/clinical/insurance/validate endpoint
2. Query seeded insurance records with case-insensitive name match
3. Implement tri-state logic: full match, partial (name only), no match
4. Sanitize input: trim, remove special chars, case-normalize
5. Persist verification status (InsuranceVerificationStatus) on appointment
6. Create GET /api/clinical/insurance/{appointmentId}/status for staff dashboard
7. Return "Verification unavailable" when records table is empty

## Current Project State

```
[PLACEHOLDER - Updated after US_002, US_011 tasks]
```

## Expected Changes

| Action | File Path                                                    | Description        |
| ------ | ------------------------------------------------------------ | ------------------ |
| CREATE | src/Modules/Clinical/Controllers/InsuranceController.cs      | Pre-check endpoint |
| CREATE | src/Modules/Clinical/Services/IInsuranceValidationService.cs | Service interface  |
| CREATE | src/Modules/Clinical/Services/InsuranceValidationService.cs  | Validation logic   |
| CREATE | src/Modules/Clinical/DTOs/InsuranceCheckRequest.cs           | Request DTO        |
| CREATE | src/Modules/Clinical/DTOs/InsuranceCheckResult.cs            | Result DTO         |
| MODIFY | src/Modules/Clinical/DependencyInjection.cs                  | Register service   |

## External References

- N/A

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] POST /validate returns Verified for full match
- [x] Returns PartialMatch when name matches but ID differs
- [x] Returns Unrecognized when no match
- [x] Empty records returns "Verification unavailable"
- [x] Staff GET /status shows verification result

## Implementation Checklist

- [x] Create POST /api/clinical/insurance/validate endpoint
- [x] Implement case-insensitive matching against seeded records
- [x] Return tri-state result (Verified/PartialMatch/Unrecognized)
- [x] Sanitize input (trim, special chars, case-normalize)
- [x] Persist verification status on appointment entity
- [x] Create GET status endpoint for staff dashboard viewing
- [x] Handle empty records table with "Verification unavailable"
