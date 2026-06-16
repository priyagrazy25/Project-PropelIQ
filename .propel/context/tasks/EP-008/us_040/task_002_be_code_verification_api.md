---
post_title: "TASK_002 - Code Verification API"
author1: "AI Senior Developer"
post_slug: "task-002-be-code-verification-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-008, US_040, backend, ASP.NET Core, verification, agreement-rate, audit"
ai_note: "Generated with AI assistance from user story US_040"
summary: "Implement medical code verification API with accept/reject/override, agreement rate tracking, and audit trail."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_CODE_VERIFICATION_API

## Requirement Reference
- User Story: us_040
- Story Location: .propel/context/tasks/EP-008/us_040/us_040.md
- Acceptance Criteria:
    - AC-1: Accept/reject/override per AIR-S04
    - AC-2: MedicalCode transitions Suggested → Verified/Rejected with VerifiedBy, VerifiedAt per DR-007
    - AC-3: Rolling 30-day agreement rate targeting >98% per AIR-Q01
    - AC-5: Manual override preserved alongside AI suggestion

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
Build the verification API: PUT endpoint for accept/reject/override, update MedicalCode status with VerifiedBy/VerifiedAt, track rolling 30-day agreement rate, preserve AI suggestion alongside override, and enforce RBAC.

## Dependent Tasks
- task_002_ai_icd10_mapping (US_038) — Requires MedicalCode records
- task_001_ai_cpt_mapping (US_039) — Requires MedicalCode records

## Impacted Components
- NEW: CodeVerificationController — Verification endpoints
- NEW: ICodeVerificationService, CodeVerificationService
- NEW: AgreementRateTracker — Rolling 30-day metric

## Implementation Plan
1. Create PUT /api/clinical/coding/{codeId}/verify endpoint
2. Accept action type: accept, reject (with reason), override (with custom code)
3. Update MedicalCode: Suggested → Verified/Rejected, set VerifiedBy/VerifiedAt
4. Preserve AI suggestion when override is applied
5. Implement AgreementRateTracker computing 30-day rolling acceptance rate
6. Create GET /api/clinical/coding/agreement-rate endpoint
7. Create audit record for each verification action

## Current Project State
```
[PLACEHOLDER - Updated after US_038, US_039 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | src/Modules/Clinical/Clinical.API/Controllers/MedicalCodingController.cs | Add PUT verify with override, GET agreement-rate endpoints |
| CREATE | src/Modules/Clinical/Clinical.Application/Abstractions/ICodeVerificationService.cs | Service interface with DTOs |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/Services/CodeVerificationService.cs | Verification logic with audit |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/Services/AgreementRateTracker.cs | 30-day metric with caching |
| MODIFY | src/Modules/Clinical/Clinical.Domain/Entities/MedicalCode.cs | Add override fields |
| MODIFY | src/Modules/Clinical/Clinical.Domain/Enums/VerificationStatus.cs | Add Overridden status |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/ClinicalModuleExtensions.cs | Register new services |

## External References
- N/A

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] Accept transitions status to "Verified"
- [x] Reject requires reason and transitions to "Rejected"
- [x] Override stores custom code, preserves AI suggestion
- [x] 30-day agreement rate computed correctly
- [x] Audit record created for each action

## Implementation Checklist
- [x] Create PUT /coding/{codeId}/verify endpoint
- [x] Support accept, reject (with reason), override (with custom code) actions
- [x] Update MedicalCode status, VerifiedBy, VerifiedAt fields
- [x] Preserve AI suggestion alongside manual override
- [x] Implement 30-day rolling agreement rate calculation
- [x] Create GET agreement-rate endpoint for dashboard
- [x] Create audit record for each verification action
