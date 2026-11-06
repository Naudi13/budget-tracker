using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// <c>transactions.budgets</c> (HU-07): a spending limit for one category over
/// a period, owned by one user (<c>owned_by</c>). The amount is
/// <c>numeric(14,2)</c> above zero, and the period ends after it starts (the
/// Budget invariants of 02-domain/entities-and-rules.md), checked here so a
/// change (BR-06) can never leave a broken row. A budget is deleted physically
/// (BR-06). The key to its category is <c>ON DELETE RESTRICT</c>: a category
/// that owns a budget cannot be deleted (BR-05).
/// </summary>
[Migration(202611061710, "Create transactions.budgets")]
public sealed class Transactions006_CreateBudgets : Migration
{
    private const string TransactionsSchema = "transactions";
    private const string Table = "budgets";

    public override void Up()
    {
        Create.Table(Table).InSchema(TransactionsSchema)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_budgets")
            .WithColumn("owned_by").AsGuid().NotNullable()
            .WithColumn("category_id").AsGuid().NotNullable()
            .WithColumn("amount").AsDecimal(14, 2).NotNullable()
            .WithColumn("period_start").AsDate().NotNullable()
            .WithColumn("period_end").AsDate().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_by").AsGuid().NotNullable();

        // Written in SQL so the rule reads RESTRICT, not the generator's NO ACTION.
        Execute.Sql(
            $"ALTER TABLE {TransactionsSchema}.{Table} " +
            "ADD CONSTRAINT fk_budgets_category_id FOREIGN KEY (category_id) " +
            $"REFERENCES {TransactionsSchema}.categories (id) ON DELETE RESTRICT, " +
            "ADD CONSTRAINT ck_budgets_amount_positive CHECK (amount > 0), " +
            "ADD CONSTRAINT ck_budgets_period_ends_after_start CHECK (period_end > period_start);");

        // The owner's list, newest period first (budgets.md).
        Execute.Sql(
            "CREATE INDEX idx_budgets_owned_by_period_start " +
            $"ON {TransactionsSchema}.{Table} (owned_by, period_start DESC);");

        // The ?categoryId= filter, and the RESTRICT check when a category is deleted.
        Create.Index("idx_budgets_category_id").OnTable(Table).InSchema(TransactionsSchema)
            .OnColumn("category_id").Ascending();
    }

    public override void Down() =>
        Delete.Table(Table).InSchema(TransactionsSchema);
}
