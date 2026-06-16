---
post_title: "TASK_001 - AI CPT Code Mapping Engine"
author1: "AI Senior Developer"
post_slug: "task-001-ai-cpt-mapping"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-008, US_039, AI, CPT, classification, coding"
ai_note: "Generated with AI assistance from user story US_039"
summary: "Implement AI-driven CPT code mapping from procedures/encounters with top-3 candidates and 'Suggested' status."
post_date: "2026-04-16"
---

# Task - TASK_001_AI_CPT_MAPPING_ENGINE

## Requirement Reference
- User Story: us_039
- Story Location: .propel/context/tasks/EP-008/us_039/us_039.md
- Acceptance Criteria:
    - AC-1: CPT mapping via classification + reference table per AIR-005
    - AC-3: MedicalCode with CodeType CPT, ConfidenceScore, Status "Suggested" per DR-007
    - AC-4: "Suggested" requiring staff verification per AIR-S04

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
| **AIR Requirements** | AIR-005, AIR-S04 |
| **AI Pattern** | Classification + CPT reference table lookup |
| **Prompt Template Path** | .propel/context/prompts/cpt-mapping.txt (to be created) |
| **Guardrails Config** | 4,096 tokens/request; status = "Suggested" |
| **Model Provider** | Ollama (Phi-3-mini 3.8B) |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build CPT code mapping engine: extract procedures/encounters from 360-view, classify using Semantic Kernel + Phi-3-mini, match against CPT reference table, rank top-3 candidates, store as MedicalCode with "Suggested" status.

## Dependent Tasks
- task_002_ai_icd10_mapping (US_038) — Shares MedicalCodingController and MedicalCode entity
- task_001_ai_runtime_orchestration_setup (US_005) — Semantic Kernel infrastructure

## Impacted Components
- NEW: ICptMappingService, CptMappingService
- NEW: CptLookupTable — CPT reference data
- MODIFY: MedicalCodingController — Add CPT endpoint

## Implementation Plan
1. Add POST /api/clinical/coding/cpt/{patientId} endpoint
2. Extract procedures/encounters from 360-view data
3. Classify entities using Semantic Kernel with Phi-3-mini
4. Match against CPT reference table for top-3 candidates
5. Rank by confidence score
6. Store MedicalCode with CodeType CPT and status "Suggested"
7. Seed CPT reference table with common procedure codes

## Current Project State
```
[PLACEHOLDER - Updated after US_005, US_038 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Clinical/AI/ICptMappingService.cs | Service interface |
| CREATE | src/Modules/Clinical/AI/CptMappingService.cs | CPT mapping |
| CREATE | src/Modules/Clinical/Data/CptLookupTable.cs | Reference data |
| MODIFY | src/Modules/Clinical/Controllers/MedicalCodingController.cs | Add CPT endpoint |

## External References
- AMA CPT codes: https://www.ama-assn.org/amaone/cpt-current-procedural-terminology

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] Procedures mapped to top-3 CPT candidates
- [x] Confidence scores computed per candidate
- [x] MedicalCode stored with status "Suggested"
- [x] Empty procedures returns "No procedures available"

## Implementation Checklist
- [x] Add CPT mapping endpoint to MedicalCodingController
- [x] Extract procedures/encounters from 360-view data
- [x] Classify using Semantic Kernel with Phi-3-mini
- [x] Match against CPT reference table for top-3 candidates
- [x] Store MedicalCode with CodeType CPT, status "Suggested"
- [x] Seed CPT reference table with common procedure codes
- [x] Handle no-procedure scenario gracefully
