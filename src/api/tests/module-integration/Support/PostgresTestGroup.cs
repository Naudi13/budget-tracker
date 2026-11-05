using Xunit;

namespace BudTack.ModuleIntegration.Tests.Support;

[CollectionDefinition(Name)]
public sealed class PostgresTestGroup : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
