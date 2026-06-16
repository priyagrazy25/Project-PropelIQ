---
post_title: "TASK_001 - Patient Dashboard UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-patient-dashboard"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_050, frontend, React, dashboard, SCR-006"
ai_note: "Generated with AI assistance from user story US_050"
summary: "Implement patient dashboard with persistent AppShell (header + sidebar), summary cards, tabbed appointment table, quick actions, and responsive design per SCR-006 wireframe."
post_date: "2026-04-20"
---

# Task - TASK_001_FE_PATIENT_DASHBOARD

## Requirement Reference
- User Story: us_050
- Story Location: .propel/context/tasks/EP-002/us_050/us_050.md
- Acceptance Criteria:
    - AC-1: App header with logo + notifications, sidebar navigation, personalized welcome, summary cards, "Book Appointment" CTA
    - AC-2: Tabbed Upcoming/Past appointment table
    - AC-3: Provider name, specialty, date/time, color-coded status badges
    - AC-4: "Reschedule" action navigates to reschedule page
    - AC-5: Empty state with "Book Your First Appointment" link
    - AC-6: Quick Actions cards for Complete Intake, Upload Documents, Health Profile
    - AC-7: Max 3-click navigation via sidebar per UXR-001
    - AC-8: Active sidebar item highlighted per current route
- Edge Cases:
    - API failure → error state with retry; waitlist loads independently
    - No data → zero counts, empty state message

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-006-patient-dashboard.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-006 |
| **UXR Requirements** | UXR-001, UXR-002, UXR-201, UXR-301, UXR-302, UXR-501 |
| **Design Tokens** | .propel/context/docs/designsystem.md#cards, #badges, #tables, #tabs |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | React with TypeScript | 18.x |
| State Management | Redux Toolkit | 2.x |
| Bundler | Vite | 8.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the AppShell layout component (persistent header + sidebar navigation) and PatientDashboardPage component per SCR-006 wireframe. The AppShell provides a fixed header with logo, notification bell, and user avatar, plus a left sidebar with NavLink items (Dashboard, Book Appointment, Intake, Documents, Health Profile, Waitlist, Sign Out) that persist across all patient routes via React Router layout route with Outlet. The dashboard page displays a personalized header, 4 summary metric cards (Upcoming Appointments, Intake Status, Documents Uploaded, Waitlist Entries), a tabbed appointment table (Upcoming/Past) with Reschedule action, and a Quick Actions grid (Complete Intake, Upload Documents, Health Profile). Data is fetched from the GET /api/scheduling/appointments/my and GET /api/scheduling/waitlist/my endpoints.

## Dependent Tasks
- task_002_be_patient_dashboard_api (US_050) — Requires GET /api/scheduling/appointments/my endpoint
- task_001_fe_provider_search (US_016) — Requires /search route for navigation
- task_001_fe_waitlist (US_019) — Requires /waitlist route and fetchWaitlistEntries API

## Impacted Components
- NEW: AppShell.tsx — Persistent layout with fixed header, sidebar navigation, and Outlet for child routes
- NEW: AppShell.css — Header, sidebar, main content area styles matching SCR-006 wireframe
- NEW: PatientDashboardPage.tsx — Main dashboard page component
- NEW: PatientDashboardPage.css — Styles matching SCR-006 wireframe
- MODIFY: schedulingApi.ts — Add fetchMyAppointments() API function and MyAppointment type
- MODIFY: App.tsx — Use layout route with AppShell wrapping all patient routes, add placeholder routes for /intake, /documents, /health-profile

## Implementation Plan
1. Add MyAppointment interface and fetchMyAppointments() to schedulingApi.ts
2. Create AppShell.tsx with fixed header (logo, notification bell, avatar), left sidebar (6 NavLink items + Sign Out button), and Outlet for child routes
3. Create AppShell.css matching SCR-006 wireframe design tokens (header 64px, sidebar 256px, responsive hide at 768px)
4. Create PatientDashboardPage.css with SCR-006 design tokens (summary cards, tabs, table, badges, quick actions, responsive breakpoints)
5. Create PatientDashboardPage.tsx:
   a. Read fullName from identity Redux state for personalized greeting
   b. Fetch appointments and waitlist entries on mount via Promise.all
   c. Partition appointments into upcoming (active statuses) and past
   d. Render 4 summary cards: Upcoming Appointments, Intake Status (Pending badge), Documents Uploaded, Waitlist Entries
   e. Render tabbed appointment table with Upcoming/Past toggle and Reschedule action
   f. Render Quick Actions grid: Complete Intake, Upload Documents, Health Profile
   g. Handle loading, error, and empty states
6. Update App.tsx to use layout route pattern: pathless Route with ProtectedRoute + AppShell wrapping all patient routes
7. Add placeholder routes for /intake, /documents, /health-profile inside the layout route

## Current Project State
```
frontend/
  src/
    features/
      scheduling/
        api/schedulingApi.ts          — existing: bookAppointment, searchProviders, fetchWaitlistEntries
        pages/ProviderSearchPage.tsx   — existing: /search route
        pages/BookingConfirmationPage.tsx — existing: /booking/confirmation route
        pages/WaitlistPage.tsx         — existing: /waitlist route
    shared/
      components/ProtectedRoute.tsx    — existing: role-based route guard
      api/authInterceptor.ts           — existing: authenticatedFetch
    App.tsx                            — existing: /dashboard route with placeholder
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/shared/components/AppShell.tsx | Persistent layout: header + sidebar + Outlet |
| CREATE | frontend/src/shared/components/AppShell.css | Header, sidebar, main content styles |
| CREATE | frontend/src/features/scheduling/pages/PatientDashboardPage.tsx | Dashboard page component |
| CREATE | frontend/src/features/scheduling/pages/PatientDashboardPage.css | Dashboard styles per SCR-006 |
| MODIFY | frontend/src/features/scheduling/api/schedulingApi.ts | Add MyAppointment type and fetchMyAppointments() |
| MODIFY | frontend/src/App.tsx | Layout route with AppShell, placeholder routes |

## Build Commands
- `npm run dev` — Start dev server
- `npx tsc --noEmit` — Type check

## Implementation Validation Strategy
- [x] Dashboard loads within 2s (UXR-201)
- [x] Summary cards show correct counts (Upcoming, Intake Status, Documents, Waitlist)
- [x] Upcoming tab filters active statuses
- [x] Past tab filters completed/cancelled/rescheduled
- [x] Empty state shown when no appointments
- [x] Quick Actions navigate to correct routes (/intake, /documents, /health-profile)
- [x] Sidebar navigation persists across patient pages with active item highlighted
- [x] App header shows logo, notification bell, and user avatar initials
- [x] Sign Out in sidebar clears session and redirects to login
- [x] Responsive layout at 1024px and 768px breakpoints (sidebar hidden at 768px)
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-006

## Implementation Checklist
- [x] Add MyAppointment interface to schedulingApi.ts
- [x] Add fetchMyAppointments() calling GET /api/scheduling/appointments/my
- [x] Create AppShell.tsx with header, sidebar NavLinks, and Outlet
- [x] Create AppShell.css with SCR-006 wireframe design tokens
- [x] Create PatientDashboardPage.css with SCR-006 tokens
- [x] Create PatientDashboardPage.tsx with header, summary, tabs, table, quick actions
- [x] Summary cards: Upcoming Appointments, Intake Status (Pending badge), Documents Uploaded, Waitlist Entries
- [x] Quick Actions: Complete Intake, Upload Documents, Health Profile
- [x] Implement Upcoming/Past tab filtering logic
- [x] Add color-coded status badges (confirmed=green, cancelled=red, rescheduled=orange)
- [x] Add Reschedule action on upcoming appointment rows
- [x] Add empty state with "Book Your First Appointment" CTA
- [x] Add error state with retry button
- [x] Update App.tsx to use layout route with AppShell wrapping patient routes
- [x] Add placeholder routes for /intake, /documents, /health-profile
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-006 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
