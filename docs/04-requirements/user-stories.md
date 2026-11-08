# 04 — User Stories

## Story identifier

`HU-NN` — sequential, two digits, starting at `01`. An id is never reused,
even for a story that gets dropped before it ships.

## Backlog

| HU-NN | Title | Module | Priority | Blocked by |
|---|---|---|---|---|
| [HU-01](./stories/HU-01-sign-in.md) | Sign in to a business account | `identity` | ⭐ | — |
| [HU-02](./stories/HU-02-create-category-or-subcategory.md) | Create a category or subcategory | `transactions` | ⭐ | HU-01 |
| [HU-03](./stories/HU-03-delete-or-deactivate-category.md) | Delete or deactivate a category or subcategory | `transactions` | ⭐ | HU-02 |
| [HU-04](./stories/HU-04-register-transaction.md) | Register an income or expense transaction | `transactions` | ⭐ | HU-02 |
| [HU-05](./stories/HU-05-view-transaction-history.md) | View transaction history | `transactions` | ⭐ | HU-04 |
| [HU-06](./stories/HU-06-delete-transaction.md) | Delete a transaction | `transactions` | ⭐ | HU-05 |
| [HU-07](./stories/HU-07-set-budget.md) | Set, change or delete a budget for a category | `transactions` | — | HU-02 |
| [HU-08](./stories/HU-08-view-dashboard.md) | View the dashboard | `dashboard` | ⭐ | HU-04 |
| [HU-09](./stories/HU-09-list-accounts-as-admin.md) | List the accounts as an administrator | `identity` | ⭐ | HU-01 |

Story points are not written here: the whole team assigns them in planning
poker, per [`agile-conventions.md`](../00-governance/agile-conventions.md).

## Writing a new story

Copy [`_template-hu.md`](./_template-hu.md) into [`stories/`](./stories/HU-01-sign-in.md)
as `HU-NN-short-title.md`, assign the next unused `HU-NN`,
fill in the narrative and Gherkin acceptance criteria, then add a row above.
A story whose acceptance criteria cannot be checked by reading the running
application is not ready for the backlog.

---

**Related:** [`./_template-hu.md`](./_template-hu.md) · [`./traceability-matrix.md`](./traceability-matrix.md)
