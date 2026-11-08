# bud-tack-infra

> Budget Tracker - Infrastructure: Compose/IaC, environments and secrets

Part of the **Budget Tracker** full-stack project — team `budget-tracker`, Complementaria III - Desarrollo Fullstack 2026-B.
Governance and documentation live in [`bud-tack-docs`](https://github.com/code-corhuila/bud-tack-docs).

## Local PostgreSQL

The one database of the application, with the two roles that
`00-governance/security-policy.md` requires. Needs Docker with Compose.

```bash
cp .env.example .env
docker compose up -d --wait postgres
sh scripts/verify-roles.sh
```

| Role | Password (local) | Used by | Can |
|---|---|---|---|
| `bud_tack_migrator` | `dev` | `bud-tack-db` (`MIGRATOR_DATABASE_URL`) | Own the database `app_dev`, create schemas and tables |
| `bud_tack_app` | `dev` | `bud-tack-api` (`DATABASE_URL`) | Connect only; table rights come from the migrations |

The roles are created by `init/01-roles.sh` on the **first** start only. After
changing a password in `.env`, recreate the volume: `docker compose down --volumes`,
then `up` again. The schemas `identity` and `transactions` are not created here:
they are the first migration in `bud-tack-db` (ADR-005).

PostgreSQL publishes port 5432 on `127.0.0.1` only: reachable from your machine
for `dotnet run` in `bud-tack-db` and `bud-tack-api`, not from the network.

## The whole application

PostgreSQL, the migrations, the API and the portal, from your clones of the
other repositories. Only the portal publishes a port: the browser and `curl`
reach the API through it, on one origin (ADR-006).

```bash
cp .env.example .env                                   # once; set the *_SOURCE paths if your clones are not side by side
docker compose up -d --build --wait                    # a few minutes the first time
sh scripts/verify-stack.sh
```

Then open `http://localhost:8080`. Swagger is at `http://localhost:8080/swagger-ui.html`.

The migrations create one test user per role (local only):

| Role | Email | Password |
|---|---|---|
| `ADMIN` | `admin@budgettracker.test` | `Admin-2026!` |
| `USER` | `usuario@budgettracker.test` | `Usuario-2026!` |

| Service | Image | Published | Does |
|---|---|---|---|
| `postgres` | `postgres:17-alpine` | `127.0.0.1:5432` | The one database and its two roles |
| `migrate` | `dotnet/sdk:10.0`, running `bud-tack-db`'s source | — | `up` as `bud_tack_migrator`, then exits |
| `api` | `bud-tack-api:${APP_TAG}`, built from `API_SOURCE` | — | Starts once `migrate` exited with 0; connects as `bud_tack_app`; signs tokens with `AUTH_JWT_SECRET`; admits only `CORS_ALLOWED_ORIGIN` across origins |
| `portal` | `bud-tack-portal:${APP_TAG}`, built from `PORTAL_SOURCE` | `${PORTAL_PORT}` → 8080 | Serves Angular; forwards `/api/`, `/health` and Swagger to `api:3000` |

`AUTH_JWT_SECRET` (at least 32 bytes) must be in `.env` for the API to start;
a `.env` copied before HU-01 needs the two `AUTH_JWT_*` lines of `.env.example`.
PostgreSQL alone does not need them.

Both images carry the same `APP_TAG` (`10-devops/ci-cd.md`). `verify-stack.sh`
checks `/`, a deep link, `/health`, `/api/` and Swagger through the portal, `401`
without a token, `200` as `ADMIN` and `403` as `USER`, that the API publishes no
port, and that the migrations created the module schemas.

### Demo data

For a demo, fill the running application with one business, through the API:

```bash
sh scripts/seed-demo.sh
```

It creates the account below (local only; override with `DEMO_EMAIL`,
`DEMO_PASSWORD` and `DEMO_NAME`), a stationery shop with income and expense
categories, 24 transactions from September to November 2026 (some with several
lines, so the history has two pages), an inactive category still seen in the
history (BR-05), and budgets for October to December.

| Email | Password |
|---|---|
| `demo@budgettracker.test` | `Demo-2026!` |

Run it once: if the account exists it changes nothing. To start over, recreate
the volume with `docker compose down --volumes`, then `up` again.

Stop everything with `docker compose down`; add `--volumes` to start from an
empty database. `docker compose up -d --wait postgres` starts PostgreSQL alone.

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
