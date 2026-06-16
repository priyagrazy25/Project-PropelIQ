# Task - TASK_001_FE_WCAG_ACCESSIBILITY

## Requirement Reference

### User Story
- **Story ID**: US_047
- **Title**: WCAG 2.2 AA Accessibility Compliance
- **Path**: `.propel/context/tasks/EP-011/us_047/us_047.md`

### Acceptance Criteria Addressed
1. Text achieves >= 4.5:1 contrast ratio and UI components achieve >= 3:1 per UXR-201.
2. All controls reachable and operable via keyboard with visible focus indicators (2px offset, >= 3:1 contrast) per UXR-202.
3. All form controls, buttons, and dynamic content regions have proper ARIA labels per UXR-203.
4. All interactive elements on mobile meet or exceed 44x44px minimum touch target size per UXR-204.
5. Error states use icon + text indicators (not color alone) per UXR-205.

### Edge Cases
- Custom components without native accessibility — custom ARIA roles and keyboard event handlers implemented to match native behavior.
- Dynamically loaded content (lazy-loaded lists, infinite scroll) — ARIA live regions announce new content; focus management preserves keyboard context.

## Design References (Frontend Tasks Only)

### Screen Specifications
- **Screen ID(s)**: All screens (SCR-001 through SCR-025)
- **Figma Spec**: `.propel/context/docs/figma_spec.md#Accessibility`
- **Wireframe Path**: All wireframes in `.propel/context/wireframes/Hi-Fi/`

### UX Requirements
| UXR ID | Requirement |
|--------|------------|
| UXR-201 | Color contrast >= 4.5:1 text, >= 3:1 UI components |
| UXR-202 | Keyboard navigation with visible 2px focus indicators |
| UXR-203 | ARIA labels on all form controls and dynamic regions |
| UXR-204 | Touch targets >= 44x44px on mobile viewports |
| UXR-205 | Error states using icon + text, not color alone |

### Design Tokens
| Token | Value | Usage |
|-------|-------|-------|
| `--focus-ring-width` | 2px | Focus indicator width |
| `--focus-ring-offset` | 2px | Focus indicator offset |
| `--focus-ring-color` | `#1A73E8` | Focus ring color (>= 3:1 on white) |
| `--min-touch-target` | 44px | Minimum touch target dimension |
| `--error-icon-size` | 16px | Error state icon size |

## Applicable Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Frontend | React TypeScript | 18.x |
| Styling | CSS Modules / SCSS | N/A |
| Testing | Playwright | latest |
| Accessibility Testing | axe-core / @axe-core/react | latest |

## AI References (AI Tasks Only)
N/A — no AI components in this task.

## Mobile References (Mobile Tasks Only)
N/A — not applicable for this project.

## Task Overview
Implement WCAG 2.2 AA accessibility standards across all React components: audit and fix color contrast ratios to meet >= 4.5:1 for text and >= 3:1 for UI elements, add visible keyboard focus indicators with consistent 2px offset styling, apply ARIA labels to all form controls, buttons, and dynamic content regions, ensure all interactive elements meet 44x44px minimum touch targets on mobile, and update all error states to use icon + text patterns (not color alone). Integrate axe-core for automated accessibility testing in the development pipeline.

## Dependent Tasks

| Task ID | Task Name | Dependency Type |
|---------|-----------|----------------|
| TASK_001_FE_REACT_SCAFFOLDING (US_001) | React Scaffolding | Must complete first — component library foundation |
| TASK_001_FE_DESIGN_SYSTEM (US_049) | Design System | Should complete first — design tokens and base styles |

## Impacted Components

| Component | Type | Impact |
|-----------|------|--------|
| Global CSS / Theme | Styles | MODIFY — Add focus indicator styles, contrast-compliant colors |
| All Form Components | React Components | MODIFY — Add ARIA labels, role attributes |
| All Button Components | React Components | MODIFY — Ensure 44x44px touch targets, ARIA labels |
| Error Display Components | React Components | MODIFY — Add icon + text pattern to all error states |
| Dynamic Content Regions | React Components | MODIFY — Add aria-live regions for lazy-loaded content |
| Accessibility Test Suite | Test Config | CREATE — axe-core integration for automated a11y testing |

## Implementation Plan

1. **Audit and Fix Color Contrast**: Scan all SCSS/CSS variables and component styles using axe-core contrast checker. Update colors that fail 4.5:1 for normal text or 3:1 for large text/UI components. Create a contrast-compliance map documenting each color pair's ratio.

2. **Implement Focus Indicators**: Create a global CSS utility class `.focus-visible` using `:focus-visible` pseudo-class with `outline: 2px solid var(--focus-ring-color); outline-offset: 2px`. Apply to all interactive elements. Ensure focus ring has >= 3:1 contrast against adjacent backgrounds. Remove any `outline: none` overrides that hide focus indicators.

3. **Add ARIA Labels to All Components**: Audit all `<input>`, `<select>`, `<textarea>`, `<button>` elements. Add `aria-label` or `aria-labelledby` where visible labels are absent. Add `aria-describedby` for help text. Add `role` attributes to custom components (modals: `role="dialog"`, tabs: `role="tablist"`). Ensure `aria-expanded`, `aria-selected`, `aria-checked` states are managed on interactive widgets.

4. **Implement ARIA Live Regions**: Add `aria-live="polite"` to all dynamically updated content regions (search results, toast notifications, loading indicators). Add `aria-live="assertive"` for critical alerts (session timeout, error messages). Ensure `aria-busy="true"` is set during loading states.

5. **Enforce 44x44px Touch Targets**: Audit all clickable/tappable elements at mobile breakpoint (390px). Apply `min-width: 44px; min-height: 44px` to all interactive elements. Use padding to achieve target size when the visual element is smaller. Add `touch-action: manipulation` to prevent double-tap zoom delays.

6. **Update Error States to Icon + Text**: Replace all color-only error indicators with icon + text patterns. Use `ErrorIcon` SVG (red circle with exclamation) + descriptive text. Apply `role="alert"` to error containers. Ensure error text is associated via `aria-describedby` with the corresponding form field.

7. **Integrate axe-core Automated Testing**: Install `@axe-core/react` for development mode overlay. Add Playwright accessibility assertions using `@axe-core/playwright` in the E2E test suite. Create a CI check that fails on any WCAG 2.2 AA violations.

## Current Project State
[PLACEHOLDER — to be filled during implementation sprint]

## Expected Changes

| Action | File/Component | Description |
|--------|---------------|-------------|
| MODIFY | `frontend/src/styles/global.scss` | Add focus indicator styles, contrast-compliant color variables |
| MODIFY | `frontend/src/styles/tokens.scss` | Add focus-ring, touch-target, error-icon design tokens |
| MODIFY | All form components in `frontend/src/features/` | Add ARIA labels, describedby, roles |
| MODIFY | All button/interactive components | Enforce 44x44px minimum touch targets |
| MODIFY | All error display components | Replace color-only with icon + text pattern |
| CREATE | `frontend/src/components/common/ErrorIndicator.tsx` | Reusable icon + text error component |
| CREATE | `frontend/src/utils/accessibility.ts` | Focus management utilities and ARIA helpers |
| CREATE | `frontend/playwright/accessibility.spec.ts` | axe-core Playwright accessibility test suite |
| MODIFY | `frontend/package.json` | Add @axe-core/react, @axe-core/playwright |

## External References
- [WCAG 2.2 AA Guidelines](https://www.w3.org/TR/WCAG22/)
- [axe-core React Integration](https://github.com/dequelabs/axe-core-npm/tree/develop/packages/react)
- [ARIA Authoring Practices Guide](https://www.w3.org/WAI/ARIA/apg/)

## Build Commands
```bash
# Install accessibility testing libraries
cd frontend
npm install --save-dev @axe-core/react @axe-core/playwright

# Run accessibility audit via Playwright
npx playwright test accessibility.spec.ts

# Check contrast ratios via axe CLI
npx axe https://localhost:3000 --tags wcag2aa

# Build
npm run build
```

## Implementation Validation Strategy
- [ ] Run axe-core scan on every screen — zero WCAG 2.2 AA violations
- [ ] Verify all text meets >= 4.5:1 contrast ratio against backgrounds
- [ ] Tab through every page using keyboard only — all controls reachable with visible focus ring
- [ ] Verify screen reader (NVDA/VoiceOver) reads all form labels and dynamic region updates
- [ ] Verify all touch targets are >= 44x44px at 390px viewport in DevTools
- [ ] Verify error states display icon + text (not color alone) on every form

## Implementation Checklist
- [ ] Audit and fix all color combinations to meet >= 4.5:1 text / >= 3:1 UI contrast ratios
- [ ] Implement global `:focus-visible` styles with 2px offset, >= 3:1 contrast focus ring
- [ ] Add ARIA labels, roles, and states to all forms, buttons, and custom widgets
- [ ] Add `aria-live` regions for all dynamically updated content areas
- [ ] Enforce 44x44px minimum touch targets on all interactive elements at mobile viewport
- [ ] Replace all color-only error indicators with icon + text ErrorIndicator component
- [ ] Integrate axe-core in dev mode and Playwright CI pipeline for automated a11y testing
- [ ] Run full WCAG 2.2 AA audit and resolve all violations across SCR-001 through SCR-025
