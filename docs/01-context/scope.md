# 01 — Scope

## In scope

| Capability | Note |
|---|---|
| User account | One installation serves one business. An administrator creates each account, and its owner activates it with a single-use link and chooses the password; there is no public sign-up. An administrator can inactivate an account, and delete one that was never activated |
| Roles | `ADMIN` manages accounts, categories, subcategories and budgets, and sees every transaction. `USER` records and sees only their own transactions ([`ADR-009`](../05-architecture/decisions/records/ADR-009-one-business-with-a-shared-catalog.md)) |
| Income registration | Record a transaction (date, description) with one or more detail lines, each an amount against an income subcategory |
| Expense registration | Record a transaction the same way, using an expense subcategory for each detail line |
| Category & subcategory management | Create custom categories (income or expense) and subcategories from Configuration; rename a category; delete only when unused, deactivate otherwise |
| Budgeting | Set, change or delete an amount limit per category for a defined period |
| Transaction listing | View and delete recorded transactions (soft delete, with a required reason); an administrator can review the deleted ones |
| Balance calculation | Net balance computed as total income minus total expenses |
| Dashboard with KPIs | One view showing total income, total expenses, net balance, how spending compares with the budget, and the 5 most recent transactions, globally or for one category |

## Out of scope

| Capability | Why it is excluded |
|---|---|
| Editing a recorded transaction | V1's flow only supports delete-and-recreate; in-place edit is not part of the planned UX yet |
| Budget alerts and notifications | V1 shows budget, spent and available on the dashboard; warning anyone about them is a later version |
| Invoicing and client billing | This product tracks the business's own income and expenses, not invoices issued to customers |
| Bank account integration / auto-import | Transactions are entered manually in V1; no bank API integration exists yet |
| Multi-currency support | The target user operates in a single currency |
| Several businesses in one installation | One installation serves one business ([`ADR-009`](../05-architecture/decisions/records/ADR-009-one-business-with-a-shared-catalog.md)) |
| Tax reporting and compliance exports | Out of scope until a specific jurisdiction's requirements are defined |
| Splitting recording and dashboard into a separately deployed application | This product ships and scales as one deployable, by design |

No ADR governs this cut yet. Each exclusion above follows directly from the
objectives in [`../03-product/vision.md`](../03-product/vision.md): a
capability stays out until an objective names it, not because a decision
record ruled it out explicitly.

## Revisiting scope

An item moves from "Out" to "In" only after `03-product/vision.md` is updated
first — scope follows vision, not the other way around.

---

**Related:** [`./overview.md`](./overview.md) · [`../03-product/problem-framing.md`](../03-product/problem-framing.md)
