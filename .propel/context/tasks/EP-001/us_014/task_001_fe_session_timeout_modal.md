---
post_title: "TASK_001 - Session Timeout Modal UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-session-timeout-modal"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-001, US_014, frontend, session-timeout, modal, re-authentication"
ai_note: "Generated with AI assistance from user story US_014"
summary: "Implement 15-minute inactivity detector with pre-expiry timeout modal showing 60-second countdown and re-authentication option."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_SESSION_TIMEOUT_MODAL

## Requirement Reference
- User Story: us_014
- Story Location: .propel/context/tasks/EP-001/us_014/us_014.md
- Acceptance Criteria:
    - AC-1: Inactivity detector triggers after 15 minutes of no user interaction per FR-003
    - AC-2: Pre-expiry modal with 60-second countdown and re-authenticate option per UXR-603
    - AC-3: No action within countdown → automatic logout and redirect to login
    - AC-4: User activity (click, keypress, scroll) resets the inactivity timer
    - AC-5: Modal accessible via keyboard (trap focus within modal)
- Edge Cases:
    - Multiple browser tabs → timer synced across tabs via BroadcastChannel API
    - Session expires during active form entry → unsaved data warning shown before logout

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-003-session-timeout.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-003 |
| **UXR Requirements** | UXR-603 |
| **Design Tokens** | .propel/context/docs/designsystem.md#modals |

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
Implement the 15-minute inactivity detection system with a pre-expiry timeout modal. The modal shows a 60-second countdown giving users the option to re-authenticate and extend their session. On countdown expiry, the system automatically logs out and redirects to login.

## Dependent Tasks
- task_001_fe_login_interface (US_013) — Requires auth state and logout action

## Impacted Components
- NEW: SessionTimeoutModal component with countdown
- NEW: useInactivityTimer custom hook
- NEW: BroadcastChannel cross-tab sync utility
- MODIFY: App.tsx to wrap with inactivity detection

## Implementation Plan
1. Create useInactivityTimer hook tracking mouse, keyboard, scroll events with 15-min threshold
2. Create SessionTimeoutModal component with 60-second countdown display
3. Implement "Stay Logged In" action that refreshes the JWT and resets the timer
4. Implement auto-logout on countdown expiry → clear auth state → redirect to login
5. Add BroadcastChannel API for cross-tab timer synchronization
6. Implement focus trap within the modal for keyboard accessibility
7. Handle unsaved form data warning before forced logout
8. Add ARIA live region for countdown announcements to screen readers

## Current Project State
```
[PLACEHOLDER - Updated after US_013 login tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/shared/hooks/useInactivityTimer.ts | Inactivity detection hook |
| CREATE | frontend/src/shared/components/SessionTimeoutModal.tsx | Timeout modal with countdown |
| CREATE | frontend/src/shared/utils/broadcastChannel.ts | Cross-tab timer sync |
| MODIFY | frontend/src/App.tsx | Wrap with inactivity detection provider |

## External References
- React Modal Accessibility: https://react.dev/reference/react-dom/createPortal
- BroadcastChannel API: https://developer.mozilla.org/en-US/docs/Web/API/BroadcastChannel

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Modal appears after 15 minutes of inactivity
- [x] Countdown timer accurate to seconds
- [x] "Stay Logged In" refreshes session and closes modal
- [x] Auto-logout on countdown expiry redirects to login
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-003

## Implementation Checklist
- [x] Create useInactivityTimer hook with 15-minute threshold and activity event listeners
- [x] Create SessionTimeoutModal with 60-second countdown and "Stay Logged In" button
- [x] Implement auto-logout and redirect on countdown expiry
- [x] Add BroadcastChannel cross-tab timer synchronization
- [x] Implement focus trap and ARIA live region for accessibility
- [x] Handle unsaved form data warning before forced logout
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-003 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
