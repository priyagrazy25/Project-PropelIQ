---
post_title: "TASK_002 - 360 View Aggregation API"
author1: "AI Senior Developer"
post_slug: "task-002-be-360-view-aggregation"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-007, US_035, backend, ASP.NET Core, aggregation, caching, 360-view"
ai_note: "Generated with AI assistance from user story US_035"
summary: "Implement 360-view aggregation API with semantic de-duplication, Redis caching, and 3-second response target."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_360_VIEW_AGGREGATION_API

## Requirement Reference
- User Story: us_035
- Story Location: .propel/context/tasks/EP-007/us_035/us_035.md
- Acceptance Criteria:
    - AC-1: Aggregate across documents with semantic de-duplication per AIR-002
    - AC-4: 99% schema validity per AIR-Q03
    - AC-5: 3-second response with Redis 15-min TTL per NFR-004 / NFR-017

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
| Caching | Upstash Redis | 7.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes (calls RAG pipeline) |
| **AIR Requirements** | AIR-002, AIR-Q03 |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the 360-view aggregation API: collect ExtractedData across all patient documents, de-duplicate equivalent entries (brand/generic medication), validate output schema, store PatientView360 JSON, and cache in Redis with 15-min TTL.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires Clinical module
- task_001_db_clinical_ai_entities (US_009) — Requires PatientView360 + ExtractedData entities
- task_001_ai_ner_clinical_extraction (US_033) — Provides extracted data

## Impacted Components
- NEW: PatientView360Controller — GET endpoint
- NEW: IPatientView360Service, PatientView360Service
- NEW: SemanticDeduplicator — Brand/generic merge logic

## Implementation Plan
1. Create GET /api/clinical/patients/{patientId}/360-view endpoint
2. Aggregate ExtractedData across all patient documents grouped by category
3. Implement SemanticDeduplicator merging equivalent entries
4. Validate output against predefined JSON schema (99% validity)
5. Store/update PatientView360 entity with aggregated JSON
6. Cache response in Redis with 15-minute TTL
7. Return categorized data with confidence scores and source references
8. Implement cache invalidation on new document processing

## Current Project State
```
[PLACEHOLDER - Updated after US_002, US_009, US_033 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Clinical/Clinical.API/Controllers/PatientView360Controller.cs | 360 endpoint ✅ |
| CREATE | src/Modules/Clinical/Clinical.Application/Abstractions/IPatientView360Service.cs | Service interface ✅ |
| CREATE | src/Modules/Clinical/Clinical.Application/Abstractions/IPatientDemographicsService.cs | Cross-module abstraction ✅ |
| CREATE | src/Modules/Clinical/Clinical.Application/DTOs/PatientView360Response.cs | Response DTOs ✅ |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/Services/PatientView360Service.cs | Aggregation logic ✅ |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/SemanticDeduplicator.cs | De-duplication ✅ |
| CREATE | src/Host/Services/PatientDemographicsService.cs | Cross-module impl ✅ |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/ClinicalModuleExtensions.cs | Service registration ✅ |
| MODIFY | src/Host/Program.cs | DI registration ✅ |

## External References
- N/A

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] GET returns aggregated data across all patient documents
- [x] Semantic de-duplication merges brand/generic names
- [x] Response cached in Redis with 15-min TTL (CacheTier.L3)
- [x] Response within 3 seconds on cache hit
- [x] Output schema validity ≥ 99%

## Implementation Checklist
- [x] Create GET /patients/{patientId}/360-view endpoint
- [x] Aggregate ExtractedData across documents by category
- [x] Implement SemanticDeduplicator for equivalent entries
- [x] Validate output against JSON schema (99% validity)
- [x] Cache response in Redis with 15-minute TTL
- [x] Invalidate cache on new document processing
- [x] Return categorized data with confidence scores and sources
