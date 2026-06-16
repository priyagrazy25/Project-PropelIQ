---
post_title: "TASK_001 - Conflict Resolution UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-conflict-resolution"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-007, US_037, frontend, React, conflict-resolution, side-by-side"
ai_note: "Generated with AI assistance from user story US_037"
summary: "Implement side-by-side conflict resolution interface with source document comparison and resolution actions."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_CONFLICT_RESOLUTION

## Requirement Reference
- User Story: us_037
- Story Location: .propel/context/tasks/EP-007/us_037/us_037.md
- Acceptance Criteria:
    - AC-1: Side-by-side comparison with source document links per UXR-108
    - AC-2: Resolution options: accept side A, accept side B, or manual override
    - AC-5: Each conflict resolved independently; 360-view updates incrementally

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-017-conflict-resolution.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-017 |
| **UXR Requirements** | UXR-108 |
| **Design Tokens** | .propel/context/docs/designsystem.md#comparison, #buttons |

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
Build the conflict resolution interface: side-by-side panel showing conflicting values with source document links, resolution action buttons (accept A/B/manual override), incremental 360-view update, and optimistic concurrency handling.

## Dependent Tasks
- task_001_fe_conflict_detection_display (US_036) — Requires conflict display
- task_001_fe_360_patient_view (US_035) — Requires 360 view for incremental update

## Impacted Components
- NEW: ConflictResolutionPanel — Side-by-side comparison
- NEW: ResolutionActions — Accept A/B/Manual buttons
- MODIFY: PatientView360Page — Open resolution panel from conflict

## Implementation Plan
1. Create ConflictResolutionPanel with side-by-side layout
2. Display conflicting values with source document links per side
3. Build ResolutionActions with accept side A, B, or manual override input
4. Implement resolution mutation via RTK Query
5. Handle optimistic concurrency (already-resolved notification)
6. Update 360-view incrementally after resolution

## Current Project State
```
✅ ConflictResolutionPanel.tsx - Side-by-side comparison with selectable panels, confidence display
✅ ResolutionActions.tsx - Radio group (A/B/manual), notes textarea, submit/cancel buttons
✅ ConflictResolutionPage.tsx - Full page with breadcrumb, API integration, concurrency handling
✅ patient360Api.ts - Added fetchConflictDetail, resolveConflict, ConflictDetail types
✅ App.tsx - Route /clinical/conflicts/:conflictId added
✅ No TypeScript errors in new files
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/clinical/components/ConflictResolutionPanel.tsx | Side-by-side comparison panels (UXR-108) |
| CREATE | frontend/src/features/clinical/components/ResolutionActions.tsx | Radio options + action buttons (AC-2) |
| CREATE | frontend/src/features/clinical/pages/ConflictResolutionPage.tsx | Full resolution page (SCR-017) |
| MODIFY | frontend/src/features/clinical/api/patient360Api.ts | fetchConflictDetail, resolveConflict APIs |
| MODIFY | frontend/src/App.tsx | Route /clinical/conflicts/:conflictId |

## External References
- N/A

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Side-by-side displays conflicting values with sources
- [x] Resolution options (A/B/manual) work correctly
- [x] 360-view updates after resolution
- [x] Concurrency conflict shows notification
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-017

## Implementation Checklist
- [x] Create ConflictResolutionPanel with side-by-side layout
- [x] Display conflicting values with source document links
- [x] Build ResolutionActions (accept A, accept B, manual override)
- [x] Implement resolution mutation with optimistic concurrency
- [x] Update 360-view incrementally after resolution
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-017 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
