---
post_title: "TASK_001 - Clinical Document Upload UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-clinical-document-upload"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-006, US_031, frontend, React, upload, drag-drop, PDF"
ai_note: "Generated with AI assistance from user story US_031"
summary: "Implement drag-and-drop PDF upload with per-file progress, retry on failure, file type validation, and processing status display."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_CLINICAL_DOCUMENT_UPLOAD

## Requirement Reference
- User Story: us_031
- Story Location: .propel/context/tasks/EP-006/us_031/us_031.md
- Acceptance Criteria:
    - AC-1: Drag-and-drop with per-file progress indicators per UXR-106
    - AC-3: Per-file retry on failure, retaining successful uploads per UXR-604
    - AC-4: Non-PDF rejection with descriptive error
    - AC-5: Document saved with "Queued" status
- Edge Cases:
    - File exceeds max size → clear size limit error
    - Network loss during upload → discard partial; retry when reconnected

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-014-clinical-document-upload.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-014 |
| **UXR Requirements** | UXR-106, UXR-604 |
| **Design Tokens** | .propel/context/docs/designsystem.md#upload, #progress |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | React with TypeScript | 18.x |
| State Management | Redux Toolkit | 2.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the clinical document upload page with drag-and-drop zone, per-file progress bars, PDF-only validation, per-file retry on failure, document list with processing status, and file size limit enforcement.

## Dependent Tasks
- task_001_fe_react_scaffolding (US_001) — Requires React project

## Impacted Components
- NEW: DocumentUploadPage — Upload interface
- NEW: DropZone, FileProgressBar, DocumentStatusList
- NEW: documentApi — RTK Query upload endpoints

## Implementation Plan
1. Create DocumentUploadPage with DropZone component
2. Implement drag-and-drop with file type validation (PDF only)
3. Build FileProgressBar showing per-file upload progress
4. Implement per-file retry on failure, preserving completed uploads
5. Enforce max file size with clear error message
6. Display DocumentStatusList with processing status (Queued/Processing/Complete/Failed)
7. Implement batch upload via multipart form data
8. Add SignalR listener for processing status updates

## Current Project State
```
frontend/src/features/clinical/
├── api/
│   ├── documentApi.ts        ← Created
│   └── intakeApi.ts
├── components/
│   ├── DocumentStatusList.tsx ← Created
│   ├── DropZone.tsx          ← Created
│   ├── FileProgressBar.tsx   ← Created
│   └── ... (existing components)
├── pages/
│   ├── AIIntakePage.tsx
│   ├── DocumentUploadPage.tsx ← Created
│   ├── IntakeSummaryPage.tsx
│   └── ManualIntakePage.tsx
└── clinicalSlice.ts

frontend/src/App.tsx           ← Modified (added /documents route)
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/clinical/pages/DocumentUploadPage.tsx | Upload page |
| CREATE | frontend/src/features/clinical/components/DropZone.tsx | Drag-drop zone |
| CREATE | frontend/src/features/clinical/components/FileProgressBar.tsx | Progress bar |
| CREATE | frontend/src/features/clinical/components/DocumentStatusList.tsx | Status list |
| CREATE | frontend/src/features/clinical/api/documentApi.ts | Upload API |
| MODIFY | frontend/src/App.tsx | Add /documents/upload route |

## External References
- N/A

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Drag-and-drop accepts PDF files
- [x] Per-file progress indicators display correctly
- [x] Non-PDF files rejected with error message
- [x] Per-file retry works without re-uploading completed files
- [x] Processing status updates via SignalR
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-014

## Implementation Checklist
- [x] Create DocumentUploadPage with DropZone component
- [x] Implement file type validation (PDF only) with rejection error
- [x] Build FileProgressBar with per-file upload progress
- [x] Implement per-file retry on failure, retaining completed uploads
- [x] Enforce max file size with clear error message
- [x] Display DocumentStatusList with processing statuses
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-014 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
