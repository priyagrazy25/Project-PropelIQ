---
post_title: "TASK_001 - Calendar Sync UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-calendar-sync"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-005, US_029, frontend, React, calendar, Google, Outlook, OAuth"
ai_note: "Generated with AI assistance from user story US_029"
summary: "Implement calendar sync UI with Google/Outlook chooser, OAuth flow, sync status indicators, and 'Sync pending' handling."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_CALENDAR_SYNC

## Requirement Reference

- User Story: us_029
- Story Location: .propel/context/tasks/EP-005/us_029/us_029.md
- Acceptance Criteria:
  - AC-1: Choose between Google Calendar and Outlook Calendar
  - AC-2: Google OAuth → event created with details
  - AC-3: Outlook OAuth → event created with details
  - AC-5: Circuit breaker open → "Sync pending" notification per NFR-014
- Edge Cases:
  - OAuth token expired → redirect to re-authorization
  - Calendar API rate limits → "Sync pending" notification

## Design References (Frontend Tasks Only)

| Reference Type         | Value                                                                 |
| ---------------------- | --------------------------------------------------------------------- |
| **UI Impact**          | Yes                                                                   |
| **Figma URL**          | N/A                                                                   |
| **Wireframe Status**   | AVAILABLE                                                             |
| **Wireframe Type**     | HTML                                                                  |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-009-calendar-sync.html |
| **Screen Spec**        | .propel/context/docs/figma_spec.md#SCR-009                            |
| **UXR Requirements**   | UXR-602                                                               |
| **Design Tokens**      | .propel/context/docs/designsystem.md#buttons, #icons                  |

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

Build the calendar sync chooser on the booking confirmation page with Google and Outlook options, OAuth popup flow, sync status indicator (synced/pending/failed), and re-authorization redirect on expired tokens.

## Dependent Tasks

- task_001_fe_appointment_booking (US_017) — Integrates into booking confirmation

## Impacted Components

- NEW: CalendarSyncChooser — Google/Outlook selection
- NEW: CalendarSyncStatus — Sync status indicator
- MODIFY: BookingConfirmationPage — Add calendar sync section

## Implementation Plan

1. Create CalendarSyncChooser with Google and Outlook buttons
2. Implement OAuth popup flow for each provider
3. Call sync API after OAuth callback with token
4. Create CalendarSyncStatus showing synced/pending/failed indicators
5. Handle re-authorization redirect on expired OAuth tokens
6. Show "Sync pending" notification when circuit breaker is open
7. Integrate into BookingConfirmationPage as optional step
8. Add synced calendar icon on patient dashboard appointment entries

## Current Project State

```
[PLACEHOLDER - Updated after US_017 tasks]
```

## Expected Changes

| Action | File Path                                                             | Description        |
| ------ | --------------------------------------------------------------------- | ------------------ |
| CREATE | frontend/src/features/notification/components/CalendarSyncChooser.tsx | Chooser            |
| CREATE | frontend/src/features/notification/components/CalendarSyncStatus.tsx  | Status indicator   |
| MODIFY | frontend/src/features/scheduling/pages/BookingConfirmationPage.tsx    | Add sync section   |
| MODIFY | frontend/src/features/scheduling/api/schedulingApi.ts                 | Add sync endpoints |

## External References

- Google OAuth: https://developers.google.com/identity/protocols/oauth2
- Microsoft Identity: https://learn.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow

## Build Commands

- `npm run dev` — Start dev server

## Implementation Validation Strategy

- [x] Google and Outlook options displayed on confirmation
- [x] OAuth popup completes authorization
- [x] Sync status shows synced/pending/failed
- [x] "Sync pending" shows on circuit breaker open
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-009

## Implementation Checklist

- [x] Create CalendarSyncChooser with Google/Outlook buttons
- [x] Implement OAuth popup flow for both providers
- [x] Create CalendarSyncStatus indicator (synced/pending/failed)
- [x] Handle expired OAuth token with re-authorization redirect
- [x] Show "Sync pending" on circuit breaker open
- [x] Integrate into BookingConfirmationPage
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-009 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
