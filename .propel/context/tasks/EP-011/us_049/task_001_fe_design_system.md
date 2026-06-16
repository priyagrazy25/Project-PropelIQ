# Task - TASK_001_FE_DESIGN_SYSTEM

## Requirement Reference

### User Story
- **Story ID**: US_049
- **Title**: Design System & Visual Standards
- **Path**: `.propel/context/tasks/EP-011/us_049/us_049.md`

### Acceptance Criteria Addressed
1. Healthcare-appropriate calming color palette with primary blue and semantic alert colors (success green, warning amber, error red) per UXR-401.
2. All spacings and paddings follow an 8px base grid system per UXR-402.
3. Clear typography hierarchy from H1 through Caption with consistent sizing, weights, and line heights per UXR-403.
4. Consistent header and sidebar navigation layout on every authenticated screen per UXR-002.
5. New components use shared design tokens from the central design system configuration.

### Edge Cases
- Content exceeds layout bounds — overflow handled with scroll or text truncation with tooltip; layout does not break.
- Component without a defined design token — inherits from nearest parent token; design debt flag logged for review.

## Design References (Frontend Tasks Only)

### Screen Specifications
- **Screen ID(s)**: All screens (SCR-001 through SCR-025)
- **Figma Spec**: `.propel/context/docs/figma_spec.md#DesignSystem`
- **Wireframe Path**: All wireframes in `.propel/context/wireframes/Hi-Fi/`

### UX Requirements
| UXR ID | Requirement |
|--------|------------|
| UXR-001 | Max 3-click navigation from dashboard |
| UXR-002 | Consistent header and sidebar navigation layout |
| UXR-401 | Healthcare-appropriate calming color palette |
| UXR-402 | 8px base grid spacing system |
| UXR-403 | Typography hierarchy H1 through Caption |

### Design Tokens
| Token Category | Tokens |
|---------------|--------|
| **Primary Colors** | `--color-primary-50` through `--color-primary-900`, `--color-primary: #1A73E8` |
| **Semantic Colors** | `--color-success: #34A853`, `--color-warning: #FBBC04`, `--color-error: #EA4335`, `--color-info: #4285F4` |
| **Neutral Colors** | `--color-neutral-50` through `--color-neutral-900` |
| **Background** | `--color-bg-primary: #FFFFFF`, `--color-bg-secondary: #F8F9FA`, `--color-bg-elevated: #FFFFFF` |
| **Spacing** | `--space-1: 4px`, `--space-2: 8px`, `--space-3: 16px`, `--space-4: 24px`, `--space-5: 32px`, `--space-6: 48px` |
| **Typography** | `--font-family: 'Inter', system-ui, sans-serif` |
| **Border Radius** | `--radius-sm: 4px`, `--radius-md: 8px`, `--radius-lg: 12px`, `--radius-full: 9999px` |
| **Shadows** | `--shadow-sm`, `--shadow-md`, `--shadow-lg` |

## Applicable Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Frontend | React TypeScript | 18.x |
| Styling | CSS Modules / SCSS | N/A |
| Font | Inter (Google Fonts) | Variable |

## AI References (AI Tasks Only)
N/A — no AI components in this task.

## Mobile References (Mobile Tasks Only)
N/A — not applicable for this project.

## Task Overview
Establish the healthcare-appropriate design system as the visual foundation for the entire platform: define design tokens (colors, spacing, typography, shadows, radii) in a central SCSS configuration, implement the 8px base grid spacing system, create the typography hierarchy scale from H1 through Caption, build the consistent header and navigation layout wrapper, and provide a component style guide that enforces token usage across all new components.

## Dependent Tasks

| Task ID | Task Name | Dependency Type |
|---------|-----------|----------------|
| TASK_001_FE_REACT_SCAFFOLDING (US_001) | React Scaffolding | Must complete first — React project structure and build pipeline |

## Impacted Components

| Component | Type | Impact |
|-----------|------|--------|
| Design Tokens Configuration | SCSS | CREATE — Central token definitions for colors, spacing, typography |
| Typography Scale | SCSS | CREATE — H1-Caption heading and body text styles |
| Spacing Utilities | SCSS | CREATE — 8px grid-based margin/padding utility classes |
| Color Palette | SCSS | CREATE — Healthcare-appropriate color palette with semantic variants |
| AppHeader | React Component | CREATE — Consistent page header with breadcrumbs |
| Theme Provider | React Context | CREATE — Theme context providing design tokens to components |
| Component Style Guide | Documentation | CREATE — Usage guide for design tokens and component patterns |

## Implementation Plan

1. **Define Design Tokens in SCSS**: Create `frontend/src/styles/tokens.scss` with CSS custom properties for all design tokens: primary color scale (50-900), semantic colors (success, warning, error, info), neutral scale (50-900), background variants, spacing scale (4px increments on 8px grid), border radii, and elevation shadows. Import tokens at the root level so all components inherit.

2. **Implement 8px Grid Spacing System**: Create `frontend/src/styles/spacing.scss` with utility classes: `.m-{1-6}` (margin), `.p-{1-6}` (padding), `.gap-{1-6}` (grid/flex gap) mapped to the 8px-based spacing scale. Add directional variants (`.mt-`, `.mr-`, `.mb-`, `.ml-`, `.mx-`, `.my-`). Ensure all values are multiples of 4px with 8px as the primary unit.

3. **Create Typography Hierarchy**: Define the type scale in `frontend/src/styles/typography.scss`: H1 (32px/40px, 700), H2 (24px/32px, 600), H3 (20px/28px, 600), H4 (18px/24px, 500), Body1 (16px/24px, 400), Body2 (14px/20px, 400), Caption (12px/16px, 400), Overline (10px/16px, 500 uppercase). Use Inter as the primary font with system-ui fallback. Load Inter from Google Fonts with variable weight support.

4. **Build Healthcare Color Palette**: Configure the calming blue primary palette with accessible contrast ratios. Primary blue (`#1A73E8`) as the brand anchor. Success green (`#34A853`), warning amber (`#FBBC04`), error red (`#EA4335`). Ensure all semantic colors meet WCAG AA contrast on white backgrounds.

5. **Create AppHeader Component**: Build a consistent `AppHeader` component that renders on every authenticated page. Include: page title, breadcrumb navigation, user avatar/menu. Use design tokens for all spacing, colors, and typography. Ensure the header works with both the sidebar (desktop/tablet) and bottom nav (mobile) layouts.

6. **Build Theme Provider Context**: Create `ThemeProvider` React context that exposes design tokens programmatically for components that need JavaScript access to token values (e.g., chart colors, dynamic styles). Export a `useTheme()` hook.

7. **Create Component Style Guide**: Write a `frontend/docs/design-system.md` documenting all tokens with visual examples, usage patterns, dos/don'ts, and code snippets. Include guidance for adding new components: "Always use `var(--space-N)` for spacing, never raw pixel values."

## Current Project State
[PLACEHOLDER — to be filled during implementation sprint]

## Expected Changes

| Action | File/Component | Description |
|--------|---------------|-------------|
| CREATE | `frontend/src/styles/tokens.scss` | Central design token definitions (CSS custom properties) |
| CREATE | `frontend/src/styles/spacing.scss` | 8px grid utility classes (m-, p-, gap-) |
| CREATE | `frontend/src/styles/typography.scss` | Type scale H1 through Caption with Inter font |
| CREATE | `frontend/src/styles/colors.scss` | Healthcare color palette with semantic variants |
| CREATE | `frontend/src/styles/shadows.scss` | Elevation shadow definitions |
| CREATE | `frontend/src/components/layout/AppHeader.tsx` | Consistent page header with breadcrumbs |
| CREATE | `frontend/src/contexts/ThemeProvider.tsx` | Theme context with useTheme() hook |
| MODIFY | `frontend/src/styles/global.scss` | Import all token/utility stylesheets |
| MODIFY | `frontend/index.html` | Add Inter font preload from Google Fonts |
| CREATE | `frontend/docs/design-system.md` | Component style guide documentation |

## External References
- [Inter Font](https://fonts.google.com/specimen/Inter)
- [8-Point Grid System](https://spec.fm/specifics/8-pt-grid)
- [Healthcare UI Design Best Practices](https://www.nngroup.com/articles/healthcare-ux/)

## Build Commands
```bash
cd frontend

# Install dependencies (no additional packages needed for SCSS tokens)
npm install

# Build to verify token compilation
npm run build

# Run Storybook (if configured) for visual token review
npx storybook dev -p 6006
```

## Implementation Validation Strategy
- [ ] Verify all colors in tokens.scss meet WCAG AA contrast ratios against their intended backgrounds
- [ ] Verify spacing utility classes produce correct 8px-grid-aligned values (inspect computed styles)
- [ ] Verify typography scale renders correctly: H1 (32px), H2 (24px), H3 (20px), Body1 (16px), Caption (12px)
- [ ] Verify Inter font loads and displays on first render without layout shift
- [ ] Verify AppHeader renders consistently across all authenticated pages
- [ ] Verify no component uses raw pixel values instead of design tokens (grep audit)

## Implementation Checklist
- [ ] Create `tokens.scss` with all CSS custom properties (colors, spacing, radii, shadows)
- [ ] Implement 8px grid spacing utilities (m-, p-, gap- classes with directional variants)
- [ ] Define typography scale from H1 through Caption using Inter font
- [ ] Configure healthcare-appropriate calming color palette with accessible contrast
- [ ] Build `AppHeader` component with page title, breadcrumbs, and user menu
- [ ] Create `ThemeProvider` context with `useTheme()` hook for dynamic token access
- [ ] Write design system documentation with usage patterns and code snippets
- [ ] Validate all tokens compile correctly and no raw pixel values bypass the system
