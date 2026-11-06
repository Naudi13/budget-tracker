# bud-tack-api

> Budget Tracker - Backend API: business logic and REST endpoints

Part of the **Budget Tracker** full-stack project — team `budget-tracker`, Complementaria III - Desarrollo Fullstack 2026-B.
Governance and documentation live in [`bud-tack-docs`](https://github.com/code-corhuila/bud-tack-docs).

## Run it locally

Needs the .NET 10 SDK, the local PostgreSQL from `bud-tack-infra` and the
migrations from `bud-tack-db` applied (`10-devops/local-setup.md`).

```bash
cp .env.example .env
dotnet run --project src/BudTack.Api
curl -i http://localhost:3000/health     # 200 {"status":"Healthy",...}
```

The API connects as `bud_tack_app` (`DATABASE_URL`), never as the migrator. It
sends no CORS headers: the portal reaches it through a same-origin proxy (ADR-006).
It refuses to start without `AUTH_JWT_SECRET` (at least 32 bytes), the key that
signs the bearer tokens.

## Endpoints

| Method and path | Auth | Does |
|---|---|---|
| `GET /health` | none | `200` / `503` with the database's state |
| `POST /api/v1/users` | none | Sign-up (HU-01): `201` + `Location`, `400` per field, `409` email in use |
| `POST /api/v1/sessions` | none | Sign-in (HU-01): `201` with `{ token, tokenType, expiresIn }`, `401` wrong email or password |
| `GET`, `POST /api/v1/categories` | Bearer | List (`?page`, `?limit`, `?type`) and create (HU-02): `201`, `400`, `409` name in use for that type |
| `DELETE`, `PATCH /api/v1/categories/{id}` | Bearer | Delete (`204`, `409` while it owns a subcategory or budget, BR-05) and activate or deactivate (HU-03) |
| `GET`, `POST /api/v1/categories/{id}/subcategories` | Bearer | List and create (HU-02): `409` name in use in that category |
| `DELETE`, `PATCH /api/v1/categories/{id}/subcategories/{id}` | Bearer | Delete (`409` while a transaction uses it, BR-05) and activate or deactivate (HU-03) |
| `GET`, `POST /api/v1/transactions` | Bearer | History newest first without deleted ones (HU-05), and record (HU-04): `201`; `400` per field; `422 BUSINESS_RULE_VIOLATED` when lines mix income and expense (BR-02) or use an inactive subcategory or category (BR-05). Each line's type comes from its category (BR-01); lines come back in the order they were entered |
| `DELETE /api/v1/transactions/{id}` | Bearer | Soft delete (HU-06, BR-04): `204`, then `404` |
| `GET`, `POST /api/v1/budgets` | Bearer | List newest period first (`?page`, `?limit`, `?categoryId`) and set (HU-07): `201` with `Location`; `400` per field; `422 BUSINESS_RULE_VIOLATED` when the period does not end after it starts or the category is inactive (TC-007, BR-05) |
| `PATCH`, `DELETE /api/v1/budgets/{id}` | Bearer | Change only the fields sent, held to the same rules (`200`, `422`), and delete physically (`204`, then `404`) (BR-06, TC-019) |
| `GET /api/v1/dashboard` | Bearer | HU-08: total income, total expenses and net balance computed on read (BR-03) without deleted transactions (BR-04), and the five most recent transactions |
| Every other controller action | `Authorization: Bearer <token>` | `401 UNAUTHORIZED` without a valid token (NFR-05) |

Contracts: `07-api/contracts/openapi/` in `bud-tack-docs`. Passwords are stored as
salted PBKDF2-HMAC-SHA256 (600 000 iterations) and never logged (NFR-06). A new
controller is protected by default; only `[AllowAnonymous]` opens it.

## Layout

| Path | Holds |
|---|---|
| `src/BudTack.Api/Program.cs` | The host: wiring only |
| `src/BudTack.Api/Platform/Config` | `.env` loading, `DATABASE_URL` → one `NpgsqlDataSource` |
| `src/BudTack.Api/Platform/Logging` | JSON log lines: `timestamp`, `level`, `correlationId`, `module`, `message` |
| `src/BudTack.Api/Platform/Web` | Controllers (internal), bearer authentication, correlation id middleware, `GET /health` |
| `src/BudTack.Api/Platform/Errors` | `{ error, message, details }` for every non-2xx response, including the errors a facade declares |
| `src/BudTack.Identity` | The identity module: `IdentityModule` (marker + registration), `Api/IdentityFacade` and `Api/Contracts`; `Internal/` holds the domain, the use cases (`SignUp`, `SignIn`, `TokenService`, `PasswordHasher`) and `UsersRepository` |
| `src/BudTack.Transactions` | The transactions module: `TransactionsModule`, `Api/TransactionsFacade` and `Api/Contracts`; `Internal/` holds the category and movement rules, `ManageCategories`, `ManageMovements` and their repositories; a movement and its lines are stored in one database transaction. Every row is read and written through its owner (`owned_by`): another business's row answers `404` |
| `src/BudTack.Dashboard` | The dashboard module: `DashboardModule`, `Api/DashboardFacade` and `Api/Contracts/DashboardView`; reads `GetTotalsAsync` and `ListTransactionsAsync` through `TransactionsFacade` only, and owns no table |
| `tests/boundary` | NetArchTest: no module `Internal` reachable from outside |
| `tests/unit` | One class at a time, no database |
| `tests/module-integration` | Each module through its facade only, against a throwaway PostgreSQL (Docker) |
| `tests/end-to-end` | The whole host over HTTP, against a throwaway PostgreSQL (Docker) |
| `tests/schema/budtack.sql` | The migrated schema both Docker layers start from; regenerated with `pg_dump` when `bud-tack-db` adds a migration |

A module is a project (ADR-003): only its marker and `Api/` are public, and
`tests/boundary` checks that nothing reaches into its `Internal/` and that the
modules depend on each other only as `09-modules/module-catalog.md` draws:
`dashboard` → `transactions` → `identity`.

## Pipeline

```bash
dotnet build -c Release
dotnet test --project tests/boundary -c Release --no-build      # boundary check
dotnet test --project tests/unit -c Release --no-build --results-directory TestResults --coverage --coverage-output-format cobertura --coverage-output unit.cobertura.xml
dotnet test --project tests/module-integration -c Release --no-build --results-directory TestResults --coverage --coverage-output-format cobertura --coverage-output module-integration.cobertura.xml   # needs Docker
python3 scripts/coverage-gate.py TestResults/unit.cobertura.xml TestResults/module-integration.cobertura.xml
dotnet test --project tests/end-to-end -c Release --no-build    # needs Docker
docker build -t bud-tack-api:local .                             # package
```

The coverage gate applies NFR-07 to each module — 80 %, and 90 % for
`BudTack.Transactions` — over the unit and module-integration layers together:
a repository is only provable against a real PostgreSQL. The host is covered by
the end-to-end layer and reported, not gated.

`dotnet test` runs on Microsoft.Testing.Platform (set in `global.json`), the
only runner xUnit v3 supports on the .NET 10 SDK.

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
