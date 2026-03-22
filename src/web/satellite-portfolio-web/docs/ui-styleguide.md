# UI Style Guide

This app uses **Tailwind CSS v4** as the single styling system. Do not add inline style objects or route-level CSS modules unless there is a clear exception.

## Design Tokens

- **Colors**
  - Background: `#020617`
  - Surface: `#0b1220`
  - Surface muted: `#131c2e`
  - Border: `#23314d`
  - Text: `#e2e8f0`
  - Text muted: `#94a3b8`
  - Primary: `#60a5fa` (hover `#3b82f6`)
- **Type scale**
  - Page title: `text-2xl` / `sm:text-3xl`
  - Section title: `text-xl`
  - Body: `text-sm` to `text-base`
  - Table labels/meta: `text-xs uppercase`
- **Radii**
  - Small: `0.375rem`
  - Medium: `0.625rem`
  - Large: `0.875rem`
- **Spacing**
  - Page rhythm via `page-stack` (`space-y-6`)
  - Card padding: `p-4` / `sm:p-5`
  - Form row gaps: `gap-4`

## Shared Shell and Components

- App shell is defined in `app/layout.tsx`.
- Reusable UI patterns live in `app/components/`:
  - `site-nav.tsx` for responsive primary nav.
  - `ui.tsx` for `PageHeader`, `CardSection`, `StatusMessage`, and `EmptyState`.

## Adding a New Page

1. Create route file under `app/<route>/page.tsx`.
2. Wrap content in `<section className="page-stack">`.
3. Use `<PageHeader />` at the top.
4. Group related content inside `<CardSection />`.
5. Use `.table-wrap` + `.table-base` for tables.
6. Use `.label`, `.input`, `.btn-primary`, `.btn-secondary` for forms/buttons.
7. Keep semantics and accessibility:
   - Use heading order (`h1` then `h2`/`h3`).
   - Always pair `label htmlFor` with input IDs.
   - Keep keyboard focus styles intact.

## Theme Direction

The app currently standardizes on a **cohesive dark theme** based on the existing slate direction in `app/layout.tsx`. No light/dark toggle is introduced in this pass.
