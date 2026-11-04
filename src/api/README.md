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

## Layout

| Path | Holds |
|---|---|
| `src/BudTack.Api/Program.cs` | The host: wiring only |
| `src/BudTack.Api/Platform/Config` | `.env` loading, `DATABASE_URL` → one `NpgsqlDataSource` |
| `src/BudTack.Api/Platform/Logging` | JSON log lines: `timestamp`, `level`, `correlationId`, `module`, `message` |
| `src/BudTack.Api/Platform/Web` | Correlation id middleware, `GET /health` |
| `src/BudTack.Api/Platform/Errors` | `{ error, message, details }` for every non-2xx response |
| `tests/boundary` | NetArchTest: no module `Internal` reachable from outside |
| `tests/unit` | One class at a time, no database |
| `tests/end-to-end` | The whole host over HTTP, against a throwaway PostgreSQL (Docker) |

Modules (`identity`, `transactions`, `dashboard`) arrive one pull request each.

## Pipeline

```bash
dotnet build -c Release
dotnet test --project tests/boundary -c Release --no-build      # boundary check
dotnet test --project tests/unit -c Release --no-build --coverage --coverage-output-format cobertura
dotnet test --project tests/end-to-end -c Release --no-build    # needs Docker
docker build -t bud-tack-api:local .                             # package
```

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
