using FluentMigrator;

namespace BudTack.Db.Migrations.Migrations;

/// <summary>
/// The schema of each table-owning module (ADR-004): <c>identity</c> and
/// <c>transactions</c>. <c>dashboard</c> owns no table and gets no schema.
/// The API's role, <c>bud_tack_app</c>, may use both schemas and read and write
/// every table the migrator creates in them later, but never run DDL
/// (00-governance/security-policy.md).
/// </summary>
[Migration(202611041500, "Create the identity and transactions schemas")]
public sealed class Platform001_CreateSchemas : Migration
{
    private const string AppRole = "bud_tack_app";
    private static readonly string[] Schemas = ["identity", "transactions"];

    public override void Up()
    {
        foreach (var schema in Schemas)
        {
            Create.Schema(schema);
            Execute.Sql($"GRANT USAGE ON SCHEMA {schema} TO {AppRole};");
            Execute.Sql(
                $"ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} " +
                $"GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {AppRole};");
        }
    }

    public override void Down()
    {
        foreach (var schema in Schemas)
        {
            Execute.Sql(
                $"ALTER DEFAULT PRIVILEGES IN SCHEMA {schema} " +
                $"REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLES FROM {AppRole};");
            Execute.Sql($"REVOKE USAGE ON SCHEMA {schema} FROM {AppRole};");
            Delete.Schema(schema);
        }
    }
}
