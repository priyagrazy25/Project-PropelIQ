---
post_title: "TASK_001 - Insurance Pre-Check UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-insurance-precheck"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-004, US_027, frontend, React, insurance, validation, inline"
ai_note: "Generated with AI assistance from user story US_027"
summary: "Implement inline insurance pre-check form with green/amber/red validation status indicators."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_INSURANCE_PRECHECK

## Requirement Reference

- User Story: us_027
- Story Location: .propel/context/tasks/EP-004/us_027/us_027.md
- Acceptance Criteria:
  - AC-1: Enter insurance name and member ID; validate inline without page reload per UXR-105
  - AC-2: Full match → green "Verified" indicator
  - AC-3: Partial match → amber "Unverified - Member ID mismatch"
  - AC-4: No match → red "Unverified - Insurance not recognized" + staff follow-up note
  - AC-5: Staff can view insurance verification status on dashboard
- Edge Cases:
  - Empty insurance records → "Verification unavailable"; patient proceeds
  - Special characters → input sanitized; case-insensitive matching

## Design References (Frontend Tasks Only)

| Reference Type         | Value                                                                      |
| ---------------------- | -------------------------------------------------------------------------- |
| **UI Impact**          | Yes                                                                        |
| **Figma URL**          | N/A                                                                        |
| **Wireframe Status**   | AVAILABLE                                                                  |
| **Wireframe Type**     | HTML                                                                       |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-013-insurance-precheck.html |
| **Screen Spec**        | .propel/context/docs/figma_spec.md#SCR-013                                 |
| **UXR Requirements**   | UXR-105                                                                    |
| **Design Tokens**      | .propel/context/docs/designsystem.md#status-indicators, #forms             |

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

Build the insurance pre-check form with insurance name and member ID inputs, inline validation via API call, and color-coded status indicators (green = Verified, amber = Partial, red = Unrecognized). Allow patient to proceed regardless of status.

## Dependent Tasks

- task_001_fe_react_scaffolding (US_001) — Requires React project

## Impacted Components

- NEW: InsurancePreCheckForm — Validation form
- NEW: InsuranceStatusBadge — Color-coded result indicator
- MODIFY: Intake flow — Insurance validation step

## Implementation Plan

1. Create InsurancePreCheckForm with insurance name and member ID inputs
2. Implement inline validation mutation via RTK Query
3. Create InsuranceStatusBadge with green/amber/red per validation result
4. Handle "Verification unavailable" when records are empty
5. Sanitize input and display case-insensitive match results
6. Allow patient to proceed with flagged status on no/partial match
7. Integrate into intake flow as a step
8. Add loading spinner during validation request

## Current Project State

```
[PLACEHOLDER - Updated after US_001 tasks]
```

## Expected Changes

| Action | File Path                                                           | Description                 |
| ------ | ------------------------------------------------------------------- | --------------------------- |
| CREATE | frontend/src/features/clinical/components/InsurancePreCheckForm.tsx | Pre-check form              |
| CREATE | frontend/src/features/clinical/components/InsuranceStatusBadge.tsx  | Status indicator            |
| MODIFY | frontend/src/features/clinical/api/intakeApi.ts                     | Add insurance endpoint      |
| MODIFY | frontend/src/App.tsx                                                | Add /intake/insurance route |

## External References

- N/A

## Build Commands

- `npm run dev` — Start dev server

## Implementation Validation Strategy

- [x] Form accepts insurance name and member ID
- [x] Full match shows green "Verified"
- [x] Partial match shows amber warning
- [x] No match shows red warning with staff follow-up note
- [x] Empty records shows "Verification unavailable"
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-013

## Implementation Checklist

- [x] Create InsurancePreCheckForm with name and member ID inputs
- [x] Implement inline validation via RTK Query mutation
- [x] Create InsuranceStatusBadge (green/amber/red) per result
- [x] Handle "Verification unavailable" for empty records
- [x] Sanitize input for special characters
- [x] Allow proceed with flagged status on partial/no match
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-013 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
