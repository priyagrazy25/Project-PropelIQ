---
post_title: "Unified Patient Access - Design Tokens Applied"
author1: "AI UX Designer"
post_slug: "unified-patient-access-tokens-applied"
categories: "Healthcare, UX, Design Tokens"
tags: "design-tokens, css-custom-properties, wireframes, high-fidelity"
ai_note: "Generated with AI assistance from designsystem.md"
summary: "Documentation of design tokens applied across all high-fidelity HTML wireframes with CSS custom property mappings."
post_date: "2026-04-15"
---

# Design Tokens Applied - Unified Patient Access

## 1. Token Source

All tokens sourced from `designsystem.md` and applied via CSS custom properties in each self-contained HTML wireframe file.

## 2. Color Tokens Applied

### Primitive Colors

| Token Name | CSS Variable         | Value   | Usage                                 |
| ---------- | -------------------- | ------- | ------------------------------------- |
| Blue 600   | `--color-blue-600`   | #1E6F9F | Primary buttons, active states, links |
| Blue 700   | `--color-blue-700`   | #175F87 | Primary hover                         |
| Blue 50    | `--color-blue-50`    | #E8F4FD | Info surfaces, selected bg            |
| Teal 600   | `--color-teal-600`   | #2D9F83 | Secondary accent, success indicators  |
| Teal 700   | `--color-teal-700`   | #24846D | Secondary hover                       |
| Red 600    | `--color-red-600`    | #D32F2F | Danger buttons, error states          |
| Red 50     | `--color-red-50`     | #FDECEC | Error surface backgrounds             |
| Orange 600 | `--color-orange-600` | #EF6C00 | Warning badges and alerts             |
| Orange 50  | `--color-orange-50`  | #FFF3E0 | Warning surface backgrounds           |
| Gray 900   | `--color-gray-900`   | #111827 | Primary text                          |
| Gray 700   | `--color-gray-700`   | #374151 | Secondary text, labels                |
| Gray 500   | `--color-gray-500`   | #6B7280 | Placeholder text, muted               |
| Gray 300   | `--color-gray-300`   | #D1D5DB | Borders, dividers                     |
| Gray 100   | `--color-gray-100`   | #F3F4F6 | Background surfaces, disabled         |
| Gray 50    | `--color-gray-50`    | #F9FAFB | Page background                       |
| White      | `--color-white`      | #FFFFFF | Card backgrounds, button text         |

### Semantic Colors

| Token Name         | CSS Variable             | Value                   | Applied To                                    |
| ------------------ | ------------------------ | ----------------------- | --------------------------------------------- |
| Primary            | `--color-primary`        | var(--color-blue-600)   | Buttons, links, active nav items, focus rings |
| Primary Hover      | `--color-primary-hover`  | var(--color-blue-700)   | Button hover, link hover                      |
| Secondary          | `--color-secondary`      | var(--color-teal-600)   | Secondary actions, success badges             |
| Danger             | `--color-danger`         | var(--color-red-600)    | Delete buttons, error messages                |
| Warning            | `--color-warning`        | var(--color-orange-600) | Warning badges, caution alerts                |
| Text Primary       | `--color-text-primary`   | var(--color-gray-900)   | Headings, body text                           |
| Text Secondary     | `--color-text-secondary` | var(--color-gray-700)   | Labels, descriptions                          |
| Text Muted         | `--color-text-muted`     | var(--color-gray-500)   | Placeholders, captions                        |
| Border Default     | `--color-border`         | var(--color-gray-300)   | Input borders, dividers                       |
| Surface Default    | `--color-surface`        | var(--color-white)      | Cards, modals                                 |
| Surface Background | `--color-bg`             | var(--color-gray-50)    | Page backgrounds                              |

### Surface Colors

| Token Name      | CSS Variable        | Value                  | Applied To                          |
| --------------- | ------------------- | ---------------------- | ----------------------------------- |
| Surface Info    | `--surface-info`    | var(--color-blue-50)   | Info alerts, info badges bg         |
| Surface Success | `--surface-success` | #E6F7F1                | Success alerts, confirmed badges bg |
| Surface Warning | `--surface-warning` | var(--color-orange-50) | Warning alerts, warning badges bg   |
| Surface Danger  | `--surface-danger`  | var(--color-red-50)    | Error alerts, error badges bg       |
| Surface Hover   | `--surface-hover`   | var(--color-gray-100)  | Row hover, nav item hover           |

### Confidence Score Colors (Clinical AI)

| Token Name        | CSS Variable          | Value   | Applied To                      |
| ----------------- | --------------------- | ------- | ------------------------------- |
| Confidence High   | `--confidence-high`   | #2D9F83 | >=90% scores (SCR-016, SCR-018) |
| Confidence Medium | `--confidence-medium` | #EF6C00 | 70-89% scores                   |
| Confidence Low    | `--confidence-low`    | #D32F2F | <70% scores                     |

## 3. Typography Tokens Applied

| Token Name | CSS Variable     | Font           | Size    | Weight         | Line Height | Applied To                  |
| ---------- | ---------------- | -------------- | ------- | -------------- | ----------- | --------------------------- |
| H1         | `--text-h1`      | Inter          | 32px    | 700 (Bold)     | 40px        | Page titles                 |
| H2         | `--text-h2`      | Inter          | 24px    | 600 (Semibold) | 32px        | Section headings            |
| H3         | `--text-h3`      | Inter          | 20px    | 600 (Semibold) | 28px        | Card titles, panel headings |
| H4         | `--text-h4`      | Inter          | 18px    | 600 (Semibold) | 24px        | Sub-section headings        |
| Body       | `--text-body`    | Inter          | 16px    | 400 (Regular)  | 24px        | Paragraph text, form fields |
| Body Small | `--text-body-sm` | Inter          | 14px    | 400 (Regular)  | 20px        | Labels, secondary text      |
| Caption    | `--text-caption` | Inter          | 12px    | 400 (Regular)  | 16px        | Timestamps, metadata        |
| Mono       | `--text-mono`    | JetBrains Mono | 14px    | 400 (Regular)  | 20px        | ICD-10/CPT codes            |
| Button     | `--text-button`  | Inter          | 14px    | 500 (Medium)   | 20px        | Button labels               |
| Link       | `--text-link`    | Inter          | inherit | 500 (Medium)   | inherit     | Interactive links           |

### Font Family Definitions

```css
--font-primary:
  "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
--font-mono: "JetBrains Mono", "Fira Code", "Consolas", monospace;
```

## 4. Spacing Tokens Applied

| Token Name | CSS Variable | Value | Applied To                               |
| ---------- | ------------ | ----- | ---------------------------------------- |
| Space 1    | `--space-1`  | 4px   | Icon padding, badge internal             |
| Space 2    | `--space-2`  | 8px   | Inline element gaps, compact padding     |
| Space 3    | `--space-3`  | 12px  | Form field gap, small card padding       |
| Space 4    | `--space-4`  | 16px  | Card padding, section gaps, input height |
| Space 5    | `--space-5`  | 20px  | Medium section spacing                   |
| Space 6    | `--space-6`  | 24px  | Large section spacing, card gap          |
| Space 8    | `--space-8`  | 32px  | Page section margins                     |
| Space 10   | `--space-10` | 40px  | Major section breaks                     |
| Space 12   | `--space-12` | 48px  | Page top/bottom margins                  |
| Space 16   | `--space-16` | 64px  | Sidebar width collapsed                  |

## 5. Border and Radius Tokens Applied

| Token Name   | CSS Variable     | Value  | Applied To                       |
| ------------ | ---------------- | ------ | -------------------------------- |
| Radius SM    | `--radius-sm`    | 4px    | Badges, small buttons, chips     |
| Radius MD    | `--radius-md`    | 8px    | Cards, modals, inputs, dropdowns |
| Radius LG    | `--radius-lg`    | 12px   | Large panels, page sections      |
| Radius Full  | `--radius-full`  | 9999px | Avatars, pills, toggles          |
| Border Width | `--border-width` | 1px    | Input borders, card borders      |
| Border Focus | `--border-focus` | 2px    | Focus ring width                 |

## 6. Elevation Tokens Applied

| Token Name | CSS Variable | Value                        | Applied To               |
| ---------- | ------------ | ---------------------------- | ------------------------ |
| Shadow 1   | `--shadow-1` | 0 1px 3px rgba(0,0,0,0.1)    | Cards (default), sidebar |
| Shadow 2   | `--shadow-2` | 0 4px 6px rgba(0,0,0,0.1)    | Cards (hover), dropdowns |
| Shadow 3   | `--shadow-3` | 0 10px 15px rgba(0,0,0,0.1)  | Modals, dialogs          |
| Shadow 4   | `--shadow-4` | 0 20px 25px rgba(0,0,0,0.15) | Popovers                 |
| Shadow 5   | `--shadow-5` | 0 25px 50px rgba(0,0,0,0.25) | Full-screen overlays     |

## 7. Motion Tokens Applied

| Token Name      | CSS Variable        | Value                        | Applied To                           |
| --------------- | ------------------- | ---------------------------- | ------------------------------------ |
| Duration Fast   | `--duration-fast`   | 150ms                        | Hover transitions, button feedback   |
| Duration Normal | `--duration-normal` | 200ms                        | Modal open/close, slide transitions  |
| Duration Slow   | `--duration-slow`   | 300ms                        | Page transitions, complex animations |
| Easing Default  | `--easing-default`  | cubic-bezier(0.4, 0, 0.2, 1) | General transitions                  |
| Easing In       | `--easing-in`       | cubic-bezier(0.4, 0, 1, 1)   | Elements entering view               |
| Easing Out      | `--easing-out`      | cubic-bezier(0, 0, 0.2, 1)   | Elements leaving view                |

## 8. Grid Tokens Applied

| Token Name           | CSS Variable          | Value  | Applied To                                    |
| -------------------- | --------------------- | ------ | --------------------------------------------- |
| Grid Columns Desktop | `--grid-cols-desktop` | 12     | 1440px layouts                                |
| Grid Columns Tablet  | `--grid-cols-tablet`  | 8      | 768px layouts                                 |
| Grid Columns Mobile  | `--grid-cols-mobile`  | 4      | 390px layouts                                 |
| Grid Gutter          | `--grid-gutter`       | 24px   | Column gaps                                   |
| Grid Margin          | `--grid-margin`       | 32px   | Page edge margins                             |
| Content Max Width    | `--content-max-width` | 1152px | Content area (1440 - 256 sidebar - 32 margin) |
| Sidebar Width        | `--sidebar-width`     | 256px  | Desktop sidebar                               |
| Sidebar Collapsed    | `--sidebar-collapsed` | 64px   | Tablet sidebar                                |
| Header Height        | `--header-height`     | 64px   | Fixed header                                  |
| Bottom Nav Height    | `--bottomnav-height`  | 56px   | Mobile bottom nav                             |

## 9. Component-Specific Token Usage

### Button Tokens

| Property      | Primary                    | Secondary                      | Ghost                | Danger              |
| ------------- | -------------------------- | ------------------------------ | -------------------- | ------------------- |
| Background    | var(--color-primary)       | transparent                    | transparent          | var(--color-danger) |
| Border        | none                       | 1px solid var(--color-primary) | none                 | none                |
| Text Color    | var(--color-white)         | var(--color-primary)           | var(--color-primary) | var(--color-white)  |
| Hover BG      | var(--color-primary-hover) | var(--surface-info)            | var(--surface-hover) | #B71C1C             |
| Border Radius | var(--radius-md)           | var(--radius-md)               | var(--radius-md)     | var(--radius-md)    |
| Height        | 40px                       | 40px                           | 40px                 | 40px                |
| Font          | var(--text-button)         | var(--text-button)             | var(--text-button)   | var(--text-button)  |

### Input Tokens

| Property      | Default                       | Focused                        | Error                         | Disabled                        |
| ------------- | ----------------------------- | ------------------------------ | ----------------------------- | ------------------------------- |
| Background    | var(--color-white)            | var(--color-white)             | var(--color-white)            | var(--color-gray-100)           |
| Border        | 1px solid var(--color-border) | 2px solid var(--color-primary) | 2px solid var(--color-danger) | 1px solid var(--color-gray-300) |
| Text          | var(--color-text-primary)     | var(--color-text-primary)      | var(--color-text-primary)     | var(--color-text-muted)         |
| Border Radius | var(--radius-md)              | var(--radius-md)               | var(--radius-md)              | var(--radius-md)                |
| Height        | 40px                          | 40px                           | 40px                          | 40px                            |

### Card Tokens

| Property      | Default                       | Hover                         | Selected                       |
| ------------- | ----------------------------- | ----------------------------- | ------------------------------ |
| Background    | var(--color-surface)          | var(--color-surface)          | var(--color-surface)           |
| Border        | 1px solid var(--color-border) | 1px solid var(--color-border) | 2px solid var(--color-primary) |
| Shadow        | var(--shadow-1)               | var(--shadow-2)               | var(--shadow-1)                |
| Border Radius | var(--radius-md)              | var(--radius-md)              | var(--radius-md)               |
| Padding       | var(--space-4)                | var(--space-4)                | var(--space-4)                 |

### Badge Tokens

| Variant | Background             | Text Color             | Border Radius      |
| ------- | ---------------------- | ---------------------- | ------------------ |
| Info    | var(--surface-info)    | var(--color-primary)   | var(--radius-full) |
| Success | var(--surface-success) | var(--color-secondary) | var(--radius-full) |
| Warning | var(--surface-warning) | var(--color-warning)   | var(--radius-full) |
| Danger  | var(--surface-danger)  | var(--color-danger)    | var(--radius-full) |

## 10. Token Coverage by Screen

| Screen  | Colors                              | Typography                       | Spacing | Radius       | Shadow | Motion       | Grid   |
| ------- | ----------------------------------- | -------------------------------- | ------- | ------------ | ------ | ------------ | ------ |
| SCR-001 | Primary, Danger, Text, Border       | H1, Body, Body-SM, Caption       | 2-8     | SM, MD       | 1,3    | Fast, Normal | 12-col |
| SCR-002 | Primary, Danger, Text, Border       | H1, Body, Body-SM, Link          | 2-8     | SM, MD       | 1,3    | Fast, Normal | 12-col |
| SCR-004 | Primary, Secondary, Text, Border    | H1, H3, Body, Body-SM, Caption   | 2-8     | SM, MD, Full | 1,2    | Fast         | 12-col |
| SCR-005 | Primary, Secondary, Text, Border    | H1, H3, Body, Body-SM            | 2-8     | MD, Full     | 1,3    | Normal       | 12-col |
| SCR-006 | Primary, Sec, Warn, Text, Border    | H1, H2, Body, Caption            | 2-8     | SM, MD, Full | 1,2    | Fast         | 12-col |
| SCR-007 | Primary, Warning, Text, Border      | H1, Body, Body-SM, Caption       | 2-6     | SM, MD, Full | 1      | Fast         | 12-col |
| SCR-008 | Primary, Danger, Text, Border       | H1, Body, Body-SM                | 2-8     | MD           | 1,3    | Normal       | 12-col |
| SCR-009 | Primary, Text, Border               | H1, Body, Body-SM                | 2-6     | MD, LG       | 1,2    | Normal       | 12-col |
| SCR-010 | Primary, Sec, Warn, Text            | H1, Body, Body-SM, Caption       | 2-6     | MD, LG, Full | 1      | Fast, Normal | 12-col |
| SCR-011 | Primary, Danger, Text, Border       | H1, H2, Body, Body-SM            | 2-8     | MD           | 1      | Fast         | 12-col |
| SCR-012 | Primary, Sec, Text, Border          | H1, H2, Body, Body-SM, Caption   | 2-8     | MD, Full     | 1,2    | Fast         | 12-col |
| SCR-013 | Primary, Sec, Danger, Text, Border  | H1, Body, Body-SM                | 2-6     | MD, Full     | 1      | Fast         | 12-col |
| SCR-014 | Primary, Sec, Danger, Text, Border  | H1, Body, Body-SM, Caption       | 2-8     | MD           | 1      | Normal       | 12-col |
| SCR-015 | Primary, Sec, Warn, Text, Border    | H1, Body, Body-SM, Caption       | 2-6     | MD, Full     | 1      | Normal, Slow | 12-col |
| SCR-016 | All semantic + Confidence           | H1, H2, H3, Body, Caption, Mono  | 2-10    | SM, MD, Full | 1,2    | Fast         | 12-col |
| SCR-017 | Primary, Danger, Warn, Text, Border | H1, H2, Body, Body-SM            | 2-8     | MD           | 1      | Fast, Normal | 12-col |
| SCR-018 | Primary, Sec, Danger, Warn, Text    | H1, Body, Body-SM, Mono, Caption | 2-8     | SM, MD, Full | 1,2,3  | Fast         | 12-col |
| SCR-019 | Primary, Text, Border               | H1, Body, Body-SM                | 2-6     | MD           | 1,3    | Fast, Normal | 12-col |
| SCR-020 | Primary, Sec, Warn, Danger, Text    | H1, Body, Body-SM, Caption       | 2-6     | SM, MD, Full | 1      | Fast         | 12-col |
| SCR-021 | Primary, Sec, Warn, Text, Border    | H1, H2, Body, Caption            | 2-8     | SM, MD, Full | 1,2    | Fast         | 12-col |
| SCR-022 | Primary, Danger, Warn, Text         | H1, Body, Body-SM, Caption       | 2-6     | SM, MD, Full | 1      | Fast         | 12-col |
| SCR-023 | Primary, Danger, Text, Border       | H1, Body, Body-SM                | 2-8     | MD           | 1,2,3  | Fast, Normal | 12-col |
| SCR-024 | Primary, Sec, Warn, Text, Border    | H1, H2, Body, Caption            | 2-8     | SM, MD, Full | 1,2    | Fast         | 12-col |
| SCR-025 | Primary, Text, Border               | H1, Body, Body-SM, Mono, Caption | 2-8     | SM, MD       | 1      | Fast         | 12-col |
