---
post_title: "TASK_001 - Intake Mode Switching & Summary UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-intake-mode-switch"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-004, US_026, frontend, React, intake, mode-switch, summary"
ai_note: "Generated with AI assistance from user story US_026"
summary: "Implement seamless AI/manual intake toggle with data preservation, summary review screen, and graceful degradation on AI failure."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_INTAKE_MODE_SWITCH

## Requirement Reference

- User Story: us_026
- Story Location: .propel/context/tasks/EP-004/us_026/us_026.md
- Acceptance Criteria:
  - AC-1: AI→manual switch preserves parsed data and pre-fills form per UXR-103
  - AC-2: Manual→AI switch passes entered data to AI context
  - AC-3: Summary screen with categorized sections and click-to-edit per UXR-104
  - AC-4: AI unavailable → auto-switch to manual with notification per NFR-013
  - AC-5: 3 low-confidence → fallback to manual with data preserved per AIR-008
- Edge Cases:
  - Mode switch during AI response → abandon response, preserve last confirmed state
  - Summary edits override AI suggestions → remove confidence score for edited fields

## Design References (Frontend Tasks Only)

| Reference Type         | Value                                                                  |
| ---------------------- | ---------------------------------------------------------------------- |
| **UI Impact**          | Yes                                                                    |
| **Figma URL**          | N/A                                                                    |
| **Wireframe Status**   | AVAILABLE                                                              |
| **Wireframe Type**     | HTML                                                                   |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-012-intake-summary.html |
| **Screen Spec**        | .propel/context/docs/figma_spec.md#SCR-012                             |
| **UXR Requirements**   | UXR-103, UXR-104                                                       |
| **Design Tokens**      | .propel/context/docs/designsystem.md#toggles, #cards, #buttons         |

## Applicable Technology Stack

| Layer            | Technology            | Version |
| ---------------- | --------------------- | ------- |
| Frontend         | React with TypeScript | 18.x    |
| State Management | Redux Toolkit         | 2.x     |

## AI References (AI Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **AI Impact**  | No    |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build the mode toggle component enabling seamless AI↔manual switching, shared Redux state for cross-mode data, intake summary page with categorized review and click-to-edit, and automatic mode switch on AI unavailability or low confidence.

## Dependent Tasks

- task_001_fe_ai_conversational_intake (US_024) — AI intake to switch from
- task_001_fe_manual_form_intake (US_025) — Manual intake to switch to

## Impacted Components

- NEW: IntakeModeToggle — Persistent mode switch component
- NEW: IntakeSummaryPage — Categorized review with edit
- MODIFY: AIIntakePage — Add mode toggle, auto-switch trigger
- MODIFY: ManualIntakePage — Add mode toggle, pre-fill from AI data
- MODIFY: Redux clinical slice — Shared intake data state

## Implementation Plan

1. Create IntakeModeToggle component visible on both AI and manual pages
2. Implement shared Redux state for intake data persisting across mode switches
3. On AI→manual: snapshot AI parsed data into shared state, pre-fill form
4. On manual→AI: pass entered data as AI context via session update
5. Create IntakeSummaryPage with categorized sections and click-to-edit
6. Remove confidence scores for manually edited fields in summary
7. Auto-trigger mode switch on AI unavailability (circuit breaker) with notification
8. Auto-trigger mode switch after 3 low-confidence exchanges

## Current Project State

```
[PLACEHOLDER - Updated after US_024, US_025 tasks]
```

## Expected Changes

| Action | File Path                                                      | Description               |
| ------ | -------------------------------------------------------------- | ------------------------- |
| CREATE | frontend/src/features/clinical/components/IntakeModeToggle.tsx | Mode switch               |
| CREATE | frontend/src/features/clinical/pages/IntakeSummaryPage.tsx     | Summary review            |
| MODIFY | frontend/src/features/clinical/pages/AIIntakePage.tsx          | Add toggle + auto-switch  |
| MODIFY | frontend/src/features/clinical/pages/ManualIntakePage.tsx      | Add toggle + pre-fill     |
| MODIFY | frontend/src/features/clinical/slices/intakeSlice.ts           | Shared state              |
| MODIFY | frontend/src/App.tsx                                           | Add /intake/summary route |

## External References

- N/A

## Build Commands

- `npm run dev` — Start dev server

## Implementation Validation Strategy

- [x] AI→manual preserves parsed data in form fields
- [x] Manual→AI passes data to AI engine context
- [x] Summary shows categorized fields with click-to-edit
- [x] AI unavailability triggers auto-switch with banner
- [x] 3 low-confidence exchanges trigger fallback
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-012

## Implementation Checklist

- [x] Create IntakeModeToggle component on AI and manual pages
- [x] Implement shared Redux state for cross-mode data persistence
- [x] Handle AI→manual pre-fill and manual→AI context passing
- [x] Create IntakeSummaryPage with categorized sections and click-to-edit
- [x] Auto-switch to manual on AI unavailability with notification
- [x] Auto-switch after 3 low-confidence exchanges per AIR-008
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-012 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
