# transactions — Data Model

## Tables owned

| Table | Schema | Entity | Key attributes |
|---|---|---|---|
| `transactions.categories` | `transactions` | Category | name, type (income \| expense), is_active |
| `transactions.subcategories` | `transactions` | Subcategory | category_id, name, is_active |
| `transactions.transactions` | `transactions` | Transaction | transaction_date, description, deleted_at |
| `transactions.transaction_details` | `transactions` | Transaction detail | transaction_id, subcategory_id, amount |
| `transactions.budgets` | `transactions` | Budget | category_id, amount, period_start, period_end |

Schema-per-module and naming rules live in
[`../../06-data/database-conventions.md`](../../06-data/database-conventions.md).

## Invariants

| Invariant | Enforced where |
|---|---|
| A detail's type is derived from its subcategory's category (BR-01) | `internal/domain`, never stored on the detail |
| All details in one transaction share one type (BR-02) | `internal/application`, at write time |
| The net balance is income details minus expense details (BR-03) | Computed by `GetTotals`, never persisted |
| Deleting a transaction is a soft delete (BR-04) | `internal/application`; `transactions.transactions.deleted_at`/`deleted_by` |
| A category or subcategory in use cannot be deleted, only deactivated (BR-05) | `internal/application`, and `ON DELETE RESTRICT` on every intra-schema key above |
| A budget can be changed or deleted at any time; its delete is physical (BR-06) | `internal/application`; a changed budget is re-checked against the Budget invariants in `internal/domain` |

## Cross-module references

| Column | Points to | Validated by |
|---|---|---|
| `created_by`, `updated_by` (every table above) | `identity.users` | `IdentityFacade`, never a foreign key |
| `deleted_by` (`transactions.transactions` only) | `identity.users` | `IdentityFacade`, never a foreign key |

No foreign key crosses this module's schema boundary — a reference to
another module's row is a plain column, checked through that module's public
API at write time, not by a database constraint.

---

**Related:** [`./README.md`](./README.md) · [`../../06-data/models.md`](../../06-data/models.md)
