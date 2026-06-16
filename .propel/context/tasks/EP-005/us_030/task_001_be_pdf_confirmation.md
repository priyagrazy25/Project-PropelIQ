---
post_title: "TASK_001 - PDF Confirmation Generator"
author1: "AI Senior Developer"
post_slug: "task-001-be-pdf-confirmation"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-005, US_030, backend, ASP.NET Core, PDF, QuestPDF, email"
ai_note: "Generated with AI assistance from user story US_030"
summary: "Implement QuestPDF appointment confirmation generator with branded layout, email delivery, dashboard preview, and text-only fallback."
post_date: "2026-04-16"
---

# Task - TASK_001_BE_PDF_CONFIRMATION

## Requirement Reference

- User Story: us_030
- Story Location: .propel/context/tasks/EP-005/us_030/us_030.md
- Acceptance Criteria:
  - AC-1: QuestPDF generates PDF with provider, date, time, location, prep notes per TR-014
  - AC-2: PDF attached to email via SendGrid
  - AC-3: Platform branding, clear layout, prep notes
  - AC-4: Dashboard "View PDF" opens in drawer overlay per OVL-004
  - AC-5: Generation failure → text-only email fallback
- Edge Cases:
  - Email service down → store PDF; retry with backoff; dashboard download
  - Rescheduled appointment → new PDF generated and sent

## Design References (Frontend Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **UI Impact**  | No    |

## Applicable Technology Stack

| Layer    | Technology         | Version   |
| -------- | ------------------ | --------- |
| Backend  | ASP.NET Core       | 8.0 LTS   |
| PDF      | QuestPDF           | 2024.x    |
| Email    | SendGrid           | Free tier |
| Database | SQL Server Express | 2022      |

## AI References (AI Tasks Only)

| Reference Type | Value |
| -------------- | ----- |
| **AI Impact**  | No    |

## Mobile References (Mobile Tasks Only)

| Reference Type    | Value |
| ----------------- | ----- |
| **Mobile Impact** | No    |

## Task Overview

Build the PDF confirmation generator using QuestPDF with branded layout (platform logo, colors per designsystem.md). On booking confirmation, generate PDF, attach to SendGrid email, store for dashboard download. Handle generation failures with text-only email fallback. Regenerate on reschedule.

## Dependent Tasks

- task_001_be_reminder_scheduler (US_028) — Reuses SendGrid email channel
- task_002_be_appointment_booking_api (US_017) — Triggered on booking confirmation

## Impacted Components

- NEW: IPdfConfirmationService, PdfConfirmationService
- NEW: AppointmentPdfDocument — QuestPDF document template
- NEW: PdfConfirmationController — Dashboard download endpoint
- MODIFY: BookingService — Trigger PDF generation on confirmation

## Implementation Plan

1. Create AppointmentPdfDocument with QuestPDF layout (branding, details, prep notes)
2. Implement PdfConfirmationService generating PDF from appointment data
3. Attach PDF to SendGrid confirmation email on booking success
4. Store PDF bytes in database or blob for dashboard download
5. Create GET /api/notification/pdf/{appointmentId} for dashboard preview
6. Implement text-only email fallback on PDF generation failure
7. Regenerate PDF on appointment reschedule
8. Log generation and delivery status to audit store

## Current Project State

```
[PLACEHOLDER - Updated after US_017, US_028 tasks]
```

## Expected Changes

| Action | File Path                                                         | Description            |
| ------ | ----------------------------------------------------------------- | ---------------------- |
| CREATE | src/Modules/Notification/Services/IPdfConfirmationService.cs      | Service interface      |
| CREATE | src/Modules/Notification/Services/PdfConfirmationService.cs       | PDF generation         |
| CREATE | src/Modules/Notification/Documents/AppointmentPdfDocument.cs      | QuestPDF template      |
| CREATE | src/Modules/Notification/Controllers/PdfConfirmationController.cs | Download endpoint      |
| MODIFY | src/Modules/Scheduling/Services/BookingService.cs                 | Trigger PDF on confirm |

## External References

- QuestPDF: https://www.questpdf.com/documentation/getting-started.html

## Build Commands

- `dotnet build` — Build solution
- `dotnet test` — Run tests

## Implementation Validation Strategy

- [x] QuestPDF generates branded PDF with appointment details
- [x] PDF attached to confirmation email via SendGrid
- [x] GET /pdf/{appointmentId} returns PDF for dashboard download
- [x] Generation failure sends text-only email
- [x] Reschedule triggers new PDF generation

## Implementation Checklist

- [x] Create AppointmentPdfDocument with QuestPDF branded layout
- [x] Implement PdfConfirmationService generating PDF from appointment data
- [x] Attach PDF to SendGrid confirmation email
- [x] Store PDF for dashboard download
- [x] Create GET endpoint for PDF preview/download
- [x] Implement text-only email fallback on generation failure
- [x] Regenerate PDF on rescheduled appointments
- [x] Log generation and delivery status
