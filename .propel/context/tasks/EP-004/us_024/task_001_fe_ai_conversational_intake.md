---
post_title: "TASK_001 - AI Conversational Intake UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-ai-conversational-intake"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-004, US_024, frontend, React, AI, chat, intake"
ai_note: "Generated with AI assistance from user story US_024"
summary: "Implement AI conversational intake chat UI with typing indicators, confidence display, field-level review, and manual fallback."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_AI_CONVERSATIONAL_INTAKE

## Requirement Reference

- User Story: us_024
- Story Location: .propel/context/tasks/EP-004/us_024/us_024.md
- Acceptance Criteria:
  - AC-1: AI guided flow asking contextual questions about medical history, symptoms, allergies, medications
  - AC-2: Structured data extracted with confidence scores displayed for review within 5s p95 per AIR-Q02
  - AC-3: 3 consecutive low-confidence exchanges → suggest manual form per AIR-008
  - AC-4: AI unavailable → notification banner and auto-switch to manual per UXR-605
  - AC-5: Final review screen with all extracted fields and field-level editing
- Edge Cases:
  - Non-English response → "I can best assist you in English"
  - Token budget exhaustion → graceful transition to summary review

## Design References (Frontend Tasks Only)

| Reference Type         | Value                                                             |
| ---------------------- | ----------------------------------------------------------------- |
| **UI Impact**          | Yes                                                               |
| **Figma URL**          | N/A                                                               |
| **Wireframe Status**   | AVAILABLE                                                         |
| **Wireframe Type**     | HTML                                                              |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-010-ai-intake.html |
| **Screen Spec**        | .propel/context/docs/figma_spec.md#SCR-010                        |
| **UXR Requirements**   | UXR-605                                                           |
| **Design Tokens**      | .propel/context/docs/designsystem.md#chat, #confidence-scores     |

## Applicable Technology Stack

| Layer            | Technology            | Version |
| ---------------- | --------------------- | ------- |
| Frontend         | React with TypeScript | 18.x    |
| State Management | Redux Toolkit         | 2.x     |

## AI References (AI Tasks Only)

| Reference Type | Value                                    |
| -------------- | ---------------------------------------- |
| **AI Impact**  | No (UI only — AI logic in separate task) |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build the AI conversational intake chat interface: message bubbles, typing indicator, confidence score badges (green ≥0.7, amber 0.5-0.7, red <0.5), 3-low-confidence fallback prompt, AI unavailability banner, and final review screen with field-level editing.

## Dependent Tasks

- task_001_fe_react_scaffolding (US_001) — Requires React project
- task_001_fe_login_interface (US_013) — Requires auth for patient access

## Impacted Components

- NEW: AIIntakePage — Chat interface container
- NEW: ChatBubble, TypingIndicator, ConfidenceScoreBadge
- NEW: IntakeReviewPanel — Parsed fields review with edit
- NEW: AIUnavailableBanner component

## Implementation Plan

1. Create AIIntakePage with scrollable chat message area
2. Build ChatBubble for user and AI messages with timestamps
3. Create TypingIndicator showing during AI processing
4. Implement ConfidenceScoreBadge with color coding per designsystem.md thresholds
5. Track consecutive low-confidence count; show fallback prompt at 3
6. Create AIUnavailableBanner with auto-redirect to manual form
7. Build IntakeReviewPanel showing extracted fields with inline editing
8. Handle token budget exhaustion with graceful summary transition

## Current Project State

```
[PLACEHOLDER - Updated after US_001, US_013 tasks]
```

## Expected Changes

| Action | File Path                                                          | Description          |
| ------ | ------------------------------------------------------------------ | -------------------- |
| CREATE | frontend/src/features/clinical/pages/AIIntakePage.tsx              | Chat intake page     |
| CREATE | frontend/src/features/clinical/components/ChatBubble.tsx           | Message bubble       |
| CREATE | frontend/src/features/clinical/components/TypingIndicator.tsx      | AI typing            |
| CREATE | frontend/src/features/clinical/components/ConfidenceScoreBadge.tsx | Score badge          |
| CREATE | frontend/src/features/clinical/components/IntakeReviewPanel.tsx    | Review/edit          |
| CREATE | frontend/src/features/clinical/components/AIUnavailableBanner.tsx  | Fallback banner      |
| CREATE | frontend/src/features/clinical/api/intakeApi.ts                    | RTK Query API        |
| MODIFY | frontend/src/App.tsx                                               | Add /intake/ai route |

## External References

- N/A

## Build Commands

- `npm run dev` — Start dev server

## Implementation Validation Strategy

- [x] Chat messages render correctly with user/AI bubbles
- [x] Confidence badges show correct colors per thresholds
- [x] 3 low-confidence exchanges trigger fallback prompt
- [x] AI unavailable banner shows and redirects to manual
- [x] Review panel displays fields with inline editing
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-010

## Implementation Checklist

- [x] Create AIIntakePage with scrollable chat and input area
- [x] Build ChatBubble and TypingIndicator components
- [x] Implement ConfidenceScoreBadge (green ≥0.7, amber 0.5-0.7, red <0.5)
- [x] Track low-confidence count and show manual fallback at 3
- [x] Create AIUnavailableBanner with auto-redirect to manual form
- [x] Build IntakeReviewPanel with field-level editing
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-010 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
