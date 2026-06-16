---
post_title: "TASK_002 - Calendar Sync API"
author1: "AI Senior Developer"
post_slug: "task-002-be-calendar-sync-api"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-005, US_029, backend, ASP.NET Core, calendar, Google, Outlook, OAuth"
ai_note: "Generated with AI assistance from user story US_029"
summary: "Implement calendar sync API integrating Google Calendar API v3 and Microsoft Graph v1.0 with OAuth, event CRUD, and circuit breaker."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_CALENDAR_SYNC_API

## Requirement Reference

- User Story: us_029
- Story Location: .propel/context/tasks/EP-005/us_029/us_029.md
- Acceptance Criteria:
  - AC-2: Google Calendar event creation via API v3
  - AC-3: Outlook Calendar event creation via Microsoft Graph v1.0
  - AC-4: Auto-update/remove on reschedule/cancel
  - AC-5: Circuit breaker open → queue for retry per NFR-014

## Design References (Frontend Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **UI Impact**  | No    |

## Applicable Technology Stack

| Layer      | Technology          | Version |
| ---------- | ------------------- | ------- |
| Backend    | ASP.NET Core        | 8.0 LTS |
| Calendar   | Google Calendar API | v3      |
| Calendar   | Microsoft Graph API | v1.0    |
| Resilience | Polly               | 8.x     |
| Database   | SQL Server Express  | 2022    |

## AI References (AI Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **AI Impact**  | No    |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build the calendar sync API with strategy pattern for Google and Outlook providers. Handle OAuth token exchange and storage, event creation with appointment details, auto-update on reschedule, auto-delete on cancel, circuit breaker for API failures, and async retry queue.

## Dependent Tasks

- task_001_be_modular_monolith_setup (US_002) — Requires Notification module
- task_002_be_cancel_reschedule_api (US_020) — Triggers calendar updates

## Impacted Components

- NEW: CalendarSyncController — OAuth callback + sync endpoints
- NEW: ICalendarProvider, GoogleCalendarProvider, OutlookCalendarProvider
- NEW: ICalendarSyncService, CalendarSyncService
- NEW: CalendarSyncRecord entity — Tracks external event IDs

## Implementation Plan

1. Create POST /api/notification/calendar/sync endpoint
2. Implement OAuth token exchange and encrypted storage per provider
3. Build ICalendarProvider strategy pattern with Google and Outlook implementations
4. Create calendar events with provider, date, time, location, notes
5. Implement auto-update on appointment reschedule via event handler
6. Implement auto-delete on appointment cancellation via event handler
7. Add Polly circuit breaker with async retry queue for failures
8. Store CalendarSyncRecord linking appointment to external event ID

## Current Project State

```
Calendar sync API fully implemented. NoOpCalendarSyncService replaced with real CalendarSyncService
in Host.Services bridging Scheduling and Notification modules. Strategy pattern with Google/Outlook
providers. OAuth token exchange via CalendarSyncController. Polly circuit breaker on provider calls.
CalendarSyncRecord entity tracks external event IDs. Build passes, 300 unit tests green.
```

## Expected Changes

| Action | File Path                                                      | Description        |
| ------ | -------------------------------------------------------------- | ------------------ |
| CREATE | src/Modules/Notification/Controllers/CalendarSyncController.cs | Sync endpoints     |
| CREATE | src/Modules/Notification/Services/ICalendarSyncService.cs      | Service interface  |
| CREATE | src/Modules/Notification/Services/CalendarSyncService.cs       | Sync logic         |
| CREATE | src/Modules/Notification/Providers/ICalendarProvider.cs        | Strategy interface |
| CREATE | src/Modules/Notification/Providers/GoogleCalendarProvider.cs   | Google impl        |
| CREATE | src/Modules/Notification/Providers/OutlookCalendarProvider.cs  | Outlook impl       |

## External References

- Google Calendar API v3: https://developers.google.com/calendar/api/v3/reference
- Microsoft Graph Calendar: https://learn.microsoft.com/graph/api/resources/calendar

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] Google Calendar event created with appointment details
- [x] Outlook Calendar event created with appointment details
- [x] Reschedule updates calendar event
- [x] Cancel deletes calendar event
- [x] Circuit breaker queues failed requests for retry

## Implementation Checklist

- [x] Create calendar sync endpoint with OAuth token exchange
- [x] Implement ICalendarProvider strategy (Google + Outlook)
- [x] Create calendar events with appointment details
- [x] Auto-update events on appointment reschedule
- [x] Auto-delete events on appointment cancellation
- [x] Add Polly circuit breaker with async retry queue
- [x] Store CalendarSyncRecord linking to external event IDs
- [x] Encrypt OAuth tokens at rest
