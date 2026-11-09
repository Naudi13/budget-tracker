using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// The single-use links that activate an account or reset its password (HU-10,
/// ADR-010). An account has no password until its owner activates it, so
/// <c>password_hash</c> becomes optional. Only the token's hash is stored.
/// </summary>
[Migration(202611092100, "Add identity.account_tokens")]
public sealed class Identity004_AddAccountTokens : Migration
{
    private const string IdentitySchema = "identity";
    private const string Table = "account_tokens";

    public override void Up()
    {
        Alter.Column("password_hash").OnTable("users").InSchema(IdentitySchema).AsString(255).Nullable();

        Create.Table(Table).InSchema(IdentitySchema)
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_account_tokens")
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("purpose").AsString(20).NotNullable()
            .WithColumn("token_hash").AsString(64).NotNullable()
            .WithColumn("expires_at").AsDateTimeOffset().NotNullable()
            .WithColumn("used_at").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTimeOffset)
            .WithColumn("created_by").AsGuid().NotNullable();

        Execute.Sql(
            $"ALTER TABLE {IdentitySchema}.{Table} " +
            "ADD CONSTRAINT fk_account_tokens_user_id FOREIGN KEY (user_id) " +
            $"REFERENCES {IdentitySchema}.users (id) ON DELETE RESTRICT, " +
            "ADD CONSTRAINT ck_account_tokens_purpose CHECK (purpose IN ('ACTIVATION', 'PASSWORD_RESET'));");

        Create.Index("idx_account_tokens_token_hash").OnTable(Table).InSchema(IdentitySchema)
            .OnColumn("token_hash").Ascending()
            .WithOptions().Unique();

        Create.Index("idx_account_tokens_user_id").OnTable(Table).InSchema(IdentitySchema)
            .OnColumn("user_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table(Table).InSchema(IdentitySchema);

        // An account that was never activated cannot exist without a password.
        Execute.Sql($"DELETE FROM {IdentitySchema}.users WHERE password_hash IS NULL;");
        Alter.Column("password_hash").OnTable("users").InSchema(IdentitySchema).AsString(255).NotNullable();
    }
}
