---
post_title: "TASK_001 - Data Integrity & Validation Rules"
author1: "AI Senior Developer"
post_slug: "task-001-db-integrity-validation-rules"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-DATA, US_010, database, integrity, constraints, validation"
ai_note: "Generated with AI assistance from user story US_010"
summary: "Implement database-level referential integrity with cascading soft-delete, unique constraints, row-level locking, and confidence score validation."
post_date: "2026-04-16"
---

# Task - TASK_001_DB_INTEGRITY_VALIDATION_RULES

## Requirement Reference
- User Story: us_010
- Story Location: .propel/context/tasks/EP-DATA/us_010/us_010.md
- Acceptance Criteria:
    - AC-1: FK constraints with cascading soft-delete on patient-related records per DR-008
    - AC-2: Unique constraints on User.Email, Slot.ProviderID+DateTime per DR-009
    - AC-3: Row-level locking during slot reservation prevents double-booking per DR-009
    - AC-4: Confidence scores validated within 0.0-1.0 range per DR-010
    - AC-5: Data below 0.7 confidence flagged as "Low Confidence" per DR-010
- Edge Cases:
    - Concurrent slot booking race condition → pessimistic locking ensures first-wins
    - Orphaned records after cascade → soft-delete audit trail preserves history

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |
| **Figma URL** | N/A |
| **Wireframe Status** | N/A |
| **Wireframe Type** | N/A |
| **Wireframe Path/URL** | N/A |
| **Screen Spec** | N/A |
| **UXR Requirements** | N/A |
| **Design Tokens** | N/A |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Database | SQL Server Express | 2022 |
| ORM | Entity Framework Core | 8.0 |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |
| **AIR Requirements** | N/A |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | N/A |
| **Model Provider** | N/A |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |
| **Platform Target** | N/A |
| **Min OS Version** | N/A |
| **Mobile Framework** | N/A |

## Task Overview
Configure comprehensive data integrity rules across all entity schemas including referential integrity with cascading soft-delete, unique constraints preventing duplicate accounts and double-booking, row-level pessimistic locking for slot reservation, and domain-level confidence score validation with low-confidence flagging.

## Dependent Tasks
- task_001_db_identity_scheduling_entities (US_008) — Requires identity/scheduling entities
- task_001_db_clinical_ai_entities (US_009) — Requires clinical entities with confidence scores

## Impacted Components
- MODIFY: All entity configurations to add integrity constraints
- NEW: Global query filters for soft-delete
- NEW: Row-level locking interceptor for slot reservation
- NEW: Confidence score validation value object

## Implementation Plan
1. Add IsDeleted flag and soft-delete global query filters on all patient-related entities
2. Configure cascading soft-delete behavior in Fluent API (deactivate patient → cascade to appointments, documents, intake)
3. Add unique index on User.Email, composite unique index on AppointmentSlot(ProviderID, DateTime)
4. Implement UPDLOCK/ROWLOCK hints for slot reservation via raw SQL or EF Core interceptor
5. Create ConfidenceScore value object with 0.0-1.0 range validation and LowConfidence flag at < 0.7
6. Add CHECK constraints at database level for confidence scores
7. Verify all integrity rules via integration tests

## Current Project State
```
[PLACEHOLDER - Updated after US_008, US_009 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | backend/src/Shared/SharedKernel/Entities/BaseEntity.cs | Add IsDeleted, DeletedAt soft-delete properties |
| MODIFY | backend/src/Modules/*/Infrastructure/Data/*DbContext.cs | Add global query filters for soft-delete |
| CREATE | backend/src/Shared/SharedKernel/ValueObjects/ConfidenceScore.cs | Value object with validation |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Infrastructure/Data/SlotLockingInterceptor.cs | Row-level locking for slot reservation |
| CREATE | backend/migrations/V004_IntegrityConstraints.sql | Additional SQL constraints |

## External References
- EF Core Global Query Filters: https://learn.microsoft.com/en-us/ef/core/querying/filters
- SQL Server Locking Hints: https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table

## Build Commands
- `dotnet ef migrations add V004_IntegrityConstraints` — Generate migration
- `dotnet ef database update` — Apply migration

## Implementation Validation Strategy
- [x] Soft-delete cascades correctly from Patient to related records
- [x] Duplicate User.Email insert is rejected
- [x] Concurrent slot booking attempts — only first succeeds
- [x] Confidence score outside 0.0-1.0 is rejected
- [x] Values below 0.7 are flagged as LowConfidence

## Implementation Checklist
- [x] Add IsDeleted/DeletedAt properties to BaseEntity and configure global query filters
- [x] Configure cascading soft-delete behavior for patient-related entity chains
- [x] Add unique index on User.Email and composite unique on Slot(ProviderID, DateTime)
- [x] Implement row-level locking for slot reservation (UPDLOCK/ROWLOCK)
- [x] Create ConfidenceScore value object with 0.0-1.0 validation and < 0.7 LowConfidence flag
- [x] Add database CHECK constraints for confidence scores
- [x] Generate and apply migration V004_IntegrityConstraints
