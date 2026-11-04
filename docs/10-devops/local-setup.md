# 10 — Local Setup

## Prerequisites

| Tool | Why | Verify with |
|---|---|---|
| Git | Clone `bud-tack-db` and `bud-tack-api` | `git --version` |
| PostgreSQL (local instance, or run in a container) | The one database this application uses — see [`database-conventions.md`](../06-data/database-conventions.md) | `psql --version` |
| .NET SDK | Build and run the application and its migrations — see [`_stacks/csharp-aspnet.md`](../_stacks/csharp-aspnet.md) for the exact version | `dotnet --version` |

No Java runtime is needed: the migrations are a .NET project
([`ADR-005`](../05-architecture/decisions/records/ADR-005-fluentmigrator-for-schema-migrations.md)).

## Steps

1. Clone `bud-tack-db` and `bud-tack-api` side by side.
2. In each, copy [`.env.example`](../.env.example) to `.env`; the defaults match a
   fresh local PostgreSQL instance and need no edits to start. `APP_PORT` is
   3000 — this framework's default port, used by every example URL here and by
   the API contracts. ASP.NET Core's Kestrel reads `ASPNETCORE_URLS`, not
   `APP_PORT`, directly — see the `local` command in
   [`csharp-aspnet.md`](../_stacks/csharp-aspnet.md).
3. Create the local database named in `DATABASE_URL`.
4. In `bud-tack-db`, run the pending migrations with
   `dotnet run --project src/BudTack.Db.Migrations -- up` — see
   [`migrations.md`](../06-data/migrations.md).
5. In `bud-tack-api`, restore dependencies with `dotnet restore` and start the
   application with the `local` command in
   [`csharp-aspnet.md`](../_stacks/csharp-aspnet.md).

## Verifying it worked

```bash
curl -i http://localhost:3000/health
```

A `200` response means the process is up, reading `.env`, and `/health`
reached the database — see [`observability.md`](../13-operations/observability.md)
for what the check reports. Anything else is a setup defect in this document,
not in the application — fix the document in the same pull request that fixes
the step.

The whole sequence above, start to this first successful request, is
**NFR-08**'s one-hour budget. A run that takes longer is a gap in this
document, reported the same way a bug is.

---

**Related:** [`./ci-cd.md`](./ci-cd.md) · [`../06-data/database-conventions.md`](../06-data/database-conventions.md)
