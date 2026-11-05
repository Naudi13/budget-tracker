# _stacks — C# and ASP.NET Core

## Version baseline

Checked **2026-11-05**, against the code that runs in `bud-tack-api` and `bud-tack-db`. Re-check every row before adopting this guide.

| Component | Version | Verify with |
|---|---|---|
| .NET SDK | 10.0.x (LTS; .NET 8 reaches end of support 2026-11-10) | `dotnet --version` |
| ASP.NET Core | 10.0.x, ships with the SDK | `dotnet --list-sdks` |
| NetArchTest.Rules | 1.3+, NuGet, targets .NET Standard 2.0 | `dotnet list package \| grep NetArchTest` |
| xUnit.v3 | 4.0+ (the legacy `xunit` package is deprecated) | `dotnet list package \| grep xunit` |
| Microsoft.Testing.Platform | The only runner xUnit.v3 supports on the .NET 10 SDK; turned on in `global.json` with `"test": { "runner": "Microsoft.Testing.Platform" }`. Coverage: `Microsoft.Testing.Extensions.CodeCoverage` | `cat global.json` |
| Testcontainers.PostgreSql | 4.x | `dotnet list package \| grep Testcontainers` |
| FluentMigrator.Runner.Postgres | 8.0.x, NuGet — `bud-tack-db` only. Not the umbrella `FluentMigrator.Runner`, which pulls the SQL Server processor and a vulnerable `System.Security.Cryptography.Pkcs` | `dotnet list package \| grep FluentMigrator` |

## Module layout

Per [`ADR-003`](../05-architecture/decisions/records/ADR-003-project-per-module-boundary.md), a module is a project, not a folder. `Platform/Web/` uses MVC
controllers, not Minimal API — the team's choice, since it already knows MVC.

```
BudTack.slnx
src/
├── BudTack.Api/                     host project — Platform/Web (one `[ApiController]` per module), Platform/Config, Platform/Errors, Platform/Logging, Program.cs, Properties/launchSettings.json
├── BudTack.Identity/                module project — IdentityModule.cs is the one public marker
│   ├── Api/IdentityFacade.cs + Api/Contracts/
│   └── Internal/Domain/, Internal/Application/, Internal/Persistence/
├── BudTack.Transactions/            same shape — TransactionsFacade, owns transactions, categories, subcategories, budgets
└── BudTack.Dashboard/               same shape, no Internal/Persistence/ — owns no table
appsettings.{Environment}.json in BudTack.Api · tests/ boundary/, unit/, module-integration/, end-to-end/ — one project each
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
`DashboardModule` are public static markers, one per module, so a test names the
assembly without importing `Internal/`. Each marker also carries the module's
one registration method — `services.AddIdentityModule()`, then
`AddTransactionsModule()`, then `AddDashboardModule()`, in the order of
[`module-catalog.md`](../09-modules/module-catalog.md) — so the host wires a
module without seeing its internals. A facade has an `internal` constructor:
only its own module can build it.

## Migrations

FluentMigrator, per [`ADR-005`](../05-architecture/decisions/records/ADR-005-fluentmigrator-for-schema-migrations.md) — no Java runtime. The console project `src/BudTack.Db.Migrations/` lives in `bud-tack-db`,
never in `BudTack.slnx`: one class per migration under `Migrations/`, and `Program.cs` calling `IMigrationRunner.MigrateUp()`
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
| `build` | `dotnet build -c Release` | Compiles every project in `BudTack.slnx`, warnings as errors; fails on an unresolved reference |
| `boundary check` | `dotnet test --project tests/boundary -c Release --no-build` | The NetArchTest suite above, before any other test |
| `test` | `dotnet test --project tests/unit -c Release --no-build --coverage --coverage-output-format cobertura`, then the same for `tests/module-integration` | `unit` and `module-integration`, one project per command (Microsoft.Testing.Platform takes `--project`, not a list of paths). Coverage lands in a `coverage.cobertura.xml` whose per-file counters apply **NFR-07**: 80% on changed files, 90% on `BudTack.Transactions` |
| `package` | `docker build -t bud-tack-api:$(git rev-parse --short HEAD) .` | The `Dockerfile` runs `dotnet publish` and wraps the one output in the `aspnet:10.0` image, as a non-root user on port 3000, tagged with the commit |
| `deploy` | `dotnet test --project tests/end-to-end` | `end-to-end` against the promoted artifact |
| local | In `bud-tack-db`: `dotnet run --project src/BudTack.Db.Migrations -- up`; then in `bud-tack-api`: `dotnet run --project src/BudTack.Api`, or F5 in the IDE | Applies pending migrations, then starts the API on `http://localhost:3000`. The API reads the nearest `.env` (`DATABASE_URL`), and the versioned launch profile sets `ASPNETCORE_URLS`; a variable already set in the environment always wins. CI in `bud-tack-db` runs `up`, `down`, `up` — see [`migrations.md`](../06-data/migrations.md) |

The IDE must ship MSBuild 18: Visual Studio 2026, VS Code with C# Dev Kit, or
Rider 2025.3+. Visual Studio 2022 cannot load a .NET 10 SDK project and shows an
empty solution.

---

**Related:** [`./README.md`](./README.md) · [`../05-architecture/boundary-enforcement.md`](../05-architecture/boundary-enforcement.md)
