using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// Whether an account may be used (HU-13, BR-09). Every account stays active until
/// an administrator turns it off.
/// </summary>
[Migration(202611092200, "Add is_active to identity.users")]
public sealed class Identity005_AddUserIsActive : Migration
{
    public override void Up() =>
        Alter.Table("users").InSchema("identity")
            .AddColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true);

    public override void Down() =>
        Delete.Column("is_active").FromTable("users").InSchema("identity");
}
