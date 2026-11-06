using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// The order of a transaction's lines (HU-04): <c>line_number</c>, 1 for the
/// first line entered. Without it the lines had no reliable order: they are
/// inserted in one database transaction, so their <c>created_at</c> is the same,
/// and two UUIDv7 ids from the same millisecond are not ordered either. Rows
/// that already exist are numbered by <c>created_at, id</c>. The unique index
/// on <c>(transaction_id, line_number)</c> also serves the foreign key, so it
/// replaces <c>idx_transaction_details_transaction_id</c>.
/// </summary>
[Migration(202611061625, "Number the lines of a transaction")]
public sealed class Transactions005_AddDetailLineNumber : Migration
{
    private const string TransactionsSchema = "transactions";
    private const string Table = "transaction_details";

    public override void Up()
    {
        Alter.Table(Table).InSchema(TransactionsSchema)
            .AddColumn("line_number").AsInt16().Nullable();

        Execute.Sql(
            $"""
            UPDATE {TransactionsSchema}.{Table} d SET line_number = n.line_number
            FROM (SELECT id, row_number() OVER (PARTITION BY transaction_id ORDER BY created_at, id) AS line_number
                  FROM {TransactionsSchema}.{Table}) n
            WHERE n.id = d.id;
            ALTER TABLE {TransactionsSchema}.{Table}
                ALTER COLUMN line_number SET NOT NULL,
                ADD CONSTRAINT ck_transaction_details_line_number_positive CHECK (line_number > 0);
            """);

        Create.Index("idx_transaction_details_transaction_id_line_number").OnTable(Table).InSchema(TransactionsSchema)
            .OnColumn("transaction_id").Ascending()
            .OnColumn("line_number").Ascending()
            .WithOptions().Unique();

        Delete.Index("idx_transaction_details_transaction_id").OnTable(Table).InSchema(TransactionsSchema);
    }

    public override void Down()
    {
        Create.Index("idx_transaction_details_transaction_id").OnTable(Table).InSchema(TransactionsSchema)
            .OnColumn("transaction_id").Ascending();
        Delete.Index("idx_transaction_details_transaction_id_line_number").OnTable(Table).InSchema(TransactionsSchema);
        Delete.Column("line_number").FromTable(Table).InSchema(TransactionsSchema);
    }
}
