---
post_title: "TASK_001 - No-Show Risk Dashboard UI"
author1: "AI Senior Developer"
post_slug: "task-001-fe-noshow-risk-dashboard"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-009, US_042, frontend, React, risk-score, dashboard, no-show"
ai_note: "Generated with AI assistance from user story US_042"
summary: "Implement staff No-Show Risk Dashboard with risk level indicators, sorting, date filtering, and escalation visibility."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_NOSHOW_RISK_DASHBOARD

## Requirement Reference
- User Story: us_042
- Story Location: .propel/context/tasks/EP-009/us_042/us_042.md
- Acceptance Criteria:
    - AC-2: Each appointment displays risk level with visual indicators (low/medium/high) on SCR-022
    - AC-5: Sort by risk level and filter by date range

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | Yes |
| **Figma URL** | N/A |
| **Wireframe Status** | AVAILABLE |
| **Wireframe Type** | HTML |
| **Wireframe Path/URL** | .propel/context/wireframes/Hi-Fi/wireframe-SCR-022-no-show-risk-dashboard.html |
| **Screen Spec** | .propel/context/docs/figma_spec.md#SCR-022 |
| **UXR Requirements** | N/A |
| **Design Tokens** | .propel/context/docs/designsystem.md#risk-indicators, #tables |

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
Build the No-Show Risk Dashboard for staff with appointment list displaying risk scores (0-100) with low/medium/high visual indicators, sortable by risk level, filterable by date range, and escalation status badges.

## Dependent Tasks
- task_001_fe_react_scaffolding (US_001) — Requires React project
- task_001_fe_login_interface (US_013) — Requires staff role access

## Impacted Components
- NEW: NoShowRiskDashboardPage — Risk dashboard
- NEW: RiskIndicator — Low/medium/high visual badge
- NEW: riskApi — RTK Query endpoints

## Implementation Plan
1. Create NoShowRiskDashboardPage with appointment table
2. Build RiskIndicator component (green=low ≤30, amber=medium 31-70, red=high >70)
3. Implement sort by risk level (highest first by default)
4. Add date range filter
5. Show escalation status badge for high-risk appointments
6. Implement RTK Query endpoint for risk data

## Current Project State
```
[PLACEHOLDER - Updated after US_001, US_013 tasks]
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/src/features/scheduling/pages/NoShowRiskDashboardPage.tsx | Dashboard |
| CREATE | frontend/src/features/scheduling/components/RiskIndicator.tsx | Risk badge |
| CREATE | frontend/src/features/scheduling/api/riskApi.ts | API functions |
| MODIFY | frontend/src/App.tsx | Add /staff/risk and /management/risk routes |

## External References
- N/A

## Build Commands
- `npm run dev` — Start dev server

## Implementation Validation Strategy
- [x] Dashboard displays appointments with risk scores
- [x] Risk indicators show correct colors per threshold
- [x] Sort by risk level works
- [x] Date range filter works
- [x] **[UI Tasks]** Visual comparison against wireframe SCR-022

## Implementation Checklist
- [x] Create NoShowRiskDashboardPage with appointment table
- [x] Build RiskIndicator (green ≤30, amber 31-70, red >70)
- [x] Implement sort by risk level with highest first default
- [x] Add date range filter for appointments
- [x] Show escalation status badge for high-risk appointments
- [x] **[UI Tasks - MANDATORY]** Reference wireframe SCR-022 during implementation
