---
post_title: "Unified Patient Access - Navigation Map"
author1: "AI UX Designer"
post_slug: "unified-patient-access-navmap"
categories: "Healthcare, UX, Navigation"
tags: "navigation-map, user-flows, screen-links, wireframes"
ai_note: "Generated with AI assistance from figma_spec.md flows FL-001 through FL-009"
summary: "Cross-screen navigation index mapping all prototype flows to screen-to-screen links with trigger actions and conditions."
post_date: "2026-04-15"
---

# Navigation Map - Unified Patient Access

## 1. Flow Index

| Flow ID | Flow Name                      | Entry Screen | Exit Screen     | Screens Involved                            | Priority |
| ------- | ------------------------------ | ------------ | --------------- | ------------------------------------------- | -------- |
| FL-001  | Patient Registration & Login   | SCR-002      | SCR-006/021/024 | SCR-001, SCR-002, SCR-006, SCR-021, SCR-024 | P0       |
| FL-002  | Provider Search & Booking      | SCR-006      | SCR-006         | SCR-004, SCR-005, SCR-006                   | P0       |
| FL-003  | Patient Intake                 | SCR-006      | SCR-006         | SCR-006, SCR-010, SCR-011, SCR-012          | P0       |
| FL-004  | Document Upload & Processing   | SCR-006      | SCR-016         | SCR-006, SCR-014, SCR-015, SCR-016          | P0       |
| FL-005  | 360-View & Conflict Resolution | SCR-006/021  | SCR-016         | SCR-016, SCR-017                            | P0       |
| FL-006  | Code Verification              | SCR-021      | SCR-021         | SCR-018, SCR-021                            | P0       |
| FL-007  | Walk-In & Queue                | SCR-021      | SCR-021         | SCR-019, SCR-020, SCR-021                   | P0       |
| FL-008  | Admin User Management          | SCR-024      | SCR-024         | SCR-023, SCR-024                            | P0       |
| FL-009  | Insurance Pre-Check            | SCR-005/012  | SCR-005/012     | SCR-005, SCR-012, SCR-013                   | P1       |

## 2. Screen-to-Screen Links

### FL-001: Patient Registration & Login

| From    | To      | Trigger                      | Condition           | Link Type  |
| ------- | ------- | ---------------------------- | ------------------- | ---------- |
| SCR-002 | SCR-001 | Click "Create Account" link  | User not registered | Navigation |
| SCR-001 | SCR-002 | Submit registration form     | Validation passes   | Redirect   |
| SCR-001 | SCR-002 | Click "Already have account" | Any                 | Navigation |
| SCR-002 | SCR-006 | Login success                | role=Patient        | Redirect   |
| SCR-002 | SCR-021 | Login success                | role=Staff          | Redirect   |
| SCR-002 | SCR-024 | Login success                | role=Admin          | Redirect   |

### FL-002: Provider Search & Booking

| From    | To      | Trigger                            | Condition               | Link Type        |
| ------- | ------- | ---------------------------------- | ----------------------- | ---------------- |
| SCR-006 | SCR-004 | Click "Book Appointment"           | Authenticated           | Navigation       |
| SCR-004 | SCR-005 | Click "Book" on provider slot card | Slot available          | Navigation       |
| SCR-005 | SCR-006 | Click "Confirm Booking" in OVL-001 | Booking created         | Redirect         |
| SCR-005 | SCR-009 | Click "Sync to Calendar"           | After booking confirmed | Modal/Navigation |
| SCR-005 | SCR-013 | Click "Pre-Check Insurance"        | Optional step           | Navigation       |
| SCR-004 | SCR-006 | Click back/breadcrumb              | Any                     | Navigation       |

### FL-003: Patient Intake

| From    | To      | Trigger                              | Condition             | Link Type                   |
| ------- | ------- | ------------------------------------ | --------------------- | --------------------------- |
| SCR-006 | SCR-010 | Click "Start Intake" (AI toggle on)  | AI available          | Navigation                  |
| SCR-006 | SCR-011 | Click "Start Intake" (AI toggle off) | Any                   | Navigation                  |
| SCR-010 | SCR-011 | Toggle AI off during intake          | Mid-conversation      | Navigation (data preserved) |
| SCR-011 | SCR-010 | Toggle AI on during intake           | AI available          | Navigation (data preserved) |
| SCR-010 | SCR-012 | AI marks intake complete             | All required answered | Navigation                  |
| SCR-011 | SCR-012 | Click "Review & Submit"              | Validation passes     | Navigation                  |
| SCR-012 | SCR-006 | Click "Confirm" on summary           | Submission success    | Redirect                    |
| SCR-012 | SCR-013 | Click "Pre-Check Insurance"          | Optional step         | Navigation                  |

### FL-004: Document Upload & Processing

| From    | To      | Trigger                                   | Condition           | Link Type       |
| ------- | ------- | ----------------------------------------- | ------------------- | --------------- |
| SCR-006 | SCR-014 | Click "Upload Documents"                  | Authenticated       | Navigation      |
| SCR-014 | SCR-015 | Files uploaded, processing starts         | Upload complete     | Auto-transition |
| SCR-015 | SCR-016 | All files processed, click "View Profile" | Processing complete | Navigation      |
| SCR-015 | SCR-014 | Click "Upload More"                       | Any                 | Navigation      |
| SCR-014 | SCR-006 | Click back/breadcrumb                     | Any                 | Navigation      |

### FL-005: 360-View & Conflict Resolution

| From    | To      | Trigger                                   | Condition           | Link Type  |
| ------- | ------- | ----------------------------------------- | ------------------- | ---------- |
| SCR-006 | SCR-016 | Click "View Health Profile"               | Data available      | Navigation |
| SCR-021 | SCR-016 | Click patient name in table               | Staff role          | Navigation |
| SCR-016 | SCR-017 | Click conflict badge/row in Conflicts tab | Conflicts exist     | Navigation |
| SCR-017 | SCR-016 | Click "Save Resolution"                   | Resolution selected | Redirect   |
| SCR-017 | SCR-016 | Click "Cancel"                            | Any                 | Navigation |

### FL-006: Code Verification

| From    | To      | Trigger                                                 | Condition       | Link Type                 |
| ------- | ------- | ------------------------------------------------------- | --------------- | ------------------------- |
| SCR-021 | SCR-018 | Click "Code Verification" sidebar or pending count card | Staff role      | Navigation                |
| SCR-018 | SCR-018 | Click "Accept" on code row                              | Any             | In-page update            |
| SCR-018 | OVL-007 | Click "Reject" on code row                              | Any             | Dialog                    |
| OVL-007 | SCR-018 | Submit rejection reason                                 | Reason provided | Dialog close + row update |
| SCR-018 | SCR-021 | Click back/sidebar Dashboard                            | Any             | Navigation                |

### FL-007: Walk-In & Queue

| From    | To      | Trigger                                         | Condition         | Link Type               |
| ------- | ------- | ----------------------------------------------- | ----------------- | ----------------------- |
| SCR-021 | SCR-019 | Click "Walk-In Booking"                         | Staff role        | Navigation              |
| SCR-019 | OVL-009 | Click "New Patient"                             | Patient not found | Modal                   |
| OVL-009 | SCR-019 | Submit new patient form                         | Created           | Modal close + auto-fill |
| SCR-019 | SCR-020 | Click "Add to Queue"                            | Booking created   | Redirect                |
| SCR-021 | SCR-020 | Click "Queue" sidebar                           | Staff role        | Navigation              |
| SCR-020 | SCR-020 | Status change (Check-In -> In Progress -> Done) | Any               | In-page update          |
| SCR-020 | SCR-021 | Click back/sidebar Dashboard                    | Any               | Navigation              |

### FL-008: Admin User Management

| From    | To      | Trigger                             | Condition  | Link Type                   |
| ------- | ------- | ----------------------------------- | ---------- | --------------------------- |
| SCR-024 | SCR-023 | Click "User Management" sidebar     | Admin role | Navigation                  |
| SCR-023 | SCR-023 | Click "Create User" -> Modal submit | Form valid | Modal close + table refresh |
| SCR-023 | SCR-023 | Click "Edit" on row -> Modal submit | Form valid | Modal close + row update    |
| SCR-023 | OVL-005 | Click "Deactivate" on row           | Any        | Dialog                      |
| OVL-005 | SCR-023 | Confirm deactivation                | Confirmed  | Dialog close + row update   |
| SCR-023 | SCR-024 | Click back/sidebar Dashboard        | Any        | Navigation                  |

### FL-009: Insurance Pre-Check

| From    | To      | Trigger                     | Condition | Link Type  |
| ------- | ------- | --------------------------- | --------- | ---------- |
| SCR-005 | SCR-013 | Click "Pre-Check Insurance" | Optional  | Navigation |
| SCR-012 | SCR-013 | Click "Pre-Check Insurance" | Optional  | Navigation |
| SCR-013 | SCR-005 | Click "Back" (from booking) | Any       | Navigation |
| SCR-013 | SCR-012 | Click "Back" (from intake)  | Any       | Navigation |

## 3. Global Navigation Links

These links are available from every authenticated screen via Header or Sidebar:

| From          | To      | Trigger                        | Condition    |
| ------------- | ------- | ------------------------------ | ------------ |
| Any (Patient) | SCR-006 | Click "Dashboard" sidebar      | role=Patient |
| Any (Patient) | SCR-004 | Click "Book" sidebar           | role=Patient |
| Any (Patient) | SCR-014 | Click "Documents" sidebar      | role=Patient |
| Any (Patient) | SCR-016 | Click "Health Profile" sidebar | role=Patient |
| Any (Staff)   | SCR-021 | Click "Dashboard" sidebar      | role=Staff   |
| Any (Staff)   | SCR-019 | Click "Walk-In" sidebar        | role=Staff   |
| Any (Staff)   | SCR-020 | Click "Queue" sidebar          | role=Staff   |
| Any (Staff)   | SCR-018 | Click "Codes" sidebar          | role=Staff   |
| Any (Admin)   | SCR-024 | Click "Dashboard" sidebar      | role=Admin   |
| Any (Admin)   | SCR-023 | Click "Users" sidebar          | role=Admin   |
| Any (Admin)   | SCR-025 | Click "Audit Logs" sidebar     | role=Admin   |
| Any (Auth)    | SCR-002 | Click "Logout" header dropdown | Any          |

## 4. Overlay Trigger Map

| Overlay                      | Trigger Screen   | Trigger Action                 | Dismiss Actions                   |
| ---------------------------- | ---------------- | ------------------------------ | --------------------------------- |
| OVL-001 Booking Confirmation | SCR-005          | Click "Confirm Booking"        | Confirm, Cancel, X, Backdrop, ESC |
| OVL-002 Preferred Slot Swap  | SCR-005          | Click "Request Preferred Slot" | Submit, Cancel, X, Backdrop, ESC  |
| OVL-003 Swap Notification    | SCR-006          | System event (slot available)  | Dismiss, Accept, Auto-dismiss 5s  |
| OVL-004 PDF Preview          | SCR-006          | Click "View PDF" on document   | X, Backdrop, ESC                  |
| OVL-005 Deactivate Confirm   | SCR-008, SCR-023 | Click destructive action       | Confirm, Cancel, X, ESC           |
| OVL-006 Conflict Detail      | SCR-017          | Click conflict row             | X, Backdrop, ESC                  |
| OVL-007 Code Rejection       | SCR-018          | Click "Reject" on code         | Submit, Cancel, X, ESC            |
| OVL-008 Session Timeout      | Any (Auth)       | 14-min inactivity              | Extend, Logout                    |
| OVL-009 Create Patient       | SCR-019          | Click "New Patient"            | Submit, Cancel, X, Backdrop, ESC  |
| OVL-010 Calendar OAuth       | SCR-009          | Calendar sync initiation       | Complete, Cancel, X               |
