namespace BudTack.Boundary.Tests;

internal static class Violations
{
    public static string Of(NetArchTest.Rules.TestResult result) =>
        "Violations: " + string.Join(", ", result.FailingTypeNames ?? []);
}
