# transactions — Data Model

## Tables owned

| Table | Schema | Entity | Key attributes |
|---|---|---|---|
| `transactions.categories` | `transactions` | Category | name, type (income \| expense), is_active |
| `transactions.subcategories` | `transactions` | Subcategory | category_id, name, is_active |
| `transactions.transactions` | `transactions` | Transaction | owned_by, transaction_date, description, deleted_at, deleted_by, deletion_reason |
| `transactions.transaction_details` | `transactions` | Transaction detail | transaction_id, line_number (the order the lines were entered, from 1), subcategory_id, amount (`numeric(14,2)`, above zero) |
| `transactions.budgets` | `transactions` | Budget | category_id, amount (`numeric(14,2)`, above zero), period_start, period_end (after period_start) |

Schema-per-module and naming rules live in
[`../../06-data/database-conventions.md`](../../06-data/database-conventions.md).

## Invariants

| Invariant | Enforced where |
|---|---|
| Each transaction belongs to the user who recorded it (`owned_by`); a `USER` reaches only their own. Categories, subcategories and budgets are the business's (ADR-009) | `internal/application` and every repository query |
| A category name is unique per type, a subcategory name per category, both ignoring case | `internal/domain` at write time, and unique indexes on `(type, lower(name))` and `(category_id, lower(name))` |
| A detail's type is derived from its subcategory's category (BR-01) | `internal/domain`, never stored on the detail |
| All details in one transaction share one type (BR-02) | `internal/application`, at write time |
| The net balance is income details minus expense details (BR-03) | Computed by `GetTotals`, never persisted |
| Deleting a transaction is a soft delete with a reason (BR-04) | `internal/application`; `deleted_at`, `deleted_by` and `deletion_reason` are set together |
| A category or subcategory in use cannot be deleted, only deactivated (BR-05) | `internal/application`, and `ON DELETE RESTRICT` on every intra-schema key above |
| A budget can be changed or deleted at any time; its delete is physical (BR-06) | `internal/application`; a changed budget is re-checked against the Budget invariants in `internal/domain` |

## Cross-module references

| Column | Points to | Validated by |
|---|---|---|
| `owned_by` (`transactions` only) | `identity.users` | The authenticated user id, never a foreign key |
| `created_by`, `updated_by` (every table above) | `identity.users` | `IdentityFacade`, never a foreign key |
| `deleted_by` (`transactions.transactions` only) | `identity.users` | `IdentityFacade`, never a foreign key |

No foreign key crosses this module's schema boundary — a reference to
another module's row is a plain column, checked through that module's public
API at write time, not by a database constraint.

---

**Related:** [`./README.md`](./README.md) · [`../../06-data/models.md`](../../06-data/models.md)
