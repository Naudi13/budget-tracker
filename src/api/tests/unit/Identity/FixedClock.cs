namespace BudTack.Unit.Tests.Identity;

/// <summary>A clock the test sets.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
