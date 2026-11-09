namespace BudTack.Unit.Tests.Identity;

/// <summary>A clock the test sets.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
