---
post_title: "Unified Patient Access & Clinical Intelligence Platform - Figma Design Specification"
author1: "AI Product Designer"
post_slug: "unified-patient-access-figma-spec"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Design, UX"
tags: "figma, UX, screens, flows, components, design-system, WCAG, healthcare-UI"
ai_note: "Generated with AI assistance from spec.md and design.md source documents"
summary: "Complete Figma design specification with UX requirements, screen inventory, prototype flows, component mapping, and state definitions for the Unified Patient Access & Clinical Intelligence Platform."
post_date: "2026-04-15"
---

# Figma Design Specification - Unified Patient Access

## 1. Figma Specification

**Platform**: Web (Responsive - Mobile 390px / Tablet 768px / Desktop 1440px)

---

## 2. Source References

### Primary Source

| Document     | Path                           | Purpose                                          |
| ------------ | ------------------------------ | ------------------------------------------------ |
| Requirements | `.propel/context/docs/spec.md` | Personas, use cases, FR-XXX with UI impact flags |

### Optional Sources

| Document     | Path                             | Purpose                            |
| ------------ | -------------------------------- | ---------------------------------- |
| Architecture | `.propel/context/docs/design.md` | NFR, DR, AIR for technical context |

### Related Documents

| Document      | Path                                   | Purpose                                    |
| ------------- | -------------------------------------- | ------------------------------------------ |
| Design System | `.propel/context/docs/designsystem.md` | Tokens, branding, component specifications |

---

## 3. UX Requirements

### UXR Enumeration Summary

| UXR-ID  | Category       | Summary                                                       | Rationale                                            |
| ------- | -------------- | ------------------------------------------------------------- | ---------------------------------------------------- |
| UXR-001 | Project-wide   | Max 3 clicks to any primary feature                           | Discoverability for all personas                     |
| UXR-002 | Project-wide   | Consistent navigation layout across all authenticated screens | Spatial consistency, reduced cognitive load          |
| UXR-101 | Usability      | Provider search results within 2 seconds                      | NFR-001; FR-005 real-time slot display               |
| UXR-102 | Usability      | Real-time slot availability update without page reload        | NFR-002; FR-006 WebSocket updates                    |
| UXR-103 | Usability      | Seamless AI/manual intake mode switch with data preservation  | FR-016 data continuity                               |
| UXR-104 | Usability      | Direct field-level editing in intake summary                  | FR-017 patient autonomy                              |
| UXR-105 | Usability      | Inline insurance validation feedback                          | FR-018 immediate status                              |
| UXR-106 | Usability      | Drag-and-drop upload with progress indicator                  | FR-023 document upload UX                            |
| UXR-107 | Usability      | Color-coded confidence scores in 360-degree view              | FR-025 trust-first display                           |
| UXR-108 | Usability      | Side-by-side conflict comparison with source references       | FR-026 conflict resolution clarity                   |
| UXR-201 | Accessibility  | WCAG 2.2 AA color contrast compliance                         | Legal baseline >=4.5:1 text, >=3:1 UI                |
| UXR-202 | Accessibility  | Keyboard navigation with visible focus states                 | Assistive technology support                         |
| UXR-203 | Accessibility  | Screen reader ARIA labels on all form controls                | Assistive technology support                         |
| UXR-204 | Accessibility  | Touch targets >=44x44px on mobile                             | Mobile usability                                     |
| UXR-205 | Accessibility  | Error states use icon+text, not color alone                   | Color-blind accessibility                            |
| UXR-301 | Responsiveness | Mobile (390px), Tablet (768px), Desktop (1440px) breakpoints  | Platform coverage per tech stack                     |
| UXR-302 | Responsiveness | Adaptive navigation per breakpoint                            | Desktop sidebar, tablet collapsed, mobile bottom nav |
| UXR-303 | Responsiveness | Tables degrade to card layout on mobile                       | Data readability on small screens                    |
| UXR-401 | Visual Design  | Healthcare-appropriate color palette                          | Trust and calming aesthetic                          |
| UXR-402 | Visual Design  | 8px base grid spacing system                                  | Consistent alignment                                 |
| UXR-403 | Visual Design  | Clear typography hierarchy                                    | Content scanability                                  |
| UXR-501 | Interaction    | Skeleton loading states for all data-fetching screens         | NFR-004 perceived performance                        |
| UXR-502 | Interaction    | Optimistic UI for slot booking with rollback                  | NFR-002 responsiveness                               |
| UXR-503 | Interaction    | Real-time queue updates via WebSocket                         | NFR-002; FR-012 queue display                        |
| UXR-504 | Interaction    | Toast notifications for async operations                      | Swap, reminder, and upload feedback                  |
| UXR-505 | Interaction    | Form autosave during intake                                   | FR-016 data loss prevention                          |
| UXR-601 | Error Handling | Inline field validation with descriptive messages             | UC-003 ext 5a; UC-001 ext 6a                         |
| UXR-602 | Error Handling | Global error banner with retry action                         | All API failure scenarios                            |
| UXR-603 | Error Handling | Session timeout modal with re-auth option                     | FR-003 session timeout UX                            |
| UXR-604 | Error Handling | Per-file retry on document upload failure                     | UC-008 ext 5a                                        |
| UXR-605 | Error Handling | AI unavailability fallback notification                       | NFR-013 graceful degradation                         |

### UXR Requirements Table

| UXR-ID  | Category       | Requirement                                                                                                                  | Acceptance Criteria                                                       | Screens Affected                            |
| ------- | -------------- | ---------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- | ------------------------------------------- |
| UXR-001 | Project-wide   | System MUST provide navigation to any primary feature in max 3 clicks from the dashboard                                     | Click-count audit passes for all primary tasks                            | All authenticated screens                   |
| UXR-002 | Project-wide   | System MUST display a consistent header and sidebar navigation layout across all authenticated screens                       | Visual audit confirms identical nav structure                             | All authenticated screens                   |
| UXR-101 | Usability      | System MUST display provider search results within 2 seconds of query submission                                             | Performance test p95 < 2s                                                 | SCR-004                                     |
| UXR-102 | Usability      | System MUST update slot availability in real time via WebSocket without requiring page reload                                | Slot booked by another user disappears within 500ms                       | SCR-004, SCR-005                            |
| UXR-103 | Usability      | System MUST allow patients to switch between AI conversational and manual form intake at any point without data loss         | Switch preserves all previously entered fields                            | SCR-010, SCR-011                            |
| UXR-104 | Usability      | System MUST provide direct field-level editing in the intake summary without restarting the intake flow                      | Click-to-edit on any field in summary view                                | SCR-012                                     |
| UXR-105 | Usability      | System MUST display inline insurance validation result (pass/fail with reason) immediately after form submission             | Validation result appears without page reload                             | SCR-013                                     |
| UXR-106 | Usability      | System MUST support drag-and-drop PDF upload with per-file progress indicators                                               | File drop triggers upload; progress bar visible per file                  | SCR-014                                     |
| UXR-107 | Usability      | System MUST display confidence scores with color-coded visual indicators (green >=0.7, amber 0.5-0.7, red <0.5)              | Visual audit confirms color-coding accuracy                               | SCR-016                                     |
| UXR-108 | Usability      | System MUST display conflicting data points side-by-side with clickable source document references                           | Source document link opens relevant page                                  | SCR-017                                     |
| UXR-201 | Accessibility  | System MUST comply with WCAG 2.2 AA standards for all screens                                                                | WAVE/axe audit passes with zero critical violations                       | All screens                                 |
| UXR-202 | Accessibility  | System MUST support full keyboard navigation with visible focus indicators on all interactive elements                       | Tab-through audit passes; focus ring visible (>=3:1 contrast, 2px offset) | All screens                                 |
| UXR-203 | Accessibility  | System MUST provide ARIA labels for all form controls, buttons, and dynamic content regions                                  | Screen reader audit passes                                                | All screens with forms                      |
| UXR-204 | Accessibility  | System MUST maintain minimum 44x44px touch targets for all interactive elements on mobile                                    | Tap target audit passes on 390px viewport                                 | All mobile screens                          |
| UXR-205 | Accessibility  | System MUST communicate error states using icon and text in addition to color                                                | Visual audit confirms non-color-only error indicators                     | All screens with error states               |
| UXR-301 | Responsiveness | System MUST adapt layout at three breakpoints: Mobile (<=767px), Tablet (768-1023px), Desktop (>=1024px)                     | Responsive audit passes at 390px, 768px, 1440px                           | All screens                                 |
| UXR-302 | Responsiveness | System MUST adapt navigation: Desktop uses fixed sidebar, Tablet uses collapsible sidebar, Mobile uses bottom navigation bar | Navigation type changes correctly at breakpoints                          | All authenticated screens                   |
| UXR-303 | Responsiveness | System MUST render data tables as stacked card layouts on mobile viewports                                                   | Table-to-card transformation at <=767px                                   | SCR-006, SCR-018, SCR-020, SCR-023, SCR-025 |
| UXR-401 | Visual Design  | System MUST use a healthcare-appropriate color palette with calming primary blue and semantic alert colors                   | Design review confirms palette adherence                                  | All screens                                 |
| UXR-402 | Visual Design  | System MUST use an 8px base grid spacing system for all layout spacings and paddings                                         | Spacing audit confirms 8px-multiple values                                | All screens                                 |
| UXR-403 | Visual Design  | System MUST implement a clear typography hierarchy from H1 through Caption                                                   | Heading levels verified via DOM audit                                     | All screens                                 |
| UXR-501 | Interaction    | System MUST display skeleton loading screens (preserving layout) during all data-fetch operations                            | Skeleton appears within 100ms of fetch start                              | All data-dependent screens                  |
| UXR-502 | Interaction    | System MUST apply optimistic UI for appointment booking with automatic rollback on failure                                   | Slot appears booked immediately; error reverts if API fails               | SCR-005                                     |
| UXR-503 | Interaction    | System MUST push queue status updates to the UI via WebSocket without manual refresh                                         | Queue order changes reflect within 500ms                                  | SCR-020                                     |
| UXR-504 | Interaction    | System MUST display toast notifications for async operations (swap execution, upload completion, reminder status)            | Toast appears and auto-dismisses after 5 seconds                          | SCR-006, SCR-014, SCR-015                   |
| UXR-505 | Interaction    | System MUST autosave intake form data at 30-second intervals to prevent data loss                                            | Autosave indicator visible; data recoverable on return                    | SCR-010, SCR-011                            |
| UXR-601 | Error Handling | System MUST display inline validation errors below the offending field with descriptive messages                             | Error message appears on blur/submit; red border on field                 | All screens with forms                      |
| UXR-602 | Error Handling | System MUST display a global error banner at the top of the screen for API failures with a retry action                      | Banner visible with retry button; retry re-attempts the failed call       | All screens                                 |
| UXR-603 | Error Handling | System MUST display a session timeout modal 60 seconds before expiry allowing re-authentication without losing page context  | Modal countdown timer; re-auth preserves URL and form state               | All authenticated screens                   |
| UXR-604 | Error Handling | System MUST allow per-file retry when a document upload fails, without re-uploading successful files                         | Retry button per failed file; successful uploads retained                 | SCR-014                                     |
| UXR-605 | Error Handling | System MUST display a notification when AI services are unavailable and automatically switch intake to manual form mode      | Banner or toast appears; AI toggle disabled; manual form loads            | SCR-010                                     |

---

## 4. Personas Summary

| Persona | Role                           | Primary Goals                                                                                           | Key Screens                   |
| ------- | ------------------------------ | ------------------------------------------------------------------------------------------------------- | ----------------------------- |
| Patient | Registered healthcare consumer | Book appointments, complete intake, upload clinical docs, view 360-degree health profile, sync calendar | SCR-001, SCR-002, SCR-004-016 |
| Staff   | Front Desk / Call Center       | Book walk-ins, manage same-day queue, mark arrivals, resolve data conflicts, verify medical codes       | SCR-002, SCR-017-022          |
| Admin   | System Administrator           | Manage user accounts, assign roles, view audit logs                                                     | SCR-002, SCR-023-025          |

---

## 5. Information Architecture

### Site Map

```text
Unified Patient Access Platform
+-- Public
|   +-- SCR-001: Registration
|   +-- SCR-002: Login
|
+-- Patient Portal
|   +-- SCR-006: Patient Dashboard / My Appointments
|   +-- SCR-004: Provider Search & Results
|   +-- SCR-005: Appointment Booking & Confirmation
|   +-- SCR-008: Reschedule / Cancel Appointment
|   +-- SCR-007: Waitlist Status
|   +-- SCR-009: Calendar Sync
|   +-- SCR-010: AI Conversational Intake
|   +-- SCR-011: Manual Form Intake
|   +-- SCR-012: Intake Summary / Review
|   +-- SCR-013: Insurance Pre-Check
|   +-- SCR-014: Clinical Document Upload
|   +-- SCR-015: Document Processing Status
|   +-- SCR-016: 360-Degree Patient View
|
+-- Staff Portal
|   +-- SCR-021: Staff Dashboard
|   +-- SCR-019: Walk-In Booking
|   +-- SCR-020: Same-Day Queue Management
|   +-- SCR-022: No-Show Risk Dashboard
|   +-- SCR-017: Data Conflict Resolution
|   +-- SCR-018: Medical Code Verification
|
+-- Admin Portal
    +-- SCR-024: Admin Dashboard
    +-- SCR-023: User Management
    +-- SCR-025: Audit Log Viewer
```

### Navigation Patterns

| Pattern       | Type                                    | Platform Behavior                                                                                                                    |
| ------------- | --------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| Primary Nav   | Sidebar (Desktop) / Bottom Nav (Mobile) | Desktop: Fixed left sidebar with icon+label; Tablet: Collapsible icon-only sidebar; Mobile: Bottom navigation bar with 4-5 key items |
| Secondary Nav | Tabs                                    | Used within Patient View (Vitals/History/Meds/Allergies/Labs/Diagnoses), Staff Queue views                                           |
| Utility Nav   | User menu (top-right)                   | Avatar dropdown with Profile, Settings, Logout; Session timer indicator                                                              |
| Breadcrumb    | Contextual trail                        | Shown on detail pages (Appointment Detail, Document Detail, Conflict Detail)                                                         |

---

## 6. Screen Inventory

### Screen List

| Screen ID | Screen Name                         | Derived From           | UXR-XXX Mapped                                       | Personas Covered | Priority | States Required                            |
| --------- | ----------------------------------- | ---------------------- | ---------------------------------------------------- | ---------------- | -------- | ------------------------------------------ |
| SCR-001   | Registration                        | UC-001 pre, FR-001     | UXR-201, UXR-301, UXR-601                            | Patient          | P0       | Default, Loading, Error, Validation        |
| SCR-002   | Login                               | FR-002                 | UXR-201, UXR-301, UXR-601, UXR-603                   | All              | P0       | Default, Loading, Error, Validation        |
| SCR-003   | Session Timeout Modal               | FR-003                 | UXR-603                                              | All              | P0       | Default (countdown)                        |
| SCR-004   | Provider Search & Results           | UC-001, FR-005, FR-006 | UXR-001, UXR-101, UXR-102, UXR-301, UXR-303, UXR-501 | Patient          | P0       | Default, Loading, Empty, Error             |
| SCR-005   | Appointment Booking & Confirmation  | UC-001, FR-007, FR-008 | UXR-001, UXR-102, UXR-502, UXR-601                   | Patient          | P0       | Default, Loading, Error, Validation        |
| SCR-006   | Patient Dashboard / My Appointments | UC-001 post, FR-010    | UXR-001, UXR-002, UXR-301, UXR-303, UXR-501, UXR-504 | Patient          | P0       | Default, Loading, Empty, Error             |
| SCR-007   | Waitlist Status                     | UC-001 ext 4b, FR-009  | UXR-001, UXR-501                                     | Patient          | P1       | Default, Loading, Empty, Error             |
| SCR-008   | Appointment Reschedule / Cancel     | FR-010                 | UXR-001, UXR-601                                     | Patient, Staff   | P0       | Default, Loading, Error, Validation        |
| SCR-009   | Calendar Sync                       | UC-014, FR-020, FR-021 | UXR-001, UXR-602                                     | Patient          | P1       | Default, Loading, Error                    |
| SCR-010   | AI Conversational Intake            | UC-002, FR-014         | UXR-103, UXR-505, UXR-605                            | Patient          | P0       | Default, Loading, Error                    |
| SCR-011   | Manual Form Intake                  | UC-003, FR-015         | UXR-103, UXR-505, UXR-601                            | Patient          | P0       | Default, Loading, Error, Validation        |
| SCR-012   | Intake Summary / Review             | UC-002, UC-003         | UXR-104, UXR-601                                     | Patient          | P0       | Default, Loading, Error, Validation        |
| SCR-013   | Insurance Pre-Check                 | UC-013, FR-018         | UXR-105, UXR-601                                     | Patient          | P1       | Default, Loading, Error, Validation        |
| SCR-014   | Clinical Document Upload            | UC-008, FR-023         | UXR-106, UXR-604                                     | Patient          | P0       | Default, Loading, Empty, Error             |
| SCR-015   | Document Processing Status          | UC-008                 | UXR-501, UXR-504                                     | Patient          | P1       | Default, Loading, Empty, Error             |
| SCR-016   | 360-Degree Patient View             | UC-009, FR-025         | UXR-107, UXR-108, UXR-501                            | Patient, Staff   | P0       | Default, Loading, Empty, Error             |
| SCR-017   | Data Conflict Resolution            | UC-011, FR-026         | UXR-108, UXR-601                                     | Staff            | P0       | Default, Loading, Empty, Error, Validation |
| SCR-018   | Medical Code Verification           | UC-010, FR-027, FR-028 | UXR-107, UXR-303, UXR-601                            | Staff            | P0       | Default, Loading, Empty, Error, Validation |
| SCR-019   | Walk-In Booking                     | UC-005, FR-011         | UXR-001, UXR-601                                     | Staff            | P0       | Default, Loading, Error, Validation        |
| SCR-020   | Same-Day Queue Management           | UC-006, FR-012, FR-013 | UXR-503, UXR-303                                     | Staff            | P0       | Default, Loading, Empty, Error             |
| SCR-021   | Staff Dashboard                     | UC-005, UC-006         | UXR-001, UXR-002, UXR-501                            | Staff            | P0       | Default, Loading, Empty, Error             |
| SCR-022   | No-Show Risk Dashboard              | FR-029                 | UXR-107, UXR-303, UXR-501                            | Staff            | P1       | Default, Loading, Empty, Error             |
| SCR-023   | Admin User Management               | UC-012, FR-004         | UXR-001, UXR-303, UXR-601                            | Admin            | P0       | Default, Loading, Empty, Error, Validation |
| SCR-024   | Admin Dashboard                     | UC-012                 | UXR-001, UXR-002, UXR-501                            | Admin            | P0       | Default, Loading, Empty, Error             |
| SCR-025   | Audit Log Viewer                    | FR-031                 | UXR-303, UXR-501                                     | Admin            | P1       | Default, Loading, Empty, Error             |

### Screen-to-Persona Coverage Matrix

| Screen                      | Patient | Staff     | Admin   | Notes                          |
| --------------------------- | ------- | --------- | ------- | ------------------------------ |
| SCR-001 Registration        | Primary | -         | -       | Public page                    |
| SCR-002 Login               | Primary | Primary   | Primary | Shared entry point             |
| SCR-003 Session Timeout     | Primary | Primary   | Primary | Overlay on all                 |
| SCR-004 Provider Search     | Primary | -         | -       | Patient booking flow           |
| SCR-005 Booking Confirm     | Primary | -         | -       | Patient booking flow           |
| SCR-006 Patient Dashboard   | Primary | -         | -       | Patient hub                    |
| SCR-007 Waitlist Status     | Primary | -         | -       | Patient option                 |
| SCR-008 Reschedule/Cancel   | Primary | Secondary | -       | Staff can reschedule on behalf |
| SCR-009 Calendar Sync       | Primary | -         | -       | Post-booking option            |
| SCR-010 AI Intake           | Primary | -         | -       | Patient intake                 |
| SCR-011 Manual Intake       | Primary | -         | -       | Patient intake                 |
| SCR-012 Intake Summary      | Primary | -         | -       | Patient review                 |
| SCR-013 Insurance Check     | Primary | -         | -       | Booking sub-flow               |
| SCR-014 Document Upload     | Primary | -         | -       | Clinical upload                |
| SCR-015 Processing Status   | Primary | -         | -       | Upload tracking                |
| SCR-016 360-Degree View     | Primary | Secondary | -       | Staff views for context        |
| SCR-017 Conflict Resolution | -       | Primary   | -       | Staff clinical task            |
| SCR-018 Code Verification   | -       | Primary   | -       | Staff clinical task            |
| SCR-019 Walk-In Booking     | -       | Primary   | -       | Staff scheduling               |
| SCR-020 Queue Management    | -       | Primary   | -       | Staff operations               |
| SCR-021 Staff Dashboard     | -       | Primary   | -       | Staff hub                      |
| SCR-022 No-Show Risk        | -       | Primary   | -       | Staff risk view                |
| SCR-023 User Management     | -       | -         | Primary | Admin CRUD                     |
| SCR-024 Admin Dashboard     | -       | -         | Primary | Admin hub                      |
| SCR-025 Audit Log Viewer    | -       | -         | Primary | Admin compliance               |

### Modal/Overlay Inventory

| Name                              | Type   | Trigger                            | Parent Screen(s)  | Priority |
| --------------------------------- | ------ | ---------------------------------- | ----------------- | -------- |
| OVL-001 Booking Confirmation      | Modal  | Click "Confirm Booking"            | SCR-005           | P0       |
| OVL-002 Preferred Slot Swap       | Modal  | Click "Request Preferred Slot"     | SCR-005           | P1       |
| OVL-003 Swap Notification         | Toast  | System event (slot swap executed)  | SCR-006           | P1       |
| OVL-004 Appointment PDF Preview   | Drawer | Click "View PDF"                   | SCR-006           | P1       |
| OVL-005 Deactivate/Cancel Confirm | Dialog | Destructive actions                | SCR-008, SCR-023  | P0       |
| OVL-006 Conflict Detail           | Modal  | Click conflict row                 | SCR-017           | P0       |
| OVL-007 Code Rejection Reason     | Dialog | Click "Reject Code"                | SCR-018           | P0       |
| OVL-008 Session Timeout           | Modal  | 14-minute inactivity trigger       | All authenticated | P0       |
| OVL-009 Create Patient Account    | Modal  | Click "New Patient" during walk-in | SCR-019           | P0       |
| OVL-010 Calendar OAuth            | Modal  | Calendar sync initiation           | SCR-009           | P1       |

---

## 7. Content & Tone

### Voice & Tone

- **Overall Tone**: Professional, warm, and reassuring (healthcare context demands trust)
- **Error Messages**: Helpful, non-blaming, actionable (e.g., "We couldn't verify your insurance. Please check the member ID and try again.")
- **Empty States**: Encouraging with clear next-action CTA (e.g., "No appointments yet. Search for a provider to get started.")
- **Success Messages**: Brief, confirming (e.g., "Appointment confirmed for Dr. Smith on April 20 at 2:00 PM.")
- **AI Outputs**: Transparent about confidence (e.g., "AI-suggested with 92% confidence - requires staff verification")

### Content Guidelines

- **Headings**: Sentence case throughout
- **CTAs**: Action-oriented verbs ("Book appointment", "Upload documents", "Verify code")
- **Labels**: Concise, descriptive (max 3 words preferred)
- **Placeholder Text**: Helpful examples (e.g., "e.g., Cardiology", "MM/DD/YYYY")
- **Clinical Data**: Always display source document reference and confidence score alongside values
- **PHI Display**: Masked by default with reveal-on-click where applicable (e.g., SSN, DOB in admin views)

---

## 8. Data & Edge Cases

### Data Scenarios

| Scenario           | Description                                        | Handling                                                           |
| ------------------ | -------------------------------------------------- | ------------------------------------------------------------------ |
| No Data            | Patient has no appointments, docs, or intake       | Empty state with illustration + CTA                                |
| First Use          | New patient registration complete, no history      | Onboarding checklist (Book, Intake, Upload)                        |
| Large Data         | Patient with 50+ documents, 100+ extracted entries | Pagination (20 items/page); virtualized lists                      |
| Slow Connection    | API response > 3s                                  | Skeleton screens preserving layout                                 |
| Offline            | Network unavailable                                | Offline banner; cached last-viewed data if available               |
| AI Unavailable     | Ollama service down (NFR-013)                      | Banner notification; AI toggle disabled; manual mode auto-selected |
| Low Confidence     | Extraction confidence < 0.7                        | Amber badge "Low Confidence"; source page reference required       |
| Critical Conflict  | Contraindicated medications detected               | Red alert badge; high-priority staff notification                  |
| Session Expiring   | 14 minutes of inactivity                           | Countdown modal at 14 min; forced logout at 15 min                 |
| Concurrent Booking | Two patients book same slot simultaneously         | Optimistic lock; second user sees "Slot no longer available" toast |

### Edge Cases

| Case                             | Screen(s) Affected     | Solution                                                             |
| -------------------------------- | ---------------------- | -------------------------------------------------------------------- |
| Long patient name                | All screens with names | Truncation with ellipsis + full name tooltip                         |
| Long medication name             | SCR-016, SCR-017       | Truncation with hover expansion                                      |
| 20-page PDF upload               | SCR-014                | Progress indicator per page; max 20 pages enforced                   |
| Multiple file upload             | SCR-014                | Batch upload UI with per-file status indicators                      |
| Duplicate appointment attempt    | SCR-005                | Server-side idempotency; client shows existing booking               |
| Expired JWT mid-form             | All form screens       | Silent token refresh; if failed, session timeout modal               |
| Queue reorder race condition     | SCR-020                | WebSocket-driven reconciliation; optimistic update                   |
| No providers for search criteria | SCR-004                | "No providers match your criteria" empty state with filter reset CTA |

---

## 9. Branding & Visual Direction

_See `designsystem.md` for all design tokens (colors, typography, spacing, shadows, etc.)_

### Branding Assets

- **Logo**: Text-based wordmark "PatientAccess" in primary blue (#1E6F9F) with a subtle health-cross icon
- **Icon Style**: Outlined, 24px default, 2px stroke, rounded joins (Lucide Icons or similar open-source set)
- **Illustration Style**: Flat, minimal healthcare-themed illustrations for empty states and onboarding (open-source: unDraw or Storyset)
- **Photography Style**: Not applicable (no stock photos in Phase 1)

---

## 10. Component Specifications

### Component Library Reference

**Source**: `.propel/context/docs/designsystem.md` (Component Specifications section)

### Required Components per Screen

| Screen ID | Components Required                                                               | Notes                                                          |
| --------- | --------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| SCR-001   | TextField (6), Button (2), Link (1), Alert (1)                                    | Registration form: name, DOB, email, password, phone, address  |
| SCR-002   | TextField (2), Button (1), Link (2), Alert (1)                                    | Login: email, password, forgot-password link, register link    |
| SCR-003   | Modal (1), Button (2), ProgressBar (1)                                            | Session timeout countdown with extend/logout                   |
| SCR-004   | TextField (1), Select (2), DatePicker (1), Card (N), Pagination (1), Skeleton (N) | Provider search + results grid                                 |
| SCR-005   | Card (1), Button (3), Checkbox (1), Badge (1), Modal (1)                          | Booking summary, confirm, preferred swap toggle                |
| SCR-006   | Card (N), Tabs (1), Badge (N), Button (N), Table (1), Toast (1)                   | Dashboard with appointment cards, status badges                |
| SCR-007   | Card (N), Badge (N), Button (1)                                                   | Waitlist entries with status                                   |
| SCR-008   | Card (1), Select (1), DatePicker (1), Button (2), Dialog (1)                      | Reschedule form + cancel confirmation                          |
| SCR-009   | Button (2), Modal (1), Alert (1)                                                  | Google/Outlook calendar options, OAuth flow                    |
| SCR-010   | ChatBubble (N), TextField (1), Button (2), Toggle (1), Alert (1)                  | AI chat interface with mode switch                             |
| SCR-011   | TextField (8), Select (3), TextArea (2), Checkbox (N), Button (2), Toggle (1)     | Structured intake form with mode switch                        |
| SCR-012   | Card (6), TextField (N inline-edit), Button (2), Badge (N)                        | Categorized intake summary with edit capability                |
| SCR-013   | TextField (2), Button (1), Alert (1), Badge (1)                                   | Insurance name, member ID, validation result                   |
| SCR-014   | FileDropzone (1), ProgressBar (N), Card (N), Button (2), Alert (1)                | Drag-drop upload with file list                                |
| SCR-015   | Card (N), Badge (N), ProgressBar (N), Skeleton (N)                                | Processing pipeline status per document                        |
| SCR-016   | Tabs (6), Card (N), Table (N), Badge (N), Tag (N), Alert (N), Skeleton (N)        | Tabbed view: Vitals, History, Meds, Allergies, Labs, Diagnoses |
| SCR-017   | Card (2), RadioGroup (1), TextArea (1), Button (2), Badge (1), Modal (1)          | Side-by-side conflict + resolution form                        |
| SCR-018   | Table (2), Badge (N), Button (3), Select (1), Dialog (1)                          | ICD-10 table + CPT table + verify/reject actions               |
| SCR-019   | TextField (4), Select (1), Button (2), Modal (1)                                  | Walk-in patient form + new-patient modal                       |
| SCR-020   | Table (1), Badge (N), Button (N), Chip (N), Skeleton (1)                          | Queue list with real-time status chips                         |
| SCR-021   | Card (4), Table (1), Badge (N), Skeleton (N)                                      | Summary cards + today's schedule                               |
| SCR-022   | Table (1), Badge (N), Card (N), ProgressBar (N)                                   | Risk score table with color-coded indicators                   |
| SCR-023   | Table (1), TextField (3), Select (1), Button (3), Modal (1), Dialog (1)           | User list + create/edit modal + deactivate dialog              |
| SCR-024   | Card (4), Table (1), Skeleton (N)                                                 | System stats + recent activity                                 |
| SCR-025   | Table (1), TextField (1), Select (2), DatePicker (2), Pagination (1)              | Searchable, filterable audit log table                         |

### Component Summary

| Category   | Components                                                                     | Variants                                                                               |
| ---------- | ------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------- |
| Actions    | Button, IconButton, Link, FAB                                                  | Primary/Secondary/Tertiary/Ghost x S/M/L x Default/Hover/Focus/Active/Disabled/Loading |
| Inputs     | TextField, TextArea, Select, Checkbox, Radio, Toggle, DatePicker, FileDropzone | States x Sizes                                                                         |
| Navigation | Header, Sidebar, BottomNav, Tabs, Breadcrumb, Pagination                       | Platform variants                                                                      |
| Content    | Card, ListItem, Table, Avatar, Badge, Tag, Chip, ChatBubble                    | Content variants                                                                       |
| Feedback   | Modal, Dialog, Drawer, Toast, Alert, Tooltip, Skeleton, ProgressBar            | Types x States                                                                         |
| Layout     | Container, Grid, Divider, Spacer                                               | Responsive variants                                                                    |

### Component Constraints

- Use only components from designsystem.md
- No custom components without design review
- All components MUST support all defined states (Default, Hover, Focus, Active, Disabled, Loading)
- Naming convention: `C/<Category>/<Name>` (e.g., `C/Actions/Button`, `C/Inputs/TextField`)

---

## 11. Prototype Flows

### Flow: FL-001 - Patient Registration & Login

**Flow ID**: FL-001
**Derived From**: UC-001 precondition, FR-001, FR-002, FR-003
**Personas Covered**: Patient, Staff, Admin
**Description**: User registers a new account or logs in to access the platform

#### Flow Sequence

```text
1. Entry: SCR-002 Login / Default
   - Trigger: User navigates to platform URL
   |
   v
2. Decision Point:
   +-- Has account -> Submit credentials
   |   |
   |   v
   |   SCR-002 Login / Loading (authenticating)
   |   |
   |   +-- Success -> SCR-006 Patient Dashboard / Default (Patient)
   |   |              SCR-021 Staff Dashboard / Default (Staff)
   |   |              SCR-024 Admin Dashboard / Default (Admin)
   |   +-- Error -> SCR-002 Login / Error (invalid credentials)
   |
   +-- No account -> Click "Register"
       |
       v
       SCR-001 Registration / Default
       |
       v
       SCR-001 Registration / Loading (submitting)
       |
       +-- Success -> SCR-002 Login / Default (redirect to login)
       +-- Validation -> SCR-001 Registration / Validation (field errors)
```

#### Required Interactions

- Form validation on submit (email format, password strength, required fields)
- Error recovery with inline field corrections
- Session timeout modal (OVL-008) at 14-minute mark on any authenticated screen

---

### Flow: FL-002 - Provider Search & Appointment Booking

**Flow ID**: FL-002
**Derived From**: UC-001, FR-005, FR-006, FR-007, FR-008, FR-009
**Personas Covered**: Patient
**Description**: Patient searches for providers, selects a slot, and books an appointment with optional preferred slot swap and calendar sync

#### Flow Sequence

```text
1. Entry: SCR-006 Patient Dashboard / Default
   - Trigger: Click "Book Appointment" or nav item
   |
   v
2. SCR-004 Provider Search / Default
   - Action: Patient enters specialty, name, date range
   |
   v
3. SCR-004 Provider Search / Loading
   - Action: Fetching results
   |
   v
4. Decision Point:
   +-- Results found -> SCR-004 Provider Search / Default (results displayed)
   |   |
   |   v
   |   Click provider slot
   |   |
   |   v
   |   SCR-005 Booking Confirmation / Default
   |   |
   |   +-- Optional: Select preferred unavailable slot -> OVL-002 opens
   |   |
   |   v
   |   Click "Confirm Booking"
   |   |
   |   v
   |   OVL-001 Booking Confirmation Modal
   |   |
   |   v
   |   SCR-005 / Loading (processing booking)
   |   |
   |   +-- Success -> SCR-006 Patient Dashboard / Default + OVL-003 Toast
   |   |              Optional: SCR-009 Calendar Sync / Default
   |   +-- Error -> SCR-005 / Error ("Slot no longer available")
   |
   +-- No results -> SCR-004 / Empty ("No providers found")
       |
       +-- Join waitlist -> SCR-007 Waitlist / Default
```

#### Required Interactions

- Real-time slot availability updates via WebSocket
- Optimistic booking with rollback on failure
- Waitlist enrollment as fallback for empty results

---

### Flow: FL-003 - Patient Intake (AI + Manual)

**Flow ID**: FL-003
**Derived From**: UC-002, UC-003, FR-014, FR-015, FR-016, FR-017
**Personas Covered**: Patient
**Description**: Patient completes pre-visit intake through AI conversational or manual form, with seamless switching between modes

#### Flow Sequence

```text
1. Entry: SCR-006 Patient Dashboard / Default
   - Trigger: Click "Complete Intake" on upcoming appointment
   |
   v
2. Decision Point (mode selection):
   +-- AI Mode -> SCR-010 AI Intake / Default
   |   |
   |   v
   |   AI asks questions -> Patient responds -> AI parses
   |   |
   |   +-- AI unavailable -> SCR-010 / Error + UXR-605 banner
   |   |                     Auto-redirect to SCR-011
   |   +-- Switch mode -> SCR-011 Manual Intake / Default (pre-filled)
   |   +-- Complete -> SCR-012 Intake Summary / Default
   |
   +-- Manual Mode -> SCR-011 Manual Intake / Default
       |
       v
       Patient fills structured fields
       |
       +-- Switch mode -> SCR-010 AI Intake / Default (context preserved)
       +-- Validation error -> SCR-011 / Validation
       +-- Submit -> SCR-012 Intake Summary / Default

3. SCR-012 Intake Summary / Default
   - Patient reviews all data; click-to-edit any field
   |
   +-- Edit field -> SCR-012 / Validation (inline edit)
   +-- Confirm -> SCR-006 Dashboard (intake status: Complete)
```

#### Required Interactions

- Mode toggle preserves all entered data
- Autosave every 30 seconds
- AI fallback to manual on three consecutive low-confidence responses

---

### Flow: FL-004 - Clinical Document Upload & Processing

**Flow ID**: FL-004
**Derived From**: UC-008, FR-023, FR-024
**Personas Covered**: Patient
**Description**: Patient uploads clinical PDFs and monitors extraction processing status

#### Flow Sequence

```text
1. Entry: SCR-006 Patient Dashboard / Default
   - Trigger: Click "Upload Documents" or nav item
   |
   v
2. SCR-014 Document Upload / Default
   - Action: Drag-and-drop or click to select PDFs
   |
   v
3. SCR-014 / Loading (upload in progress, per-file progress bars)
   |
   +-- File validation error -> SCR-014 / Error (invalid type/size per file)
   +-- Upload success -> SCR-015 Processing Status / Default

4. SCR-015 Processing Status / Default
   - Displays per-document status: Queued → Processing → Completed/Failed
   |
   +-- All completed -> SCR-016 360-Degree View / Default (auto-navigable)
   +-- Failed document -> SCR-015 / Error (retry button per file)
```

#### Required Interactions

- Drag-and-drop with file type/size validation
- Per-file progress indicators
- Per-file retry on failure without re-uploading successful files

---

### Flow: FL-005 - 360-Degree Patient View & Conflict Resolution

**Flow ID**: FL-005
**Derived From**: UC-009, UC-011, FR-025, FR-026
**Personas Covered**: Patient (view), Staff (resolve conflicts)
**Description**: View consolidated patient clinical profile and resolve data conflicts

#### Flow Sequence

```text
1. Entry: SCR-006 Patient Dashboard / Default (Patient)
         SCR-021 Staff Dashboard / Default (Staff)
   - Trigger: Click "View Health Profile" or "Resolve Conflicts"
   |
   v
2. SCR-016 360-Degree View / Default
   - Tabbed sections: Vitals, History, Medications, Allergies, Labs, Diagnoses
   - Confidence badges per data point
   - Conflict alert badges on sections with unresolved conflicts
   |
   +-- Click conflict badge -> SCR-017 Conflict Resolution / Default (Staff only)
   |
   v
3. SCR-017 Conflict Resolution / Default
   - Side-by-side conflicting values with source document references
   |
   v
4. Staff action:
   +-- Select authoritative value -> Submit resolution
   |   |
   |   v
   |   SCR-017 / Loading
   |   |
   |   +-- Success -> SCR-016 / Default (conflict resolved, badge removed)
   |   +-- Error -> SCR-017 / Error
   |
   +-- Mark "Pending Review" -> SCR-017 / Default (conflict flagged pending)
```

#### Required Interactions

- Tabbed navigation within 360-degree view
- Conflict count badges on affected section tabs
- Source document link opens document at referenced page

---

### Flow: FL-006 - Medical Code Verification

**Flow ID**: FL-006
**Derived From**: UC-010, FR-027, FR-028
**Personas Covered**: Staff
**Description**: Staff reviews and verifies AI-suggested ICD-10 and CPT codes

#### Flow Sequence

```text
1. Entry: SCR-021 Staff Dashboard / Default
   - Trigger: Click "Verify Codes" or notification badge
   |
   v
2. SCR-018 Medical Code Verification / Default
   - Two tables: ICD-10 suggested codes, CPT suggested codes
   - Each row: Code, Description, Confidence, Source, Status
   |
   v
3. Staff action per code:
   +-- Accept -> Status changes to "Verified" (inline update)
   +-- Reject -> OVL-007 Rejection Reason Dialog
   |   |
   |   v
   |   Enter reason -> Submit -> Status "Rejected"
   +-- Add manual code -> Inline add row with staff attribution

4. All codes reviewed -> SCR-018 shows completion summary
```

#### Required Interactions

- Inline status update (no page reload)
- Confidence score color-coding (green/amber/red)
- Rejection requires reason entry (OVL-007)
- Manual code addition with search/autocomplete

---

### Flow: FL-007 - Walk-In Booking & Queue Management

**Flow ID**: FL-007
**Derived From**: UC-005, UC-006, FR-011, FR-012, FR-013
**Personas Covered**: Staff
**Description**: Staff registers walk-in patients, manages same-day queue, and marks arrivals

#### Flow Sequence

```text
1. Entry: SCR-021 Staff Dashboard / Default
   - Trigger: Click "Walk-In" or "Queue" nav item
   |
   v
2. SCR-019 Walk-In Booking / Default
   - Staff searches existing patient or creates new
   |
   +-- New patient -> OVL-009 Create Patient Account Modal
   |   |
   |   v
   |   Fill demographics -> Create -> Return to SCR-019
   |
   +-- Existing patient found -> Select and proceed
   |
   v
3. Staff selects same-day slot or adds to queue
   |
   v
4. SCR-020 Queue Management / Default
   - Ordered list with status chips: Waiting, In-Progress, Completed
   - Real-time WebSocket updates
   |
   v
5. Staff actions:
   +-- Mark Arrived (scheduled appointment) -> Status update
   +-- Update status: Waiting -> In-Progress -> Completed
   +-- Mark No-Show -> Status update with audit log
```

#### Required Interactions

- Real-time queue reordering via WebSocket
- Patient search with autocomplete
- Status chip transitions with confirmation

---

### Flow: FL-008 - Admin User Management

**Flow ID**: FL-008
**Derived From**: UC-012, FR-004
**Personas Covered**: Admin
**Description**: Admin creates, updates, and deactivates user accounts with role assignment

#### Flow Sequence

```text
1. Entry: SCR-024 Admin Dashboard / Default
   - Trigger: Click "Manage Users" nav item
   |
   v
2. SCR-023 User Management / Default
   - Searchable, filterable user table
   |
   v
3. Admin actions:
   +-- Create user -> Modal form (name, email, role) -> Submit
   |   +-- Success -> Table updated, toast confirmation
   |   +-- Duplicate email -> Validation error in modal
   |
   +-- Edit user -> Inline edit or modal -> Save
   |   +-- Success -> Row updated
   |
   +-- Deactivate user -> OVL-005 Confirmation Dialog
       +-- Confirm -> Status set to "Deactivated"
       +-- Cancel -> Return to table
```

#### Required Interactions

- User search with filter by role and status
- Deactivation requires explicit confirmation dialog
- Role change triggers notification to affected user

---

### Flow: FL-009 - Insurance Pre-Check

**Flow ID**: FL-009
**Derived From**: UC-013, FR-018
**Personas Covered**: Patient
**Description**: Patient validates insurance information during booking or intake

#### Flow Sequence

```text
1. Entry: SCR-005 Booking Confirmation / Default
         OR SCR-012 Intake Summary / Default
   - Trigger: Insurance section during booking or intake
   |
   v
2. SCR-013 Insurance Pre-Check / Default
   - Fields: Insurance Provider Name, Member ID
   |
   v
3. Submit -> SCR-013 / Loading
   |
   +-- Verified -> SCR-013 / Default (green badge "Verified")
   +-- Unverified -> SCR-013 / Error (amber badge "Unverified" + reason)
   |
   v
4. Return to parent flow (booking/intake continues regardless of result)
```

#### Required Interactions

- Inline validation result (no page navigation)
- Unverified status does not block booking (soft check)
- Result flagged for staff visibility

---

## 12. Export Requirements

### JPG Export Settings

| Setting        | Value      |
| -------------- | ---------- |
| Format         | JPG        |
| Quality        | High (85%) |
| Scale - Mobile | 2x         |
| Scale - Web    | 2x         |
| Color Profile  | sRGB       |

### Export Naming Convention

`PatientAccess__<Platform>__<ScreenName>__<State>__v1.jpg`

### Export Manifest

| Screen                     | State      | Platform | Filename                                                   |
| -------------------------- | ---------- | -------- | ---------------------------------------------------------- |
| SCR-001 Registration       | Default    | Web      | PatientAccess**Web**Registration**Default**v1.jpg          |
| SCR-001 Registration       | Loading    | Web      | PatientAccess**Web**Registration**Loading**v1.jpg          |
| SCR-001 Registration       | Error      | Web      | PatientAccess**Web**Registration**Error**v1.jpg            |
| SCR-001 Registration       | Validation | Web      | PatientAccess**Web**Registration**Validation**v1.jpg       |
| SCR-002 Login              | Default    | Web      | PatientAccess**Web**Login**Default**v1.jpg                 |
| SCR-002 Login              | Loading    | Web      | PatientAccess**Web**Login**Loading**v1.jpg                 |
| SCR-002 Login              | Error      | Web      | PatientAccess**Web**Login**Error**v1.jpg                   |
| SCR-002 Login              | Validation | Web      | PatientAccess**Web**Login**Validation**v1.jpg              |
| SCR-004 ProviderSearch     | Default    | Web      | PatientAccess**Web**ProviderSearch**Default**v1.jpg        |
| SCR-004 ProviderSearch     | Loading    | Web      | PatientAccess**Web**ProviderSearch**Loading**v1.jpg        |
| SCR-004 ProviderSearch     | Empty      | Web      | PatientAccess**Web**ProviderSearch**Empty**v1.jpg          |
| SCR-004 ProviderSearch     | Error      | Web      | PatientAccess**Web**ProviderSearch**Error**v1.jpg          |
| SCR-005 BookingConfirm     | Default    | Web      | PatientAccess**Web**BookingConfirm**Default**v1.jpg        |
| SCR-005 BookingConfirm     | Loading    | Web      | PatientAccess**Web**BookingConfirm**Loading**v1.jpg        |
| SCR-005 BookingConfirm     | Error      | Web      | PatientAccess**Web**BookingConfirm**Error**v1.jpg          |
| SCR-005 BookingConfirm     | Validation | Web      | PatientAccess**Web**BookingConfirm**Validation**v1.jpg     |
| SCR-006 PatientDashboard   | Default    | Web      | PatientAccess**Web**PatientDashboard**Default**v1.jpg      |
| SCR-006 PatientDashboard   | Loading    | Web      | PatientAccess**Web**PatientDashboard**Loading**v1.jpg      |
| SCR-006 PatientDashboard   | Empty      | Web      | PatientAccess**Web**PatientDashboard**Empty**v1.jpg        |
| SCR-006 PatientDashboard   | Error      | Web      | PatientAccess**Web**PatientDashboard**Error**v1.jpg        |
| SCR-010 AIIntake           | Default    | Web      | PatientAccess**Web**AIIntake**Default**v1.jpg              |
| SCR-010 AIIntake           | Loading    | Web      | PatientAccess**Web**AIIntake**Loading**v1.jpg              |
| SCR-010 AIIntake           | Error      | Web      | PatientAccess**Web**AIIntake**Error**v1.jpg                |
| SCR-011 ManualIntake       | Default    | Web      | PatientAccess**Web**ManualIntake**Default**v1.jpg          |
| SCR-011 ManualIntake       | Loading    | Web      | PatientAccess**Web**ManualIntake**Loading**v1.jpg          |
| SCR-011 ManualIntake       | Error      | Web      | PatientAccess**Web**ManualIntake**Error**v1.jpg            |
| SCR-011 ManualIntake       | Validation | Web      | PatientAccess**Web**ManualIntake**Validation**v1.jpg       |
| SCR-012 IntakeSummary      | Default    | Web      | PatientAccess**Web**IntakeSummary**Default**v1.jpg         |
| SCR-012 IntakeSummary      | Loading    | Web      | PatientAccess**Web**IntakeSummary**Loading**v1.jpg         |
| SCR-012 IntakeSummary      | Error      | Web      | PatientAccess**Web**IntakeSummary**Error**v1.jpg           |
| SCR-012 IntakeSummary      | Validation | Web      | PatientAccess**Web**IntakeSummary**Validation**v1.jpg      |
| SCR-014 DocumentUpload     | Default    | Web      | PatientAccess**Web**DocumentUpload**Default**v1.jpg        |
| SCR-014 DocumentUpload     | Loading    | Web      | PatientAccess**Web**DocumentUpload**Loading**v1.jpg        |
| SCR-014 DocumentUpload     | Empty      | Web      | PatientAccess**Web**DocumentUpload**Empty**v1.jpg          |
| SCR-014 DocumentUpload     | Error      | Web      | PatientAccess**Web**DocumentUpload**Error**v1.jpg          |
| SCR-016 360DegreeView      | Default    | Web      | PatientAccess**Web**360DegreeView**Default**v1.jpg         |
| SCR-016 360DegreeView      | Loading    | Web      | PatientAccess**Web**360DegreeView**Loading**v1.jpg         |
| SCR-016 360DegreeView      | Empty      | Web      | PatientAccess**Web**360DegreeView**Empty**v1.jpg           |
| SCR-016 360DegreeView      | Error      | Web      | PatientAccess**Web**360DegreeView**Error**v1.jpg           |
| SCR-017 ConflictResolution | Default    | Web      | PatientAccess**Web**ConflictResolution**Default**v1.jpg    |
| SCR-017 ConflictResolution | Loading    | Web      | PatientAccess**Web**ConflictResolution**Loading**v1.jpg    |
| SCR-017 ConflictResolution | Empty      | Web      | PatientAccess**Web**ConflictResolution**Empty**v1.jpg      |
| SCR-017 ConflictResolution | Error      | Web      | PatientAccess**Web**ConflictResolution**Error**v1.jpg      |
| SCR-017 ConflictResolution | Validation | Web      | PatientAccess**Web**ConflictResolution**Validation**v1.jpg |
| SCR-018 CodeVerification   | Default    | Web      | PatientAccess**Web**CodeVerification**Default**v1.jpg      |
| SCR-018 CodeVerification   | Loading    | Web      | PatientAccess**Web**CodeVerification**Loading**v1.jpg      |
| SCR-018 CodeVerification   | Empty      | Web      | PatientAccess**Web**CodeVerification**Empty**v1.jpg        |
| SCR-018 CodeVerification   | Error      | Web      | PatientAccess**Web**CodeVerification**Error**v1.jpg        |
| SCR-018 CodeVerification   | Validation | Web      | PatientAccess**Web**CodeVerification**Validation**v1.jpg   |
| SCR-019 WalkInBooking      | Default    | Web      | PatientAccess**Web**WalkInBooking**Default**v1.jpg         |
| SCR-019 WalkInBooking      | Loading    | Web      | PatientAccess**Web**WalkInBooking**Loading**v1.jpg         |
| SCR-019 WalkInBooking      | Error      | Web      | PatientAccess**Web**WalkInBooking**Error**v1.jpg           |
| SCR-019 WalkInBooking      | Validation | Web      | PatientAccess**Web**WalkInBooking**Validation**v1.jpg      |
| SCR-020 QueueManagement    | Default    | Web      | PatientAccess**Web**QueueManagement**Default**v1.jpg       |
| SCR-020 QueueManagement    | Loading    | Web      | PatientAccess**Web**QueueManagement**Loading**v1.jpg       |
| SCR-020 QueueManagement    | Empty      | Web      | PatientAccess**Web**QueueManagement**Empty**v1.jpg         |
| SCR-020 QueueManagement    | Error      | Web      | PatientAccess**Web**QueueManagement**Error**v1.jpg         |
| SCR-021 StaffDashboard     | Default    | Web      | PatientAccess**Web**StaffDashboard**Default**v1.jpg        |
| SCR-021 StaffDashboard     | Loading    | Web      | PatientAccess**Web**StaffDashboard**Loading**v1.jpg        |
| SCR-021 StaffDashboard     | Empty      | Web      | PatientAccess**Web**StaffDashboard**Empty**v1.jpg          |
| SCR-021 StaffDashboard     | Error      | Web      | PatientAccess**Web**StaffDashboard**Error**v1.jpg          |
| SCR-023 UserManagement     | Default    | Web      | PatientAccess**Web**UserManagement**Default**v1.jpg        |
| SCR-023 UserManagement     | Loading    | Web      | PatientAccess**Web**UserManagement**Loading**v1.jpg        |
| SCR-023 UserManagement     | Empty      | Web      | PatientAccess**Web**UserManagement**Empty**v1.jpg          |
| SCR-023 UserManagement     | Error      | Web      | PatientAccess**Web**UserManagement**Error**v1.jpg          |
| SCR-023 UserManagement     | Validation | Web      | PatientAccess**Web**UserManagement**Validation**v1.jpg     |
| SCR-024 AdminDashboard     | Default    | Web      | PatientAccess**Web**AdminDashboard**Default**v1.jpg        |
| SCR-024 AdminDashboard     | Loading    | Web      | PatientAccess**Web**AdminDashboard**Loading**v1.jpg        |
| SCR-024 AdminDashboard     | Empty      | Web      | PatientAccess**Web**AdminDashboard**Empty**v1.jpg          |
| SCR-024 AdminDashboard     | Error      | Web      | PatientAccess**Web**AdminDashboard**Error**v1.jpg          |
| SCR-025 AuditLogViewer     | Default    | Web      | PatientAccess**Web**AuditLogViewer**Default**v1.jpg        |
| SCR-025 AuditLogViewer     | Loading    | Web      | PatientAccess**Web**AuditLogViewer**Loading**v1.jpg        |
| SCR-025 AuditLogViewer     | Empty      | Web      | PatientAccess**Web**AuditLogViewer**Empty**v1.jpg          |
| SCR-025 AuditLogViewer     | Error      | Web      | PatientAccess**Web**AuditLogViewer**Error**v1.jpg          |

### Total Export Count

- **Screens**: 25
- **States per screen**: 4-5 average
- **Total JPGs**: 104 (web platform)

---

## 13. Figma File Structure

### Page Organization

```text
PatientAccess Figma File
+-- 00_Cover
|   +-- Project: Unified Patient Access & Clinical Intelligence Platform
|   +-- Version: 1.0
|   +-- Stakeholders: Product, Engineering, Clinical
|   +-- Last Updated: 2026-04-15
+-- 01_Foundations
|   +-- Color tokens (Light + Dark + Semantic)
|   +-- Typography scale (Inter family)
|   +-- Spacing scale (4-64px)
|   +-- Radius tokens (sm/md/lg/full)
|   +-- Elevation levels (1-5)
|   +-- Grid definitions (12-col, 4-col mobile)
+-- 02_Components
|   +-- C/Actions/[Button, IconButton, Link, FAB]
|   +-- C/Inputs/[TextField, TextArea, Select, Checkbox, Radio, Toggle, DatePicker, FileDropzone]
|   +-- C/Navigation/[Header, Sidebar, BottomNav, Tabs, Breadcrumb, Pagination]
|   +-- C/Content/[Card, ListItem, Table, Avatar, Badge, Tag, Chip, ChatBubble]
|   +-- C/Feedback/[Modal, Dialog, Drawer, Toast, Alert, Tooltip, Skeleton, ProgressBar]
|   +-- C/Layout/[Container, Grid, Divider, Spacer]
+-- 03_Patterns
|   +-- Auth form pattern (login/register)
|   +-- Search + filter + results pattern
|   +-- Detail page with tabs pattern
|   +-- Upload + progress pattern
|   +-- Chat interface pattern
|   +-- Side-by-side comparison pattern
|   +-- Dashboard summary cards pattern
|   +-- Error/Empty/Loading state patterns
+-- 04_Screens
|   +-- SCR-001_Registration/[Default, Loading, Error, Validation]
|   +-- SCR-002_Login/[Default, Loading, Error, Validation]
|   +-- SCR-004_ProviderSearch/[Default, Loading, Empty, Error]
|   +-- SCR-005_BookingConfirm/[Default, Loading, Error, Validation]
|   +-- SCR-006_PatientDashboard/[Default, Loading, Empty, Error]
|   +-- SCR-010_AIIntake/[Default, Loading, Error]
|   +-- SCR-011_ManualIntake/[Default, Loading, Error, Validation]
|   +-- SCR-012_IntakeSummary/[Default, Loading, Error, Validation]
|   +-- SCR-014_DocumentUpload/[Default, Loading, Empty, Error]
|   +-- SCR-016_360DegreeView/[Default, Loading, Empty, Error]
|   +-- SCR-017_ConflictResolution/[Default, Loading, Empty, Error, Validation]
|   +-- SCR-018_CodeVerification/[Default, Loading, Empty, Error, Validation]
|   +-- SCR-019_WalkInBooking/[Default, Loading, Error, Validation]
|   +-- SCR-020_QueueManagement/[Default, Loading, Empty, Error]
|   +-- SCR-021_StaffDashboard/[Default, Loading, Empty, Error]
|   +-- SCR-022_NoShowRisk/[Default, Loading, Empty, Error]
|   +-- SCR-023_UserManagement/[Default, Loading, Empty, Error, Validation]
|   +-- SCR-024_AdminDashboard/[Default, Loading, Empty, Error]
|   +-- SCR-025_AuditLogViewer/[Default, Loading, Empty, Error]
|   +-- [Remaining: SCR-003, SCR-007, SCR-008, SCR-009, SCR-013, SCR-015]
+-- 05_Prototype
|   +-- FL-001: Patient Registration & Login
|   +-- FL-002: Provider Search & Booking
|   +-- FL-003: Patient Intake (AI + Manual)
|   +-- FL-004: Document Upload & Processing
|   +-- FL-005: 360-View & Conflict Resolution
|   +-- FL-006: Medical Code Verification
|   +-- FL-007: Walk-In & Queue Management
|   +-- FL-008: Admin User Management
|   +-- FL-009: Insurance Pre-Check
+-- 06_Handoff
    +-- Token usage rules (when to use which token level)
    +-- Component guidelines (props, variants, usage context)
    +-- Responsive specs (breakpoint changes, layout shifts)
    +-- Edge cases (truncation rules, max content, overflow)
    +-- Accessibility notes (focus management, ARIA patterns)
    +-- Assumptions (decisions made when specs were unclear)
```

---

## 14. Quality Checklist

### Pre-Export Validation

- [x] All 25 screens have required states (Default/Loading/Empty/Error/Validation as applicable)
- [ ] All components use design tokens (no hard-coded values)
- [ ] Color contrast meets WCAG AA (>=4.5:1 text, >=3:1 UI)
- [ ] Focus states defined for all interactive elements
- [ ] Touch targets >= 44x44px (mobile)
- [ ] Prototype flows (9) wired and functional
- [x] Naming conventions followed (`C/<Category>/<Name>`, `<ScreenName>/<State>`)
- [x] Export manifest complete (104 JPGs)

### Post-Generation

- [x] designsystem.md created with token definitions
- [x] Export manifest generated
- [x] JPG files named per convention
- [ ] Handoff documentation pending Figma implementation
