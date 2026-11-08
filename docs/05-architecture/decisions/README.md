# 05 — Architecture Decision Records

An ADR records one decision that is expensive to reverse: what was decided,
what else was considered, and what it costs. Copy `_template-adr.md` into
`records/`, never edit the template in place, and add a row to the register
below in the same pull request.

## When an ADR is written

| Write one when | Do not write one when |
|---|---|
| The decision changes the shape of the code — layers, modules, boundaries | The choice is reversible in an afternoon |
| Reversing it later means moving files across boundaries | It only affects one file |
| A reviewer asked "why did we do it this way?" twice | The answer is already in a document in this repository |
| A boundary suppression is being introduced | A style question a linter can settle |
| A dependency is adopted that the whole application will import | A library used inside one module only |

A decision taken in a meeting and not written down has not been made; it has
only been discussed out loud.

## Statuses

| Status | Meaning | Next step |
|---|---|---|
| `Proposed` | Written, under review, not yet binding | Review at the architecture gate |
| `Accepted` | Binding on the codebase from its date onward | None — it is now the rule |
| `Rejected` | Considered and turned down; kept so it is not re-proposed | None |
| `Superseded by ADR-NNN` | Replaced by a later decision | Read the ADR that replaced it |

An accepted ADR is never edited or deleted. It is superseded by a new record
that names it, so the history of the reasoning stays readable.

## Register

| # | Title | Status | Date |
|---|---|---|---|
| [ADR-001](./records/ADR-001-modular-monolith-as-default.md) | Modular monolith as the default architecture | Accepted | 2026-11-04 |
| [ADR-002](./records/ADR-002-angular-dotnet-postgresql-stack.md) | Angular, .NET and PostgreSQL as the technology stack | Accepted | 2026-11-04 |
| [ADR-003](./records/ADR-003-project-per-module-boundary.md) | One project per module, checked by the compiler and NetArchTest | Accepted | 2026-11-04 |
| [ADR-004](./records/ADR-004-shared-database-and-per-piece-repositories.md) | One shared database and one repository per piece | Accepted | 2026-11-04 |
| [ADR-005](./records/ADR-005-fluentmigrator-for-schema-migrations.md) | FluentMigrator for schema migrations, in `bud-tack-db` | Accepted | 2026-11-04 |
| [ADR-006](./records/ADR-006-portal-image-behind-reverse-proxy.md) | The portal ships as its own image, behind a reverse proxy | Accepted | 2026-11-04 |
| [ADR-007](./records/ADR-007-two-roles-in-the-token.md) | Two roles, carried in the token | Accepted | 2026-11-08 |
| [ADR-008](./records/ADR-008-bcrypt-cors-and-swagger.md) | BCrypt, a CORS allow-list and Swagger | Accepted | 2026-11-08 |

## Naming

`ADR-NNN-short-title.md` — three digits, sequential from `001`, never reused,
in [`records/`](./records/ADR-001-modular-monolith-as-default.md). The number is
assigned when the record is written, not when it is accepted.

---

**Related:** [`./_template-adr.md`](./_template-adr.md) · [`../README.md`](../README.md)
