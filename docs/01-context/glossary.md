# 01 — Glossary

## Terms

| Term | Definition | Forbidden synonyms |
|---|---|---|
| Income | Money entering the business, recorded as a transaction | Revenue (use only in reporting-specific context), Earning |
| Expense | Money leaving the business, recorded as a transaction | Cost, Outflow |
| Transaction | A single recorded income or expense entry with an amount, a date, a category, and a description | Entry, Movement, Record |
| Category | A label used to classify a transaction for reporting (e.g. "Sales", "Rent", "Supplies") | Tag, Type |
| Balance | The net result of total income minus total expenses over a period | Net worth, Profit, Cash flow |
| Dashboard | The summary view showing KPIs for a fast read of financial health | Report, Home page |
| KPI | A single metric on the dashboard measuring one aspect of financial health (e.g. total income) | Metric, Indicator (used only when it means KPI) |
| Module | A bounded context inside the single deployable, defined in [`../02-domain/module-boundaries.md`](../02-domain/module-boundaries.md) | Service; and "component" **when it means a module** — the word keeps its own meanings as a C4 level in [`../08-uml/diagram-index.md`](../08-uml/diagram-index.md) and as a UI part in [`../12-ux-ui/design-system.md`](../12-ux-ui/design-system.md) |

## Why forbidden synonyms matter

Two words for the same thing let two documents describe the same rule
differently and both look correct. When a reviewer sees a forbidden synonym in
a pull request, the fix is to rename the usage, not to add the synonym here —
unless the row above already carves out a sense in which that word names
something else entirely, in which case the usage is not a synonym at all.

## Adding a term

A term is added here the first time it appears in a second document. A term
used in exactly one document belongs in that document, not here.

---

**Related:** [`./overview.md`](./overview.md) · [`../02-domain/module-boundaries.md`](../02-domain/module-boundaries.md)
