# 05 — Module Structure

## The tree

```
src/
├── identity/                 module — one bounded context
│   ├── api/                  public API of the module
│   │   ├── IdentityFacade    the only entry point other modules may import
│   │   └── contracts/        immutable request and response objects
│   └── internal/             everything else; unreachable from outside
│       ├── domain/           entities, value objects, BR-NN invariants
│       ├── application/      use cases, transaction boundaries
│       └── persistence/      queries and mappings for this module's tables
├── transactions/             same shape: api/ + internal/
├── dashboard/                same shape, without persistence/: it owns no tables
└── platform/                 technical code owned by no bounded context
    ├── web/                  HTTP endpoints; each maps a request to one facade call
    ├── config/               framework wiring — the only place it may live
    └── errors/               shared error types and the global handler
resources/
└── app-config                one configuration file per environment
tests/
├── unit/                     mirrors src/, one folder per module
├── module-integration/       one folder per module; imports api/ only
└── end-to-end/               the whole deployable; no module folders
```

## What each folder holds

| Folder | Contains | Never contains |
|---|---|---|
| `<module>/api/` | The facade and the objects that cross the boundary | Business rules, database access |
| `<module>/internal/` | Domain, application and persistence code | Imports from another module's `internal/` |
| `platform/web/` | Endpoints: request shape, one facade call, response format | Business rules, an import of any module's `internal/` |
| `platform/config/` | Framework wiring, dependency registration | Business rules |
| `platform/errors/` | Error types and the handler that renders them | Module-specific error handling |
| `tests/module-integration/` | Tests that call a module through its public API | Assertions about another module's internals |

## Rules that survive any stack

| Rule | Reason |
|---|---|
| One unit per file, filename equal to the unit name | Grep and stack traces stay readable |
| Objects crossing a boundary are immutable | A shared mutable object is a boundary hole |
| `internal/` is never named in an import outside its own module | This is the line the `boundary check` reads |
| Test folders mirror `src/` exactly | A missing test folder is visible without running anything |
| One migration project for the whole application, in `bud-tack-db`, never in the application tree | There is one database, so there is one ordered history — [`ADR-005`](./decisions/records/ADR-005-fluentmigrator-for-schema-migrations.md) |

## Concrete names for this project

The backend is C# on .NET; the concrete tree is in
[`../_stacks/csharp-aspnet.md`](../_stacks/csharp-aspnet.md), fixed by
[`ADR-003`](./decisions/records/ADR-003-project-per-module-boundary.md): one
project per module, and the endpoints in the host project's `platform/web/`.
Migrations live in `bud-tack-db`, not in this tree, per `ADR-005`.

This project is modular, not flat. The flat variant, with its four layer folders,
is described in [`layered-architecture.md`](./layered-architecture.md).

---

**Related:** [`./modular-monolith.md`](./modular-monolith.md) · [`./boundary-enforcement.md`](./boundary-enforcement.md)
