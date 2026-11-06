using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// <c>transactions.transaction_details</c> (HU-04): the lines of a transaction,
/// each an amount against one subcategory. A line has no type column: its type
/// is its subcategory's category's (BR-01). Amounts are <c>numeric(14,2)</c>,
/// strictly above zero. Both keys are <c>ON DELETE RESTRICT</c>: a subcategory a
/// line uses cannot be deleted, only deactivated (BR-05), and a transaction is
/// never physically deleted while it has lines (BR-04).
/// </summary>
[Migration(202611061611, "Create transactions.transaction_details")]
public sealed class Transactions004_CreateTransactionDetails : Migration
{
    private const string TransactionsSchema = "transactions";
    private const string Table = "transaction_details";

    public override void Up()
    {
        Create.Table(Table).InSchema(TransactionsSchema)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_transaction_details")
            .WithColumn("transaction_id").AsGuid().NotNullable()
            .WithColumn("subcategory_id").AsGuid().NotNullable()
            .WithColumn("amount").AsDecimal(14, 2).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_by").AsGuid().NotNullable();

        // Written in SQL so the rule reads RESTRICT, not the generator's NO ACTION.
        Execute.Sql(
            $"ALTER TABLE {TransactionsSchema}.{Table} " +
            "ADD CONSTRAINT fk_transaction_details_transaction_id FOREIGN KEY (transaction_id) " +
            $"REFERENCES {TransactionsSchema}.transactions (id) ON DELETE RESTRICT, " +
            "ADD CONSTRAINT fk_transaction_details_subcategory_id FOREIGN KEY (subcategory_id) " +
            $"REFERENCES {TransactionsSchema}.subcategories (id) ON DELETE RESTRICT, " +
            "ADD CONSTRAINT ck_transaction_details_amount_positive CHECK (amount > 0);");

        // Reading a transaction's lines, and the RESTRICT check when a subcategory is deleted.
        Create.Index("idx_transaction_details_transaction_id").OnTable(Table).InSchema(TransactionsSchema)
            .OnColumn("transaction_id").Ascending();
        Create.Index("idx_transaction_details_subcategory_id").OnTable(Table).InSchema(TransactionsSchema)
            .OnColumn("subcategory_id").Ascending();
    }

    public override void Down() =>
        Delete.Table(Table).InSchema(TransactionsSchema);
}
