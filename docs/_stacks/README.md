# _stacks — Stack Guides

## Purpose

Fourteen numbered sections say what to build and why, in words no language owns.
This folder is the one place that answers *how it looks on disk*: a real folder
tree, a boundary configuration that runs, the migration library, and commands
that can be pasted into a terminal.

## Documents

| Document | Answers | Priority |
|---|---|---|
| [csharp-aspnet.md](./csharp-aspnet.md) | What is the concrete shape in C# and ASP.NET Core, checked by NetArchTest? | ⭐ |

This project's language is C#, so this is the only guide kept; the Java, Node,
Python and PHP guides the framework ships with were deleted in the same pull
request, per [`ADR-002`](../05-architecture/decisions/records/ADR-002-angular-dotnet-postgresql-stack.md).
A future change of backend language replaces this guide the same way, and the
Angular frontend's own build tooling stays out of this folder — see Out of scope.

What the guide renders, filled in with this project's own modules and rules:

| Heading | Renders |
|---|---|
| `Module layout` | The stack-agnostic tree in [`../05-architecture/module-structure.md`](../05-architecture/module-structure.md), worked through with `identity`, `transactions` and `dashboard` |
| `Boundary enforcement` | The tool fixed for that stack in [`../05-architecture/boundary-enforcement.md`](../05-architecture/boundary-enforcement.md) — where its file lives, how it is invoked, what a violation prints |
| `Migrations` | The one-sequence-per-application policy in [`../06-data/migrations.md`](../06-data/migrations.md) |
| `Test layers` | `unit`, `module-integration` and `end-to-end` from [`../11-quality/testing-strategy.md`](../11-quality/testing-strategy.md), named verbatim |
| `Commands` | The five stages in [`../10-devops/ci-cd.md`](../10-devops/ci-cd.md), producing one artifact |

## Out of scope

These guides describe **one build, one artifact, one database**. Permanently
out of scope here, not deferred: a build per module, a pipeline per module, a
second artifact from the same commit, network calls between parts of this
application, and the tooling that exists to split an application apart —
microservices, service mesh, api gateway, service discovery, saga
orchestration, circuit breaker, and distributed transaction coordination.

Also out of scope: front-end build tooling, which belongs to
[`../12-ux-ui/README.md`](../12-ux-ui/README.md), and cloud provider setup,
which belongs to [`../10-devops/README.md`](../10-devops/README.md).

Distributed-system concerns belong to the
[microservices governance framework](https://github.com/jesusarielgb-works/microservices-governance-framework).

## Ready when

- [x] One guide is chosen, the other three are deleted from this folder, and their rows are removed from the Documents table above
- [x] The chosen guide's INSTRUCTIONS block is removed
- [ ] The tree in `csharp-aspnet.md` exists on disk under this project's own module names, and `catalog` and `billing` appear nowhere in the source — open until `bud-tack-api` exists
- [ ] The boundary configuration file the guide names is committed, and has failed once on a deliberate violation — open until `bud-tack-api` exists
- [ ] Every command in the `Commands` table has been run once on a fresh clone — open until `bud-tack-api` exists
- [ ] Every version in the `Version baseline` table matches what the build actually resolves — checked against each package's registry page on 2026-10-30; re-verify once a build exists

---

**Related:** [`../05-architecture/README.md`](../05-architecture/README.md) · [`../10-devops/README.md`](../10-devops/README.md)
