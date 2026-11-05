# 10 — Local Setup

## Prerequisites

| Tool | Why | Verify with |
|---|---|---|
| Git | Clone `bud-tack-infra`, `-db`, `-api` and `-portal` | `git --version` |
| Docker with Compose | Runs the local PostgreSQL from `bud-tack-infra` — see [`database-conventions.md`](../06-data/database-conventions.md) | `docker compose version` |
| .NET SDK | Build and run the application and its migrations — see [`_stacks/csharp-aspnet.md`](../_stacks/csharp-aspnet.md) for the exact version | `dotnet --version` |
| Node.js LTS and npm | Angular's own toolchain, for `bud-tack-portal` only; the version is the one its `package.json` `engines` field names (22.12 or later) | `node --version` |
| An IDE with MSBuild 18 (optional) | Visual Studio 2026, VS Code with C# Dev Kit, or Rider 2025.3+. Visual Studio 2022 cannot open the .NET 10 projects | — |

No Java runtime is needed: the migrations are a .NET project
([`ADR-005`](../05-architecture/decisions/records/ADR-005-fluentmigrator-for-schema-migrations.md)).
A PostgreSQL installed on the machine works too, in place of step 2.

## Steps

1. Clone `bud-tack-infra`, `bud-tack-db`, `bud-tack-api` and `bud-tack-portal`
   side by side. In each, copy its `.env.example` to `.env`; the defaults, the
   same as this repository's [`.env.example`](../.env.example), match the local
   database and need no edits to start.
2. In `bud-tack-infra`, start PostgreSQL with `docker compose up -d`. It
   creates the database named in `DATABASE_URL`, the application's role and the
   migrator role from [`security-policy.md`](../00-governance/security-policy.md).
3. In `bud-tack-db`, apply the pending migrations with
   `dotnet run --project src/BudTack.Db.Migrations -- up` — see
   [`migrations.md`](../06-data/migrations.md).
4. In `bud-tack-api`, run `dotnet run --project src/BudTack.Api` (or F5 in the
   IDE) — the `local` command in [`csharp-aspnet.md`](../_stacks/csharp-aspnet.md).
   It listens on port 3000, the port every example URL and API contract uses,
   set by `ASPNETCORE_URLS` in `.env` and in the versioned launch profile.
5. In `bud-tack-portal`, run `npm ci`, then `npm start`. The Angular dev server
   listens on `http://localhost:4200` and its `proxy.conf.json` forwards `/api/`
   and `/health` to `localhost:3000`, so the browser sees one origin and no CORS
   is needed — see [`environments.md`](./environments.md).

## Verifying it worked

```bash
curl -i http://localhost:3000/health
```

A `200` response means the API is up, reading `.env`, and `/health` reached the
database — see [`observability.md`](../13-operations/observability.md) for what
the check reports. Then open `http://localhost:4200`: until HU-01 ships, the
start page says whether the API and its database are up; after it, the sign-in
screen loads.

## The whole application in containers

To run what `staging` runs instead of steps 3 to 5, in `bud-tack-infra` set
`DB_SOURCE`, `API_SOURCE` and `PORTAL_SOURCE` in `.env` if your clones are not
side by side, then:

```bash
docker compose --profile app up -d --build --wait
sh scripts/verify-stack.sh
```

The portal answers on `http://localhost:8080` and is the only published port:
the API is reachable through it alone, as in every deployed environment.
Anything else is a setup defect in this document, not in the application — fix
the document in the same pull request that fixes the step.

The whole sequence above, start to this first successful request, is
**NFR-08**'s one-hour budget. A run that takes longer is a gap in this
document, reported the same way a bug is.

---

**Related:** [`./ci-cd.md`](./ci-cd.md) · [`../06-data/database-conventions.md`](../06-data/database-conventions.md)
