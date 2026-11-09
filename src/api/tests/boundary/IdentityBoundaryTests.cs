using BudTack.Identity;
using NetArchTest.Rules;
using Xunit;

namespace BudTack.Boundary.Tests;

/// <summary>
/// The two checks of 05-architecture/boundary-enforcement.md for the identity
/// module: it reaches no other module's Internal namespace, and nothing in its
/// own Internal namespace is public.
/// </summary>
public sealed class IdentityBoundaryTests
{
    private static readonly string[] OtherModuleInternals =
    [
        "BudTack.Transactions.Internal",
        "BudTack.Dashboard.Internal",
    ];

    [Fact]
    public void Identity_dependsOnNoOtherModuleInternals()
    {
        var result = Types.InAssembly(typeof(IdentityModule).Assembly)
            .ShouldNot().HaveDependencyOnAny(OtherModuleInternals)
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }

    [Fact]
    public void IdentityInternal_exposesNoPublicTypes()
    {
        var result = Types.InAssembly(typeof(IdentityModule).Assembly)
            .That().ResideInNamespaceStartingWith("BudTack.Identity.Internal")
            .ShouldNot().BePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }

    [Fact]
    public void Identity_dependsOnNoOtherModule()
    {
        // identity is the base of the graph in 09-modules/module-catalog.md.
        var result = Types.InAssembly(typeof(IdentityModule).Assembly)
            .ShouldNot().HaveDependencyOnAny("BudTack.Transactions", "BudTack.Dashboard", "BudTack.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Violations.Of(result));
    }
}
