---
post_title: "TASK_002 - Clinical Document Upload API"
author1: "AI Senior Developer"
post_slug: "task-002-be-document-upload-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-006, US_031, backend, ASP.NET Core, upload, storage, encryption"
ai_note: "Generated with AI assistance from user story US_031"
summary: "Implement document upload API with encrypted storage, ClinicalDocument entity creation, and extraction job queuing."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_DOCUMENT_UPLOAD_API

## Requirement Reference
- User Story: us_031
- Story Location: .propel/context/tasks/EP-006/us_031/us_031.md
- Acceptance Criteria:
    - AC-2: Upload completes → document queued for OCR/NLP with status "Queued"
    - AC-4: Non-PDF rejected server-side
    - AC-5: File saved to encrypted storage per DR-004 with unique DocumentID + PatientID link

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
Build the document upload API: accept multipart PDF uploads, validate file type and size, store to encrypted path, create ClinicalDocument entity with status "Queued", and enqueue job for OCR/NLP extraction pipeline.

## Dependent Tasks
- task_001_be_modular_monolith_setup (US_002) — Requires Clinical module
- task_001_db_clinical_ai_entities (US_009) — Requires ClinicalDocument entity

## Impacted Components
- NEW: DocumentUploadController — Upload endpoint
- NEW: IDocumentStorageService, DocumentStorageService
- NEW: Document upload validation filter (type, size)

## Implementation Plan
1. Create POST /api/clinical/documents/upload multipart endpoint
2. Validate file type (application/pdf) and size (configurable max)
3. Store file to encrypted disk path with unique filename
4. Create ClinicalDocument entity with DocumentID, PatientID, status "Queued"
5. Enqueue extraction job to processing queue
6. Return DocumentID and processing status to client
7. Add antivirus/content validation for uploaded files

## Current Project State
```
backend/src/Modules/Clinical/
├── Clinical.API/
│   └── Controllers/
│       ├── ClinicalController.cs
│       ├── DocumentUploadController.cs  ← Created
│       ├── InsuranceController.cs
│       └── IntakeController.cs
├── Clinical.Application/
│   ├── Abstractions/
│   │   ├── IDocumentStorageService.cs   ← Created
│   │   ├── IDocumentUploadService.cs    ← Created
│   │   └── ... (existing)
│   └── DTOs/
│       ├── DocumentUploadResponse.cs    ← Created
│       └── ... (existing)
├── Clinical.Infrastructure/
│   ├── Services/
│   │   ├── DocumentStorageService.cs    ← Created
│   │   ├── DocumentUploadService.cs     ← Created
│   │   └── ... (existing)
│   └── ClinicalModuleExtensions.cs      ← Modified
└── Clinical.Domain/
    └── Entities/
        └── ClinicalDocument.cs          (existing)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | src/Modules/Clinical/Controllers/DocumentUploadController.cs | Upload endpoint |
| CREATE | src/Modules/Clinical/Services/IDocumentStorageService.cs | Storage interface |
| CREATE | src/Modules/Clinical/Services/DocumentStorageService.cs | Encrypted storage |
| CREATE | src/Modules/Clinical/Filters/DocumentUploadValidationFilter.cs | Type/size filter |
| MODIFY | src/Modules/Clinical/DependencyInjection.cs | Register services |

## External References
- File uploads in ASP.NET Core: https://learn.microsoft.com/aspnet/core/mvc/models/file-uploads

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] POST /upload accepts PDF files via multipart
- [x] Non-PDF files rejected with 400
- [x] File stored to encrypted path
- [x] ClinicalDocument created with "Queued" status
- [x] Extraction job enqueued (background workers poll for pending documents)

## Implementation Checklist
- [x] Create POST upload endpoint accepting multipart PDF
- [x] Validate file type (PDF only) and configurable max size
- [x] Store file to encrypted disk path with unique filename
- [x] Create ClinicalDocument entity with DocumentID and PatientID
- [x] Enqueue extraction job to processing queue (ExtractionJobWorker/OcrProcessingWorker/NerProcessingWorker)
- [x] Add content validation for uploaded files (PDF magic bytes)
- [x] Return DocumentID and status in response
