using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// <c>transactions.transactions</c> (HU-04, HU-05, HU-06): the header of a
/// recorded movement of money, owned by one user (<c>owned_by</c>, a plain
/// column to identity.users). A delete is soft (BR-04): <c>deleted_at</c> and
/// <c>deleted_by</c> are set together or not at all, and the row stays. The
/// history reads the owner's undeleted rows newest first, which the partial
/// index serves directly.
/// </summary>
[Migration(202611061610, "Create transactions.transactions")]
public sealed class Transactions003_CreateTransactions : Migration
{
    private const string TransactionsSchema = "transactions";
    private const string Table = "transactions";

    public override void Up()
    {
        Create.Table(Table).InSchema(TransactionsSchema)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_transactions")
            .WithColumn("owned_by").AsGuid().NotNullable()
            .WithColumn("transaction_date").AsDate().NotNullable()
            .WithColumn("description").AsString(200).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_by").AsGuid().NotNullable()
            .WithColumn("deleted_at").AsDateTimeOffset().Nullable()
            .WithColumn("deleted_by").AsGuid().Nullable();

        Execute.Sql(
            $"ALTER TABLE {TransactionsSchema}.{Table} " +
            "ADD CONSTRAINT ck_transactions_description_not_blank CHECK (btrim(description) <> ''), " +
            "ADD CONSTRAINT ck_transactions_deleted_together CHECK ((deleted_at IS NULL) = (deleted_by IS NULL));");

        // The history and the dashboard's recent list: one owner, not deleted, newest first.
        Execute.Sql(
            "CREATE INDEX idx_transactions_owned_by_transaction_date " +
            $"ON {TransactionsSchema}.{Table} (owned_by, transaction_date DESC, created_at DESC) " +
            "WHERE deleted_at IS NULL;");
    }

    public override void Down() =>
        Delete.Table(Table).InSchema(TransactionsSchema);
}
