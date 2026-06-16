---
post_title: "TASK_001 - Medical Coding UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-medical-coding"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-008, US_038, US_039, frontend, React, ICD-10, CPT, coding"
ai_note: "Generated with AI assistance from user stories US_038 and US_039"
summary: "Implement unified medical coding UI displaying ICD-10 and CPT candidate codes with confidence rankings and staff verification actions."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_MEDICAL_CODING

## Requirement Reference
- User Story: us_038, us_039
- Story Location: .propel/context/tasks/EP-008/us_038/us_038.md, .propel/context/tasks/EP-008/us_039/us_039.md
- Acceptance Criteria:
    - US_038 AC-2: Top-3 ICD-10 codes ranked by confidence with color-coded indicators
    - US_039 AC-2: Top-3 CPT codes ranked by confidence with color-coded indicators
    - US_039 AC-5: Unified view for both ICD-10 and CPT on SCR-018
- Edge Cases:
    - No diagnoses → "No diagnoses available for coding"
    - No procedures → "No procedures available for coding"
    - All candidates below 0.5 → red indicators + manual entry prompt

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-018-medical-coding.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-018 |
| **UXR Requirements** | N/A |
| **Design Tokens** | .propel/context/docs/designsystem.md#confidence-scores, #tables |

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
Build the unified medical coding page displaying ICD-10 diagnosis codes and CPT procedure codes side by side. Each code shows top-3 candidates ranked by confidence with color-coded badges. Include "No data" empty states.

## Dependent Tasks
- task_001_fe_360_patient_view (US_035) — Navigates from 360-view diagnoses/procedures

## Impacted Components
- NEW: MedicalCodingPage — Unified coding view
- NEW: CodeCandidateCard — Top-3 with confidence badges
- NEW: codingApi — RTK Query endpoints

## Implementation Plan
1. Create MedicalCodingPage with ICD-10 and CPT sections
2. Build CodeCandidateCard showing top-3 candidates with confidence badges
3. Implement confidence color-coding (green/amber/red)
4. Handle empty states for no diagnoses/procedures
5. Show manual entry prompt when all candidates below 0.5
6. Implement RTK Query endpoints for coding data

## Current Project State
```
[PLACEHOLDER - Updated after US_035 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/clinical/pages/MedicalCodingPage.tsx | Coding page |
| CREATE | frontend/src/features/clinical/components/CodeCandidateCard.tsx | Code card |
| CREATE | frontend/src/features/clinical/api/codingApi.ts | RTK Query API |
| MODIFY | frontend/src/App.tsx | Add /patients/{id}/coding route |

## External References
- N/A

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] ICD-10 and CPT codes displayed in unified view
- [x] Top-3 candidates ranked by confidence per code type
- [x] Color-coded confidence badges render correctly
- [x] Empty states for no diagnoses/procedures
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-018

## Implementation Checklist
- [x] Create MedicalCodingPage with ICD-10 and CPT sections
- [x] Build CodeCandidateCard with top-3 confidence-ranked candidates
- [x] Implement confidence color-coding (green ≥0.7, amber 0.5-0.7, red <0.5)
- [x] Handle empty states for no diagnoses and no procedures
- [x] Show manual entry prompt when all candidates below 0.5
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-018 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
