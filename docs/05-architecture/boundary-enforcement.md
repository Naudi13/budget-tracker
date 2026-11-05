# 05 — Boundary Enforcement

## What is being enforced

One rule, in every language: **a module may import another module's `api/`, and
never its `internal/`.** Names come from [`module-boundaries.md`](../02-domain/module-boundaries.md),
folders from [`module-structure.md`](./module-structure.md).

## Tool for this stack

| Stack | Tool | The rule you write |
|---|---|---|
| C# / .NET | The `internal` access modifier, plus **NetArchTest.Rules** | A test asserting no type in a module's `Internal` namespace is public, and none is depended on from another module |

The tools for the other stacks are described in
[`../_stacks/README.md`](../_stacks/README.md); this project does not run them.

## Two lines of defence

| Line | Catches | When |
|---|---|---|
| One project per module, everything but the facade `internal` | A project using another module's non-public type | The compiler refuses the build |
| A NetArchTest suite | A type made `public` inside `Internal` by mistake, or a dependency by namespace | The `boundary check` stage |

```csharp
// tests/boundary/ModuleBoundaryTests.cs — an ordinary test in the build
var dependency = Types.InAssembly(typeof(TransactionsModule).Assembly)
    .That().ResideInNamespace("BudTack.Transactions")
    .ShouldNot().HaveDependencyOn("BudTack.Identity.Internal")
    .GetResult();
Assert.True(dependency.IsSuccessful);

var visibility = Types.InAssembly(typeof(TransactionsModule).Assembly)
    .That().ResideInNamespace("BudTack.Transactions.Internal")
    .ShouldNot().BePublic()
    .GetResult();
Assert.True(visibility.IsSuccessful);
```

`TransactionsModule` is one empty marker type per module assembly, so a test can
name the assembly without touching internals. The same two checks are written
once per module.

## Where it runs

| Stage | Scope | On failure |
|---|---|---|
| Pre-commit hook, optional | Changed files only | The commit is refused locally |
| `boundary check`, defined in [`../10-devops/ci-cd.md`](../10-devops/ci-cd.md) | The whole tree, on every pull request | The build fails; the pull request cannot merge |

It runs before the test stages: a boundary violation makes test results
uninteresting. The suite is its own project: `dotnet test --project tests/boundary`.

## Breaking the rule on purpose

In C# the exemption is an `InternalsVisibleTo` grant, or making a type public.
Either needs a comment naming the `HU-NN` that forced it, and an ADR recording
why — see [`decisions/README.md`](./decisions/README.md). A grant to a test
assembly is not a violation; a grant to another module's assembly, with no ADR,
is a defect.

---

**Related:** [`./module-structure.md`](./module-structure.md) · [`../02-domain/module-boundaries.md`](../02-domain/module-boundaries.md)
