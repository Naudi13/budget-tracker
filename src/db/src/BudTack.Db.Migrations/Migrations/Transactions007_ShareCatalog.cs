using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// One business per installation (ADR-009): categories and budgets stop having an
/// owner, and a category name becomes unique per type. Names that were unique only
/// per owner get a number, so no row is lost. Transactions keep their owner.
/// </summary>
[Migration(202611092101, "Share categories and budgets")]
public sealed class Transactions007_ShareCatalog : Migration
{
    private const string Categories = "transactions.categories";
    private const string Budgets = "transactions.budgets";

    public override void Up()
    {
        Execute.Sql($"""
            UPDATE {Categories} c SET name = left(c.name, 90) || ' (' || n.position || ')'
            FROM (SELECT id, row_number() OVER (PARTITION BY type, lower(name) ORDER BY created_at, id) AS position
                  FROM {Categories}) n
            WHERE n.id = c.id AND n.position > 1;

            DROP INDEX transactions.idx_categories_owned_by_type_name;
            ALTER TABLE {Categories} DROP COLUMN owned_by;
            CREATE UNIQUE INDEX idx_categories_type_name ON {Categories} (type, lower(name));

            DROP INDEX transactions.idx_budgets_owned_by_period_start;
            ALTER TABLE {Budgets} DROP COLUMN owned_by;
            CREATE INDEX idx_budgets_period_start ON {Budgets} (period_start DESC);
            """);
    }

    public override void Down()
    {
        Execute.Sql($"""
            DROP INDEX transactions.idx_budgets_period_start;
            ALTER TABLE {Budgets} ADD COLUMN owned_by uuid;
            UPDATE {Budgets} SET owned_by = created_by;
            ALTER TABLE {Budgets} ALTER COLUMN owned_by SET NOT NULL;
            CREATE INDEX idx_budgets_owned_by_period_start ON {Budgets} (owned_by, period_start DESC);

            DROP INDEX transactions.idx_categories_type_name;
            ALTER TABLE {Categories} ADD COLUMN owned_by uuid;
            UPDATE {Categories} SET owned_by = created_by;
            ALTER TABLE {Categories} ALTER COLUMN owned_by SET NOT NULL;
            CREATE UNIQUE INDEX idx_categories_owned_by_type_name ON {Categories} (owned_by, type, lower(name));
            """);
    }
}
