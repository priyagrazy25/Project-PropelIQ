---
post_title: "TASK_001 - 360-Degree Patient View UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-360-patient-view"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-007, US_035, frontend, React, 360-view, tabs, caching"
ai_note: "Generated with AI assistance from user story US_035"
summary: "Implement tabbed 360-Degree Patient View with Vitals, History, Medications, Allergies, Labs, Diagnoses sections and 3s render."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_360_PATIENT_VIEW

## Requirement Reference
- User Story: us_035
- Story Location: .propel/context/tasks/EP-007/us_035/us_035.md
- Acceptance Criteria:
    - AC-5: Renders within 3 seconds per NFR-004
    - AC-6: Tabbed sections: Vitals, Medical History, Medications, Allergies, Lab Results, Diagnoses per DR-006
- Edge Cases:
    - Single document → partial sections
    - Redis cache unavailable → degraded performance

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-016-patient-360-view.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-016 |
| **UXR Requirements** | N/A |
| **Design Tokens** | .propel/context/docs/designsystem.md#tabs, #cards, #confidence-scores |

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
Build the 360-Degree Patient View page with tabbed navigation (Vitals, Medical History, Medications, Allergies, Lab Results, Diagnoses). Display aggregated data with confidence score badges. Support partial-section rendering for patients with limited data.

## Dependent Tasks
- task_001_fe_react_scaffolding (US_001) — Requires React project

## Impacted Components
- NEW: PatientView360Page — Tabbed patient view
- NEW: VitalsTab, HistoryTab, MedicationsTab, AllergiesTab, LabsTab, DiagnosesTab
- NEW: patient360Api — RTK Query endpoints

## Implementation Plan
1. Create PatientView360Page with tab navigation
2. Build each tab component rendering categorized data with confidence badges
3. Implement RTK Query endpoint for GET patient 360 view
4. Add skeleton loading for each tab during data fetch
5. Handle partial-section rendering for limited data
6. Display data source references per data point
7. Add print-friendly layout for clinical staff
8. Ensure 3-second render with optimistic caching

## Current Project State
```
[PLACEHOLDER - Updated after US_001 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/clinical/pages/PatientView360Page.tsx | 360 view page ✅ |
| CREATE | frontend/src/features/clinical/api/patient360Api.ts | API + types ✅ |
| MODIFY | frontend/src/App.tsx | Add /health-profile route ✅ |

## External References
- N/A

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Tabbed layout renders 6 categories
- [x] Confidence badges show correct colors
- [x] Renders within 3 seconds
- [x] Partial sections handled gracefully
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-016

## Implementation Checklist
- [x] Create PatientView360Page with tab navigation
- [x] Build tab components for each clinical category
- [x] Implement RTK Query endpoint for 360 view data
- [x] Display confidence score badges per data point
- [x] Handle partial-section rendering for limited data
- [x] Add skeleton loading states per tab
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-016 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
