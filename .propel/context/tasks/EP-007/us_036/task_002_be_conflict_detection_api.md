---
post_title: "TASK_002 - Conflict Detection API"
author1: "AI Senior Developer"
post_slug: "task-002-be-conflict-detection-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-007, US_036, backend, ASP.NET Core, conflict, severity, clinical"
ai_note: "Generated with AI assistance from user story US_036"
summary: "Implement cross-document conflict detection with severity classification, confidence scoring, and DataConflict persistence."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_CONFLICT_DETECTION_API

## Requirement Reference
- User Story: us_036
- Story Location: .propel/context/tasks/EP-007/us_036/us_036.md
- Acceptance Criteria:
    - AC-1: Contradictions flagged (medications, allergies, diagnoses) per FR-026 / AIR-006
    - AC-2: DataConflict with severity (Critical/Warning), source references, conflicting values

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
| **AI Impact** | Yes (conflict detection uses NER output) |
| **AIR Requirements** | AIR-006 |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build conflict detection engine: compare ExtractedData entries of the same category across documents, identify contradictions (different values for same DataKey), classify severity (Critical for medications/allergies, Warning for others), create DataConflict records with source references.

## Dependent Tasks
- task_002_be_360_aggregation_api (US_035) — Runs during 360-view aggregation
- task_001_db_clinical_ai_entities (US_009) — Requires DataConflict entity

## Impacted Components
- NEW: IConflictDetectionService, ConflictDetectionService
- NEW: SeverityClassifier — Critical vs Warning rules
- MODIFY: PatientView360Service — Integrate conflict detection

## Implementation Plan
1. Create ConflictDetectionService comparing same-category entries
2. Identify contradictions: different DataValue for same DataKey across documents
3. Implement SeverityClassifier: medications/allergies = Critical; others = Warning
4. Create DataConflict records with both source references and conflicting values
5. Handle intra-document conflicts with "Same Source" indicator
6. Integrate into 360-view aggregation pipeline
7. Return conflicts alongside 360-view data in API response

## Current Project State
```
✅ IConflictDetectionService.cs - Interface with DetectConflictsAsync, PersistConflictsAsync methods
✅ SeverityClassifier.cs - CriticalCategories: {Medication, Allergy}; Classify() returns Critical/Warning
✅ ConflictDetectionService.cs - Groups by Category:Key, compares values, creates DataConflict entities
✅ PatientView360Service.cs - Calls conflict detection after loading extractedData; links ConflictId to data points
✅ ClinicalModuleExtensions.cs - Registered SeverityClassifier (singleton) and IConflictDetectionService (scoped)
✅ Build: 0 errors, 5 warnings (xUnit unrelated)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Clinical/Clinical.Application/Abstractions/IConflictDetectionService.cs | Service interface (FR-026, AIR-006) |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/ConflictDetectionService.cs | Detection logic with cross-document comparison |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/SeverityClassifier.cs | Severity rules (Critical: Medication/Allergy; Warning: others) |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/Services/PatientView360Service.cs | Integrated detection + conflict-datapoint linking |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/ClinicalModuleExtensions.cs | DI registration for services |

## External References
- N/A

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] Contradicting entries across documents flagged as conflicts
- [x] Severity: medications/allergies = Critical, others = Warning
- [x] DataConflict includes source references and conflicting values
- [x] Intra-document conflicts flagged with "Same Source"
- [x] No-conflict scenario returns empty conflicts array

## Implementation Checklist
- [x] Create ConflictDetectionService comparing same-category entries
- [x] Identify contradictions for same DataKey across documents
- [x] Implement SeverityClassifier (Critical/Warning rules)
- [x] Create DataConflict records with source references
- [x] Handle intra-document conflicts with "Same Source" indicator
- [x] Integrate into 360-view aggregation pipeline
- [x] Return conflicts in 360-view API response
