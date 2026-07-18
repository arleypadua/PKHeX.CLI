---
name: PKHeX Everywhere
description: A familiar, efficient, and trustworthy cross-platform Pokemon save workbench.
colors:
  primary-action: "#1890ff"
  primary-action-dark: "#177ddc"
  primary-action-dark-hover: "#115696"
  neutral-ink: "#000000d9"
  neutral-muted: "#00000073"
  neutral-border: "#d9d9d9"
  neutral-surface: "#ffffff"
  neutral-layout: "#f0f2f5"
  danger: "#b32121"
typography:
  title:
    fontFamily: "Inter, ui-sans-serif, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "20px"
    fontWeight: 500
    lineHeight: 1.4
  body:
    fontFamily: "Inter, ui-sans-serif, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.5715
  label:
    fontFamily: "Inter, ui-sans-serif, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.5715
rounded:
  control: "2px"
  overlay: "4px"
  pill: "999px"
spacing:
  xs: "5px"
  sm: "8px"
  md: "16px"
  lg: "20px"
  xl: "24px"
components:
  button-primary:
    backgroundColor: "{colors.primary-action}"
    textColor: "{colors.neutral-surface}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "4px 15px"
    height: "32px"
  button-secondary:
    backgroundColor: "{colors.neutral-surface}"
    textColor: "{colors.neutral-ink}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "4px 15px"
    height: "32px"
  input:
    backgroundColor: "{colors.neutral-surface}"
    textColor: "{colors.neutral-ink}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "4px 11px"
    height: "32px"
---

# Design System: PKHeX Everywhere

## 1. Overview

**Creative North Star: "The Save Workbench"**

The interface is a practical work surface for inspecting and changing a Pokemon save. Save context, available actions, editable data, and operation results should remain easy to locate, with the product's visual personality coming from Pokemon sprites and direct, familiar interactions rather than decorative effects.

The system is flat and structured. Ant Design supplies familiar browser controls and state behavior; spacing and hierarchy separate tool areas from data. Density is intentional, but labels and consequences stay explicit. The system rejects neon gaming dashboards, cramped WinForms imitation, generic card grids, and bulk actions with unclear scope.

**Key Characteristics:**

- Compact, information-dense layouts with 20-24px section spacing.
- Visible save context, capacity, and compatibility before mutation.
- One blue action accent used for primary actions, selection, and focus.
- Existing Ant Design light and dark themes, not a separate visual rebrand.
- Pokemon sprites and icons used as functional recognition aids.

## 2. Colors

The palette is restrained: Ant Design blue identifies action and selection while neutral surfaces carry dense save data in either supported theme.

### Primary

- **Workbench Blue:** The primary action color for buttons, links, selected states, progress, and visible keyboard focus. Use the dark-theme variation supplied by the existing theme service when the app is in dark mode.

### Neutral

- **Data Ink:** Primary text and table values. Use muted ink only for supporting metadata, never required instructions.
- **Tool Surface:** Page, control, table, and overlay surfaces inherited from the active Ant Design theme.
- **Structural Border:** Dividers, table boundaries, and field outlines that organize dense information without shadows.
- **Layout Field:** The quiet page-level layer behind working surfaces.
- **Failure Red:** Reserved for destructive errors and failed operations; pair it with an icon and text.

### Named Rules

**The One Action Color Rule.** Blue means interactive, selected, or focused. It is never background decoration.

**The Theme Inheritance Rule.** New components inherit Ant Design light/dark tokens. Hard-coded light surfaces are prohibited.

## 3. Typography

- **Display Font:** Inter with the native system UI stack
- **Body Font:** Inter with the native system UI stack
- **Label Font:** Inter with the native system UI stack

**Character:** A neutral, familiar sans-serif keeps dense save data readable across platforms. Pokemon identity comes from sprites, terminology, and workflows rather than a display font applied to controls and tables.

### Hierarchy

- **Title** (500, 20px, 1.4): Page titles and major workflow headings.
- **Body** (400, 14px, 1.5715): Instructions, table values, descriptions, and result summaries. Prose is capped near 70 characters per line.
- **Label** (400, 14px, 1.5715): Buttons, fields, tabs, status labels, and compact metadata.

### Named Rules

**The Utility Type Rule.** Do not add display-font treatments, oversized headings, wide tracking, or uppercase decoration to task screens.

## 4. Elevation

The system is flat by default. Borders, spacing, and tonal layers establish structure; shadows are reserved for Ant Design dropdowns, drawers, modals, notifications, and other surfaces that must visibly float above the current task.

### Shadow Vocabulary

- **Overlay:** Use the active Ant Design theme's standard overlay shadow for menus, dialogs, and notifications. Do not reproduce it on static containers.

### Named Rules

**The Flat Workbench Rule.** Static tool areas and data tables remain flat. If a container needs a wide soft shadow to be understandable, its hierarchy is wrong.

## 5. Components

Components are compact and explicit. Every action has a visible label unless the icon's meaning is universal and an accessible name is present.

### Buttons

- **Shape:** Compact rectangular controls with gently curved corners (2px).
- **Primary:** Workbench Blue with white text and standard Ant Design medium dimensions (32px high).
- **Hover / Focus:** Use Ant Design state colors and a visible focus treatment; never scale or shift the button.
- **Secondary / Ghost:** Neutral surface or link treatment for import, export, calculator, and advanced paths. Destructive actions require danger styling and confirmation.

### Cards / Containers

- **Corner Style:** Minimal rounding inherited from Ant Design.
- **Background:** Active-theme surface colors.
- **Shadow Strategy:** Flat at rest; overlay-only elevation.
- **Border:** Structural borders separate sections and tabular data.
- **Internal Padding:** 16px for compact tools and 24px for page-level work surfaces.

### Inputs / Fields

- **Style:** Active-theme surface, structural border, compact 32px height, and persistent visible labels.
- **Focus:** Ant Design blue focus border/ring with no layout shift.
- **Error / Disabled:** Pair state styling with adjacent text; disabled controls explain why when the reason is not obvious.

### Navigation

- The dark collapsible side navigation remains the primary desktop structure.
- Active items use Ant Design's selected treatment; links remain keyboard reachable and visibly focused.
- At smaller breakpoints, navigation collapses before working content is compressed.

### Data Tables and Bulk Tools

- Tables keep filtering, sorting, pagination, and row selection when those functions are relevant.
- Bulk tools show selection count, operation scope, and capacity before submission.
- Mobile layouts may stack tool controls, but data and actions must not overflow the viewport.

## 6. Do's and Don'ts

### Do:

- **Do** keep current-save context, available box capacity, and compatibility visible near bulk actions.
- **Do** use existing Ant Design controls and state behavior before creating custom primitives.
- **Do** use Pokemon icons to make species and item choices faster to scan.
- **Do** preserve keyboard operation, visible focus, 44px touch targets where practical, and announced results.
- **Do** use 16px internal spacing and 20-24px section gaps to organize dense workflows.

### Don't:

- **Don't** create a neon gaming dashboard that prioritizes visual effects over data clarity.
- **Don't** clone cramped WinForms controls without adapting them for browsers or touch.
- **Don't** build a generic card-heavy dashboard that hides frequent actions behind decorative containers.
- **Don't** ship destructive or bulk actions with unclear scope, silent partial completion, or implicit overwrite behavior.
- **Don't** add a new palette, font family, custom modal, or button primitive when the active Ant Design system already provides it.
- **Don't** use shadows on static containers, decorative gradients, gradient text, glassmorphism, or motion unrelated to state.
