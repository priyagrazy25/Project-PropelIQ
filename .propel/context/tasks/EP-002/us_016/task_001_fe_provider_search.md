---
post_title: "TASK_001 - Provider Search & Slot Display UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-provider-search-slot-display"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-002, US_016, frontend, React, search, SignalR, real-time"
ai_note: "Generated with AI assistance from user story US_016"
summary: "Implement provider search interface with specialty/name/date filters and real-time slot availability via SignalR WebSocket."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_PROVIDER_SEARCH_SLOT_DISPLAY

## Requirement Reference
- User Story: us_016
- Story Location: .propel/context/tasks/EP-002/us_016/us_016.md
- Acceptance Criteria:
    - AC-1: Search by specialty, provider name, or date range returns results within 2s at p95 per UXR-101
    - AC-2: Real-time slot disappearance via SignalR within 500ms per UXR-102
    - AC-3: Skeleton loading screens during search per UXR-501
    - AC-4: Empty state with "No providers match your criteria" and filter reset CTA
    - AC-5: Paginate results at 20 per page with virtual scrolling for 100+ results
- Edge Cases:
    - SignalR disconnection → auto-reconnect with REST fallback for missed updates
    - Very broad search returning 100+ results → virtual scrolling pagination

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-004-provider-search.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-004 |
| **UXR Requirements** | UXR-101, UXR-102, UXR-501 |
| **Design Tokens** | .propel/context/docs/designsystem.md#search, #cards |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | React with TypeScript | 18.x |
| State Management | Redux Toolkit | 2.x |
| Real-Time | @microsoft/signalr | 8.x |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |

## Task Overview
Build the provider search page with filters for specialty, provider name, and date range. Display results as provider cards with available time slots. Integrate SignalR for real-time slot availability updates. Include skeleton loading, empty states, and virtual scrolling for large result sets.

## Dependent Tasks
- task_001_fe_react_scaffolding (US_001) — Requires React project
- task_001_be_caching_realtime_setup (US_004) — Requires SignalR AppointmentHub

## Impacted Components
- NEW: ProviderSearchPage with search filters
- NEW: ProviderCard, SlotGrid, SearchFilters components
- NEW: useSignalRSlots hook for real-time updates
- NEW: schedulingApi RTK Query endpoints

## Implementation Plan
1. Create ProviderSearchPage with SearchFilters component (specialty dropdown, name input, date range picker)
2. Build ProviderCard component displaying provider info and available time slots grid
3. Implement RTK Query endpoint for GET /api/scheduling/providers/search with filter params
4. Create useSignalRSlots hook connecting to AppointmentHub for slot-availability events
5. Add SignalR auto-reconnect with REST API fallback on disconnect
6. Implement skeleton loading states matching card layout during search
7. Add empty state with filter reset CTA when no results
8. Implement virtual scrolling with 20-item pages for large result sets

## Current Project State
```
[PLACEHOLDER - Updated after US_001, US_004 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/scheduling/pages/ProviderSearchPage.tsx | Search page container |
| CREATE | frontend/src/features/scheduling/components/SearchFilters.tsx | Filter bar component |
| CREATE | frontend/src/features/scheduling/components/ProviderCard.tsx | Provider result card |
| CREATE | frontend/src/features/scheduling/components/SlotGrid.tsx | Time slot grid display |
| CREATE | frontend/src/features/scheduling/hooks/useSignalRSlots.ts | SignalR real-time hook |
| CREATE | frontend/src/features/scheduling/api/schedulingApi.ts | RTK Query API slice |
| MODIFY | frontend/src/App.tsx | Add /search route |

## External References
- @microsoft/signalr client: https://learn.microsoft.com/aspnet/core/signalr/javascript-client
- React virtual scrolling: https://react.dev/learn/rendering-lists

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Search by specialty returns providers with slots within 2s
- [x] SignalR slot updates reflect within 500ms
- [x] Skeleton loading displays during search
- [x] Empty state shows when no results match
- [x] Virtual scrolling handles 100+ results
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-004

## Implementation Checklist
- [x] Create ProviderSearchPage with specialty, name, and date range filters
- [x] Build ProviderCard with SlotGrid showing available time slots
- [x] Implement RTK Query search endpoint with debounced input
- [x] Create useSignalRSlots hook for real-time slot availability
- [x] Add SignalR auto-reconnect with REST fallback reconciliation
- [x] Implement skeleton loading and empty state with filter reset
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-004 during implementation
- [x] **[UI Tasks - MANDATORY]** Validate UI matches wireframe before marking complete
