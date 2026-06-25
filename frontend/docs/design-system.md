# Design System

This document defines the visual standards and token usage for the Unified Patient Access frontend.

## Core Principles

- Use design tokens for all colors, spacing, and focus states.
- Maintain the 8px baseline rhythm for layout and spacing.
- Keep navigation shell consistent across authenticated screens.
- Meet WCAG 2.2 AA accessibility baseline for contrast and keyboard focus.

## Token Usage

### Color Tokens

Use semantic CSS variables instead of hard-coded values.

- `--primary`, `--primary-foreground`
- `--secondary`, `--secondary-foreground`
- `--success`, `--warning`, `--destructive`, `--info`
- `--background`, `--surface`, `--card`, `--border`, `--muted`

Example:

```tsx
<div className="bg-[var(--surface)] text-foreground border border-border" />
```

### Spacing Scale (8px base)

Use spacing values that map to the tokenized scale in global styles.

- `4px`, `8px`, `12px`, `16px`, `24px`, `32px`, `48px`, `64px`

Guideline:

- Component padding: prefer `px-4 py-3` or `p-4`
- Section spacing: prefer `space-y-6` or `gap-6`

### Typography Hierarchy

- H1: 32px, bold
- H2: 24px, semibold
- H3: 20px, semibold
- H4: 18px, semibold
- Body: 14px-16px
- Caption: 12px

`Inter Variable` is the primary font for UI text.

## Accessibility Tokens

- `--focus-ring-width`: `2px`
- `--focus-ring-offset`: `2px`
- `--focus-ring-color`: `#1A73E8`
- `--min-touch-target`: `44px`

Interactive controls must keep visible focus and at least 44x44 touch targets.

## Navigation Shell Standard

Authenticated routes use a shared responsive shell:

- Desktop: fixed sidebar
- Tablet: collapsible sidebar
- Mobile: bottom navigation + More sheet

Implemented components:

- `src/shared/components/layout/ResponsiveNavShell.tsx`
- `src/shared/components/layout/AppHeader.tsx`
- `src/shared/hooks/useBreakpoint.ts`

## Do / Do Not

- Do: use `var(--token-name)` and utility classes backed by tokens.
- Do: use `role`, `aria-label`, and `aria-live` on dynamic regions.
- Do not: introduce raw one-off color hex values in new components.
- Do not: remove focus outlines without replacing them with accessible focus styles.

## New Component Checklist

- Uses semantic color tokens.
- Uses 8px grid-aligned spacing.
- Includes keyboard focus-visible behavior.
- Meets 44x44 touch target on mobile for interactive controls.
- Includes ARIA semantics for controls and dynamic updates.
