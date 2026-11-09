using NetArchTest.Rules;
using Xunit;

namespace BudTack.Boundary.Tests;

/// <summary>
/// The host (<c>BudTack.Api</c>) reaches a module only through its public
/// <c>Api</c> namespace, never its <c>Internal</c> one
/// (05-architecture/boundary-enforcement.md, ADR-003).
/// </summary>
public sealed class HostBoundaryTests
{
    private static readonly string[] ModuleInternals =
    [
        "BudTack.Identity.Internal",
        "BudTack.Transactions.Internal",
        "BudTack.Dashboard.Internal",
    ];

    [Fact]
    public void Host_dependsOnNoModuleInternals()
    {
        var result = Types.InAssembly(typeof(Program).Assembly)
            .ShouldNot().HaveDependencyOnAny(ModuleInternals)
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }

    [Fact]
    public void Platform_exposesNoPublicTypes()
    {
        var result = Types.InAssembly(typeof(Program).Assembly)
            .That().ResideInNamespaceStartingWith("BudTack.Api.Platform")
            .ShouldNot().BePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }
}
