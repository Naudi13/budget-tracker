using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// <c>identity.users</c>, the one account a business signs in with (HU-01,
/// 09-modules/identity/data-model.md). The API generates the id; the password
/// is stored only as a salted hash (NFR-06). An email is unique once lowercased:
/// the API lowercases it before writing, and the check constraint refuses any
/// row that skipped that step, so the unique index can never be bypassed by
/// case. <c>bud_tack_app</c> reads and writes the table through the default
/// privileges of <c>Platform001</c>.
/// </summary>
[Migration(202611052230, "Create identity.users")]
public sealed class Identity001_CreateUsers : Migration
{
    private const string IdentitySchema = "identity";
    private const string Table = "users";

    public override void Up()
    {
        Create.Table(Table).InSchema(IdentitySchema)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_users")
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("email").AsString(254).NotNullable()
            .WithColumn("password_hash").AsString(255).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            // Plain columns, per database-conventions.md. On sign-up there is no
            // signed-in actor yet, so they stay null or hold the new user's own id.
            .WithColumn("created_by").AsGuid().Nullable()
            .WithColumn("updated_by").AsGuid().Nullable();

        Create.Index("idx_users_email").OnTable(Table).InSchema(IdentitySchema)
            .OnColumn("email").Ascending()
            .WithOptions().Unique();

        Execute.Sql(
            $"ALTER TABLE {IdentitySchema}.{Table} " +
            "ADD CONSTRAINT ck_users_email_lowercase CHECK (email = lower(email)), " +
            "ADD CONSTRAINT ck_users_name_not_blank CHECK (btrim(name) <> '');");
    }

    public override void Down() =>
        Delete.Table(Table).InSchema(IdentitySchema);
}
