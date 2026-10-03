# 12 — Design System

## Tokens

### Color

| Token | Value | Use |
|---|---|---|
| `--color-primary` | `#0B5FFF` | Primary actions: save a transaction, confirm a category, sign in |
| `--color-danger` | `#D1293D` | Destructive actions: delete a transaction, delete a category or subcategory |
| `--color-success` | `#1E824C` | Confirmation banners: transaction recorded, budget saved |
| `--color-text` | `#1A1A1A` | Default body text |
| `--color-bg-surface` | `#FFFFFF` | Card and form backgrounds |

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
| Form field with inline validation | New record, category form, subcategory form, budget form, sign-up | Error message states the fix, not just the failure |
| Confirm dialog | Delete a transaction, delete a category or subcategory | Danger button for the action, and a cancel that returns to the list |
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
