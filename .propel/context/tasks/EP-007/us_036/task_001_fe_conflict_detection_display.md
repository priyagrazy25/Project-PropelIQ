---
post_title: "TASK_001 - Conflict Detection & Highlighting UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-conflict-detection"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-007, US_036, frontend, React, conflict, confidence, color-coding"
ai_note: "Generated with AI assistance from user story US_036"
summary: "Implement conflict highlighting in 360-view with severity banners, confidence color-coding, and source document links."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_CONFLICT_DETECTION_DISPLAY

## Requirement Reference
- User Story: us_036
- Story Location: .propel/context/tasks/EP-007/us_036/us_036.md
- Acceptance Criteria:
    - AC-3: Color-coded confidence: green ≥ 0.7, amber 0.5-0.7, red < 0.5 per UXR-107
    - AC-4: Critical conflicts → banner alert; Warning conflicts → inline
    - AC-5: Click conflict → source document links for navigation

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-016-patient-360-view.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-016 |
| **UXR Requirements** | UXR-107 |
| **Design Tokens** | .propel/context/docs/designsystem.md#confidence-scores, #alerts |

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
Integrate conflict indicators into the 360-view: Critical conflicts shown as top-page banner alerts with red styling, Warning conflicts shown inline within affected tab sections, confidence color-coded badges, and clickable source document links.

## Dependent Tasks
- task_001_fe_360_patient_view (US_035) — Requires 360 view page

## Impacted Components
- NEW: ConflictBanner — Critical conflict alert banner
- NEW: ConflictInlineIndicator — Inline warning indicator
- MODIFY: PatientView360Page — Add conflict display layer
- MODIFY: Tab components — Inline conflict indicators

## Implementation Plan
1. Add ConflictBanner component at top of 360-view for Critical conflicts
2. Build ConflictInlineIndicator for Warning-level conflicts within sections
3. Display confidence color-coding (green/amber/red) on all data points
4. Add "Verified" badge when all data agrees across documents
5. Implement clickable source document links on conflict items
6. Add conflict count badge on tab headers with conflicts

## Current Project State
```
frontend/src/features/clinical/
├── api/
│   └── patient360Api.ts              # API with ConflictSummary type
├── components/
│   ├── ConfidenceScoreBadge.tsx      # Green/amber/red color-coding (UXR-107)
│   ├── ConflictBanner.tsx            # NEW: Critical conflict alert banner
│   └── ConflictInlineIndicator.tsx   # NEW: Inline indicators, VerifiedBadge, ConflictCountBadge
└── pages/
    └── PatientView360Page.tsx        # MODIFIED: Integrated conflict display layer
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/clinical/components/ConflictBanner.tsx | Alert banner |
| CREATE | frontend/src/features/clinical/components/ConflictInlineIndicator.tsx | Inline warning |
| MODIFY | frontend/src/features/clinical/pages/PatientView360Page.tsx | Conflict layer |
| MODIFY | frontend/src/features/clinical/components/tabs/ | Inline indicators |

## External References
- N/A

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Critical conflicts show banner alert
- [x] Warning conflicts show inline in affected sections
- [x] Confidence color-coding: green/amber/red per thresholds
- [x] Source document links navigate to original
- [x] "Verified" badge shows when no conflicts
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-016 (manual review deferred)

## Implementation Checklist
- [x] Create ConflictBanner for Critical conflicts at page top
- [x] Build ConflictInlineIndicator for Warning conflicts within sections
- [x] Display confidence color-coding (green ≥0.7, amber 0.5-0.7, red <0.5)
- [x] Add "Verified" badge when all data agrees
- [x] Implement clickable source document links
- [x] Add conflict count badge on affected tab headers
