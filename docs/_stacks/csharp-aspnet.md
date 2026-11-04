# _stacks — C# and ASP.NET Core

## Version baseline

Checked **2026-10-30**. Re-check every row before adopting this guide.

| Component | Version | Verify with |
|---|---|---|
| .NET SDK | 10.0.x (LTS; .NET 8 reaches end of support 2026-11-10) | `dotnet --version` |
| ASP.NET Core | 10.0.x, ships with the SDK | `dotnet --list-sdks` |
| NetArchTest.Rules | 1.3+, NuGet, targets .NET Standard 2.0 | `dotnet list package \| grep NetArchTest` |
| xUnit.v3 | 4.0+ (the legacy `xunit` package is deprecated) | `dotnet list package \| grep xunit` |
| Testcontainers.PostgreSql | 4.x | `dotnet list package \| grep Testcontainers` |
| FluentMigrator.Runner | 8.0.x, NuGet, PostgreSQL processor included — `bud-tack-db` only | `dotnet list package \| grep FluentMigrator` |

## Module layout

Per [`ADR-003`](../05-architecture/decisions/records/ADR-003-project-per-module-boundary.md), a module is a project, not a folder. `Platform/Web/` uses MVC
controllers, not Minimal API — the team's choice, since it already knows MVC.

```
BudTack.sln
src/
├── BudTack.Api/                     host project — Platform/Web (one `[ApiController]` per module), Platform/Config, Platform/Errors, Program.cs
├── BudTack.Identity/                module project — IdentityModule.cs is the one public marker
│   ├── Api/IdentityFacade.cs + Api/Contracts/
│   └── Internal/Domain/, Internal/Application/, Internal/Persistence/
├── BudTack.Transactions/            same shape — TransactionsFacade, owns transactions, categories, subcategories, budgets
└── BudTack.Dashboard/               same shape, no Internal/Persistence/ — owns no table
resources/ appsettings.{Environment}.json · tests/ unit/, module-integration/, end-to-end/, boundary/
```

`Internal/` is the C# `internal` modifier: `BudTack.Api` cannot reference
`BudTack.Transactions.Internal.*` by mistake — a compile error, not a lint
finding. NetArchTest below only catches a type made `public` that should not be.

## Boundary enforcement

```csharp
// tests/boundary/ModuleBoundaryTests.cs — run by `dotnet test`
var result = Types.InAssembly(typeof(TransactionsModule).Assembly)
    .That().ResideInNamespace("BudTack.Transactions")
    .ShouldNot().HaveDependencyOn("BudTack.Identity.Internal")
    .GetResult();
Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames));
```

A second assertion swaps `HaveDependencyOn` for `.ResideInNamespace("BudTack.Transactions.Internal").ShouldNot().BePublic()`. `TransactionsModule`, `IdentityModule`,
`DashboardModule` are empty public markers, one per module, so a test names the
assembly without importing `Internal/`.

## Migrations

FluentMigrator, per [`ADR-005`](../05-architecture/decisions/records/ADR-005-fluentmigrator-for-schema-migrations.md) — no Java runtime. The console project `src/BudTack.Db.Migrations/` lives in `bud-tack-db`,
never in `BudTack.sln`: one class per migration under `Migrations/`, and `Program.cs` calling `IMigrationRunner.MigrateUp()`
for `up` or `MigrateDown(0)` for `down`. It reads `MIGRATOR_DATABASE_URL`, the API reads `DATABASE_URL`; both are `postgresql://`
URIs parsed once at startup into an `NpgsqlConnectionStringBuilder`, since Npgsql takes key-value pairs.

## Test layers

| Layer | Runner | Shape |
|---|---|---|
| `unit` | xUnit.v3, `NSubstitute` for collaborators, no database | Constructs a use-case class directly from `Internal/Application/` |
| `module-integration` | xUnit.v3 with `Testcontainers.PostgreSql`, a real schema per run | References only `BudTack.Transactions.Api`; calls `TransactionsFacade` from outside the project |
| `end-to-end` | xUnit.v3 with `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) | `POST /api/v1/transactions` against the host exactly as it deploys |

## Commands

| Stage | Command | What it does |
|---|---|---|
| `build` | `dotnet build` | Compiles every project in `BudTack.sln`; fails on an unresolved reference |
| `boundary check` | `dotnet test tests/boundary` | The NetArchTest suite above, before any other test |
| `test` | `dotnet test tests/unit tests/module-integration --collect:"XPlat Code Coverage"` | `unit` and `module-integration`. Coverage lands in `coverage.cobertura.xml`, whose per-file counters apply **NFR-07**: 80% on changed files, 90% on `BudTack.Transactions` |
| `package` | `dotnet publish src/BudTack.Api -c Release -o publish`, then `docker build -t app:$(git rev-parse --short HEAD) .` | One published output, wrapped in one image tagged with the commit |
| `deploy` | `dotnet test tests/end-to-end` | `end-to-end` against the promoted artifact |
| local | In `bud-tack-db`: `dotnet run --project src/BudTack.Db.Migrations -- up`; then in `bud-tack-api`: `ASPNETCORE_URLS=http://localhost:3000 dotnet run --project src/BudTack.Api` | Applies pending migrations, then starts the API. Kestrel reads `ASPNETCORE_URLS`, not `APP_PORT`, so it is set explicitly to the framework's default port. CI in `bud-tack-db` runs `up`, `down`, `up` — see [`migrations.md`](../06-data/migrations.md) |

---

**Related:** [`./README.md`](./README.md) · [`../05-architecture/boundary-enforcement.md`](../05-architecture/boundary-enforcement.md)
