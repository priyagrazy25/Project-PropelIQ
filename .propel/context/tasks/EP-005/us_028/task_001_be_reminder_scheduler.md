---
post_title: "TASK_001 - Reminder Scheduler Service"
author1: "AI Senior Developer"
post_slug: "task-001-be-reminder-scheduler"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-005, US_028, backend, ASP.NET Core, reminders, scheduler, circuit-breaker"
ai_note: "Generated with AI assistance from user story US_028"
summary: "Implement background reminder scheduler evaluating 72h/24h/2h windows with SMS/email dispatch, circuit breaker, and retry."
post_date: "2026-04-16"
---

# Task - TASK_001_BE_REMINDER_SCHEDULER

## Requirement Reference

- User Story: us_028
- Story Location: .propel/context/tasks/EP-005/us_028/us_028.md
- Acceptance Criteria:
  - AC-1: Reminders at 72h, 24h, 2h before appointment
  - AC-2: SMS retry with exponential backoff; fallback to email per NFR-020
  - AC-3: Circuit breaker opens on 3 failures in 60s; recovery at 120s per NFR-014
  - AC-4: Delivery status logged per channel
  - AC-5: SendGrid 100/day limit → prioritize by proximity and no-show risk
- Edge Cases:
  - Both circuits open → queue and alert admin
  - Cancelled appointments → skip

## Design References (Frontend Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **UI Impact**  | No    |

## Applicable Technology Stack

| Layer      | Technology         | Version    |
| ---------- | ------------------ | ---------- |
| Backend    | ASP.NET Core       | 8.0 LTS    |
| SMS        | Twilio             | Free trial |
| Email      | SendGrid           | Free tier  |
| Resilience | Polly              | 8.x        |
| Database   | SQL Server Express | 2022       |

## AI References (AI Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **AI Impact**  | No    |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build a background hosted service that evaluates upcoming appointments at configurable intervals (72h, 24h, 2h) and dispatches SMS (Twilio) and email (SendGrid) reminders. Implement Polly circuit breaker for both channels, exponential backoff retry, prioritization logic for SendGrid daily limits, and delivery audit logging.

## Dependent Tasks

- task_001_be_modular_monolith_setup (US_002) — Requires Notification module
- task_001_db_identity_scheduling_entities (US_008) — Requires Appointment entity
- task_001_be_logging_health_monitoring (US_007) — Requires audit logging

## Impacted Components

- NEW: ReminderSchedulerWorker — IHostedService background job
- NEW: IReminderService, ReminderService
- NEW: ISmsChannel, TwilioSmsChannel
- NEW: IEmailChannel, SendGridEmailChannel
- NEW: ReminderDeliveryLog entity

## Implementation Plan

1. Create ReminderSchedulerWorker as IHostedService with configurable evaluation interval
2. Query confirmed future appointments with 72h/24h/2h windows
3. Implement IReminderService dispatching to both SMS and email channels
4. Build TwilioSmsChannel with Polly retry (exponential backoff, max 3) and circuit breaker
5. Build SendGridEmailChannel with Polly retry and circuit breaker
6. Implement prioritization by proximity and no-show risk when near daily limit
7. Log delivery status per channel to ReminderDeliveryLog
8. Skip cancelled/rescheduled appointments during evaluation

## Current Project State

```
[PLACEHOLDER - Updated after US_002, US_007, US_008 tasks]
```

## Expected Changes

| Action | File Path                                                   | Description             |
| ------ | ----------------------------------------------------------- | ----------------------- |
| CREATE | src/Modules/Notification/Workers/ReminderSchedulerWorker.cs | Background scheduler    |
| CREATE | src/Modules/Notification/Services/IReminderService.cs       | Service interface       |
| CREATE | src/Modules/Notification/Services/ReminderService.cs        | Reminder logic          |
| CREATE | src/Modules/Notification/Channels/ISmsChannel.cs            | SMS interface           |
| CREATE | src/Modules/Notification/Channels/TwilioSmsChannel.cs       | Twilio implementation   |
| CREATE | src/Modules/Notification/Channels/IEmailChannel.cs          | Email interface         |
| CREATE | src/Modules/Notification/Channels/SendGridEmailChannel.cs   | SendGrid implementation |

## External References

- Twilio .NET: https://www.twilio.com/docs/sms/quickstart/csharp-dotnet-core
- SendGrid .NET: https://docs.sendgrid.com/for-developers/sending-email/quickstart-csharp
- Polly circuit breaker: https://github.com/App-vNext/Polly

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] Scheduler evaluates appointments at 72h, 24h, 2h windows
- [x] SMS and email sent successfully
- [x] Circuit breaker opens on 3 failures in 60s
- [x] Retry with exponential backoff works
- [x] Cancelled appointments skipped

## Implementation Checklist

- [x] Create ReminderSchedulerWorker as IHostedService
- [x] Query confirmed appointments with configurable reminder windows
- [x] Build TwilioSmsChannel with Polly retry and circuit breaker
- [x] Build SendGridEmailChannel with Polly retry and circuit breaker
- [x] Implement prioritization by proximity and no-show risk
- [x] Log delivery status per channel to ReminderDeliveryLog
- [x] Skip cancelled/rescheduled appointments
- [x] Alert admin when both circuits are open simultaneously
