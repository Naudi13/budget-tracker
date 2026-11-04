using FluentMigrator.Runner.VersionTableInfo;

namespace BudTack.Db.Migrations;

/// <summary>
/// The one version table for the whole database, <c>public.schema_version</c>,
/// named in snake_case per 06-data/database-conventions.md (ADR-005).
/// </summary>
public sealed class SchemaVersionTable : IVersionTableMetaData
{
    public bool OwnsSchema => false;

    public string SchemaName => "public";

    public string TableName => "schema_version";

    public string ColumnName => "version";

    public string DescriptionColumnName => "description";

    public string UniqueIndexName => "uc_schema_version_version";

    public string AppliedOnColumnName => "applied_on";

    public bool CreateWithPrimaryKey => false;
}
