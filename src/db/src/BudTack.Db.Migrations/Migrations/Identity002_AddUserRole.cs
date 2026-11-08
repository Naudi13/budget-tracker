using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// The role of an account, <c>USER</c> or <c>ADMIN</c> (HU-09, ADR-007). Existing
/// accounts and every sign-up get <c>USER</c>; only a seed makes an administrator.
/// </summary>
[Migration(202611082300, "Add role to identity.users")]
public sealed class Identity002_AddUserRole : Migration
{
    private const string IdentitySchema = "identity";
    private const string Table = "users";

    public override void Up()
    {
        Alter.Table(Table).InSchema(IdentitySchema)
            .AddColumn("role").AsString(10).NotNullable().WithDefaultValue("USER");

        Execute.Sql(
            $"ALTER TABLE {IdentitySchema}.{Table} " +
            "ADD CONSTRAINT ck_users_role CHECK (role IN ('USER', 'ADMIN'));");
    }

    public override void Down() =>
        Delete.Column("role").FromTable(Table).InSchema(IdentitySchema);
}
