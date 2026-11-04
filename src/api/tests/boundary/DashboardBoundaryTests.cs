using BudTack.Dashboard;
using NetArchTest.Rules;
using Xunit;

namespace BudTack.Boundary.Tests;

/// <summary>
/// The two checks of 05-architecture/boundary-enforcement.md for the dashboard
/// module, plus its place in 09-modules/module-catalog.md: it reads through the
/// public API of transactions only, and it owns no table.
/// </summary>
public sealed class DashboardBoundaryTests
{
    private static readonly string[] OtherModuleInternals =
    [
        "BudTack.Identity.Internal",
        "BudTack.Transactions.Internal",
    ];

    [Fact]
    public void Dashboard_dependsOnNoOtherModuleInternals()
    {
        var result = Types.InAssembly(typeof(DashboardModule).Assembly)
            .ShouldNot().HaveDependencyOnAny(OtherModuleInternals)
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }

    [Fact]
    public void DashboardInternal_exposesNoPublicTypes()
    {
        var result = Types.InAssembly(typeof(DashboardModule).Assembly)
            .That().ResideInNamespaceStartingWith("BudTack.Dashboard.Internal")
            .ShouldNot().BePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }

    [Fact]
    public void Dashboard_dependsOnNeitherIdentityNorHost()
    {
        // The only arrow out of dashboard in module-catalog.md goes to transactions.
        var result = Types.InAssembly(typeof(DashboardModule).Assembly)
            .ShouldNot().HaveDependencyOnAny("BudTack.Identity", "BudTack.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }

    [Fact]
    public void Dashboard_touchesNoDatabase()
    {
        // dashboard computes on read and persists nothing (09-modules/dashboard).
        var result = Types.InAssembly(typeof(DashboardModule).Assembly)
            .ShouldNot().HaveDependencyOnAny("Npgsql", "System.Data")
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }
}
