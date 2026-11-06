using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// <c>transactions.subcategories</c> (HU-02). A subcategory belongs to one
/// category, and through it to the category's owner. Its name is unique within
/// the category, ignoring case. The key to its category is <c>ON DELETE
/// RESTRICT</c>, so the database itself refuses to delete a category that still
/// owns a subcategory (BR-05, 06-data/database-conventions.md).
/// </summary>
[Migration(202611060036, "Create transactions.subcategories")]
public sealed class Transactions002_CreateSubcategories : Migration
{
    private const string TransactionsSchema = "transactions";
    private const string Table = "subcategories";

    public override void Up()
    {
        Create.Table(Table).InSchema(TransactionsSchema)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_subcategories")
            .WithColumn("category_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_by").AsGuid().NotNullable();

        // Written in SQL so the rule reads RESTRICT, not the generator's NO ACTION.
        Execute.Sql(
            $"ALTER TABLE {TransactionsSchema}.{Table} " +
            "ADD CONSTRAINT fk_subcategories_category_id FOREIGN KEY (category_id) " +
            $"REFERENCES {TransactionsSchema}.categories (id) ON DELETE RESTRICT, " +
            "ADD CONSTRAINT ck_subcategories_name_not_blank CHECK (btrim(name) <> '');");

        // Also serves the foreign key and "list the subcategories of a category".
        Execute.Sql(
            "CREATE UNIQUE INDEX idx_subcategories_category_id_name " +
            $"ON {TransactionsSchema}.{Table} (category_id, lower(name));");
    }

    public override void Down() =>
        Delete.Table(Table).InSchema(TransactionsSchema);
}
