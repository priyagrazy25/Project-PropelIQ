---
post_title: "TASK_001 - OCR & Text Extraction Pipeline"
author1: "AI Senior Developer"
post_slug: "task-001-ai-ocr-extraction-pipeline"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-006, US_032, AI, Tesseract, OCR, PII-redaction, chunking"
ai_note: "Generated with AI assistance from user story US_032"
summary: "Implement Tesseract OCR pipeline for scanned PDFs with PII redaction, 512-token chunking, and quality scoring."
post_date: "2026-04-16"
---

# Task - TASK_001_AI_OCR_EXTRACTION_PIPELINE

## Requirement Reference
- User Story: us_032
- Story Location: .propel/context/tasks/EP-006/us_032/us_032.md
- Acceptance Criteria:
    - AC-1: Tesseract OCR 5.x extracts text from all pages per TR-008
    - AC-2: Processing within 5 min for ≤20 pages per NFR-003
    - AC-3: PII (name, SSN, DOB, contact) redacted before NLP per AIR-S02
    - AC-4: Failure → status "Failed" with ProcessingError per DR-004
    - AC-5: Text chunked into 512-token / 51-token overlap per AIR-R01

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| AI-OCR | Tesseract OCR (.NET wrapper) | 5.x |
| Backend | ASP.NET Core | 8.0 LTS |
| Database | SQL Server Express | 2022 |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes |
| **AIR Requirements** | AIR-001, AIR-S02, AIR-R01 |
| **AI Pattern** | OCR extraction → PII redaction → chunking |
| **Prompt Template Path** | N/A (rule-based pipeline) |
| **Guardrails Config** | 512-token chunks, 10% overlap, 5-min max processing |
| **Model Provider** | Tesseract OCR 5.x |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the OCR extraction pipeline: dequeue document from processing queue, run Tesseract OCR on all pages, apply PII redaction (regex + pattern matching for name, SSN, DOB, contact), chunk text into 512-token segments with 51-token overlap, and update document status. Enforce 5-minute processing timeout.

## Dependent Tasks
- task_002_be_document_upload_api (US_031) — Documents queued for processing
- task_001_ai_runtime_orchestration_setup (US_005) — Tesseract .NET wrapper setup

## Impacted Components
- NEW: IOcrExtractionPipeline, OcrExtractionPipeline
- NEW: PiiRedactor — Regex-based PII redaction
- NEW: TextChunker — 512-token / 51-overlap chunking
- NEW: OcrProcessingWorker — Background dequeue worker

## Implementation Plan
1. Create OcrProcessingWorker dequeuing documents from processing queue
2. Implement Tesseract OCR extraction for all PDF pages
3. Build PiiRedactor with regex patterns for name, SSN, DOB, phone, email
4. Implement TextChunker with 512-token segments and 51-token overlap
5. Update ClinicalDocument status (Processing → Complete or Failed)
6. Store extracted chunks to DocumentChunk entity
7. Enforce 5-minute timeout with CancellationToken
8. Handle blank pages and garbled output with quality flags

## Current Project State
```
backend/src/Modules/Clinical/
├── Clinical.Application/AI/
│   ├── IOcrService.cs (existing)
│   └── IOcrExtractionPipeline.cs (new)
├── Clinical.Infrastructure/AI/
│   ├── TesseractOcrService.cs (existing)
│   ├── OcrExtractionPipeline.cs (new)
│   ├── PiiRedactor.cs (new)
│   ├── TextChunker.cs (new)
│   └── OcrProcessingWorker.cs (new)
├── Clinical.Domain/Entities/
│   ├── ClinicalDocument.cs (updated - added Chunks navigation)
│   └── DocumentChunk.cs (new)
└── Clinical.Infrastructure/Data/
    ├── ClinicalDbContext.cs (updated - added DocumentChunks DbSet)
    └── Configurations/DocumentChunkConfiguration.cs (new)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Clinical/Clinical.Application/AI/IOcrExtractionPipeline.cs | Pipeline interface |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/OcrExtractionPipeline.cs | OCR extraction orchestrator |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/PiiRedactor.cs | Regex-based PII redaction |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/TextChunker.cs | 512-token / 51-overlap chunker |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/AI/OcrProcessingWorker.cs | Background worker |
| CREATE | src/Modules/Clinical/Clinical.Domain/Entities/DocumentChunk.cs | Chunk entity |
| CREATE | src/Modules/Clinical/Clinical.Infrastructure/Data/Configurations/DocumentChunkConfiguration.cs | EF configuration |
| MODIFY | src/Modules/Clinical/Clinical.Domain/Entities/ClinicalDocument.cs | Added Chunks navigation |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/Data/ClinicalDbContext.cs | Added DocumentChunks DbSet |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/ClinicalModuleExtensions.cs | Registered pipeline services |
| MODIFY | src/Modules/Clinical/Clinical.Infrastructure/Clinical.Infrastructure.csproj | Added Docnet.Core package |

## External References
- Tesseract .NET wrapper: https://github.com/charlesw/tesseract
- PII redaction patterns: HIPAA identifier types

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] Tesseract extracts text from scanned PDF pages
- [x] Processing completes within 5 minutes for 20-page document
- [x] PII (name, SSN, DOB, contact) redacted from text
- [x] Text chunked into 512-token segments with 51-token overlap
- [x] Failed extraction sets status "Failed" with error message

## Implementation Checklist
- [x] Create OcrProcessingWorker dequeuing from processing queue
- [x] Implement Tesseract OCR extraction for all PDF pages
- [x] Build PiiRedactor with regex patterns for HIPAA identifiers
- [x] Implement TextChunker with 512-token / 51-overlap segments
- [x] Update document status on completion or failure
- [x] Enforce 5-minute processing timeout
- [x] Handle blank pages and garbled output with quality flags
- [x] Store extracted chunks to DocumentChunk entity
