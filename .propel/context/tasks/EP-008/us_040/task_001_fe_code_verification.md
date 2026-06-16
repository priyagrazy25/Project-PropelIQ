---
post_title: "TASK_001 - Code Verification UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-code-verification"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-008, US_040, frontend, React, verification, accept-reject, coding"
ai_note: "Generated with AI assistance from user story US_040"
summary: "Implement staff verification workflow UI for AI-suggested ICD-10/CPT codes with accept/reject and agreement rate display."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_CODE_VERIFICATION

## Requirement Reference
- User Story: us_040
- Story Location: .propel/context/tasks/EP-008/us_040/us_040.md
- Acceptance Criteria:
    - AC-1: Accept, reject (with reason), or manual override per AIR-S04
    - AC-4: Status updates inline without page reload

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
| **Design Tokens** | .propel/context/docs/designsystem.md#buttons, #status-indicators |

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
Add accept/reject/override actions to the medical coding page. Accept verifies the code; reject requires a mandatory reason; override allows manual code entry. Status updates inline. Display rolling agreement rate metric.

## Dependent Tasks
- task_001_fe_medical_coding (US_038) — Requires coding page with candidate display

## Impacted Components
- NEW: VerificationActions — Accept/reject/override buttons
- NEW: RejectionReasonDialog — Mandatory reason on reject
- NEW: ManualCodeOverrideInput — Manual code entry
- MODIFY: MedicalCodingPage — Add verification actions

## Implementation Plan
1. Add VerificationActions to each CodeCandidateCard
2. Implement accept mutation updating status inline to "Verified"
3. Create RejectionReasonDialog with mandatory reason field
4. Implement reject mutation with reason, updating status to "Rejected"
5. Create ManualCodeOverrideInput for custom code entry
6. Display rolling agreement rate metric on page

## Current Project State
```
MedicalCodingPage - Existing with accept/reject handlers
CodeCandidateCard - Existing with inline action buttons
codingApi.ts - Existing with verify endpoint
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/clinical/components/VerificationActions.tsx | Reusable action buttons component |
| CREATE | frontend/src/features/clinical/components/ManualCodeOverrideInput.tsx | Manual code entry dialog |
| MODIFY | frontend/src/features/clinical/components/CodeCandidateCard.tsx | Add Override button |
| MODIFY | frontend/src/features/clinical/pages/MedicalCodingPage.tsx | Add override + agreement rate |
| MODIFY | frontend/src/features/clinical/api/codingApi.ts | Add override + agreement rate APIs |

## External References
- N/A

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Accept updates status to "Verified" inline
- [x] Reject requires reason and updates status to "Rejected"
- [x] Manual override stores custom code
- [x] Agreement rate metric displays
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-018

## Implementation Checklist
- [x] Add VerificationActions to each code candidate card
- [x] Implement accept mutation with inline status update
- [x] Create RejectionReasonDialog with mandatory reason
- [x] Build ManualCodeOverrideInput for custom code entry
- [x] Display rolling 30-day agreement rate metric
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-018 during implementation
