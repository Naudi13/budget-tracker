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

| Version | Migration | Story | Creates |
|---|---|---|---|
| `202611041500` | `Platform001_CreateSchemas` | Sprint 0 | Schemas `identity` and `transactions`, and the app role's rights on them |
| `202611052230` | `Identity001_CreateUsers` | HU-01 | `identity.users`: unique email (`idx_users_email`), stored lowercase (`ck_users_email_lowercase`), a non-blank name, the password only as a hash |
| `202611060035` | `Transactions001_CreateCategories` | HU-02 | `transactions.categories`: an owner (`owned_by`), `income` or `expense`, a non-blank name unique per owner and type ignoring case (`idx_categories_owned_by_type_name`), `is_active` |
| `202611060036` | `Transactions002_CreateSubcategories` | HU-02 | `transactions.subcategories`: a name unique per category ignoring case; its key to the category is `ON DELETE RESTRICT`, so a category that owns a subcategory cannot be deleted (BR-05) |

**Round trip**, the check CI runs on every pull request: up, down to 0, up
again with the same schema, plus the rules of each table tried as the app role
— build first:

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
