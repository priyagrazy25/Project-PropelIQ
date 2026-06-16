---
post_title: "Unified Patient Access - Component Inventory"
author1: "AI UX Designer"
post_slug: "unified-patient-access-components"
categories: "Healthcare, UX, Components"
tags: "component-inventory, design-system, wireframes, react"
ai_note: "Generated with AI assistance from figma_spec.md and designsystem.md"
summary: "Complete component inventory documenting all UI components used across wireframes with states, variants, responsive behavior, and accessibility requirements."
post_date: "2026-04-15"
---

# Component Inventory - Unified Patient Access

## 1. Component Summary

| Component    | Category    | Variants                                     | States                                              | Screens Used                            | Priority |
| ------------ | ----------- | -------------------------------------------- | --------------------------------------------------- | --------------------------------------- | -------- |
| Button       | Interactive | Primary, Secondary, Ghost, Danger, Icon-only | Default, Hover, Active, Focused, Disabled, Loading  | All                                     | P0       |
| TextField    | Interactive | Standard, Password, Search, Inline-edit      | Default, Focused, Filled, Error, Disabled, ReadOnly | SCR-001,002,004,005,011,013,019,023,025 | P0       |
| Select       | Interactive | Single, Multi                                | Default, Open, Selected, Error, Disabled            | SCR-004,011,018,023,025                 | P0       |
| DatePicker   | Interactive | Single, Range                                | Default, Open, Selected, Error                      | SCR-004,008,025                         | P0       |
| Checkbox     | Interactive | Standard, Indeterminate                      | Unchecked, Checked, Indeterminate, Disabled         | SCR-005,011,023                         | P0       |
| RadioGroup   | Interactive | Vertical, Horizontal                         | Unselected, Selected, Disabled                      | SCR-017                                 | P0       |
| Toggle       | Interactive | Standard                                     | Off, On, Disabled                                   | SCR-010                                 | P1       |
| TextArea     | Interactive | Standard, Auto-resize                        | Default, Focused, Filled, Error                     | SCR-011,017                             | P0       |
| FileDropzone | Interactive | Standard, Compact                            | Idle, Drag-over, Uploading, Success, Error          | SCR-014                                 | P0       |
| Card         | Content     | Standard, Summary, Provider, Status          | Default, Hover, Selected, Loading                   | SCR-004,005,006,012,016,021,022,024     | P0       |
| Table        | Content     | Standard, Sortable, Actionable               | Default, Loading, Empty, Error                      | SCR-006,018,020,023,025                 | P0       |
| Badge        | Content     | Status, Count, Confidence                    | Info, Success, Warning, Danger                      | SCR-005,006,007,015,016,018,020,022     | P0       |
| ChatBubble   | Content     | User, AI, System                             | Sent, Received, Loading                             | SCR-010                                 | P0       |
| Tabs         | Navigation  | Standard, Underline                          | Default, Active, Disabled                           | SCR-006,016,020                         | P0       |
| Pagination   | Navigation  | Standard                                     | Default, Active-page, Disabled-nav                  | SCR-004,023,025                         | P1       |
| Breadcrumb   | Navigation  | Standard                                     | Default, Current                                    | Detail screens                          | P1       |
| Header       | Layout      | Standard                                     | Default, Scrolled                                   | All authenticated                       | P0       |
| Sidebar      | Layout      | Expanded, Collapsed                          | Default, Active-item, Hover-item                    | All authenticated                       | P0       |
| BottomNav    | Layout      | Standard                                     | Default, Active-item                                | Mobile only                             | P0       |
| Modal        | Feedback    | Standard, Confirmation                       | Open, Closing                                       | SCR-005,019,023                         | P0       |
| Dialog       | Feedback    | Confirm, Destructive                         | Open, Closing                                       | SCR-008,018,023                         | P0       |
| Toast        | Feedback    | Success, Error, Info, Warning                | Appearing, Visible, Dismissing                      | SCR-005,006,020                         | P0       |
| Alert        | Feedback    | Info, Success, Warning, Error                | Default, Dismissable                                | SCR-001,002,010,016                     | P0       |
| Skeleton     | Feedback    | Card, Table-row, Text-block                  | Loading                                             | All data screens                        | P1       |
| ProgressBar  | Feedback    | Determinate, Indeterminate                   | Active, Complete, Error                             | SCR-014,015                             | P0       |

**Total Components**: 25
**Coverage**: 24/24 screens (100%)

## 2. Layout Components

### 2.1 Header

- **Description**: Fixed top navigation bar with logo, search, notifications, and user avatar
- **Dimensions**: 100% width x 64px height
- **Variants**: Standard (all authenticated screens)
- **States**: Default (static), Scrolled (shadow elevation-1)
- **Content Slots**: Logo (left), Global Search (center), Notification Bell (right), Avatar Dropdown (right)
- **Responsive**: Desktop full width; Mobile logo + hamburger menu
- **Accessibility**: role="banner", aria-label="Main navigation"
- **Screens**: All authenticated screens

### 2.2 Sidebar

- **Description**: Fixed left navigation panel with section links and active state
- **Dimensions**: 256px (expanded), 64px (collapsed), 0px (mobile hidden)
- **Variants**: Expanded (desktop), Collapsed (tablet icon-only)
- **States**: Default, Active-item (primary color left border + bg tint), Hover-item (surface-hover bg)
- **Content Slots**: Logo area (top), Nav items (middle), User section (bottom)
- **Responsive**: Desktop 256px expanded; Tablet 64px icon-only; Mobile hidden (replaced by BottomNav)
- **Accessibility**: role="navigation", aria-label="Sidebar navigation", aria-current="page" on active
- **Screens**: All authenticated screens (Patient: 7 items, Staff: 7 items, Admin: 3 items)

### 2.3 BottomNav

- **Description**: Fixed bottom navigation bar for mobile viewport
- **Dimensions**: 100% width x 56px height
- **Variants**: Standard (mobile only, replaces sidebar)
- **States**: Default, Active-item (primary color icon + label)
- **Content Slots**: 4-5 icon+label items depending on persona
- **Responsive**: Visible only below 768px breakpoint
- **Accessibility**: role="navigation", aria-label="Bottom navigation"
- **Screens**: All authenticated screens (mobile only)

## 3. Navigation Components

### 3.1 Tabs

- **Description**: Horizontal tab strip for in-page section navigation
- **Variants**: Standard (pill-style), Underline (border-bottom indicator)
- **States**: Default (gray text), Active (primary color + indicator), Disabled (muted), Hover (bg tint)
- **Sizing**: Min 80px per tab, max content-width
- **Content**: Icon (optional) + Label text
- **Responsive**: Desktop horizontal scroll; Mobile swipeable with overflow indicator
- **Accessibility**: role="tablist", role="tab", aria-selected, aria-controls per panel
- **Screens**: SCR-006 (Upcoming/Past tabs), SCR-016 (Demographics/Medications/Diagnoses/Labs/Encounters/Conflicts), SCR-020 (Waiting/In-Progress/Completed)

### 3.2 Breadcrumb

- **Description**: Hierarchical path trail showing navigation context
- **Variants**: Standard (> separator)
- **States**: Default (link), Current (non-interactive, aria-current="page")
- **Responsive**: Desktop full path; Mobile shows last 2 items with ellipsis
- **Accessibility**: nav aria-label="Breadcrumb", ordered list
- **Screens**: Detail and nested screens

### 3.3 Pagination

- **Description**: Page navigation for multi-page data sets
- **Variants**: Standard (numbered + prev/next)
- **States**: Default, Active-page (primary bg), Disabled-nav (muted arrows at bounds)
- **Responsive**: Desktop numbered; Mobile simplified (prev/next only)
- **Accessibility**: nav aria-label="Pagination", aria-current="page" on active
- **Screens**: SCR-004, SCR-023, SCR-025

## 4. Content Components

### 4.1 Card

- **Description**: Contained content block with optional header, body, and footer
- **Variants**:
  - Standard: Generic content card (16px padding)
  - Summary: Metric card with icon + value + label (dashboard KPIs)
  - Provider: Search result card with avatar, name, specialty, availability slots
  - Status: Status indicator card with icon + badge + description
- **States**: Default (elevation-1), Hover (elevation-2 + scale 1.01), Selected (primary border), Loading (skeleton)
- **Dimensions**: Min 280px, Max 100% parent width, border-radius-md (8px)
- **Responsive**: Desktop 3-4 per row; Tablet 2 per row; Mobile 1 per row (stacked)
- **Accessibility**: role="article" or semantic grouping, heading hierarchy within
- **Screens**: SCR-004, SCR-005, SCR-006, SCR-012, SCR-016, SCR-021, SCR-022, SCR-024

### 4.2 Table

- **Description**: Data table with sortable columns, row actions, and pagination
- **Variants**: Standard (read-only), Sortable (click headers), Actionable (row action buttons)
- **States**: Default (data loaded), Loading (skeleton rows), Empty (illustration + message), Error (error alert)
- **Columns**: Dynamic based on data; min 3, max 8 visible
- **Row Actions**: View, Edit, Delete (icon buttons right-aligned)
- **Responsive**: Desktop full table; Tablet horizontal scroll; Mobile card-list transformation
- **Accessibility**: role="table", scope="col" on headers, aria-sort on sortable, aria-label on action buttons
- **Screens**: SCR-006, SCR-018, SCR-020, SCR-023, SCR-025

### 4.3 Badge

- **Description**: Small status or count indicator
- **Variants**: Status (dot + text), Count (numeric), Confidence (percentage with color scale)
- **States**:
  - Info: bg surface-info (#E8F4FD), text #1E6F9F
  - Success: bg surface-success (#E6F7F1), text #2D9F83
  - Warning: bg surface-warning (#FFF3E0), text #EF6C00
  - Danger: bg surface-danger (#FDECEC), text #D32F2F
- **Sizing**: Height 24px, padding 4px 8px, border-radius-full (9999px)
- **Accessibility**: Descriptive text (not color-only), aria-label for icon-only badges
- **Screens**: SCR-005, SCR-006, SCR-007, SCR-015, SCR-016, SCR-018, SCR-020, SCR-022

### 4.4 ChatBubble

- **Description**: Chat message bubble for AI intake conversation
- **Variants**: User (right-aligned, primary bg), AI (left-aligned, surface bg), System (centered, muted)
- **States**: Sent (solid), Received (with read indicator), Loading (typing animation dots)
- **Sizing**: Max 80% container width, min 40px height
- **Content**: Text, structured data preview, action buttons
- **Accessibility**: role="log", aria-live="polite", individual messages with role="article"
- **Screens**: SCR-010

## 5. Interactive Components

### 5.1 Button

- **Description**: Primary interactive trigger element
- **Variants**:
  - Primary: bg #1E6F9F, text white, 500 weight
  - Secondary: bg transparent, border #1E6F9F, text #1E6F9F
  - Ghost: bg transparent, text #1E6F9F (no border)
  - Danger: bg #D32F2F, text white
  - Icon-only: 40x40px square, icon centered
- **States**: Default, Hover (darken 8%), Active (darken 12%), Focused (2px focus ring), Disabled (40% opacity), Loading (spinner replacing text)
- **Sizing**: Height 40px (md), 32px (sm), 48px (lg); min-width 80px; padding 0 16px
- **Accessibility**: Descriptive text or aria-label, disabled state removes from tab flow
- **Screens**: All

### 5.2 TextField

- **Description**: Single-line text input with label and validation
- **Variants**: Standard, Password (with show/hide toggle), Search (with search icon + clear), Inline-edit (click to toggle edit mode)
- **States**: Default (border #D1D5DB), Focused (border #1E6F9F, ring), Filled (border #6B7280), Error (border #D32F2F + error message), Disabled (bg #F3F4F6, 60% opacity), ReadOnly (no border, text only)
- **Sizing**: Height 40px, width 100% parent, label 14px above, error 12px below
- **Validation**: Real-time on blur, submit-time batch validation, regex support
- **Accessibility**: label htmlFor, aria-describedby for error/help text, aria-invalid="true" on error
- **Screens**: SCR-001, SCR-002, SCR-004, SCR-005, SCR-011, SCR-013, SCR-019, SCR-023, SCR-025

### 5.3 Select

- **Description**: Dropdown selection with search/filter for long lists
- **Variants**: Single (one selection), Multi (tag-based multiple selection)
- **States**: Default, Open (dropdown visible, border primary), Selected (value shown), Error (border danger), Disabled
- **Options**: Max 7 visible in dropdown; scroll for more; search filter for >10 options
- **Accessibility**: role="combobox", aria-expanded, aria-activedescendant, keyboard arrow navigation
- **Screens**: SCR-004, SCR-011, SCR-018, SCR-023, SCR-025

### 5.4 DatePicker

- **Description**: Calendar-based date selection
- **Variants**: Single (one date), Range (start/end pair)
- **States**: Default (placeholder), Open (calendar popup), Selected (formatted date), Error
- **Calendar**: Month grid with today highlight, disabled past/future dates as needed
- **Accessibility**: aria-label on input, keyboard navigation in calendar grid, role="grid"
- **Screens**: SCR-004, SCR-008, SCR-025

### 5.5 FileDropzone

- **Description**: Drag-and-drop file upload area with click-to-browse fallback
- **Variants**: Standard (centered area, dashed border)
- **States**: Idle (dashed border, upload icon, "Drag files here" text), Drag-over (primary border, bg tint), Uploading (per-file progress bars), Success (checkmark per file), Error (red indicator per file)
- **Constraints**: Max 10 files, 25 MB each, PDF/JPEG/PNG/TIFF
- **Accessibility**: role="button" on dropzone, aria-label "Upload files", file list with status per item
- **Screens**: SCR-014

### 5.6 Toggle

- **Description**: Binary on/off switch for mode selection
- **Variants**: Standard (with on/off label)
- **States**: Off (gray track), On (primary track + white knob), Disabled (muted)
- **Usage**: AI/Manual intake mode switch
- **Accessibility**: role="switch", aria-checked, descriptive label
- **Screens**: SCR-010

## 6. Feedback Components

### 6.1 Modal

- **Description**: Overlay dialog for focused tasks without leaving context
- **Variants**: Standard (form/content), Confirmation (action + cancel)
- **States**: Open (visible with backdrop), Closing (fade-out animation)
- **Sizing**: max-width 560px, max-height 80vh, scrollable body
- **Behavior**: Focus trap, ESC to close, backdrop click to close (configurable)
- **Accessibility**: role="dialog", aria-modal="true", aria-labelledby, focus trap, return focus on close
- **Screens**: SCR-005 (booking), SCR-019 (new patient), SCR-023 (create/edit user)

### 6.2 Dialog

- **Description**: Confirmation dialog for destructive or critical actions
- **Variants**: Confirm (primary action), Destructive (danger action)
- **States**: Open, Closing
- **Sizing**: max-width 400px, fixed height
- **Content**: Title, message, cancel + confirm buttons
- **Accessibility**: role="alertdialog", aria-describedby for message
- **Screens**: SCR-008 (cancel appointment), SCR-018 (reject code), SCR-023 (deactivate user)

### 6.3 Toast

- **Description**: Transient notification message (auto-dismiss)
- **Variants**: Success (#2D9F83), Error (#D32F2F), Info (#1E6F9F), Warning (#EF6C00)
- **States**: Appearing (slide-in 200ms), Visible (3-5s), Dismissing (fade-out 150ms)
- **Position**: Top-right corner, stacked (max 3 visible)
- **Accessibility**: role="status", aria-live="polite", dismiss button
- **Screens**: SCR-005 (booking confirmed), SCR-006 (swap notification), SCR-020 (status update)

### 6.4 Alert

- **Description**: Persistent inline notification banner
- **Variants**: Info, Success, Warning, Error (each with semantic color from designsystem)
- **States**: Default (visible), Dismissable (with close button)
- **Sizing**: Full width of parent container, 16px padding
- **Content**: Icon + title + description + optional action link
- **Accessibility**: role="alert" (for errors/warnings), role="status" (for info/success)
- **Screens**: SCR-001 (registration errors), SCR-002 (login errors), SCR-010 (AI unavailable), SCR-016 (conflict alert)

### 6.5 Skeleton

- **Description**: Loading placeholder with shimmer animation
- **Variants**: Card (rounded rect), Table-row (horizontal bars), Text-block (line bars)
- **States**: Loading (shimmer animation 1.5s cycle)
- **Usage**: Replace content areas during API calls
- **Accessibility**: aria-busy="true" on parent, aria-label="Loading content"
- **Screens**: All data-driven screens

### 6.6 ProgressBar

- **Description**: Linear progress indicator for file uploads and processing
- **Variants**: Determinate (known percentage), Indeterminate (looping animation)
- **States**: Active (primary color fill), Complete (success color), Error (danger color)
- **Sizing**: 100% parent width, 8px height, border-radius-full
- **Accessibility**: role="progressbar", aria-valuenow, aria-valuemin, aria-valuemax, aria-label
- **Screens**: SCR-014 (upload progress), SCR-015 (extraction progress)

## 7. Component Relationships

### Composition Patterns

| Parent Component | Child Components                                       | Context             |
| ---------------- | ------------------------------------------------------ | ------------------- |
| Header           | Button (icon-only), Badge (notification count), Avatar | All authenticated   |
| Sidebar          | Link/Button (nav items), Badge (count), Divider        | All authenticated   |
| Card (Provider)  | Badge (availability), Button (book), Avatar (provider) | SCR-004             |
| Card (Summary)   | Badge (status), Icon, Text                             | Dashboards          |
| Table            | Badge (status), Button (actions), Checkbox (selection) | SCR-018,020,023,025 |
| Modal            | TextField, Select, Button, Alert                       | SCR-005,019,023     |
| FileDropzone     | ProgressBar, Badge (status), Button (retry/remove)     | SCR-014             |
| ChatBubble (AI)  | Card (structured data), Button (action)                | SCR-010             |

### States Matrix

| Component   | Default | Hover | Active | Focused | Disabled | Loading | Error | Empty |
| ----------- | ------- | ----- | ------ | ------- | -------- | ------- | ----- | ----- |
| Button      | Yes     | Yes   | Yes    | Yes     | Yes      | Yes     | -     | -     |
| TextField   | Yes     | -     | -      | Yes     | Yes      | -       | Yes   | -     |
| Select      | Yes     | -     | -      | Yes     | Yes      | -       | Yes   | -     |
| Card        | Yes     | Yes   | Yes    | -       | -        | Yes     | -     | -     |
| Table       | Yes     | -     | -      | -       | -        | Yes     | Yes   | Yes   |
| Modal       | Yes     | -     | -      | -       | -        | -       | -     | -     |
| ProgressBar | Yes     | -     | -      | -       | -        | Yes     | Yes   | -     |

## 8. Reusability Analysis

### High Reuse (>5 screens)

- **Button**: Used on all 24 screens - fully reusable with variant props
- **Badge**: Used on 8+ screens - status, count, confidence variants cover all cases
- **Card**: Used on 8+ screens - Standard base with specialized children per context
- **Alert**: Used on 4+ screens plus all error states - semantic variant system

### Medium Reuse (3-5 screens)

- **TextField**: Used on 9 screens - standard variant handles most; inline-edit for SCR-012
- **Table**: Used on 5 screens - sortable + actionable variants cover all needs
- **Select**: Used on 5 screens - single/multi system adequate
- **Tabs**: Used on 3 screens - consistent underline variant

### Low Reuse (1-2 screens)

- **ChatBubble**: SCR-010 only - specialized for AI intake
- **FileDropzone**: SCR-014 only - specialized for document upload
- **Toggle**: SCR-010 only - AI/manual mode switch
- **RadioGroup**: SCR-017 only - conflict resolution choice

## 9. Responsive Breakpoints

### Component Transformations

| Component  | Desktop (1440px)        | Tablet (768px)        | Mobile (390px)       |
| ---------- | ----------------------- | --------------------- | -------------------- |
| Sidebar    | 256px expanded          | 64px icon-only        | Hidden (BottomNav)   |
| Table      | Full columns visible    | Horizontal scroll     | Stacked card layout  |
| Card grid  | 3-4 columns             | 2 columns             | 1 column (stacked)   |
| Modal      | Centered 560px max      | Centered 90% width    | Full-screen sheet    |
| Tabs       | Horizontal, all visible | Horizontal, scroll    | Swipeable overflow   |
| DatePicker | Inline calendar popup   | Inline calendar popup | Full-screen calendar |
| ChatBubble | Max 80% width           | Max 85% width         | Max 90% width        |

## 10. Implementation Priority Matrix

### Phase 1 - Foundation (Sprint 1-2)

| Component       | Reason                   | Blocked By |
| --------------- | ------------------------ | ---------- |
| Button          | Required by all screens  | None       |
| TextField       | Required by auth + forms | None       |
| Header          | Layout shell             | None       |
| Sidebar         | Layout shell             | None       |
| Card (Standard) | Dashboard KPIs           | None       |
| Alert           | Error handling           | None       |
| Skeleton        | Loading states           | None       |

### Phase 2 - Core Flows (Sprint 3-4)

| Component  | Reason             | Blocked By  |
| ---------- | ------------------ | ----------- |
| Select     | Forms + filters    | TextField   |
| DatePicker | Booking + filters  | TextField   |
| Table      | Data views         | Card, Badge |
| Badge      | Status indicators  | None        |
| Tabs       | Section navigation | None        |
| Modal      | CRUD operations    | Button      |
| Toast      | Action feedback    | None        |

### Phase 3 - Specialized (Sprint 5-6)

| Component    | Reason               | Blocked By  |
| ------------ | -------------------- | ----------- |
| ChatBubble   | AI intake            | None        |
| FileDropzone | Doc upload           | ProgressBar |
| ProgressBar  | Upload/processing    | None        |
| Toggle       | Mode switch          | None        |
| RadioGroup   | Conflict resolution  | None        |
| Pagination   | Large datasets       | Table       |
| Dialog       | Destructive confirms | Modal       |

## 11. Framework Notes

### React + TypeScript Implementation

- **Component Library**: Custom components built on headless UI patterns (Radix UI primitives recommended)
- **Styling**: CSS Modules or Tailwind CSS with design token CSS custom properties
- **State Management**: Redux Toolkit for global state; React Hook Form for form state
- **Prop Patterns**: Discriminated unions for variants; controlled components for forms
- **Testing**: React Testing Library for component tests; Storybook for visual documentation

## 12. Accessibility Considerations

### ARIA Patterns Per Component

| Component   | ARIA Role    | Required Attributes                                   | Keyboard               |
| ----------- | ------------ | ----------------------------------------------------- | ---------------------- |
| Button      | button       | aria-label (icon-only), aria-disabled                 | Enter/Space            |
| TextField   | textbox      | aria-label/labelledby, aria-describedby, aria-invalid | Tab, typing            |
| Select      | combobox     | aria-expanded, aria-activedescendant, aria-owns       | Tab, Arrow, Enter, Esc |
| Tabs        | tablist/tab  | aria-selected, aria-controls                          | Arrow keys, Tab        |
| Modal       | dialog       | aria-modal, aria-labelledby                           | Tab trap, Esc to close |
| Dialog      | alertdialog  | aria-describedby                                      | Tab trap, Esc to close |
| Table       | table        | scope="col", aria-sort, aria-label                    | Tab, Arrow keys        |
| Alert       | alert/status | aria-live                                             | - (not interactive)    |
| Toast       | status       | aria-live="polite"                                    | Dismiss button         |
| ProgressBar | progressbar  | aria-valuenow, aria-valuemin, aria-valuemax           | - (not interactive)    |
| Toggle      | switch       | aria-checked                                          | Space/Enter            |

## 13. Design System Integration

### Token Mapping

All components consume design tokens from `designsystem.md` via CSS custom properties:

- **Colors**: `--color-primary`, `--color-secondary`, `--color-danger`, `--color-warning`
- **Typography**: `--font-family-primary` (Inter), `--font-size-*`, `--font-weight-*`
- **Spacing**: `--space-*` (4px increments from 4-64px)
- **Border Radius**: `--radius-sm` (4px), `--radius-md` (8px), `--radius-lg` (12px), `--radius-full` (9999px)
- **Elevation**: `--shadow-1` through `--shadow-5`
- **Motion**: `--duration-fast` (150ms), `--duration-normal` (200ms), `--duration-slow` (300ms)
