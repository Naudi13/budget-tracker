using BudTack.Transactions;
using NetArchTest.Rules;
using Xunit;

namespace BudTack.Boundary.Tests;

/// <summary>
/// The two checks of 05-architecture/boundary-enforcement.md for the
/// transactions module, plus its place in 09-modules/module-catalog.md: it may
/// call the public API of identity, and nothing above it.
/// </summary>
public sealed class TransactionsBoundaryTests
{
    private static readonly string[] OtherModuleInternals =
    [
        "BudTack.Identity.Internal",
        "BudTack.Dashboard.Internal",
    ];

    [Fact]
    public void Transactions_dependsOnNoOtherModuleInternals()
    {
        var result = Types.InAssembly(typeof(TransactionsModule).Assembly)
            .ShouldNot().HaveDependencyOnAny(OtherModuleInternals)
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }

    [Fact]
    public void TransactionsInternal_exposesNoPublicTypes()
    {
        var result = Types.InAssembly(typeof(TransactionsModule).Assembly)
            .That().ResideInNamespaceStartingWith("BudTack.Transactions.Internal")
            .ShouldNot().BePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }

    [Fact]
    public void Transactions_dependsOnNeitherDashboardNorHost()
    {
        var result = Types.InAssembly(typeof(TransactionsModule).Assembly)
            .ShouldNot().HaveDependencyOnAny("BudTack.Dashboard", "BudTack.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }
}
