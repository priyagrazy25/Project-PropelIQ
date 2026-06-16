---
post_title: "TASK_001 - Manual Form Intake UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-manual-form-intake"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-004, US_025, frontend, React, intake, form, autosave"
ai_note: "Generated with AI assistance from user story US_025"
summary: "Implement structured manual intake form with autosave, inline validation, field-level editing, and AI data pre-fill."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_MANUAL_FORM_INTAKE

## Requirement Reference

- User Story: us_025
- Story Location: .propel/context/tasks/EP-004/us_025/us_025.md
- Acceptance Criteria:
  - AC-1: Structured fields for history, symptoms, allergies, medications
  - AC-2: Autosave every 30s with visible indicator per UXR-505
  - AC-3: Inline validation with descriptive error messages per UXR-601
  - AC-4: Direct field-level editing without staff assistance per FR-017
  - AC-5: Pre-fill from AI session data when switching modes
- Edge Cases:
  - Autosave network failure → localStorage backup; retry on reconnect
  - Browser back button → navigation guard warns unsaved changes

## Design References (Frontend Tasks Only)

| Reference Type         | Value                                                                 |
| ---------------------- | --------------------------------------------------------------------- |
| **UI Impact**          | Yes                                                                   |
| **Figma URL**          | N/A                                                                   |
| **Wireframe Status**   | AVAILABLE                                                             |
| **Wireframe Type**     | HTML                                                                  |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-011-manual-intake.html |
| **Screen Spec**        | .propel/context/docs/figma_spec.md#SCR-011                            |
| **UXR Requirements**   | UXR-505, UXR-601                                                      |
| **Design Tokens**      | .propel/context/docs/designsystem.md#forms, #inputs                   |

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

Build the manual intake form with structured sections (medical history, symptoms, allergies, medications), 30-second autosave with visual indicator, inline validation, localStorage backup, navigation guard, and AI data pre-fill support.

## Dependent Tasks

- task_001_fe_react_scaffolding (US_001) — Requires React project
- task_001_fe_ai_conversational_intake (US_024) — Shares intake data for pre-fill

## Impacted Components

- NEW: ManualIntakePage — Form container
- NEW: IntakeFormSection, AutosaveIndicator, NavigationGuard
- NEW: useAutosave hook for 30-second interval persistence

## Implementation Plan

1. Create ManualIntakePage with sectioned form (history, symptoms, allergies, medications)
2. Implement form state with React Hook Form or controlled inputs
3. Build useAutosave hook with 30-second interval and visual indicator
4. Add localStorage backup fallback when network autosave fails
5. Implement inline validation with descriptive error messages per UXR-601
6. Support AI data pre-fill from shared intake Redux state
7. Add navigation guard preventing accidental data loss on back/close
8. Implement submit with final validation and IntakeRecord creation

## Current Project State

```
[PLACEHOLDER - Updated after US_001, US_024 tasks]
```

## Expected Changes

| Action | File Path                                                       | Description              |
| ------ | --------------------------------------------------------------- | ------------------------ |
| CREATE | frontend/src/features/clinical/pages/ManualIntakePage.tsx       | Manual form page         |
| CREATE | frontend/src/features/clinical/components/IntakeFormSection.tsx | Form section             |
| CREATE | frontend/src/features/clinical/components/AutosaveIndicator.tsx | Save indicator           |
| CREATE | frontend/src/features/clinical/hooks/useAutosave.ts             | Autosave hook            |
| CREATE | frontend/src/features/clinical/components/NavigationGuard.tsx   | Nav guard                |
| MODIFY | frontend/src/App.tsx                                            | Add /intake/manual route |

## External References

- N/A

## Build Commands

- `npm run dev` — Start dev server

## Implementation Validation Strategy

- [x] Form displays structured sections with proper labels
- [x] Autosave fires every 30s with visible indicator
- [x] Network failure falls back to localStorage
- [x] Inline validation shows descriptive errors
- [x] AI data pre-fills when switching from AI mode
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-011

## Implementation Checklist

- [x] Create ManualIntakePage with history, symptoms, allergies, medications sections
- [x] Implement useAutosave hook with 30-second interval and indicator
- [x] Add localStorage fallback for network autosave failures
- [x] Implement inline validation with descriptive error messages
- [x] Support AI data pre-fill from shared Redux state
- [x] Add NavigationGuard for unsaved changes warning
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-011 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
