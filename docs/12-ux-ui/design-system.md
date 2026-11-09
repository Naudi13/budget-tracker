# 12 — Design System

## Brand

The logo and the palette come from the team's identity manual
(*BudgetTrack — Manual básico de identidad v1*, proposal C): an icon of three
rising bars in a rounded navy square, beside the word BUDGETTRACK.

| Rule (manual, section 5) | In the portal |
|---|---|
| Use the provided files; never redraw, stretch, skew or recolour | `public/brand/logo.png` (colour), `logo-reverse.png` (white on navy), `icon.png`, extracted from the manual and only trimmed |
| Colour logo on light backgrounds, reverse logo on navy or dark ones | The header is navy and carries the reverse logo |
| At least 24 px high on screen | 32 px in the header |
| The icon alone only at very small sizes | `favicon.png` and `apple-touch-icon.png` only |
| Clear space around the logo at least the icon's height | No text or control within that distance of the logo |

### Palette

| Token | Value | Role (manual, section 3) |
|---|---|---|
| `--brand-navy` | `#1B2A4A` | Principal — trust and stability |
| `--brand-mint` | `#2EC4B6` | Secundario — growth and clarity |
| `--brand-coral` | `#FF6B35` | Apoyo / acento — energy, used sparingly on actions and details |

## Language

Everything the user sees is in **Spanish (Colombia)**: labels, buttons,
messages, page titles, and dates, numbers and money formatted for `es-CO`
(`<html lang="es">`). The tone is the one the identity manual asks for: *tuteo*,
short sentences, no financial jargon, never blaming the user. Code, names,
comments, commits, API contracts and these documents stay in English.

The screens quote every visible text in Spanish, as it must appear. The API's
error `message` is English and for developers; a screen never shows it, it
picks its own Spanish text from the status, the `error` code and
`details[].field` of the response.

## Tokens

### Color

Every pair below meets the WCAG 2.1 AA contrast of the accessibility target.
Mint and coral are light: they never carry white text (2.2:1 and 2.8:1) and
never serve as text on white; they sit on navy, or carry navy text.

| Token | Value | Use | Contrast |
|---|---|---|---|
| `--color-primary` | `#1B2A4A` (navy) | Links, focus outline, secondary buttons, the header band | 14:1 on white |
| `--color-action` | `#FF6B35` (coral) with navy text | The one primary button of a screen: save a transaction, confirm a category, sign in | 5:1 |
| `--color-accent` | `#2EC4B6` (mint) | Decorative bands, the header's bottom line; focus and outlines on navy | 6.6:1 on navy |
| `--color-danger` | `#D1293D` | Destructive actions: delete a transaction, a category or subcategory, or a budget. Not coral, which reads as the action colour | 5.1:1 on white |
| `--color-success` | `#127A70` (mint, darkened) | Confirmation banners: transaction recorded, budget saved | 5.2:1 on white |
| `--color-text` | `#1A1A1A` | Default body text | 17:1 on white |
| `--color-bg-surface` | `#FFFFFF` | Card and form backgrounds | — |

### Typography and spacing

| Token | Value | Use |
|---|---|---|
| `--font-size-h1` | `1.75rem` / 700 weight | Page titles, e.g. "Transaction history" |
| `--font-size-body` | `1rem` / 400 weight | Table rows, labels, body copy |
| `--font-size-caption` | `0.8125rem` / 400 weight | Field hints, timestamps |
| `--space-unit` | `4px` | Base of the spacing scale: 4, 8, 16, 24, 32px |

## Component inventory

| Component | Used by | Notes |
|---|---|---|
| Button (primary / secondary / danger) | Every screen | One primary button per screen, at most |
| KPI tile | Dashboard ([`screens/dashboard.md`](./screens/dashboard.md)) | One figure and its label: total income, total expenses, net balance |
| Data table | Transaction history, category list, subcategory list | Paginated, newest first for transactions |
| Form field with inline validation | New record, category form, subcategory form, budget form, new account | Error message states the fix, not just the failure |
| Confirm dialog | Delete a transaction, a category or subcategory, or a budget | Danger button for the action, and a cancel that returns to the list |
| Blocked notice | Delete a category or subcategory that is still in use | Says why the delete is refused and offers deactivation instead (BR-05) |
| Empty state | Dashboard recent list, transaction history, category list | Message plus the one action that fills it |

## Accessibility target

Target level: **WCAG 2.1 AA**.

| Aspect | Requirement |
|---|---|
| Contrast | 4.5:1 for normal text, 3:1 for large text and UI controls |
| Keyboard | Every interactive element reachable and operable without a mouse |
| Labels | Every form field has a programmatically associated label |

---

**Related:** [`./README.md`](./README.md) · [`./_template-screen.md`](./_template-screen.md)
