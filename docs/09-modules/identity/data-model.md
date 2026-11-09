# identity — Data Model

## Tables owned

| Table | Schema | Entity | Key attributes |
|---|---|---|---|
| `identity.users` | `identity` | User | name, email, password_hash (null until activated), role, is_active |
| `identity.account_tokens` | `identity` | Account link | user_id, purpose, token_hash, expires_at, used_at, created_by |

Schema-per-module and naming rules live in
[`../../06-data/database-conventions.md`](../../06-data/database-conventions.md).

## Invariants

| Invariant | Enforced where |
|---|---|
| Email is unique | This module's `internal/domain`, at write time, and a unique index on `identity.users.email` |
| Nobody signs up: an `ADMIN` creates each account, with no password (BR-07) | This module's `internal/application` |
| An inactive account cannot sign in and its sessions stop at once; only a never-activated account can be deleted (BR-09) | This module's `internal/application` |
| A link works once and until it expires; a new one cancels the earlier ones (ADR-010) | This module's `internal/application` |

## Cross-module references

| Column | Points to | Validated by |
|---|---|---|
| none | — | `identity` is pointed at by other modules; it points at none itself |

No foreign key crosses this module's schema boundary — a reference to
another module's row is a plain column, checked through that module's public
API at write time, not by a database constraint.

---

**Related:** [`./README.md`](./README.md) · [`../../06-data/models.md`](../../06-data/models.md)
