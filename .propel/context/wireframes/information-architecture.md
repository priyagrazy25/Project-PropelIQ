---
post_title: "Unified Patient Access - Information Architecture"
author1: "AI UX Designer"
post_slug: "unified-patient-access-ia"
categories: "Healthcare, UX, Wireframes"
tags: "information-architecture, wireframes, navigation, screen-hierarchy"
ai_note: "Generated with AI assistance from figma_spec.md and designsystem.md"
summary: "Information architecture document with wireframe references, user flows, screen hierarchy, and navigation architecture for the Unified Patient Access Platform."
post_date: "2026-04-15"
---

# Information Architecture - Unified Patient Access

## 1. Wireframe Specification

**Fidelity Level**: High
**Screen Type**: Web (Responsive)
**Viewport**: 1440px x 900px (Desktop primary)

## 2. System Overview

The Unified Patient Access & Clinical Intelligence Platform is a HIPAA-compliant healthcare web application serving three personas (Patient, Staff, Admin). It combines appointment booking, clinical data intelligence with AI-powered extraction, and administrative controls into a single unified platform built with React and TypeScript.

## 3. Wireframe References

### HTML Wireframes

| Screen/Feature               | File Path                                                                                                  | Description                          | Fidelity | Date Created |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------- | ------------------------------------ | -------- | ------------ |
| SCR-001 Registration         | [./Hi-Fi/wireframe-SCR-001-registration.html](./Hi-Fi/wireframe-SCR-001-registration.html)                 | Patient registration form            | High     | 2026-04-15   |
| SCR-002 Login                | [./Hi-Fi/wireframe-SCR-002-login.html](./Hi-Fi/wireframe-SCR-002-login.html)                               | Authentication login screen          | High     | 2026-04-15   |
| SCR-004 Provider Search      | [./Hi-Fi/wireframe-SCR-004-provider-search.html](./Hi-Fi/wireframe-SCR-004-provider-search.html)           | Provider search with results grid    | High     | 2026-04-15   |
| SCR-005 Booking Confirmation | [./Hi-Fi/wireframe-SCR-005-booking-confirmation.html](./Hi-Fi/wireframe-SCR-005-booking-confirmation.html) | Appointment booking and confirmation | High     | 2026-04-15   |
| SCR-006 Patient Dashboard    | [./Hi-Fi/wireframe-SCR-006-patient-dashboard.html](./Hi-Fi/wireframe-SCR-006-patient-dashboard.html)       | Patient hub with appointments        | High     | 2026-04-15   |
| SCR-007 Waitlist Status      | [./Hi-Fi/wireframe-SCR-007-waitlist-status.html](./Hi-Fi/wireframe-SCR-007-waitlist-status.html)           | Waitlist entries with status         | High     | 2026-04-15   |
| SCR-008 Reschedule Cancel    | [./Hi-Fi/wireframe-SCR-008-reschedule-cancel.html](./Hi-Fi/wireframe-SCR-008-reschedule-cancel.html)       | Appointment reschedule/cancel        | High     | 2026-04-15   |
| SCR-009 Calendar Sync        | [./Hi-Fi/wireframe-SCR-009-calendar-sync.html](./Hi-Fi/wireframe-SCR-009-calendar-sync.html)               | Google/Outlook calendar sync         | High     | 2026-04-15   |
| SCR-010 AI Intake            | [./Hi-Fi/wireframe-SCR-010-ai-intake.html](./Hi-Fi/wireframe-SCR-010-ai-intake.html)                       | AI conversational intake chat        | High     | 2026-04-15   |
| SCR-011 Manual Intake        | [./Hi-Fi/wireframe-SCR-011-manual-intake.html](./Hi-Fi/wireframe-SCR-011-manual-intake.html)               | Manual form intake                   | High     | 2026-04-15   |
| SCR-012 Intake Summary       | [./Hi-Fi/wireframe-SCR-012-intake-summary.html](./Hi-Fi/wireframe-SCR-012-intake-summary.html)             | Intake review and confirm            | High     | 2026-04-15   |
| SCR-013 Insurance Pre-Check  | [./Hi-Fi/wireframe-SCR-013-insurance-precheck.html](./Hi-Fi/wireframe-SCR-013-insurance-precheck.html)     | Insurance validation form            | High     | 2026-04-15   |
| SCR-014 Document Upload      | [./Hi-Fi/wireframe-SCR-014-document-upload.html](./Hi-Fi/wireframe-SCR-014-document-upload.html)           | Clinical document upload             | High     | 2026-04-15   |
| SCR-015 Processing Status    | [./Hi-Fi/wireframe-SCR-015-processing-status.html](./Hi-Fi/wireframe-SCR-015-processing-status.html)       | Document extraction status           | High     | 2026-04-15   |
| SCR-016 360-Degree View      | [./Hi-Fi/wireframe-SCR-016-360-degree-view.html](./Hi-Fi/wireframe-SCR-016-360-degree-view.html)           | Consolidated patient view            | High     | 2026-04-15   |
| SCR-017 Conflict Resolution  | [./Hi-Fi/wireframe-SCR-017-conflict-resolution.html](./Hi-Fi/wireframe-SCR-017-conflict-resolution.html)   | Data conflict side-by-side           | High     | 2026-04-15   |
| SCR-018 Code Verification    | [./Hi-Fi/wireframe-SCR-018-code-verification.html](./Hi-Fi/wireframe-SCR-018-code-verification.html)       | ICD-10/CPT code verification         | High     | 2026-04-15   |
| SCR-019 Walk-In Booking      | [./Hi-Fi/wireframe-SCR-019-walkin-booking.html](./Hi-Fi/wireframe-SCR-019-walkin-booking.html)             | Staff walk-in booking                | High     | 2026-04-15   |
| SCR-020 Queue Management     | [./Hi-Fi/wireframe-SCR-020-queue-management.html](./Hi-Fi/wireframe-SCR-020-queue-management.html)         | Same-day queue management            | High     | 2026-04-15   |
| SCR-021 Staff Dashboard      | [./Hi-Fi/wireframe-SCR-021-staff-dashboard.html](./Hi-Fi/wireframe-SCR-021-staff-dashboard.html)           | Staff operations hub                 | High     | 2026-04-15   |
| SCR-022 No-Show Risk         | [./Hi-Fi/wireframe-SCR-022-noshow-risk.html](./Hi-Fi/wireframe-SCR-022-noshow-risk.html)                   | No-show risk dashboard               | High     | 2026-04-15   |
| SCR-023 User Management      | [./Hi-Fi/wireframe-SCR-023-user-management.html](./Hi-Fi/wireframe-SCR-023-user-management.html)           | Admin user CRUD                      | High     | 2026-04-15   |
| SCR-024 Admin Dashboard      | [./Hi-Fi/wireframe-SCR-024-admin-dashboard.html](./Hi-Fi/wireframe-SCR-024-admin-dashboard.html)           | Admin operations hub                 | High     | 2026-04-15   |
| SCR-025 Audit Log Viewer     | [./Hi-Fi/wireframe-SCR-025-audit-log.html](./Hi-Fi/wireframe-SCR-025-audit-log.html)                       | Audit log search and view            | High     | 2026-04-15   |

### Component Inventory

**Reference**: See [Component Inventory](./component-inventory.md) for detailed component documentation.

## 4. User Personas & Flows

### Persona 1: Patient

- **Role**: Registered healthcare consumer
- **Goals**: Book appointments, complete intake, upload clinical docs, view health profile
- **Key Screens**: SCR-001, SCR-002, SCR-004 through SCR-016
- **Primary Flow**: Login -> Dashboard -> Search Providers -> Book Slot -> Complete Intake -> Upload Docs -> View 360-Degree Profile
- **Decision Points**: AI vs Manual intake mode, Calendar sync provider, Preferred slot swap

### Persona 2: Staff (Front Desk / Call Center)

- **Role**: Authorized clinical and administrative staff
- **Goals**: Book walk-ins, manage queue, resolve conflicts, verify codes
- **Key Screens**: SCR-002, SCR-017 through SCR-022
- **Primary Flow**: Login -> Staff Dashboard -> Walk-In Booking -> Queue Management -> Conflict Resolution -> Code Verification
- **Decision Points**: New vs existing patient, Queue priority, Code accept/reject

### Persona 3: Admin

- **Role**: System administrator
- **Goals**: Manage users, assign roles, audit compliance
- **Key Screens**: SCR-002, SCR-023 through SCR-025
- **Primary Flow**: Login -> Admin Dashboard -> User Management -> Audit Logs
- **Decision Points**: Role assignment, Account activation/deactivation

### User Flow Diagrams

- **FL-001**: Patient Registration & Login - [SCR-002 -> SCR-001 -> SCR-002 -> SCR-006/021/024]
- **FL-002**: Provider Search & Booking - [SCR-006 -> SCR-004 -> SCR-005 -> SCR-006]
- **FL-003**: Patient Intake - [SCR-006 -> SCR-010/011 -> SCR-012 -> SCR-006]
- **FL-004**: Document Upload - [SCR-006 -> SCR-014 -> SCR-015 -> SCR-016]
- **FL-005**: 360-View & Conflicts - [SCR-006/021 -> SCR-016 -> SCR-017 -> SCR-016]
- **FL-006**: Code Verification - [SCR-021 -> SCR-018]
- **FL-007**: Walk-In & Queue - [SCR-021 -> SCR-019 -> SCR-020]
- **FL-008**: Admin User Mgmt - [SCR-024 -> SCR-023]
- **FL-009**: Insurance Pre-Check - [SCR-005/012 -> SCR-013]

## 5. Screen Hierarchy

### Level 1: Public (Unauthenticated)

- **SCR-001 Registration** (P0) - [Wireframe: wireframe-SCR-001-registration.html](./Hi-Fi/wireframe-SCR-001-registration.html)
  - Description: Patient self-registration with demographics
  - User Entry Point: Yes (new users)
  - Key Components: TextField (6), Button (2), Link (1), Alert (1)

- **SCR-002 Login** (P0) - [Wireframe: wireframe-SCR-002-login.html](./Hi-Fi/wireframe-SCR-002-login.html)
  - Description: Email/password authentication for all roles
  - User Entry Point: Yes (all users)
  - Key Components: TextField (2), Button (1), Link (2), Alert (1)

### Level 2: Patient Portal

- **SCR-006 Patient Dashboard** (P0) - [Wireframe: wireframe-SCR-006-patient-dashboard.html](./Hi-Fi/wireframe-SCR-006-patient-dashboard.html)
  - Description: Patient hub with upcoming appointments, intake status, document status
  - Parent Screen: Login (SCR-002)
  - Key Components: Card (N), Tabs (1), Badge (N), Button (N), Table (1)

- **SCR-004 Provider Search** (P0) - [Wireframe: wireframe-SCR-004-provider-search.html](./Hi-Fi/wireframe-SCR-004-provider-search.html)
  - Description: Search providers by specialty, name, date with real-time slot results
  - Parent Screen: Patient Dashboard (SCR-006)
  - Key Components: TextField (1), Select (2), DatePicker (1), Card (N), Pagination (1)

- **SCR-005 Booking Confirmation** (P0) - [Wireframe: wireframe-SCR-005-booking-confirmation.html](./Hi-Fi/wireframe-SCR-005-booking-confirmation.html)
  - Description: Appointment summary with confirm, preferred slot swap, calendar sync
  - Parent Screen: Provider Search (SCR-004)
  - Key Components: Card (1), Button (3), Checkbox (1), Badge (1), Modal (1)

- **SCR-010 AI Intake** (P0) - [Wireframe: wireframe-SCR-010-ai-intake.html](./Hi-Fi/wireframe-SCR-010-ai-intake.html)
  - Description: AI conversational intake with chat interface
  - Parent Screen: Patient Dashboard (SCR-006)
  - Key Components: ChatBubble (N), TextField (1), Button (2), Toggle (1), Alert (1)

- **SCR-011 Manual Intake** (P0) - [Wireframe: wireframe-SCR-011-manual-intake.html](./Hi-Fi/wireframe-SCR-011-manual-intake.html)
  - Description: Traditional structured intake form
  - Parent Screen: Patient Dashboard (SCR-006)
  - Key Components: TextField (8), Select (3), TextArea (2), Checkbox (N), Button (2)

- **SCR-012 Intake Summary** (P0) - [Wireframe: wireframe-SCR-012-intake-summary.html](./Hi-Fi/wireframe-SCR-012-intake-summary.html)
  - Description: Review and confirm collected intake data with field-level edit
  - Parent Screen: AI/Manual Intake (SCR-010/011)
  - Key Components: Card (6), TextField (N inline-edit), Button (2), Badge (N)

- **SCR-014 Document Upload** (P0) - [Wireframe: wireframe-SCR-014-document-upload.html](./Hi-Fi/wireframe-SCR-014-document-upload.html)
  - Description: Drag-and-drop PDF upload with per-file progress
  - Parent Screen: Patient Dashboard (SCR-006)
  - Key Components: FileDropzone (1), ProgressBar (N), Card (N), Button (2)

- **SCR-016 360-Degree View** (P0) - [Wireframe: wireframe-SCR-016-360-degree-view.html](./Hi-Fi/wireframe-SCR-016-360-degree-view.html)
  - Description: Consolidated clinical profile with tabbed sections and confidence scores
  - Parent Screen: Patient Dashboard (SCR-006)
  - Key Components: Tabs (6), Card (N), Table (N), Badge (N), Tag (N), Alert (N)

- **SCR-008 Reschedule/Cancel** (P0) - [Wireframe: wireframe-SCR-008-reschedule-cancel.html](./Hi-Fi/wireframe-SCR-008-reschedule-cancel.html)
  - Description: Appointment modification with date picker and cancel confirmation
  - Parent Screen: Patient Dashboard (SCR-006)
  - Key Components: Card (1), Select (1), DatePicker (1), Button (2), Dialog (1)

- **SCR-007 Waitlist Status** (P1) - [Wireframe: wireframe-SCR-007-waitlist-status.html](./Hi-Fi/wireframe-SCR-007-waitlist-status.html)
  - Description: Waitlist entries with notification preferences
  - Parent Screen: Patient Dashboard (SCR-006)

- **SCR-009 Calendar Sync** (P1) - [Wireframe: wireframe-SCR-009-calendar-sync.html](./Hi-Fi/wireframe-SCR-009-calendar-sync.html)
  - Description: Google/Outlook calendar provider selection with OAuth
  - Parent Screen: Booking Confirmation (SCR-005)

- **SCR-013 Insurance Pre-Check** (P1) - [Wireframe: wireframe-SCR-013-insurance-precheck.html](./Hi-Fi/wireframe-SCR-013-insurance-precheck.html)
  - Description: Insurance name and member ID validation
  - Parent Screen: Booking/Intake (SCR-005/012)

- **SCR-015 Processing Status** (P1) - [Wireframe: wireframe-SCR-015-processing-status.html](./Hi-Fi/wireframe-SCR-015-processing-status.html)
  - Description: Document extraction pipeline progress per file
  - Parent Screen: Document Upload (SCR-014)

### Level 3: Staff Portal

- **SCR-021 Staff Dashboard** (P0) - [Wireframe: wireframe-SCR-021-staff-dashboard.html](./Hi-Fi/wireframe-SCR-021-staff-dashboard.html)
  - Description: Staff hub with queue summary, pending tasks, today's schedule
  - Parent Screen: Login (SCR-002)
  - Key Components: Card (4), Table (1), Badge (N)

- **SCR-019 Walk-In Booking** (P0) - [Wireframe: wireframe-SCR-019-walkin-booking.html](./Hi-Fi/wireframe-SCR-019-walkin-booking.html)
  - Description: Walk-in patient registration and slot assignment
  - Parent Screen: Staff Dashboard (SCR-021)
  - Key Components: TextField (4), Select (1), Button (2), Modal (1)

- **SCR-020 Queue Management** (P0) - [Wireframe: wireframe-SCR-020-queue-management.html](./Hi-Fi/wireframe-SCR-020-queue-management.html)
  - Description: Real-time same-day queue with status management
  - Parent Screen: Staff Dashboard (SCR-021)
  - Key Components: Table (1), Badge (N), Button (N), Chip (N)

- **SCR-017 Conflict Resolution** (P0) - [Wireframe: wireframe-SCR-017-conflict-resolution.html](./Hi-Fi/wireframe-SCR-017-conflict-resolution.html)
  - Description: Side-by-side data conflict comparison and resolution
  - Parent Screen: 360-Degree View (SCR-016) / Staff Dashboard (SCR-021)
  - Key Components: Card (2), RadioGroup (1), TextArea (1), Button (2), Badge (1)

- **SCR-018 Code Verification** (P0) - [Wireframe: wireframe-SCR-018-code-verification.html](./Hi-Fi/wireframe-SCR-018-code-verification.html)
  - Description: ICD-10 and CPT code review, accept, reject workflow
  - Parent Screen: Staff Dashboard (SCR-021)
  - Key Components: Table (2), Badge (N), Button (3), Select (1), Dialog (1)

- **SCR-022 No-Show Risk** (P1) - [Wireframe: wireframe-SCR-022-noshow-risk.html](./Hi-Fi/wireframe-SCR-022-noshow-risk.html)
  - Description: Appointment risk scores with color-coded indicators
  - Parent Screen: Staff Dashboard (SCR-021)

### Level 4: Admin Portal

- **SCR-024 Admin Dashboard** (P0) - [Wireframe: wireframe-SCR-024-admin-dashboard.html](./Hi-Fi/wireframe-SCR-024-admin-dashboard.html)
  - Description: System stats, recent activity, user summary
  - Parent Screen: Login (SCR-002)
  - Key Components: Card (4), Table (1)

- **SCR-023 User Management** (P0) - [Wireframe: wireframe-SCR-023-user-management.html](./Hi-Fi/wireframe-SCR-023-user-management.html)
  - Description: User create, edit, deactivate with role assignment
  - Parent Screen: Admin Dashboard (SCR-024)
  - Key Components: Table (1), TextField (3), Select (1), Button (3), Modal (1)

- **SCR-025 Audit Log Viewer** (P1) - [Wireframe: wireframe-SCR-025-audit-log.html](./Hi-Fi/wireframe-SCR-025-audit-log.html)
  - Description: Searchable, filterable audit event log
  - Parent Screen: Admin Dashboard (SCR-024)
  - Key Components: Table (1), TextField (1), Select (2), DatePicker (2), Pagination (1)

### Modal/Dialog/Overlay Inventory

| Modal/Dialog Name                 | Type   | Trigger Context                | Parent Screen     | Wireframe Reference           | Priority |
| --------------------------------- | ------ | ------------------------------ | ----------------- | ----------------------------- | -------- |
| OVL-001 Booking Confirmation      | Modal  | Click "Confirm Booking"        | SCR-005           | Embedded in SCR-005 wireframe | P0       |
| OVL-002 Preferred Slot Swap       | Modal  | Click "Request Preferred Slot" | SCR-005           | Embedded in SCR-005 wireframe | P1       |
| OVL-003 Swap Notification         | Toast  | System event                   | SCR-006           | Embedded in SCR-006 wireframe | P1       |
| OVL-004 Appointment PDF Preview   | Drawer | Click "View PDF"               | SCR-006           | Embedded in SCR-006 wireframe | P1       |
| OVL-005 Deactivate/Cancel Confirm | Dialog | Destructive actions            | SCR-008, SCR-023  | Embedded in parent wireframes | P0       |
| OVL-006 Conflict Detail           | Modal  | Click conflict row             | SCR-017           | Embedded in SCR-017 wireframe | P0       |
| OVL-007 Code Rejection Reason     | Dialog | Click "Reject Code"            | SCR-018           | Embedded in SCR-018 wireframe | P0       |
| OVL-008 Session Timeout           | Modal  | 14-min inactivity              | All authenticated | Shared overlay component      | P0       |
| OVL-009 Create Patient Account    | Modal  | Click "New Patient"            | SCR-019           | Embedded in SCR-019 wireframe | P0       |
| OVL-010 Calendar OAuth            | Modal  | Calendar sync init             | SCR-009           | Embedded in SCR-009 wireframe | P1       |

**Modal Behavior Notes:**

- **Responsive Behavior**: Desktop centered modal; mobile full-screen transformation
- **Dismissal Actions**: Close button (X), overlay click, ESC key
- **Focus Management**: Focus trap within modal; return focus to trigger on close
- **Accessibility**: role="dialog", aria-labelledby, aria-modal="true"

## 6. Navigation Architecture

```text
Unified Patient Access Platform
+-- Public
|   +-- SCR-002 Login (wireframe-SCR-002-login.html)
|   +-- SCR-001 Registration (wireframe-SCR-001-registration.html)
|
+-- Patient Portal (Sidebar: Dashboard, Book, Intake, Documents, Health Profile, Waitlist)
|   +-- SCR-006 Patient Dashboard (wireframe-SCR-006-patient-dashboard.html)
|   |   +-- SCR-004 Provider Search (wireframe-SCR-004-provider-search.html)
|   |   |   +-- SCR-005 Booking Confirmation (wireframe-SCR-005-booking-confirmation.html)
|   |   |       +-- SCR-009 Calendar Sync (wireframe-SCR-009-calendar-sync.html)
|   |   |       +-- SCR-013 Insurance Pre-Check (wireframe-SCR-013-insurance-precheck.html)
|   |   +-- SCR-008 Reschedule/Cancel (wireframe-SCR-008-reschedule-cancel.html)
|   |   +-- SCR-010 AI Intake (wireframe-SCR-010-ai-intake.html)
|   |   |   +-- SCR-012 Intake Summary (wireframe-SCR-012-intake-summary.html)
|   |   +-- SCR-011 Manual Intake (wireframe-SCR-011-manual-intake.html)
|   |   |   +-- SCR-012 Intake Summary (wireframe-SCR-012-intake-summary.html)
|   |   +-- SCR-014 Document Upload (wireframe-SCR-014-document-upload.html)
|   |   |   +-- SCR-015 Processing Status (wireframe-SCR-015-processing-status.html)
|   |   +-- SCR-016 360-Degree View (wireframe-SCR-016-360-degree-view.html)
|   |   +-- SCR-007 Waitlist Status (wireframe-SCR-007-waitlist-status.html)
|
+-- Staff Portal (Sidebar: Dashboard, Walk-In, Queue, Conflicts, Codes, Risk, Patient View)
|   +-- SCR-021 Staff Dashboard (wireframe-SCR-021-staff-dashboard.html)
|   |   +-- SCR-019 Walk-In Booking (wireframe-SCR-019-walkin-booking.html)
|   |   +-- SCR-020 Queue Management (wireframe-SCR-020-queue-management.html)
|   |   +-- SCR-017 Conflict Resolution (wireframe-SCR-017-conflict-resolution.html)
|   |   +-- SCR-018 Code Verification (wireframe-SCR-018-code-verification.html)
|   |   +-- SCR-022 No-Show Risk (wireframe-SCR-022-noshow-risk.html)
|   |   +-- SCR-016 360-Degree View (wireframe-SCR-016-360-degree-view.html) [shared]
|
+-- Admin Portal (Sidebar: Dashboard, Users, Audit Logs)
    +-- SCR-024 Admin Dashboard (wireframe-SCR-024-admin-dashboard.html)
    |   +-- SCR-023 User Management (wireframe-SCR-023-user-management.html)
    |   +-- SCR-025 Audit Log Viewer (wireframe-SCR-025-audit-log.html)
```

### Navigation Patterns

- **Primary Navigation**: Fixed left sidebar (Desktop 256px, Tablet 64px icon-only, Mobile hidden)
- **Secondary Navigation**: Tabs within detail screens (360-View sections, Queue views)
- **Mobile Navigation**: Bottom nav bar (56px) replacing sidebar
- **Utility Navigation**: Header avatar dropdown (Profile, Settings, Logout)
- **Breadcrumb**: Contextual trail on detail pages (Appointment Detail, Document Detail)

## 7. Interaction Patterns

### Pattern 1: Appointment Booking

- **Trigger**: Patient clicks "Book Appointment" on dashboard
- **Flow**: SCR-006 -> SCR-004 -> SCR-005 -> OVL-001 -> SCR-006
- **Screens Involved**: SCR-004, SCR-005, SCR-006, SCR-009
- **Feedback**: Optimistic slot reservation, confirmation toast, PDF email notification
- **Components Used**: TextField, Select, DatePicker, Card, Button, Modal, Toast

### Pattern 2: Clinical Data Flow

- **Trigger**: Patient clicks "Upload Documents" on dashboard
- **Flow**: SCR-006 -> SCR-014 -> SCR-015 -> SCR-016
- **Screens Involved**: SCR-014, SCR-015, SCR-016, SCR-017
- **Feedback**: Per-file upload progress, processing status badges, confidence score indicators
- **Components Used**: FileDropzone, ProgressBar, Tabs, Badge, Card, Alert

### Pattern 3: Intake Mode Switch

- **Trigger**: Patient toggles between AI and Manual intake
- **Flow**: SCR-010 <-> SCR-011 -> SCR-012
- **Screens Involved**: SCR-010, SCR-011, SCR-012
- **Feedback**: Data preserved on toggle, autosave indicator, inline field edit
- **Components Used**: Toggle, ChatBubble, TextField, TextArea, Button

### Pattern 4: Medical Code Verification

- **Trigger**: Staff opens code verification from dashboard
- **Flow**: SCR-021 -> SCR-018 -> OVL-007 (reject) -> SCR-018
- **Screens Involved**: SCR-018
- **Feedback**: Inline status update on accept, dialog for rejection reason
- **Components Used**: Table, Badge, Button, Dialog, Select

## 8. Error Handling

### Error Scenario 1: Network/API Error

- **Trigger**: API request fails (timeout, 500, connectivity)
- **Error Screen/State**: Global error banner (Alert component, role="alert")
- **User Action**: Click "Retry" button in banner
- **Recovery Flow**: Re-attempt failed API call; on success, remove banner

### Error Scenario 2: Form Validation Error

- **Trigger**: Required fields missing or invalid format on submit
- **Error Screen/State**: Inline field errors (red border, error icon + message below field)
- **User Action**: Correct highlighted fields and resubmit
- **Recovery Flow**: Errors clear per-field on correction; submit re-validates

### Error Scenario 3: Session Timeout

- **Trigger**: 14 minutes of inactivity
- **Error Screen/State**: OVL-008 Session Timeout Modal with countdown
- **User Action**: Click "Extend Session" or re-authenticate
- **Recovery Flow**: Token refresh preserves page state; forced logout at 15 min

### Error Scenario 4: AI Service Unavailable

- **Trigger**: Ollama service down or circuit breaker open
- **Error Screen/State**: Alert banner on SCR-010; AI toggle disabled
- **User Action**: Auto-redirect to SCR-011 Manual Intake
- **Recovery Flow**: Manual form pre-filled with any collected data; AI option re-enabled on recovery

### Error Scenario 5: Document Upload Failure

- **Trigger**: File validation fails or upload interrupted
- **Error Screen/State**: Per-file error indicator with retry button
- **User Action**: Click retry per failed file; successful uploads preserved
- **Recovery Flow**: Individual file retry without re-uploading batch

## 9. Responsive Strategy

| Breakpoint | Width  | Layout Changes               | Navigation Changes                 | Component Adaptations                                  |
| ---------- | ------ | ---------------------------- | ---------------------------------- | ------------------------------------------------------ |
| Mobile     | 390px  | Single column, stacked cards | Bottom nav (56px), no sidebar      | Tables become stacked cards, modals become full-screen |
| Tablet     | 768px  | 2-column grid, 8-col system  | Collapsed icon-only sidebar (64px) | Cards 2-up, tables scrollable, modals centered         |
| Desktop    | 1440px | Multi-column, 12-col system  | Expanded sidebar (256px)           | Full table views, side-by-side comparisons             |

## 10. Accessibility

### WCAG Compliance

- **Target Level**: AA (WCAG 2.2)
- **Color Contrast**: All text >=4.5:1, UI elements >=3:1 per designsystem.md tokens
- **Keyboard Navigation**: Full tab-through with visible 2px focus rings (>=3:1 contrast)
- **Screen Reader Support**: ARIA labels on all form controls, role="dialog" on modals, role="alert" on errors

### Focus Order

- Tab order follows visual reading order (left-to-right, top-to-bottom)
- Modals trap focus; return to trigger element on close
- Form errors move focus to first invalid field on submit
- Page navigation moves focus to main content heading

## 11. Content Strategy

### Content Hierarchy

- **H1**: Page title (one per screen, 32px Inter Bold)
- **H2**: Section headings (24px Inter Semibold)
- **H3**: Card/panel titles (20px Inter Semibold)
- **Body**: Default content text (16px Inter Regular)
- **Caption**: Metadata, timestamps, confidence scores (12px Inter Regular)

### Content Types by Screen

| Screen                    | Content Types                                        | Wireframe Reference |
| ------------------------- | ---------------------------------------------------- | ------------------- |
| SCR-004 Provider Search   | Search inputs, provider cards with availability      | wireframe-SCR-004   |
| SCR-010 AI Intake         | Chat bubbles, structured data summary                | wireframe-SCR-010   |
| SCR-016 360-Degree View   | Tabbed data tables, confidence badges, alert banners | wireframe-SCR-016   |
| SCR-018 Code Verification | Data tables with inline actions, code descriptions   | wireframe-SCR-018   |
| SCR-023 User Management   | Data table with CRUD modals                          | wireframe-SCR-023   |
