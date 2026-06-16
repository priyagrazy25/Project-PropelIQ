---
post_title: "TASK_001 - NER Clinical Data Extraction"
author1: "AI Senior Developer"
post_slug: "task-001-ai-ner-clinical-extraction"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-006, US_033, AI, scispaCy, NER, biomedical, confidence-scoring"
ai_note: "Generated with AI assistance from user story US_033"
summary: "Implement scispaCy biomedical NER extraction with confidence scoring, schema validation, and low-confidence flagging."
post_date: "2026-04-16"
---

# Task - TASK_001_AI_NER_CLINICAL_EXTRACTION

## Requirement Reference
- User Story: us_033
- Story Location: .propel/context/tasks/EP-006/us_033/us_033.md
- Acceptance Criteria:
    - AC-1: scispaCy (en_ner_bc5cdr_md) identifies medications, diseases, chemicals, vitals, labs per AIR-001
    - AC-2: ExtractedData with DataCategory, DataKey, DataValue, Unit, ConfidenceScore, SourcePage per DR-005
    - AC-3: 99% structured output schema validity per AIR-Q03
    - AC-4: 90% extraction recall per AIR-Q04
    - AC-5: Confidence < 0.7 flagged as "Low Confidence" per DR-010

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| AI-NER | scispaCy | 0.5.x |
| AI-NER Model | en_ner_bc5cdr_md | Latest |
| Backend | Python microservice (FastAPI) | 3.11+ |
| Database | SQL Server Express | 2022 |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-001, AIR-Q03, AIR-Q04, AIR-S02 |
| **AI Pattern** | Biomedical Named Entity Recognition |
| **Prompt Template Path** | N/A (model-based pipeline) |
| **Guardrails Config** | Confidence threshold 0.7; schema validation 99%; recall target 90% |
| **Model Provider** | scispaCy (en_ner_bc5cdr_md) |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the NER extraction service as a Python microservice (FastAPI) calling scispaCy en_ner_bc5cdr_md on PII-redacted text chunks. Map extracted entities to DataCategory/DataKey/DataValue/Unit. Compute per-entity confidence. Validate output against JSON schema. Flag entities below 0.7 confidence. Store ExtractedData records.

## Dependent Tasks
- task_001_ai_ocr_extraction_pipeline (US_032) — Provides chunked PII-redacted text
- task_001_ai_runtime_orchestration_setup (US_005) — scispaCy microservice infrastructure
- task_001_db_clinical_ai_entities (US_009) — Requires ExtractedData entity

## Impacted Components
- NEW: NerExtractionService (Python FastAPI microservice)
- NEW: EntityMapper — Map scispaCy entities to ExtractedData schema
- NEW: ConfidenceScorer — Compute per-entity confidence
- NEW: SchemaValidator — Validate output JSON schema
- NEW: .NET NerClient — HTTP client calling Python service

## Implementation Plan
1. Create Python FastAPI microservice loading scispaCy en_ner_bc5cdr_md
2. Implement NER endpoint accepting text chunks, returning typed entities
3. Build EntityMapper mapping scispaCy labels to DataCategory/DataKey/DataValue/Unit
4. Implement ConfidenceScorer using entity probability + context heuristics
5. Build SchemaValidator enforcing 99% output schema validity
6. Flag entities below 0.7 confidence as "Low Confidence"
7. Create .NET NerClient calling Python service via HTTP
8. Store ExtractedData records with SourcePageReference

## Current Project State
```
ai-services/ner-service/
├── app.py (updated - v2.0.0 with clinical extraction)
├── entity_mapper.py (new - scispaCy label to DataCategory mapping)
├── confidence_scorer.py (new - weighted confidence scoring)
├── schema_validator.py (new - 99% validity enforcement)
└── requirements.txt (existing)

backend/src/Modules/Clinical/
├── Clinical.Application/AI/
│   ├── INerService.cs (updated - added ExtractClinicalEntitiesAsync)
│   └── INerExtractionPipeline.cs (new)
├── Clinical.Infrastructure/AI/
│   ├── ScispaCyNerService.cs (updated - clinical extraction support)
│   ├── NerExtractionPipeline.cs (new)
│   └── NerProcessingWorker.cs (new)
└── Clinical.Infrastructure/ClinicalModuleExtensions.cs (updated - NER pipeline registration)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | ai-services/ner-service/app.py | Enhanced FastAPI NER with /extract/clinical endpoint |
| CREATE | ai-services/ner-service/entity_mapper.py | scispaCy label to DataCategory mapping |
| CREATE | ai-services/ner-service/confidence_scorer.py | Weighted confidence scoring with 0.7 threshold |
| CREATE | ai-services/ner-service/schema_validator.py | 99% output validity enforcement |
| MODIFY | src/Modules/Clinical/Clinical.Application/AI/INerService.cs | Added ExtractClinicalEntitiesAsync method |
| CREATE | src/Modules/Clinical/Clinical.Application/AI/INerExtractionPipeline.cs | Pipeline interface |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/AI/ScispaCyNerService.cs | Clinical extraction support |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/NerExtractionPipeline.cs | Full NER pipeline orchestration |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/NerProcessingWorker.cs | Background worker |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/ClinicalModuleExtensions.cs | NER pipeline DI registration |

## External References
- scispaCy: https://allenai.github.io/scispacy/
- en_ner_bc5cdr_md: https://allenai.github.io/scispacy/models

## Build Commands
- `pip install -r services/ner-service/requirements.txt` — Install Python deps
- `dotnet build` — Build .NET solution

## Implementation Validation Strategy
- [x] scispaCy extracts medications, diseases, chemicals, vitals, labs
- [x] ExtractedData includes all required fields per DR-005
- [x] Output schema validity ≥ 99%
- [x] Entities below 0.7 confidence flagged
- [x] Extraction recall target ≥ 90% against test set

## Implementation Checklist
- [x] Create Python FastAPI microservice with scispaCy en_ner_bc5cdr_md
- [x] Implement NER endpoint extracting biomedical entities from chunks
- [x] Build EntityMapper mapping labels to DataCategory/Key/Value/Unit
- [x] Implement ConfidenceScorer with per-entity confidence computation
- [x] Build SchemaValidator enforcing 99% output validity
- [x] Flag entities below 0.7 as "Low Confidence" per DR-010
- [x] Create .NET NerClient HTTP client for service integration
- [x] Store ExtractedData records with SourcePageReference
