---
post_title: "TASK_001 - Clinical & AI Entity Schema"
author1: "AI Senior Developer"
post_slug: "task-001-db-clinical-ai-entities"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-DATA, US_009, database, entities, ClinicalDocument, ExtractedData, PatientView360, MedicalCode"
ai_note: "Generated with AI assistance from user story US_009"
summary: "Create EF Core entity configurations for ClinicalDocument, ExtractedData, PatientView360, DataConflict, MedicalCode, AuditLog, and NoShowRiskScore."
post_date: "2026-04-16"
---

# Task - TASK_001_DB_CLINICAL_AI_ENTITIES

## Requirement Reference
- User Story: us_009
- Story Location: .propel/context/tasks/EP-DATA/us_009/us_009.md
- Acceptance Criteria:
    - AC-1: ClinicalDocument entity with encrypted storage path and processing status per DR-004
    - AC-2: ExtractedData with confidence scores (0.0-1.0) and source page references per DR-005
    - AC-3: PatientView360 with JSON-structured clinical sections per DR-006
    - AC-4: MedicalCode with ICD-10/CPT types and verification workflow per DR-007
    - AC-5: AuditLog as append-only table (no UPDATE/DELETE permissions) per DR-011
    - AC-6: NoShowRiskScore entity linked to Appointment
- Edge Cases:
    - Confidence score outside 0.0-1.0 → database CHECK constraint rejects insert
    - AuditLog UPDATE/DELETE attempted → SQL Server denies with permission error

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
| Vector Store | SQL Server custom vector table | N/A |

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
Create all clinical data and AI-related domain entities with EF Core configurations. This includes ClinicalDocument (encrypted storage, processing pipeline), ExtractedData (confidence-scored data points), PatientView360 (JSON-structured aggregated view), DataConflict (severity-classified contradictions), MedicalCode (ICD-10/CPT with verification), AuditLog (immutable append-only), NoShowRiskScore (ML prediction storage), and the custom vector table for document embeddings.

## Dependent Tasks
- task_001_db_schema_orm_setup (US_003) — Requires DbContext infrastructure
- task_001_db_identity_scheduling_entities (US_008) — Requires PatientID/AppointmentID FKs

## Impacted Components
- NEW: Clinical module entities (ClinicalDocument, ExtractedData, PatientView360, DataConflict, MedicalCode)
- NEW: Shared entities (AuditLog, NoShowRiskScore)
- NEW: Custom vector table for document embeddings
- NEW: EF Core migration V003_ClinicalAIEntities

## Implementation Plan
1. Create ClinicalDocument entity (DocumentID, PatientID FK, EncryptedFilePath, ProcessingStatus enum)
2. Create ExtractedData entity (DataCategory enum, ConfidenceScore with CHECK constraint [0.0-1.0], SourcePageReference)
3. Create PatientView360 entity with JSON columns (Vitals, MedicalHistory, Medications, Allergies, LabResults, Diagnoses)
4. Create DataConflict entity (Severity enum: Critical/Warning, resolution status, staff notes)
5. Create MedicalCode entity (CodeType enum: ICD10/CPT, verification Status enum, VerifiedBy FK)
6. Create AuditLog entity (append-only, ActorID, Action, Resource, BeforeState, AfterState, Timestamp)
7. Create NoShowRiskScore entity (AppointmentID FK, Score 0-100, CalculatedAt)
8. Create DocumentEmbedding vector table with indexed float arrays for cosine similarity

## Current Project State
```
[PLACEHOLDER - Updated after US_003, US_008 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | backend/src/Modules/Clinical/Clinical.Domain/Entities/ClinicalDocument.cs | Document with encrypted path and processing status |
| CREATE | backend/src/Modules/Clinical/Clinical.Domain/Entities/ExtractedData.cs | Confidence-scored extracted data points |
| CREATE | backend/src/Modules/Clinical/Clinical.Domain/Entities/PatientView360.cs | JSON-structured aggregated view |
| CREATE | backend/src/Modules/Clinical/Clinical.Domain/Entities/DataConflict.cs | Severity-classified conflict records |
| CREATE | backend/src/Modules/Clinical/Clinical.Domain/Entities/MedicalCode.cs | ICD-10/CPT with verification workflow |
| CREATE | backend/src/Modules/Clinical/Clinical.Domain/Entities/DocumentEmbedding.cs | Vector table for embeddings |
| CREATE | backend/src/Shared/SharedKernel/Entities/AuditLog.cs | Immutable audit record |
| CREATE | backend/src/Modules/Scheduling/Scheduling.Domain/Entities/NoShowRiskScore.cs | Risk score entity |
| CREATE | scripts/audit-log-permissions.sql | DENY UPDATE/DELETE on AuditLog table |

## External References
- EF Core JSON Columns: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/whatsnew#json-columns
- SQL Server CHECK Constraints: https://learn.microsoft.com/en-us/sql/relational-databases/tables/create-check-constraints

## Build Commands
- `dotnet ef migrations add V003_ClinicalAIEntities` — Generate migration
- `dotnet ef database update` — Apply migration
- `sqlcmd -i scripts/audit-log-permissions.sql` — Apply audit log permissions

## Implementation Validation Strategy
- [x] Migration applies without errors
- [x] ConfidenceScore CHECK constraint rejects values outside 0.0-1.0
- [x] AuditLog INSERT succeeds, UPDATE/DELETE denied
- [x] JSON columns store and retrieve structured clinical data

## Implementation Checklist
- [x] Create ClinicalDocument entity with processing status enum and encrypted file path
- [x] Create ExtractedData entity with CHECK constraint on ConfidenceScore (0.0-1.0)
- [x] Create PatientView360 with JSON columns for clinical sections
- [x] Create DataConflict entity with Severity enum and resolution workflow
- [x] Create MedicalCode entity with CodeType, verification Status, VerifiedBy FK
- [x] Create AuditLog entity and SQL permission script (DENY UPDATE/DELETE)
- [x] Create NoShowRiskScore and DocumentEmbedding (vector table with indexed floats)
- [x] Generate and apply EF Core migration V003_ClinicalAIEntities
