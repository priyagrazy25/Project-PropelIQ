---
post_title: "TASK_001 - Login Interface UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-login-interface"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-001, US_013, frontend, React, login, JWT, authentication"
ai_note: "Generated with AI assistance from user story US_013"
summary: "Implement login form with email/password authentication, JWT token storage, role-based redirect, and error handling."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_LOGIN_INTERFACE

## Requirement Reference
- User Story: us_013
- Story Location: .propel/context/tasks/EP-001/us_013/us_013.md
- Acceptance Criteria:
    - AC-1: Login form with email and password fields per FR-002
    - AC-2: JWT access token stored securely; refresh token managed via httpOnly cookie
    - AC-3: Role-based redirect after login (Patient→dashboard, Staff→queue, Admin→management)
    - AC-4: Failed login displays inline error with remaining attempts info
    - AC-5: p95 API response under 2 seconds per NFR-001
- Edge Cases:
    - Account locked after max failed attempts → display lockout message with support contact
    - Expired token → silent refresh via refresh token; if refresh fails → redirect to login

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-002-login.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-002 |
| **UXR Requirements** | UXR-601, UXR-602 |
| **Design Tokens** | .propel/context/docs/designsystem.md#typography, #colors |

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
Build the login page with email/password form, JWT token management (access token in memory, refresh token via httpOnly cookie), role-based post-login redirect, inline validation errors, and automatic token refresh on expiry.

## Dependent Tasks
- task_001_fe_react_scaffolding (US_001) — Requires React project and Redux store
- task_001_fe_registration_form (US_012) — Reuses shared validation and error components

## Impacted Components
- NEW: LoginForm component
- NEW: Login page route
- NEW: Auth interceptor for JWT token management
- MODIFY: Redux identity slice for auth state

## Implementation Plan
1. Create LoginForm component with email/password controlled inputs
2. Implement login API call and store JWT access token in Redux state (memory-only)
3. Configure axios/fetch interceptor for automatic Authorization header injection
4. Implement refresh token rotation via httpOnly cookie flow
5. Create role-based redirect logic (decode JWT role claim → navigate to appropriate dashboard)
6. Implement protected route wrapper checking auth state
7. Display inline validation errors and account lockout messages
8. Add ARIA labels and keyboard submit support

## Current Project State
```
[PLACEHOLDER - Updated after US_001, US_012 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/identity/pages/LoginPage.tsx | Login page container |
| CREATE | frontend/src/features/identity/components/LoginForm.tsx | Login form with validation |
| CREATE | frontend/src/shared/api/authInterceptor.ts | JWT token injection interceptor |
| CREATE | frontend/src/shared/components/ProtectedRoute.tsx | Auth-gated route wrapper |
| MODIFY | frontend/src/features/identity/identitySlice.ts | Add auth state, login/logout actions |

## External References
- JWT in React: https://react.dev/learn/managing-state
- React Router Protected Routes: https://reactrouter.com/en/main/start/concepts

## Build Commands
- `npm run dev` — Start dev server
- `npm run build` — Build production

## Implementation Validation Strategy
- [x] Login with valid credentials sets auth state and redirects by role
- [x] Invalid credentials display inline error message
- [x] Token refresh works silently on access token expiry
- [x] Protected routes redirect unauthenticated users to login
- [ ] **[UI Tasks]** Visual comparison against wireframe SCR-002

## Implementation Checklist
- [x] Create LoginForm component with email/password and loading state
- [x] Implement JWT access token storage in Redux (memory-only, never localStorage)
- [x] Create axios interceptor for Authorization header and refresh token rotation
- [x] Implement role-based redirect (Patient→dashboard, Staff→queue, Admin→management)
- [x] Create ProtectedRoute wrapper checking auth state
- [x] Display inline validation errors and lockout messages
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-002 during implementation
- [ ] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
