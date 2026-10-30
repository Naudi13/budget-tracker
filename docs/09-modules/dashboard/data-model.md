# dashboard — Data Model

## Tables owned

None. `dashboard` is the one module in
[`../module-catalog.md`](../module-catalog.md) with no row in this table —
every figure it returns is read through `TransactionsFacade` at request time,
never stored or cached here. See
[`../../06-data/models.md`](../../06-data/models.md) for the tables that
back those figures.

## Invariants

| Invariant | Enforced where |
|---|---|
| `netBalance` always equals `totalIncome` minus `totalExpenses` (BR-03) | `internal/application`, recomputed on every call — never persisted |
| `recentTransactions` never exceeds 5 rows | `internal/application`, a fixed limit on the `ListTransactions` call |

## Cross-module references

| Column | Points to | Validated by |
|---|---|---|
| none | — | `dashboard` stores no row, so it has no column to validate |

No foreign key crosses this module's schema boundary, because this module
has no schema: a reference to another module's data is a public API call,
never a database read.

---

**Related:** [`./README.md`](./README.md) · [`../../06-data/models.md`](../../06-data/models.md)
