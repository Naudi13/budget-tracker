using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// <c>transactions.categories</c> (HU-02, 09-modules/transactions/data-model.md).
/// Each row belongs to one user (<c>owned_by</c>, a plain column to
/// identity.users, never a foreign key). A name is unique per owner and type,
/// ignoring case, so "Oficina" and "oficina" are one expense category, while an
/// income category may reuse it. <c>bud_tack_app</c> reads and writes the table
/// through the default privileges of <c>Platform001</c>.
/// </summary>
[Migration(202611060035, "Create transactions.categories")]
public sealed class Transactions001_CreateCategories : Migration
{
    private const string TransactionsSchema = "transactions";
    private const string Table = "categories";

    public override void Up()
    {
        Create.Table(Table).InSchema(TransactionsSchema)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_categories")
            .WithColumn("owned_by").AsGuid().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("type").AsString(10).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_by").AsGuid().NotNullable();

        Execute.Sql(
            $"ALTER TABLE {TransactionsSchema}.{Table} " +
            "ADD CONSTRAINT ck_categories_type CHECK (type IN ('income', 'expense')), " +
            "ADD CONSTRAINT ck_categories_name_not_blank CHECK (btrim(name) <> '');");

        // Also serves "list my categories of one type": owned_by and type lead it.
        Execute.Sql(
            $"CREATE UNIQUE INDEX idx_categories_owned_by_type_name " +
            $"ON {TransactionsSchema}.{Table} (owned_by, type, lower(name));");
    }

    public override void Down() =>
        Delete.Table(Table).InSchema(TransactionsSchema);
}
