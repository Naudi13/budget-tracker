# bud-tack-infra

> Budget Tracker - Infrastructure: Compose/IaC, environments and secrets

Part of the **Budget Tracker** full-stack project — team `budget-tracker`, Complementaria III - Desarrollo Fullstack 2026-B.
Governance and documentation live in [`bud-tack-docs`](https://github.com/code-corhuila/bud-tack-docs).

## Local PostgreSQL

The one database of the application, with the two roles that
`00-governance/security-policy.md` requires. Needs Docker with Compose.

```bash
cp .env.example .env
docker compose up -d --wait
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
docker compose --profile app up -d --build --wait      # about 1 minute the first time
sh scripts/verify-stack.sh
```

Then open `http://localhost:8080`.

| Service | Image | Published | Does |
|---|---|---|---|
| `postgres` | `postgres:17-alpine` | `127.0.0.1:5432` | The one database and its two roles |
| `migrate` | `dotnet/sdk:10.0`, running `bud-tack-db`'s source | — | `up` as `bud_tack_migrator`, then exits |
| `api` | `bud-tack-api:${APP_TAG}`, built from `API_SOURCE` | — | Starts once `migrate` exited with 0; connects as `bud_tack_app` |
| `portal` | `bud-tack-portal:${APP_TAG}`, built from `PORTAL_SOURCE` | `${PORTAL_PORT}` → 8080 | Serves Angular; forwards `/api/` and `/health` to `api:3000` |

Both images carry the same `APP_TAG` (`10-devops/ci-cd.md`). `verify-stack.sh`
checks `/`, a deep link, `/health` and `/api/` through the portal, that the API
publishes no port, and that the migrations created the module schemas.

Without `--profile app`, `docker compose up -d --wait` starts PostgreSQL alone,
as before. Stop everything with `docker compose --profile app down`; add
`--volumes` to start from an empty database.

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
