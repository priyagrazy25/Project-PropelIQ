# Task - TASK_001_FE_RESPONSIVE_LAYOUT

## Requirement Reference

### User Story
- **Story ID**: US_048
- **Title**: Responsive Layout & Adaptive Navigation
- **Path**: `.propel/context/tasks/EP-011/us_048/us_048.md`

### Acceptance Criteria Addressed
1. Layout responds at three breakpoints: 390px (Mobile), 768px (Tablet), 1440px (Desktop) per UXR-301.
2. Desktop: fixed sidebar navigation. Tablet: collapsible sidebar. Mobile: bottom navigation bar per UXR-302.
3. Data tables transform into stacked-card layout on mobile per UXR-303.
4. All content remains readable and interactive without horizontal scrolling at any viewport.
5. Any primary feature is reachable within maximum 3 clicks from the dashboard per UXR-001.

### Edge Cases
- Viewport widths between breakpoints (e.g., 500px) — fluid layout scales using relative units; no content clipped.
- Landscape orientation on mobile — layout adapts to available width; bottom navigation remains accessible.

## Design References (Frontend Tasks Only)

### Screen Specifications
- **Screen ID(s)**: All screens (SCR-001 through SCR-025)
- **Figma Spec**: `.propel/context/docs/figma_spec.md#Responsive`
- **Wireframe Path**: All wireframes in `.propel/context/wireframes/Hi-Fi/`

### UX Requirements
| UXR ID | Requirement |
|--------|------------|
| UXR-301 | Responsive layout at 390px, 768px, 1440px breakpoints |
| UXR-302 | Adaptive navigation: fixed sidebar → collapsible sidebar → bottom nav |
| UXR-303 | Table-to-stacked-card layout transformation on mobile |
| UXR-001 | Max 3-click navigation to any primary feature from dashboard |

### Design Tokens
| Token | Value | Usage |
|-------|-------|-------|
| `--breakpoint-mobile` | 390px | Mobile breakpoint |
| `--breakpoint-tablet` | 768px | Tablet breakpoint |
| `--breakpoint-desktop` | 1440px | Desktop breakpoint |
| `--sidebar-width` | 240px | Fixed sidebar width (desktop) |
| `--sidebar-collapsed` | 64px | Collapsed sidebar width (tablet) |
| `--bottom-nav-height` | 56px | Bottom navigation height (mobile) |

## Applicable Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Frontend | React TypeScript | 18.x |
| Styling | CSS Modules / SCSS | N/A |
| State Management | Redux Toolkit | 2.x |
| Routing | React Router | 6.x |

## AI References (AI Tasks Only)
N/A — no AI components in this task.

## Mobile References (Mobile Tasks Only)
N/A — not applicable for this project.

## Task Overview
Implement the responsive layout system across the entire React application: create a breakpoint-aware layout shell with three adaptive navigation modes (fixed sidebar on desktop, collapsible sidebar on tablet, bottom navigation on mobile), build a responsive data table component that transforms to stacked cards on mobile, establish fluid layout utilities using CSS Grid/Flexbox with relative units between breakpoints, and ensure all screens are usable without horizontal scrolling at every viewport width.

## Dependent Tasks

| Task ID | Task Name | Dependency Type |
|---------|-----------|----------------|
| TASK_001_FE_REACT_SCAFFOLDING (US_001) | React Scaffolding | Must complete first — SPA shell with routing |
| TASK_001_FE_DESIGN_SYSTEM (US_049) | Design System | Should complete first — spacing grid and typography tokens |

## Impacted Components

| Component | Type | Impact |
|-----------|------|--------|
| AppLayout | React Component | CREATE — Breakpoint-aware layout shell |
| Sidebar | React Component | CREATE — Fixed (desktop) / collapsible (tablet) sidebar navigation |
| BottomNav | React Component | CREATE — Mobile bottom navigation bar |
| ResponsiveTable | React Component | CREATE — Table-to-card transformation component |
| useBreakpoint Hook | React Hook | CREATE — Viewport detection hook |
| Breakpoint SCSS Mixins | Styles | CREATE — Responsive media query mixins |

## Implementation Plan

1. **Create useBreakpoint Hook**: Build a custom React hook `useBreakpoint()` that returns the current viewport category (`mobile`, `tablet`, `desktop`) using `window.matchMedia`. Listen for resize events with debounce (150ms). Expose breakpoint values via React context for child consumption.

2. **Build AppLayout Shell**: Create `AppLayout` component that renders the appropriate navigation mode based on `useBreakpoint()`. Desktop: `<Sidebar />` + `<MainContent />` side-by-side via CSS Grid. Tablet: `<CollapsibleSidebar />` + `<MainContent />`. Mobile: `<MainContent />` + `<BottomNav />`. Use CSS Grid with `grid-template-columns` to switch layouts without re-mounting children.

3. **Implement Desktop Fixed Sidebar**: Build `Sidebar` component at 240px fixed width. Render navigation items: Dashboard, Appointments, Walk-In Queue, Patient Intake, Documents, Clinical Intelligence, Reports, Admin. Highlight active route. Ensure any primary feature is within 3 clicks from Dashboard.

4. **Implement Tablet Collapsible Sidebar**: Build `CollapsibleSidebar` that collapses to 64px showing only icons. Toggle button expands to full 240px with text labels. Collapsed state preserves tooltips on hover for icon-only items. Animate width transition (200ms ease).

5. **Implement Mobile Bottom Navigation**: Build `BottomNav` at 56px fixed height with 5 primary navigation items (Dashboard, Appointments, Queue, Intake, More). The "More" item opens a full-screen menu for secondary features. Apply 44x44px minimum touch targets per UXR-204.

6. **Build ResponsiveTable Component**: Create `ResponsiveTable` that renders as `<table>` on desktop/tablet and transforms to stacked `<div>` cards on mobile. Each card shows row data with label-value pairs. Use `useBreakpoint()` to toggle rendering mode. Preserve sort/filter controls in both modes.

7. **Create SCSS Breakpoint Mixins**: Define `@mixin mobile`, `@mixin tablet`, `@mixin desktop` mixins using `@media` queries at 390px, 768px, 1440px. Create fluid spacing utilities using `clamp()` for values that scale between breakpoints. Ensure no horizontal scrollbar appears at any width (`overflow-x: hidden` on body with proper content sizing).

## Current Project State
[PLACEHOLDER — to be filled during implementation sprint]

## Expected Changes

| Action | File/Component | Description |
|--------|---------------|-------------|
| CREATE | `frontend/src/components/layout/AppLayout.tsx` | Breakpoint-aware layout shell with CSS Grid |
| CREATE | `frontend/src/components/layout/Sidebar.tsx` | Fixed desktop sidebar navigation (240px) |
| CREATE | `frontend/src/components/layout/CollapsibleSidebar.tsx` | Collapsible tablet sidebar (64px-240px) |
| CREATE | `frontend/src/components/layout/BottomNav.tsx` | Mobile bottom navigation bar (56px) |
| CREATE | `frontend/src/components/common/ResponsiveTable.tsx` | Table-to-card responsive component |
| CREATE | `frontend/src/hooks/useBreakpoint.ts` | Viewport detection hook with context |
| CREATE | `frontend/src/styles/breakpoints.scss` | SCSS breakpoint mixins and fluid utilities |
| MODIFY | `frontend/src/App.tsx` | Wrap routes in AppLayout shell |

## External References
- [CSS Grid Layout](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_grid_layout)
- [matchMedia API](https://developer.mozilla.org/en-US/docs/Web/API/Window/matchMedia)
- [Responsive Design Patterns](https://web.dev/responsive-web-design-basics/)

## Build Commands
```bash
cd frontend

# Build
npm run build

# Run dev server and test at various viewports
npm run dev

# Playwright responsive tests
npx playwright test --project=mobile
npx playwright test --project=tablet
npx playwright test --project=desktop
```

## Implementation Validation Strategy
- [ ] Verify layout switches to bottom nav at 390px, collapsible sidebar at 768px, fixed sidebar at 1440px
- [ ] Verify no horizontal scrollbar appears at any viewport width (320px through 1920px)
- [ ] Verify data tables transform to stacked cards on mobile with readable label-value pairs
- [ ] Verify all primary features reachable within 3 clicks from Dashboard on all breakpoints
- [ ] Verify collapsible sidebar animates between 64px and 240px states smoothly
- [ ] Verify bottom nav touch targets are >= 44x44px

## Implementation Checklist
- [ ] Build `useBreakpoint` hook with `matchMedia` listeners and React context provider
- [ ] Create `AppLayout` shell with CSS Grid switching between sidebar and bottom nav modes
- [ ] Implement `Sidebar` (240px fixed, desktop) with navigation items and active route highlighting
- [ ] Implement `CollapsibleSidebar` (64px collapsed, tablet) with icon tooltips and animation
- [ ] Implement `BottomNav` (56px fixed, mobile) with 5 primary items and "More" menu
- [ ] Build `ResponsiveTable` with table-to-stacked-card transformation
- [ ] Create SCSS breakpoint mixins and fluid spacing utilities with `clamp()`
- [ ] Validate all screens: no horizontal scroll, correct navigation mode, 3-click max
