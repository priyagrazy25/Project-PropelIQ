---
post_title: "TASK_002 - Conflict Resolution API"
author1: "AI Senior Developer"
post_slug: "task-002-be-conflict-resolution-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-007, US_037, backend, ASP.NET Core, conflict-resolution, audit"
ai_note: "Generated with AI assistance from user story US_037"
summary: "Implement conflict resolution API with accept/override actions, audit trail, optimistic concurrency, and 7-year retention."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_CONFLICT_RESOLUTION_API

## Requirement Reference
- User Story: us_037
- Story Location: .propel/context/tasks/EP-007/us_037/us_037.md
- Acceptance Criteria:
    - AC-2: Resolution (accept A, B, or manual override) updates 360-view + conflict status
    - AC-3: Audit record: staff identity, action, before/after, timestamp
    - AC-4: Retention: account lifecycle + 7 years per DR-012

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
Build conflict resolution API: PUT endpoint accepting resolution action (accept-a, accept-b, manual-override), update 360-view with resolved value, create audit record, implement optimistic concurrency for simultaneous resolution, and enforce retention policy.

## Dependent Tasks
- task_002_be_conflict_detection_api (US_036) — Requires DataConflict entity
- task_002_be_360_aggregation_api (US_035) — Requires 360-view update

## Impacted Components
- NEW: ConflictResolutionController — Resolution endpoint
- NEW: IConflictResolutionService, ConflictResolutionService
- MODIFY: PatientView360Service — Update resolved data

## Implementation Plan
1. Create PUT /api/clinical/conflicts/{conflictId}/resolve endpoint
2. Accept resolution action (accept-a, accept-b, manual-override with value)
3. Apply resolved value to PatientView360 entity
4. Update DataConflict status to "Resolved"
5. Create audit record with before/after state, staff identity, timestamp
6. Implement optimistic concurrency on DataConflict entity
7. Invalidate Redis cache for affected patient 360-view

## Current Project State
```
✅ IConflictResolutionService.cs - Interface with GetConflictDetailAsync, ResolveConflictAsync
✅ ConflictResolutionService.cs - Resolution logic with audit trail, cache invalidation
✅ ConflictResolutionController.cs - GET /{conflictId}, POST /{conflictId}/resolve endpoints
✅ ClinicalModuleExtensions.cs - Registered IConflictResolutionService (scoped)
✅ Build: 0 errors, 0 warnings
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Clinical/Clinical.Application/Abstractions/IConflictResolutionService.cs | Service interface with DTOs |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/Services/ConflictResolutionService.cs | Resolution logic + audit |
| CREATE | src/Modules/Clinical/Clinical.API/Controllers/ConflictResolutionController.cs | GET/POST endpoints |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/ClinicalModuleExtensions.cs | DI registration |

## External References
- N/A

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] PUT /resolve updates conflict status and 360-view
- [x] Audit record captures staff, action, before/after
- [x] Optimistic concurrency prevents double-resolution
- [x] Redis cache invalidated on resolution
- [x] Manual override stores custom value

## Implementation Checklist
- [x] Create PUT /conflicts/{conflictId}/resolve endpoint
- [x] Support accept-a, accept-b, manual-override actions
- [x] Apply resolved value to PatientView360 entity
- [x] Update DataConflict status to "Resolved"
- [x] Create audit record with before/after and staff identity
- [x] Implement optimistic concurrency on DataConflict
- [x] Invalidate Redis cache for affected patient 360-view
