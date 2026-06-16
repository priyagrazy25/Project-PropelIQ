---
post_title: "TASK_002 - AI ICD-10 Mapping Engine"
author1: "AI Senior Developer"
post_slug: "task-002-ai-icd10-mapping"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-008, US_038, AI, ICD-10, NER, lookup, coding"
ai_note: "Generated with AI assistance from user story US_038"
summary: "Implement AI-driven ICD-10-CM mapping using NER classification and lookup table with top-3 confidence-ranked candidates."
post_date: "2026-04-16"
---

# Task - TASK_002_AI_ICD10_MAPPING_ENGINE

## Requirement Reference
- User Story: us_038
- Story Location: .propel/context/tasks/EP-008/us_038/us_038.md
- Acceptance Criteria:
    - AC-1: ICD-10-CM mapping via NER classification + lookup table per AIR-004
    - AC-3: 4,096 token budget per request per AIR-O01
    - AC-4: MedicalCode with CodeType ICD10, ConfidenceScore, Status "Suggested" per DR-007
    - AC-5: "Suggested" status requiring staff verification per AIR-S04

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| AI-Orchestration | Semantic Kernel | 1.x (.NET) |
| AI-LLM | Ollama (Phi-3-mini) | 0.3.x |
| Backend | ASP.NET Core | 8.0 LTS |
| Database | SQL Server Express | 2022 |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-004, AIR-O01, AIR-S04 |
| **AI Pattern** | NER classification + ICD-10 lookup table matching |
| **Prompt Template Path** | .propel/context/prompts/icd10-mapping.txt (to be created) |
| **Guardrails Config** | 4,096 tokens/request; status = "Suggested" |
| **Model Provider** | Ollama (Phi-3-mini 3.8B) |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the ICD-10 mapping engine: convert 360-view diagnoses into NER-classified entities, match against ICD-10-CM lookup table, rank top-3 candidates by confidence, enforce token budget, and store as MedicalCode with "Suggested" status.

## Dependent Tasks
- task_001_ai_runtime_orchestration_setup (US_005) — Semantic Kernel + Ollama
- task_002_be_360_aggregation_api (US_035) — Provides diagnosis data

## Impacted Components
- NEW: IIcd10MappingService, Icd10MappingService
- NEW: Icd10LookupTable — ICD-10-CM reference data
- NEW: MedicalCodingController — Mapping endpoints

## Implementation Plan
1. Create POST /api/clinical/coding/icd10/{patientId} endpoint
2. Extract diagnoses from 360-view PatientView360 data
3. Classify entities using Semantic Kernel with Phi-3-mini
4. Match classified entities against ICD-10-CM lookup table
5. Rank top-3 candidates by confidence score
6. Enforce 4,096 token budget per request
7. Store MedicalCode records with status "Suggested"
8. Seed ICD-10-CM lookup table (common codes subset)

## Current Project State
```
[PLACEHOLDER - Updated after US_005, US_035 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Clinical/Controllers/MedicalCodingController.cs | Coding endpoints |
| CREATE | src/Modules/Clinical/AI/IIcd10MappingService.cs | Service interface |
| CREATE | src/Modules/Clinical/AI/Icd10MappingService.cs | ICD-10 mapping |
| CREATE | src/Modules/Clinical/Data/Icd10LookupTable.cs | Reference data |

## External References
- ICD-10-CM: https://www.cms.gov/medicare/coding-billing/icd-10-codes

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] Diagnoses mapped to top-3 ICD-10-CM candidates
- [x] Confidence scores computed per candidate
- [x] Token budget limited to 4,096 per request
- [x] MedicalCode stored with "Suggested" status
- [x] Empty diagnoses returns "No diagnoses available"

## Implementation Checklist
- [x] Create ICD-10 mapping endpoint
- [x] Extract diagnoses from 360-view data
- [x] Classify entities using Semantic Kernel with Phi-3-mini
- [x] Match against ICD-10-CM lookup table for top-3 candidates
- [x] Enforce 4,096 token budget per request
- [x] Store MedicalCode with status "Suggested" per AIR-S04
- [x] Seed ICD-10-CM lookup table with common codes
- [x] Handle no-diagnosis scenario gracefully
