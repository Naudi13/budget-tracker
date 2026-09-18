# 01 — Scope

## In scope

| Capability | Note |
|---|---|
| Income registration | Record a money-in transaction with amount, date, category, and description |
| Expense registration | Record a money-out transaction with amount, date, category, and description |
| Transaction listing | View, edit, and delete recorded income and expense transactions |
| Balance calculation | Net balance computed as total income minus total expenses |
| Dashboard with KPIs | One view showing total income, total expenses, and net balance for a fast read |

## Out of scope

| Capability | Why it is excluded |
|---|---|
| Budgeting and spending limits | V1 records what happened; planning what should happen is a later version |
| Invoicing and client billing | This product tracks the business's own income and expenses, not invoices issued to customers |
| Bank account integration / auto-import | Transactions are entered manually in V1; no bank API integration exists yet |
| Multi-currency support | The target user operates in a single currency |
| Multi-user roles and permissions | V1 serves a single owner/user per business; team collaboration is a later version |
| Tax reporting and compliance exports | Out of scope until a specific jurisdiction's requirements are defined |
| Splitting recording and dashboard into a separately deployed application | This product ships and scales as one deployable, by design |

## Revisiting scope

An item moves from "Out" to "In" only after `03-product/vision.md` is updated
first — scope follows vision, not the other way around.

---

**Related:** [`./overview.md`](./overview.md) · [`../03-product/problem-framing.md`](../03-product/problem-framing.md)
