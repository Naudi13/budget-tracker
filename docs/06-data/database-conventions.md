# 06 — Database Conventions

## One database, divided by module

| Mechanism | How it works | Enforced by | Reviewer check |
|---|---|---|---|
| **Schema per module** (recommended) | Each table-owning module gets one namespace — `identity.users`, `transactions.categories` | A database role per module, granted only on its own schema | Query the grant catalog: a module's role has zero grants outside its own schema |
| Table-prefix per module (fallback) | One namespace, tables prefixed — `identity_users`, `transactions_categories` | Naming convention, checked by a lint script | The script maps every table's prefix to [`module-boundaries.md`](../02-domain/module-boundaries.md)'s "Owns" column and fails on an unrecognized prefix |

Default to schema per module — it is what
[`modular-monolith.md`](../05-architecture/modular-monolith.md)'s diagram
names, and a database role can enforce it the same way
[`boundary-enforcement.md`](../05-architecture/boundary-enforcement.md)
enforces boundaries in code: a check that fails on its own, not a comment.
Fall back to table-prefix only on an engine with no real schema namespace
(SQLite, or MySQL without a second database). `dashboard` owns no table and so
gets no schema of its own; every value it shows is read through
`TransactionsFacade`.

**No foreign key crosses a module boundary**, under either mechanism. A module
storing another module's id — `transactions.categories.created_by`, pointing at
`identity.users` — keeps it as a plain column, validated by the owning module's
public API at write time, never by a `REFERENCES` constraint. Reviewer check: no
row in the foreign-key catalog references a table outside its own schema or
prefix group. This project's shared instance is itself a documented exception,
in [`ADR-004`](../05-architecture/decisions/records/ADR-004-shared-database-and-per-piece-repositories.md).

The reason is ownership, not a plan to split anything apart. A constraint
declared by `transactions` on `identity.users` is one module writing a rule into
another module's table, and it makes every later change to that table
`transactions`'s problem to review. The cost is accepted rather than denied: the
database no longer refuses an orphaned `created_by`, so the facade check and a
`module-integration` test standing on it are what replace the constraint. A
write that spans two modules still commits inside the one transaction on the
one database [`../01-context/overview.md`](../01-context/overview.md) requires
— what moves is who checks the reference, not where the commit happens.

Inside one module's own schema, a foreign key is an ordinary constraint: every
key from `transactions.subcategories` to `.categories`, from `.budgets` to
`.categories`, and from `.transaction_details` to `.transactions` and
`.subcategories` is declared `ON DELETE RESTRICT`, enforcing **BR-05** in the
data itself, not only in code.

## Naming

| Element | Rule | Example |
|---|---|---|
| Entity — the concept, not a table | Singular, in the business's own words; the plural rule below is for tables and never reaches this column | `Transaction`, `Transaction detail` |
| Table | snake_case, plural | `categories`, `transaction_details` |
| Column | snake_case | `period_start`, `transaction_date` |
| Primary key | `id`, UUID | `id UUID PRIMARY KEY` |
| Foreign key, same module | `<singular table>_id` | `category_id`, `subcategory_id` |
| Foreign key, another module (plain column, no constraint) | `<action>_by` for a user reference | `created_by`, `updated_by`, `deleted_by` |
| Boolean | prefixed `is_` / `has_` | `is_active`, `has_discount` |
| Index | `idx_<table>_<column>` | `idx_transaction_details_transaction_id` |

## Dates and audit columns

Every table carries `created_at` and `updated_at` as `TIMESTAMPTZ NOT NULL
DEFAULT NOW()`, plus `created_by` and `updated_by` — a plain `UUID` column
pointing at `identity.users`, per the cross-module rule above. A business date
with no time component — `transaction_date`, `period_start` — is `DATE`, never
`TIMESTAMPTZ`.

A table whose rows are deactivated rather than removed — `categories` and
`subcategories`, per **BR-05** — adds `is_active` (`BOOLEAN NOT NULL DEFAULT TRUE`).
A table whose rows are soft-deleted, per its module's own `BR-NN` — in this
project, only `transactions.transactions`, per **BR-04** — adds `deleted_by`
(`UUID`, nullable, same cross-module rule) and `deleted_at`
(`TIMESTAMPTZ`, nullable). The two are set together or not at all; every read
outside an audit trail filters `WHERE deleted_at IS NULL`.

Stack-specific ORM mapping rules (fetch strategy, relationship laziness)
belong in [`../_stacks/`](../_stacks/README.md) once that guide exists for
this project's language — not here, so this document stays true for every
stack.

---

**Related:** [`../02-domain/module-boundaries.md`](../02-domain/module-boundaries.md) · [`../05-architecture/modular-monolith.md`](../05-architecture/modular-monolith.md)
