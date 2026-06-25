---
post_title: "TASK_001 - Patient Registration Form UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-registration-form"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-001, US_012, frontend, React, registration, form-validation"
ai_note: "Generated with AI assistance from user story US_012"
summary: "Implement patient registration form with email validation, demographic fields, inline validation, and error handling."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_REGISTRATION_FORM

## Requirement Reference
- User Story: us_012
- Story Location: .propel/context/tasks/EP-001/us_012/us_012.md
- Acceptance Criteria:
    - AC-1: Registration form with name, DOB, email, phone, address fields per FR-001
    - AC-2: Inline field validation with descriptive error messages per UXR-601
    - AC-3: Email format validation before submission
    - AC-4: Global error banner with retry action for API failures per UXR-602
    - AC-5: Success state navigates to login page
- Edge Cases:
    - Duplicate email → inline error "This email is already registered"
    - Network failure during submission → global error banner with retry button

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-001-patient-registration.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-001 |
| **UXR Requirements** | UXR-601, UXR-602 |
| **Design Tokens** | .propel/context/docs/designsystem.md#typography, #colors, #spacing |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | React with TypeScript | 18.x |
| State Management | Redux Toolkit | 2.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |
| **AIR Requirements** | N/A |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | N/A |
| **Model Provider** | N/A |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |
| **Platform Target** | N/A |
| **Min OS Version** | N/A |
| **Mobile Framework** | N/A |

## Task Overview
Build the patient self-registration form component with email validation, demographic field capture (name, DOB, phone, address), inline field-level validation with descriptive error messages, and global error banner with retry action for API failures. The form follows the design system tokens and matches the Hi-Fi wireframe for SCR-001.

## Dependent Tasks
- task_001_fe_react_scaffolding (US_001) — Requires React project structure

## Impacted Components
- NEW: RegistrationForm component
- NEW: Registration page route
- NEW: Form validation utilities
- NEW: API service for registration endpoint

## Implementation Plan
1. Create RegistrationForm component with controlled inputs for all demographic fields
2. Implement client-side validation rules (email format, required fields, DOB date picker, phone format)
3. Create inline validation error display (appears on blur with descriptive messages per UXR-601)
4. Implement form submission with loading state and API call
5. Create global error banner component for API failure with retry action per UXR-602
6. Implement success state → navigate to login page
7. Handle duplicate email API response with inline error message
8. Add ARIA labels and keyboard navigation per WCAG requirements

## Current Project State
```
[PLACEHOLDER - Updated after US_001 frontend scaffolding]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/identity/pages/RegistrationPage.tsx | Registration page container |
| CREATE | frontend/src/features/identity/components/RegistrationForm.tsx | Form with validation |
| CREATE | frontend/src/shared/components/ErrorBanner.tsx | Global error banner with retry |
| CREATE | frontend/src/features/identity/api/authApi.ts | Registration API service |
| CREATE | frontend/src/shared/utils/validation.ts | Shared validation utilities |

## External References
- React Hook Form: https://react-hook-form.com/get-started
- React 18 Forms: https://react.dev/reference/react-dom/components/input

## Build Commands
- `npm run dev` — Start dev server
- `npm run build` — Build production

## Implementation Validation Strategy
- [x] All form fields render and accept input
- [x] Inline validation fires on blur with descriptive messages
- [x] API error displays global error banner with retry action
- [x] Successful registration navigates to login page
- [x] **[UI Tasks]** Visual comparison against wireframe completed at 375px, 768px, 1440px
- [x] **[UI Tasks]** Run `/analyze-ux` to validate wireframe alignment

## Implementation Checklist
- [x] Create RegistrationForm component with controlled inputs for all demographic fields
- [x] Implement inline validation (email format, required fields, DOB, phone) per UXR-601
- [x] Create global ErrorBanner component with retry action per UXR-602
- [x] Implement form submission with loading state spinner
- [x] Handle success → navigate to login; handle 409 duplicate email inline
- [x] Add ARIA labels, keyboard navigation, and focus management
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-001 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
