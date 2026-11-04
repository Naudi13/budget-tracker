# 06 — Migrations

## One sequence for the whole application

There is one database, so there is one ordered sequence of migrations — never
one per module. A migration touching `identity`'s schema and one touching
`transactions`'s schema still apply in the same numbered sequence, from the one
project `BudTack.Db.Migrations` in the `bud-tack-db` repository, per
[`ADR-005`](../05-architecture/decisions/records/ADR-005-fluentmigrator-for-schema-migrations.md).
`bud-tack-api` holds no migrations, and `dashboard` never appears here: it owns
no table.

| Rule | Detail |
|---|---|
| Tool | FluentMigrator, a .NET library — no Java runtime is involved |
| Sequence | One version table, `public.schema_version`, one increasing version number for the entire database |
| Version number | `[Migration(yyyyMMddHHmm)]` — the UTC time the migration was written, so two branches never collide on a number |
| Migration scope | One logical change per migration class; never edit a migration already run anywhere |
| Module tag | The class name starts with its owning module and number, e.g. `Transactions003_CreateTransactions`, for traceability only — it does not split the sequence |
| Schema | Every table is created with `.InSchema("identity")` or `.InSchema("transactions")`, per [`database-conventions.md`](./database-conventions.md) |
| Role | The runner connects as the migrator role, never as the application's role — see [`security-policy.md`](../00-governance/security-policy.md) |

## Rollback policy

| Change type | Rule |
|---|---|
| Additive (new nullable column, new table, new index) | Inherits `AutoReversingMigration`, so `Down()` is generated |
| Destructive (drop column, drop table, rename) | Two phases: deploy N adds the replacement and stops using the old shape in code; deploy N+1 drops it |
| Any migration | Has a working `Down()`, verified in CI before merge — a migration with no `Down()` is incomplete |

CI proves the rollback with a round trip against an empty PostgreSQL: migrate
up to the latest version, down to version `0`, and up again. The second `up`
must succeed and leave the same schema as the first.

## Example

```csharp
[Migration(202611050900)]
public sealed class Transactions001_CreateCategories : AutoReversingMigration
{
    public override void Up() =>
        Create.Table("categories").InSchema("transactions")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("type").AsString(10).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true);
}
```

Audit columns are omitted above for brevity; every real table carries them,
per [`database-conventions.md`](./database-conventions.md). Project layout and
commands are in [`../_stacks/csharp-aspnet.md`](../_stacks/csharp-aspnet.md).

---

**Related:** [`./database-conventions.md`](./database-conventions.md) · [`../05-architecture/decisions/records/ADR-005-fluentmigrator-for-schema-migrations.md`](../05-architecture/decisions/records/ADR-005-fluentmigrator-for-schema-migrations.md)
