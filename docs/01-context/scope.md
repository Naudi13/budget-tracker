# 01 — Scope

## In scope

| Capability | Note |
|---|---|
| User account | A single account per business signs in to access its own data |
| Roles | Two roles, `USER` and `ADMIN`: every account is a `USER`, and an `ADMIN` can also list the accounts ([`ADR-007`](../05-architecture/decisions/records/ADR-007-two-roles-in-the-token.md)) |
| Income registration | Record a transaction (date, description) with one or more detail lines, each an amount against an income subcategory |
| Expense registration | Record a transaction the same way, using an expense subcategory for each detail line |
| Category & subcategory management | Create custom categories (income or expense) and subcategories from Configuration; rename a category; delete only when unused, deactivate otherwise |
| Budgeting | Set, change or delete an amount limit per category for a defined period |
| Transaction listing | View and delete recorded transactions (soft delete — the record is hidden, never permanently removed) |
| Balance calculation | Net balance computed as total income minus total expenses |
| Dashboard with KPIs | One view showing total income, total expenses, net balance, and the 5 most recent transactions |

## Out of scope

| Capability | Why it is excluded |
|---|---|
| Editing a recorded transaction | V1's flow only supports delete-and-recreate; in-place edit is not part of the planned UX yet |
| Budget vs. actual comparison and alerts | V1 only stores the limit; comparing it against recorded spending is a later version |
| Invoicing and client billing | This product tracks the business's own income and expenses, not invoices issued to customers |
| Bank account integration / auto-import | Transactions are entered manually in V1; no bank API integration exists yet |
| Multi-currency support | The target user operates in a single currency |
| Several people sharing one business's data | V1 serves a single account per business; team collaboration is a later version |
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
