---
post_title: "TASK_001 - Waitlist Management UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-waitlist-management"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_019, frontend, React, waitlist, notification"
ai_note: "Generated with AI assistance from user story US_019"
summary: "Implement waitlist enrollment UI, dashboard status display, quick-book action on availability notification, and self-removal."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_WAITLIST_MANAGEMENT

## Requirement Reference
- User Story: us_019
- Story Location: .propel/context/tasks/EP-002/us_019/us_019.md
- Acceptance Criteria:
    - AC-1: "Join Waitlist" button on fully booked providers with date range and preference
    - AC-3: Available slot displayed prominently with quick-book action
    - AC-5: Dashboard shows active waitlist entries with status and remove option
- Edge Cases:
    - Multiple waitlists → each entry independent
    - Expired date range → auto-removed from active waitlists display

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-007-waitlist-status.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-007 |
| **UXR Requirements** | UXR-501 |
| **Design Tokens** | .propel/context/docs/designsystem.md#badges, #status-indicators |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | React with TypeScript | 18.x |
| State Management | Redux Toolkit | 2.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build waitlist enrollment from provider search (join when fully booked), patient dashboard waitlist section showing active entries with status badges, quick-book banner when slot becomes available, and self-removal capability.

## Dependent Tasks
- task_001_fe_provider_search (US_016) — Requires provider search results for join action
- task_001_fe_react_scaffolding (US_001) — Requires React project structure

## Impacted Components
- NEW: WaitlistJoinDialog — Enrollment with date range preference
- NEW: WaitlistDashboardSection — Active entries on patient dashboard
- NEW: WaitlistAvailableBanner — Quick-book notification banner
- MODIFY: ProviderCard — Add "Join Waitlist" button when fully booked

## Implementation Plan
1. Add "Join Waitlist" button to ProviderCard when all slots are booked
2. Create WaitlistJoinDialog with date range picker and provider preference
3. Implement waitlist enrollment mutation via RTK Query
4. Build WaitlistDashboardSection with active entries, status badges, and remove button
5. Create WaitlistAvailableBanner with quick-book action on slot availability
6. Listen to SignalR waitlist-notification events for real-time updates
7. Handle expired entries by filtering on client side
8. Add skeleton loading for waitlist section on dashboard

## Current Project State
```
[PLACEHOLDER - Updated after US_001, US_016 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/scheduling/components/WaitlistJoinDialog.tsx | Enrollment dialog |
| CREATE | frontend/src/features/scheduling/components/WaitlistDashboardSection.tsx | Dashboard section |
| CREATE | frontend/src/features/scheduling/components/WaitlistAvailableBanner.tsx | Availability banner |
| MODIFY | frontend/src/features/scheduling/components/ProviderCard.tsx | Add waitlist button |
| MODIFY | frontend/src/features/scheduling/api/schedulingApi.ts | Add waitlist endpoints |

## External References
- N/A

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] "Join Waitlist" appears on fully booked providers
- [x] Enrollment dialog captures date range preference
- [x] Dashboard shows active waitlist entries with status
- [x] Quick-book banner appears on slot availability
- [x] Remove from waitlist works and updates display
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-007

## Implementation Checklist
- [x] Add "Join Waitlist" button to ProviderCard for fully booked providers
- [x] Create WaitlistJoinDialog with date range and preference selection
- [x] Build WaitlistDashboardSection with status badges and remove action
- [x] Create WaitlistAvailableBanner with quick-book CTA
- [x] Implement SignalR listener for waitlist availability notifications
- [x] Add skeleton loading states for waitlist dashboard section
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-007 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
