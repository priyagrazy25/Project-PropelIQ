---
post_title: "TASK_002 - Risk Score Integration & Escalation API"
author1: "AI Senior Developer"
post_slug: "task-002-be-risk-score-integration"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-009, US_042, backend, ASP.NET Core, risk-score, escalation, reminders"
ai_note: "Generated with AI assistance from user story US_042"
summary: "Integrate no-show risk scoring into booking workflow, store NoShowRiskScore, expose dashboard API, and trigger escalated reminders."
post_date: "2026-04-16"
---

# Task - TASK_002_BE_RISK_SCORE_INTEGRATION_API

## Requirement Reference
- User Story: us_042
- Story Location: .propel/context/tasks/EP-009/us_042/us_042.md
- Acceptance Criteria:
    - AC-1: Risk score calculated on booking confirmation; stored per FR-029 / DR-013
    - AC-3: High-risk triggers escalated reminders at closer intervals
    - AC-4: Appointment records stored indefinitely for analytics per DR-013
    - AC-5: Sort by risk level and filter by date

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Backend | ASP.NET Core | 8.0 LTS |
| ORM | Entity Framework Core | 8.0 |
| Database | SQL Server Express | 2022 |
| AI-ML | ML.NET | 3.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | Yes (calls ML.NET prediction) |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Integrate risk scoring into booking: call ML.NET prediction on confirmation, store NoShowRiskScore entity, expose dashboard GET with sort/filter, and configure reminder escalation rules for high-risk appointments (score >70).

## Dependent Tasks
- task_001_ai_noshow_risk_model (US_041) — Trained model for predictions
- task_001_be_reminder_scheduler (US_028) — Reminder infrastructure for escalation
- task_002_be_appointment_booking_api (US_017) — Booking confirmation trigger

## Impacted Components
- NEW: IRiskScoreIntegrationService, RiskScoreIntegrationService
- MODIFY: BookingService — Trigger risk score on confirmation
- MODIFY: ReminderSchedulerWorker — Escalation rules for high-risk
- MODIFY: NoShowRiskController — Dashboard GET endpoint

## Implementation Plan
1. Call ML.NET prediction on booking confirmation
2. Store NoShowRiskScore entity with PatientID, AppointmentID, Score, ModelVersion
3. Create GET /api/clinical/risk/dashboard endpoint with sort and date filter
4. Classify risk levels: low (≤30), medium (31-70), high (>70)
5. Integrate with ReminderSchedulerWorker for escalated reminders
6. High-risk escalation: add reminders at 48h and 12h intervals
7. Handle model unavailability with default medium-risk fallback

## Current Project State
```
[PLACEHOLDER - Updated after US_041, US_028, US_017 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| MODIFY | src/Modules/Scheduling/Scheduling.Application/Abstractions/INoShowRiskService.cs | Added CalculateAndStoreRiskAsync method |
| MODIFY | src/Modules/Scheduling/Scheduling.Application/Abstractions/ISchedulingDbContext.cs | Added NoShowRiskScores DbSet |
| MODIFY | src/Modules/Scheduling/Scheduling.Infrastructure/Services/NoShowRiskService.cs | Implemented CalculateAndStoreRiskAsync with fallback |
| MODIFY | src/Modules/Scheduling/Scheduling.Application/Commands/BookAppointment/BookAppointmentCommandHandler.cs | Added risk score calculation on booking |
| MODIFY | src/Modules/Notification/Notification.Application/Services/ReminderService.cs | Added 48h/12h escalated windows for high-risk |
| MODIFY | src/Modules/Scheduling/Scheduling.API/Controllers/NoShowRiskController.cs | Added FrontDesk role authorization |
| CREATE | backend/scripts/seed-noshow-risk-data.sql | Sample data seed script |

## External References
- ML.NET prediction: https://learn.microsoft.com/dotnet/machine-learning/how-to-guides/serve-model-web-api-ml-net

## Build Commands
- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy
- [x] Risk score calculated on booking confirmation
- [x] NoShowRiskScore stored with appointment linkage
- [x] Dashboard endpoint returns sortable/filterable results
- [x] High-risk triggers additional reminders at 48h and 12h
- [x] Model unavailability falls back to medium-risk

## Implementation Checklist
- [x] Call ML.NET prediction on booking confirmation
- [x] Store NoShowRiskScore with PatientID, AppointmentID, Score
- [x] Create dashboard GET endpoint with sort by risk and date filter
- [x] Classify risk levels (low ≤30, medium 31-70, high >70)
- [x] Integrate escalated reminders for high-risk appointments
- [x] Handle model unavailability with medium-risk fallback
- [x] Retain appointment records indefinitely per DR-013
