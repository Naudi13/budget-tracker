# bud-tack-db

> Budget Tracker - Database: schema, seeds and migrations

Part of the **Budget Tracker** full-stack project — team `budget-tracker`, Complementaria III - Desarrollo Fullstack 2026-B.
Governance and documentation live in [`bud-tack-docs`](https://github.com/code-corhuila/bud-tack-docs).

## Migrations

The one migration sequence of the database, with **FluentMigrator** on .NET 10 —
no Java runtime (ADR-005). Policy: `06-data/migrations.md` in `bud-tack-docs`.

Start the local PostgreSQL from `bud-tack-infra` first, then:

```bash
cp .env.example .env
dotnet run --project src/BudTack.Db.Migrations -- up     # apply pending migrations
dotnet run --project src/BudTack.Db.Migrations -- list   # show applied / pending
dotnet run --project src/BudTack.Db.Migrations -- down   # roll everything back to 0
```

It connects with `MIGRATOR_DATABASE_URL`, as `bud_tack_migrator`; the API never
does. The versions live in `public.schema_version`.

| Rule | Detail |
|---|---|
| Location | `src/BudTack.Db.Migrations/Migrations/`, one class per migration |
| Version | `[Migration(yyyyMMddHHmm)]`, the UTC time it was written |
| Name | `<Module><NNN>_<Change>`, e.g. `Transactions001_CreateCategories` |
| Schema | Every table `.InSchema("identity")` or `.InSchema("transactions")` |
| Rollback | Additive changes inherit `AutoReversingMigration`; any other has a real `Down()` |

The API's role gets `SELECT, INSERT, UPDATE, DELETE` on every table the migrator
creates in those schemas automatically (default privileges, `Platform001`).

**Round trip**, the check CI runs on every pull request — build first:

```bash
dotnet build -c Release
PG_CONTAINER=bud-tack-postgres-1 sh scripts/round-trip.sh
```

## Branching

Three permanent branches. **None of them accepts a direct commit** — you enter through a child
branch and leave through a Pull Request.

```
develop  <--PR--  feat/... fix/... chore/...
qa       <--PR--  develop
main     <--PR--  qa (release)   hotfix/...
```

`develop` is the default branch: start every task from it. `main` requires **1 approval from
`ariel5253`**. On `develop` and `qa` the team sets its own review rule.

Commit and branch naming: `00-governance/git-conventions.md` in `bud-tack-docs`.
