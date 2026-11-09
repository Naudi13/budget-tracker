using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// Why a transaction was deleted (HU-06, BR-04): set together with <c>deleted_at</c>,
/// never alone. Transactions deleted before this rule get a fixed text.
/// </summary>
[Migration(202611092102, "Add deletion_reason to transactions.transactions")]
public sealed class Transactions008_AddDeletionReason : Migration
{
    private const string Table = "transactions.transactions";

    public override void Up() =>
        Execute.Sql($"""
            ALTER TABLE {Table} ADD COLUMN deletion_reason varchar(200);
            UPDATE {Table} SET deletion_reason = 'Sin razón registrada' WHERE deleted_at IS NOT NULL;
            ALTER TABLE {Table}
              ADD CONSTRAINT ck_transactions_deletion_reason CHECK (
                (deleted_at IS NULL AND deletion_reason IS NULL)
                OR (deleted_at IS NOT NULL AND deletion_reason IS NOT NULL AND length(btrim(deletion_reason)) >= 3));
            """);

    public override void Down() =>
        Execute.Sql($"ALTER TABLE {Table} DROP COLUMN deletion_reason;");
}
