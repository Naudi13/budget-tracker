# bud-tack-infra

> Budget Tracker - Infrastructure: Compose/IaC, environments and secrets

Part of the **Budget Tracker** full-stack project — team `budget-tracker`, Complementaria III - Desarrollo Fullstack 2026-B.
Governance and documentation live in [`bud-tack-docs`](https://github.com/code-corhuila/bud-tack-docs).

## Implementation manual

Everything needed to run Budget Tracker on a machine that has never seen it.

### 1. Requirements

| Tool | Version | Needed for |
|---|---|---|
| Docker Desktop, with Docker Compose | Docker 29.7, Compose 5.3 (tested) | Running the application |
| Git | 2.35 or later | Cloning the repositories |
| curl and `sh` | Included with Git for Windows, macOS and Linux | The scripts in `scripts/` |
| .NET SDK | 10.0.402 or later | Only to run the API's tests |
| Node.js | 22.12 or later | Only to run the portal's tests and Newman |

Docker Desktop must be running before step 4. Ports `8080` and `5432` must be free.

### 2. Clone the four repositories

The four folders must end up side by side, which is where `docker-compose.yml`
looks for them.

```bash
mkdir budget-tracker
cd budget-tracker
git clone -b qa https://github.com/code-corhuila/bud-tack-infra.git
git clone -b qa https://github.com/code-corhuila/bud-tack-db.git
git clone -b qa https://github.com/code-corhuila/bud-tack-api.git
git clone -b qa https://github.com/code-corhuila/bud-tack-portal.git
cd bud-tack-infra
```

Every command below runs from `bud-tack-infra`, unless it says otherwise.

### 3. Create the environment file

```bash
cp .env.example .env
```

The example values work as they are. `.env` is never committed.

### 4. Start the application

```bash
docker compose up -d --build --wait
```

This starts PostgreSQL, applies the migrations, and starts the API and the portal.
It took about one minute with the base images already on the machine. The first
time on a new machine it also downloads them, so allow several more minutes,
depending on the connection. The command returns when every service is healthy.

To confirm it worked:

```bash
sh scripts/verify-stack.sh
```

On Windows PowerShell, where `sh` does not exist, use Git's:

```powershell
& "C:\Program Files\Git\bin\sh.exe" scripts/verify-stack.sh
```

### 5. URLs and ports

| What | URL | Port |
|---|---|---|
| Portal | `http://localhost:8080` | `8080` (`PORTAL_PORT`) |
| API | `http://localhost:8080/api/v1` | Through the portal; the API publishes no port of its own (ADR-006) |
| Swagger | `http://localhost:8080/swagger-ui.html` | Through the portal |
| Health | `http://localhost:8080/health` | Through the portal |
| PostgreSQL | `127.0.0.1:5432` | `5432` (`POSTGRES_PORT`), this machine only |

### 6. Test users

Created by the migrations. Local values only.

| Role | Email | Password | Can do |
|---|---|---|---|
| `ADMIN` | `admin@budgettracker.test` | `Admin-2026!` | Everything a `USER` can, and list the accounts |
| `USER` | `usuario@budgettracker.test` | `Usuario-2026!` | Manage only their own data |

Both start with empty screens. To see the application with data, load the demo
business once, using `sh` or Git's `sh.exe` as in step 4:

```bash
sh scripts/seed-demo.sh
```

| Role | Email | Password |
|---|---|---|
| `USER` | `demo@budgettracker.test` | `Demo-2026!` |

It adds a stationery shop with categories, 24 transactions from September to
November 2026, an inactive category (BR-05) and budgets. If the account exists it
changes nothing.

### 7. Run the tests

**API**, from `bud-tack-api`. The last two need Docker, for a throwaway PostgreSQL:

```bash
dotnet build -c Release
dotnet test --project tests/unit -c Release --no-build
dotnet test --project tests/boundary -c Release --no-build
dotnet test --project tests/module-integration -c Release --no-build
dotnet test --project tests/end-to-end -c Release --no-build
```

**Portal**, from `bud-tack-portal`:

```bash
npm ci
npx ng test --watch=false
```

**Postman**, from `bud-tack-api`, with the application running. It signs in with both
roles, checks `401` and `403`, and runs a full CRUD of categories with rule BR-05.
It can also be imported into Postman.

```bash
npx newman run postman/budget-tracker.postman_collection.json
```

### 8. Stop the application and delete the data

```bash
docker compose down            # stop; the data stays
docker compose down --volumes  # stop and delete the data
```

### 9. Frequent problems

These are errors the team met and solved while building the project.

| Problem | Cause | Solution |
|---|---|---|
| PowerShell says `sh` is not recognized | PowerShell has no `sh` | Run the script with `& "C:\Program Files\Git\bin\sh.exe" scripts/<name>.sh`, or open Git Bash |
| Docker cannot find `../bud-tack-api` or another repository | The clones are not side by side | Clone them as in step 2, or set `DB_SOURCE`, `API_SOURCE` and `PORTAL_SOURCE` in `.env` to the real paths |
| The `api` service never becomes healthy and its log says `AUTH_JWT_SECRET is not set` | `.env` was copied before that variable existed | Copy `.env.example` to `.env` again, then repeat step 4 |
| Sign-in refuses a test user with the right password | An older database volume or image from before the seed users and BCrypt | Run `docker compose down --volumes`, then repeat step 4 with `--build` |

## Reference

### Services

| Service | Image | Published | Does |
|---|---|---|---|
| `postgres` | `postgres:17-alpine` | `127.0.0.1:5432` | The one database and its two roles |
| `migrate` | `dotnet/sdk:10.0`, running `bud-tack-db`'s source | — | `up` as `bud_tack_migrator`, then exits |
| `api` | `bud-tack-api:${APP_TAG}`, built from `API_SOURCE` | — | Starts once `migrate` exited with 0; connects as `bud_tack_app`; signs tokens with `AUTH_JWT_SECRET`; admits only `CORS_ALLOWED_ORIGIN` across origins |
| `portal` | `bud-tack-portal:${APP_TAG}`, built from `PORTAL_SOURCE` | `${PORTAL_PORT}` → 8080 | Serves Angular; forwards `/api/`, `/health` and Swagger to `api:3000` |

Both images carry the same `APP_TAG` (`10-devops/ci-cd.md`). `verify-stack.sh`
checks `/`, a deep link, `/health`, `/api/` and Swagger through the portal, `401`
without a token, `200` as `ADMIN` and `403` as `USER`, that the API publishes no
port, and that the migrations created the module schemas.

### PostgreSQL alone

For `dotnet run` in `bud-tack-db` and `bud-tack-api` during development:

```bash
docker compose up -d --wait postgres
sh scripts/verify-roles.sh
```

| Role | Password (local) | Used by | Can |
|---|---|---|---|
| `bud_tack_migrator` | `dev` | `bud-tack-db` (`MIGRATOR_DATABASE_URL`) | Own the database `app_dev`, create schemas and tables |
| `bud_tack_app` | `dev` | `bud-tack-api` (`DATABASE_URL`) | Connect only; table rights come from the migrations |

The roles are created by `init/01-roles.sh` on the **first** start only. After
changing a password in `.env`, recreate the volume with `docker compose down
--volumes`, then `up` again. The schemas `identity` and `transactions` are not
created here: they are the first migration in `bud-tack-db` (ADR-005).

## Pipeline

| Job | Checks |
|---|---|
| Local PostgreSQL and roles | `docker compose config`, PostgreSQL healthy, `verify-roles.sh` |
| Whole application through the portal | The `develop` branch of `bud-tack-db`, `-api` and `-portal`, built with one tag; `verify-stack.sh` |

The other repositories are private, so the second job needs the repository
secret **`BUD_TACK_REPOS_TOKEN`**: a fine-grained personal access token whose
resource owner is `code-corhuila`, with access to `bud-tack-db`, `bud-tack-api`
and `bud-tack-portal` only, and the permission **Contents: Read-only**. Create
it in GitHub → Settings → Developer settings → Fine-grained tokens, then add it
in this repository → Settings → Secrets and variables → Actions.

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
