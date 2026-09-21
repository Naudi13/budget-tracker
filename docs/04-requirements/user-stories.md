# 04 — User Stories

## Story identifier

`HU-NN` — sequential, two digits, starting at `01`. An id is never reused,
even for a story that gets dropped before it ships.

## Backlog

| HU-NN | Title | Module | Priority |
|---|---|---|---|
| HU-01 | Sign in to a business account | `identity` | ⭐ |
| HU-02 | Create a category or subcategory | `transactions` | ⭐ |
| HU-03 | Delete a category or subcategory | `transactions` | ⭐ |
| HU-04 | Register an income or expense transaction | `transactions` | ⭐ |
| HU-05 | View transaction history | `transactions` | ⭐ |
| HU-06 | Delete a transaction | `transactions` | ⭐ |
| HU-07 | Set a budget for a category | `transactions` | — |
| HU-08 | View the dashboard | `dashboard` | ⭐ |

## Writing a new story

Copy [`_template-hu.md`](./_template-hu.md), assign the next unused `HU-NN`,
fill in the narrative and Gherkin acceptance criteria, then add a row above.
A story whose acceptance criteria cannot be checked by reading the running
application is not ready for the backlog.

---

**Related:** [`./_template-hu.md`](./_template-hu.md) · [`./traceability-matrix.md`](./traceability-matrix.md)
