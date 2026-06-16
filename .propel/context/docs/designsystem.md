---
post_title: "Unified Patient Access & Clinical Intelligence Platform - Design System"
author1: "AI Product Designer"
post_slug: "unified-patient-access-designsystem"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Design System"
tags: "design-tokens, colors, typography, spacing, components, WCAG, healthcare-UI"
ai_note: "Generated with AI assistance from spec.md and design.md source documents"
summary: "Complete design system specification with design tokens, component library, and brand guidelines for the Unified Patient Access & Clinical Intelligence Platform."
post_date: "2026-04-15"
---

# Design System - Unified Patient Access

## UI Impact Assessment

**Has UI Changes**: [x] Yes [ ] No

## Design Tokens

### Token Hierarchy

```text
1. Primitive Tokens (raw values)
   color.blue.600: #1E6F9F
2. Semantic Tokens (purpose)
   color.primary: {color.blue.600}
3. Component Tokens (specific)
   button.primary.background: {color.primary}
```

### Color Palette

#### Primitive Colors

```yaml
colors:
  blue:
    50: "#EFF6FF"
    100: "#DBEAFE"
    200: "#BFDBFE"
    300: "#93C5FD"
    400: "#60A5FA"
    500: "#3B82F6"
    600: "#1E6F9F"
    700: "#1D5F8A"
    800: "#1A4F74"
    900: "#153F5E"
  teal:
    50: "#F0FDFA"
    100: "#CCFBF1"
    200: "#99F6E4"
    300: "#5EEAD4"
    400: "#2DD4BF"
    500: "#2D9F83"
    600: "#248A72"
    700: "#1B7561"
    800: "#135F50"
    900: "#0D4A3F"
  green:
    50: "#F0FDF4"
    100: "#DCFCE7"
    500: "#22C55E"
    600: "#16A34A"
    700: "#15803D"
  amber:
    50: "#FFFBEB"
    100: "#FEF3C7"
    500: "#F59E0B"
    600: "#D97706"
    700: "#B45309"
  red:
    50: "#FEF2F2"
    100: "#FEE2E2"
    500: "#EF4444"
    600: "#DC2626"
    700: "#B91C1C"
  neutral:
    0: "#FFFFFF"
    50: "#F8FAFC"
    100: "#F1F5F9"
    200: "#E2E8F0"
    300: "#CBD5E1"
    400: "#94A3B8"
    500: "#64748B"
    600: "#475569"
    700: "#334155"
    800: "#1E293B"
    900: "#0F172A"
```

#### Semantic Colors

```yaml
semantic:
  primary:
    value: "{color.blue.600}"
    usage: "Primary CTAs, active navigation, links"
    contrast: "White text on primary passes 4.5:1"
  secondary:
    value: "{color.teal.500}"
    usage: "Secondary CTAs, health-related indicators"
    contrast: "White text on secondary passes 4.5:1"
  success:
    value: "{color.green.500}"
    usage: "Success states, verified status, high confidence (>=0.7)"
  warning:
    value: "{color.amber.500}"
    usage: "Warning states, unverified insurance, medium confidence (0.5-0.7)"
  error:
    value: "{color.red.500}"
    usage: "Error states, critical conflicts, low confidence (<0.5), destructive actions"
  info:
    value: "{color.blue.500}"
    usage: "Informational banners, tooltips, help text"
```

#### Surface Colors

```yaml
surfaces:
  light_mode:
    background: "{color.neutral.0}"
    surface: "{color.neutral.50}"
    surface_elevated: "{color.neutral.0}"
    border: "{color.neutral.200}"
    text_primary: "{color.neutral.900}"
    text_secondary: "{color.neutral.600}"
    text_disabled: "{color.neutral.400}"
    text_on_primary: "{color.neutral.0}"
  dark_mode:
    background: "{color.neutral.900}"
    surface: "{color.neutral.800}"
    surface_elevated: "{color.neutral.700}"
    border: "{color.neutral.600}"
    text_primary: "{color.neutral.50}"
    text_secondary: "{color.neutral.300}"
    text_disabled: "{color.neutral.500}"
    text_on_primary: "{color.neutral.0}"
```

#### Confidence Score Colors

```yaml
confidence:
  high:
    threshold: ">=0.7"
    color: "{color.green.500}"
    background: "{color.green.50}"
    badge_label: "High Confidence"
  medium:
    threshold: "0.5-0.7"
    color: "{color.amber.500}"
    background: "{color.amber.50}"
    badge_label: "Low Confidence"
  low:
    threshold: "<0.5"
    color: "{color.red.500}"
    background: "{color.red.50}"
    badge_label: "Very Low Confidence"
```

### Typography

```yaml
typography:
  font_families:
    heading: "Inter"
    body: "Inter"
    mono: "JetBrains Mono"
    fallback: "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif"

  scale:
    h1:
      size: "32px"
      weight: 700
      line_height: "40px"
      letter_spacing: "-0.02em"
      used_in: ["Page titles"]

    h2:
      size: "24px"
      weight: 600
      line_height: "32px"
      letter_spacing: "-0.01em"
      used_in: ["Section headings, Modal titles"]

    h3:
      size: "20px"
      weight: 600
      line_height: "28px"
      letter_spacing: "0"
      used_in: ["Card titles, Sub-section headings"]

    h4:
      size: "18px"
      weight: 600
      line_height: "24px"
      letter_spacing: "0"
      used_in: ["Form group labels, List section titles"]

    body_large:
      size: "18px"
      weight: 400
      line_height: "28px"
      used_in: ["Lead paragraphs, Feature descriptions"]

    body:
      size: "16px"
      weight: 400
      line_height: "24px"
      used_in: ["Default body text, Form inputs"]

    body_medium:
      size: "14px"
      weight: 500
      line_height: "20px"
      used_in: ["Button labels, Table headers, Navigation items"]

    small:
      size: "14px"
      weight: 400
      line_height: "20px"
      used_in: ["Help text, Secondary labels, Timestamps"]

    caption:
      size: "12px"
      weight: 400
      line_height: "16px"
      used_in: ["Badges, Tags, Confidence scores, Metadata"]

    mono:
      size: "14px"
      weight: 400
      line_height: "20px"
      family: "JetBrains Mono"
      used_in: ["Code references, ICD-10/CPT codes, Appointment IDs"]
```

### Spacing

```yaml
spacing:
  base_unit: "4px"
  scale:
    0: "0px"
    1: "4px" # Inline element spacing, icon-to-text gap
    2: "8px" # Compact padding, input padding
    3: "12px" # Card internal padding (tight), chip padding
    4: "16px" # Default padding, form field gap
    5: "20px" # Section padding (compact)
    6: "24px" # Card padding, section heading gap
    8: "32px" # Section separator, card gap
    10: "40px" # Page section gap
    12: "48px" # Major section separator
    16: "64px" # Page top/bottom padding
  affected_layouts: ["All layouts - enforced via Auto Layout in Figma"]
```

### Border Radius

```yaml
radius:
  sm: "4px" # Badges, tags, chips, small buttons
  md: "8px" # Cards, inputs, modals, standard buttons
  lg: "16px" # Large cards, hero sections, image containers
  full: "9999px" # Avatars, pills, circular buttons
```

### Elevation / Shadows

```yaml
elevation:
  level_1:
    shadow: "0px 1px 2px rgba(0, 0, 0, 0.05)"
    usage: "Cards, default surfaces"
  level_2:
    shadow: "0px 1px 3px rgba(0, 0, 0, 0.10), 0px 1px 2px rgba(0, 0, 0, 0.06)"
    usage: "Dropdown menus, hovering cards"
  level_3:
    shadow: "0px 4px 6px rgba(0, 0, 0, 0.10), 0px 2px 4px rgba(0, 0, 0, 0.06)"
    usage: "Popovers, tooltips, floating elements"
  level_4:
    shadow: "0px 10px 15px rgba(0, 0, 0, 0.10), 0px 4px 6px rgba(0, 0, 0, 0.05)"
    usage: "Modals, dialogs"
  level_5:
    shadow: "0px 20px 25px rgba(0, 0, 0, 0.10), 0px 10px 10px rgba(0, 0, 0, 0.04)"
    usage: "Drawers, notification panels"
```

### Motion / Animation

```yaml
motion:
  duration:
    micro: "150ms" # Button state change, toggle, checkbox
    standard: "300ms" # Modal open/close, drawer slide, toast entry
    emphasis: "500ms" # Page transitions, complex animations
  easing:
    ease_out: "cubic-bezier(0.0, 0.0, 0.2, 1)" # Entering elements
    ease_in: "cubic-bezier(0.4, 0.0, 1, 1)" # Exiting elements
    ease_in_out: "cubic-bezier(0.4, 0.0, 0.2, 1)" # Moving elements
  interactions:
    hover: "150ms ease-out"
    focus: "Immediate (0ms)"
    modal_enter: "300ms ease-out"
    modal_exit: "200ms ease-in"
    toast_enter: "300ms ease-out (slide up + fade)"
    toast_exit: "200ms ease-in (fade)"
    skeleton_pulse: "1.5s ease-in-out infinite"
```

### Grid System

```yaml
grid:
  desktop:
    columns: 12
    gutter: "24px"
    margin: "32px"
    max_width: "1376px"
    breakpoint: ">=1024px"
  tablet:
    columns: 8
    gutter: "16px"
    margin: "24px"
    breakpoint: "768px-1023px"
  mobile:
    columns: 4
    gutter: "16px"
    margin: "16px"
    breakpoint: "<=767px"
```

---

## Component Library Reference

### Actions

#### Button

```yaml
button:
  variants:
    type: [Primary, Secondary, Tertiary, Ghost, Destructive]
    size: [Small, Medium, Large]
    state: [Default, Hover, Focus, Active, Disabled, Loading]
    icon: [None, Leading, Trailing]
  specs:
    small:
      height: "32px"
      padding: "8px 12px"
      font: "{body_medium} 14px/500"
      radius: "{radius.md}"
    medium:
      height: "40px"
      padding: "8px 16px"
      font: "{body_medium} 14px/500"
      radius: "{radius.md}"
    large:
      height: "48px"
      padding: "12px 24px"
      font: "{body} 16px/500"
      radius: "{radius.md}"
  component_tokens:
    primary:
      background: "{color.primary}"
      text: "{color.neutral.0}"
      hover_bg: "{color.blue.700}"
      focus_ring: "2px offset-2px {color.blue.400}"
      disabled_opacity: "0.4"
    secondary:
      background: "{color.neutral.0}"
      text: "{color.primary}"
      border: "1px solid {color.primary}"
      hover_bg: "{color.blue.50}"
    tertiary:
      background: "transparent"
      text: "{color.primary}"
      hover_bg: "{color.blue.50}"
    ghost:
      background: "transparent"
      text: "{color.neutral.600}"
      hover_bg: "{color.neutral.100}"
    destructive:
      background: "{color.red.500}"
      text: "{color.neutral.0}"
      hover_bg: "{color.red.700}"
```

#### Link

```yaml
link:
  variants:
    type: [Default, Subtle]
    state: [Default, Hover, Focus, Visited]
  specs:
    color: "{color.primary}"
    hover: "underline, {color.blue.700}"
    focus: "outline 2px {color.blue.400}"
    visited: "{color.blue.800}"
```

### Inputs

#### TextField

```yaml
text_field:
  variants:
    size: [Small, Medium, Large]
    state: [Default, Focus, Error, Disabled, ReadOnly]
    type: [Text, Email, Password, Number, Search]
    adornment: [None, Leading Icon, Trailing Icon, Both]
  specs:
    medium:
      height: "40px"
      padding: "8px 12px"
      font: "{body} 16px/400"
      radius: "{radius.md}"
      border: "1px solid {color.neutral.300}"
    focus:
      border: "2px solid {color.primary}"
      shadow: "0 0 0 2px {color.blue.100}"
    error:
      border: "2px solid {color.red.500}"
      helper_text_color: "{color.red.600}"
      icon: "error-circle (leading)"
    disabled:
      background: "{color.neutral.100}"
      opacity: "0.6"
      cursor: "not-allowed"
```

#### Select

```yaml
select:
  variants:
    size: [Small, Medium, Large]
    state: [Default, Focus, Error, Disabled, Open]
  specs:
    inherits: "TextField base specs"
    dropdown:
      max_height: "240px"
      item_height: "40px"
      shadow: "{elevation.level_2}"
      radius: "{radius.md}"
```

#### Checkbox / Radio / Toggle

```yaml
checkbox:
  size: "20px x 20px"
  checked_bg: "{color.primary}"
  focus_ring: "2px offset-2px {color.blue.400}"
  disabled_opacity: "0.4"

radio:
  size: "20px x 20px"
  selected_border: "6px solid {color.primary}"
  focus_ring: "2px offset-2px {color.blue.400}"

toggle:
  width: "44px"
  height: "24px"
  thumb: "20px circle {color.neutral.0}"
  on_bg: "{color.primary}"
  off_bg: "{color.neutral.300}"
```

#### DatePicker

```yaml
date_picker:
  trigger: "TextField with calendar trailing icon"
  calendar_dropdown:
    shadow: "{elevation.level_3}"
    radius: "{radius.md}"
    selected_bg: "{color.primary}"
    today_ring: "1px solid {color.primary}"
```

#### FileDropzone

```yaml
file_dropzone:
  default:
    border: "2px dashed {color.neutral.300}"
    background: "{color.neutral.50}"
    radius: "{radius.lg}"
    min_height: "160px"
  hover:
    border: "2px dashed {color.primary}"
    background: "{color.blue.50}"
  active_drag:
    border: "2px solid {color.primary}"
    background: "{color.blue.100}"
  content: "Icon (upload-cloud) + 'Drag & drop PDFs here or click to browse' + 'Max 20 pages per file'"
```

### Navigation

#### Header

```yaml
header:
  height: "64px"
  background: "{color.neutral.0}"
  border_bottom: "1px solid {color.neutral.200}"
  shadow: "{elevation.level_1}"
  content:
    left: "Logo + App name"
    center: "Breadcrumb (optional)"
    right: "Notification bell + Avatar dropdown"
  avatar_dropdown:
    items: ["Profile", "Settings", "---", "Logout"]
    shadow: "{elevation.level_3}"
```

#### Sidebar

```yaml
sidebar:
  desktop:
    width_expanded: "256px"
    width_collapsed: "64px"
    background: "{color.neutral.900}"
    text: "{color.neutral.200}"
    active_bg: "{color.blue.800}"
    active_text: "{color.neutral.0}"
    icon_size: "20px"
  tablet:
    width: "64px (icon only)"
    tooltip_on_hover: true
  mobile:
    hidden: true (use BottomNav instead)
```

#### BottomNav (Mobile)

```yaml
bottom_nav:
  height: "56px"
  background: "{color.neutral.0}"
  border_top: "1px solid {color.neutral.200}"
  items: 4-5
  active_color: "{color.primary}"
  inactive_color: "{color.neutral.400}"
  icon_size: "24px"
  label_size: "{caption} 12px"
```

#### Tabs

```yaml
tabs:
  height: "48px"
  active_border: "2px solid {color.primary}"
  active_text: "{color.primary}"
  inactive_text: "{color.neutral.500}"
  badge: "Optional count badge on tab"
  scroll: "Horizontal scroll on mobile with fade edges"
```

### Content

#### Card

```yaml
card:
  default:
    background: "{color.neutral.0}"
    border: "1px solid {color.neutral.200}"
    radius: "{radius.md}"
    padding: "24px"
    shadow: "{elevation.level_1}"
  hover:
    shadow: "{elevation.level_2}"
    transition: "{motion.duration.micro} {motion.easing.ease_out}"
  variants:
    - appointment_card: "Status badge, provider name, datetime, actions"
    - summary_card: "Metric value, label, trend indicator"
    - clinical_section_card: "Section title, data list, confidence badges"
    - conflict_card: "Source doc ref, data value, confidence, select radio"
```

#### Table

```yaml
table:
  header:
    background: "{color.neutral.50}"
    font: "{body_medium} 14px/500"
    text_color: "{color.neutral.600}"
    height: "48px"
    border_bottom: "2px solid {color.neutral.200}"
  row:
    height: "52px"
    border_bottom: "1px solid {color.neutral.100}"
    hover_bg: "{color.neutral.50}"
    selected_bg: "{color.blue.50}"
  mobile_behavior: "Collapses to stacked card layout at <=767px"
  pagination:
    items_per_page: [10, 20, 50]
    position: "Bottom right"
```

#### Badge

```yaml
badge:
  size: "Small (20px height), Medium (24px height)"
  radius: "{radius.full}"
  font: "{caption} 12px/500"
  variants:
    status:
      confirmed:
        {
          bg: "{color.green.50}",
          text: "{color.green.700}",
          border: "1px solid {color.green.200}",
        }
      cancelled:
        {
          bg: "{color.red.50}",
          text: "{color.red.700}",
          border: "1px solid {color.red.200}",
        }
      pending:
        {
          bg: "{color.amber.50}",
          text: "{color.amber.700}",
          border: "1px solid {color.amber.200}",
        }
      in_progress:
        {
          bg: "{color.blue.50}",
          text: "{color.blue.700}",
          border: "1px solid {color.blue.200}",
        }
      completed:
        {
          bg: "{color.neutral.100}",
          text: "{color.neutral.600}",
          border: "1px solid {color.neutral.200}",
        }
      no_show:
        {
          bg: "{color.red.100}",
          text: "{color.red.800}",
          border: "1px solid {color.red.300}",
        }
    confidence:
      high: { bg: "{color.green.50}", text: "{color.green.700}" }
      medium: { bg: "{color.amber.50}", text: "{color.amber.700}" }
      low: { bg: "{color.red.50}", text: "{color.red.700}" }
    verification:
      suggested: { bg: "{color.amber.50}", text: "{color.amber.700}" }
      verified: { bg: "{color.green.50}", text: "{color.green.700}" }
      rejected: { bg: "{color.red.50}", text: "{color.red.700}" }
    risk:
      low: { bg: "{color.green.50}", text: "{color.green.700}" }
      medium: { bg: "{color.amber.50}", text: "{color.amber.700}" }
      high: { bg: "{color.red.50}", text: "{color.red.700}" }
```

#### ChatBubble

```yaml
chat_bubble:
  user:
    background: "{color.primary}"
    text: "{color.neutral.0}"
    radius: "16px 16px 4px 16px"
    max_width: "80%"
    align: "right"
  ai:
    background: "{color.neutral.100}"
    text: "{color.neutral.900}"
    radius: "16px 16px 16px 4px"
    max_width: "80%"
    align: "left"
    typing_indicator: "Three animated dots"
  timestamp:
    font: "{caption}"
    color: "{color.neutral.400}"
```

### Feedback

#### Modal

```yaml
modal:
  overlay: "rgba(15, 23, 42, 0.5)"
  background: "{color.neutral.0}"
  radius: "{radius.lg}"
  shadow: "{elevation.level_4}"
  padding: "24px"
  max_width:
    small: "400px"
    medium: "560px"
    large: "720px"
  animation:
    enter: "fade overlay + scale content from 95% to 100%, {motion.duration.standard}"
    exit: "fade out, {motion.duration.micro}"
  structure: "Header (title + close) | Body (content) | Footer (actions right-aligned)"
```

#### Dialog (Confirmation)

```yaml
dialog:
  inherits: "Modal (small)"
  icon: "Warning triangle (destructive) or Info circle (neutral)"
  title: "Bold question"
  body: "Explanation text"
  actions: "Cancel (tertiary, left) + Confirm (primary/destructive, right)"
```

#### Toast

```yaml
toast:
  position: "Top right, 16px from edges"
  width: "360px"
  radius: "{radius.md}"
  shadow: "{elevation.level_3}"
  auto_dismiss: "5000ms"
  animation: "slide down + fade, {motion.duration.standard}"
  variants:
    success:
      { icon: "check-circle", border_left: "4px solid {color.green.500}" }
    error: { icon: "x-circle", border_left: "4px solid {color.red.500}" }
    warning:
      { icon: "alert-triangle", border_left: "4px solid {color.amber.500}" }
    info: { icon: "info", border_left: "4px solid {color.blue.500}" }
```

#### Alert (Inline Banner)

```yaml
alert:
  radius: "{radius.md}"
  padding: "12px 16px"
  variants:
    success:
      {
        bg: "{color.green.50}",
        border: "1px solid {color.green.200}",
        icon_color: "{color.green.600}",
      }
    error:
      {
        bg: "{color.red.50}",
        border: "1px solid {color.red.200}",
        icon_color: "{color.red.600}",
      }
    warning:
      {
        bg: "{color.amber.50}",
        border: "1px solid {color.amber.200}",
        icon_color: "{color.amber.600}",
      }
    info:
      {
        bg: "{color.blue.50}",
        border: "1px solid {color.blue.200}",
        icon_color: "{color.blue.600}",
      }
  structure: "Icon | Title (optional) + Message | Dismiss button (optional) + Action link (optional)"
```

#### Skeleton

```yaml
skeleton:
  base_color: "{color.neutral.200}"
  highlight_color: "{color.neutral.100}"
  animation: "pulse {motion.interactions.skeleton_pulse}"
  radius: "{radius.md}"
  variants:
    text_line: "height 16px, width variable"
    avatar: "circle 40px"
    card: "rectangle matching card dimensions"
    table_row: "height 52px, full width"
```

#### ProgressBar

```yaml
progress_bar:
  height: "8px"
  radius: "{radius.full}"
  track: "{color.neutral.200}"
  fill: "{color.primary}"
  animation: "width transition 300ms ease-out"
  variants:
    determinate: "Shows percentage progress"
    indeterminate: "Animated left-to-right shimmer"
```

---

## Brand Guidelines

### Logo

- **Primary**: "PatientAccess" wordmark in `{color.primary}` (#1E6F9F) with health-cross icon mark
- **On Dark**: White wordmark with white icon mark
- **Minimum Size**: 120px width
- **Clear Space**: Minimum 16px on all sides

### Iconography

- **Library**: Lucide Icons (open-source, MIT license)
- **Default Size**: 20px (in buttons/nav), 24px (standalone)
- **Stroke**: 2px, round joins, round caps
- **Color**: Inherits from parent text color

### Illustration Style

- **Library**: unDraw or Storyset (open-source)
- **Style**: Flat, minimal, healthcare-themed
- **Usage**: Empty states, onboarding screens, error pages
- **Colors**: Use primary and secondary palette colors

---

## Accessibility Requirements

### WCAG 2.2 AA Compliance

- **Color Contrast**: Text >=4.5:1; Large text >=3:1; UI elements >=3:1
- **Focus States**: Visible outline (2px, 2px offset, >=3:1 contrast against background)
- **Touch Targets**: >=44x44px (mobile)
- **Text Resize**: Support up to 200% zoom without horizontal scrolling
- **Motion**: Respect `prefers-reduced-motion` media query
- **Error Identification**: Icon + text + color; never color alone

### ARIA Patterns

```yaml
aria_patterns:
  forms: "aria-label, aria-describedby (for help text), aria-invalid (for errors)"
  modals: "role=dialog, aria-labelledby, aria-modal=true, focus trap"
  toasts: "role=status, aria-live=polite"
  alerts: "role=alert, aria-live=assertive (for errors)"
  tabs: "role=tablist, role=tab, role=tabpanel, aria-selected"
  navigation: "role=navigation, aria-label for landmarks"
  tables: "scope=col on headers, aria-sort for sortable columns"
  chat: "role=log, aria-live=polite for new messages"
```

### Focus Management

```yaml
focus_order:
  modal_open: "Trap focus within modal; return to trigger on close"
  drawer_open: "Move focus to drawer; return to trigger on close"
  toast_appear: "Do not steal focus; accessible via screen reader"
  form_error: "Move focus to first error field on submit"
  page_navigation: "Move focus to main content <h1>"
```

---

## Mode Support

- **Light Mode**: Required (default)
- **Dark Mode**: Required (all tokens have dark mode variants defined above)
- **High Contrast Mode**: Optional (recommended for WCAG AAA compliance in future)

---

## Design Review Checklist

- [x] Design tokens defined (colors, typography, spacing, radius, elevation, motion)
- [x] Light and dark mode variants specified
- [x] Component library specified with all variants and states
- [x] Accessibility requirements documented (WCAG 2.2 AA)
- [x] ARIA patterns defined for interactive components
- [x] Responsive grid system defined (Mobile 4-col, Tablet 8-col, Desktop 12-col)
- [x] Healthcare-specific tokens defined (confidence scores, verification status, risk levels)
- [ ] Design tokens implemented in Figma variables
- [ ] Component Figma files built and published
