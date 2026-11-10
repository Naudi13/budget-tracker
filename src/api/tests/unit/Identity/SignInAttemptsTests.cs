using BudTack.Identity.Internal.Application;
using Xunit;

namespace BudTack.Unit.Tests.Identity;

/// <summary>RNF-02: five failures with one email within 15 minutes block that email until the oldest leaves the window.</summary>
public sealed class SignInAttemptsTests
{
    private const string Email = "ana@example.com";

    private static (SignInAttempts Attempts, FixedClock Clock) Create()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 11, 9, 10, 0, 0, TimeSpan.Zero));
        return (new SignInAttempts(clock), clock);
    }

    private static void Fail(SignInAttempts attempts, FixedClock clock, int times)
    {
        for (var i = 0; i < times; i++)
        {
            attempts.Failed(Email);
            clock.Advance(TimeSpan.FromMinutes(1));
        }
    }

    [Fact]
    public void FourFailures_doNotBlock()
    {
        var (attempts, clock) = Create();

        Fail(attempts, clock, 4);

        Assert.Null(attempts.RetryAfter(Email));
    }

    [Fact]
    public void FiveFailures_blockUntilTheOldestLeavesTheWindow()
    {
        var (attempts, clock) = Create();

        Fail(attempts, clock, 5);

        // The first failure was 5 minutes ago: 10 minutes of the window are left.
        Assert.Equal(TimeSpan.FromMinutes(10), attempts.RetryAfter(Email));
        clock.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromSeconds(1));
        Assert.Equal(TimeSpan.FromSeconds(1), attempts.RetryAfter(Email));
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Null(attempts.RetryAfter(Email));
    }

    [Fact]
    public void FailuresOlderThanTheWindow_doNotCount()
    {
        var (attempts, clock) = Create();

        Fail(attempts, clock, 4);
        clock.Advance(SignInAttempts.Window);
        Fail(attempts, clock, 1);

        Assert.Null(attempts.RetryAfter(Email));
    }

    [Fact]
    public void ASuccess_clearsTheFailures()
    {
        var (attempts, clock) = Create();

        Fail(attempts, clock, 4);
        attempts.Succeeded(Email);
        Fail(attempts, clock, 4);

        Assert.Null(attempts.RetryAfter(Email));
    }

    [Fact]
    public void AnotherEmail_isNotBlocked()
    {
        var (attempts, clock) = Create();

        Fail(attempts, clock, 5);

        Assert.NotNull(attempts.RetryAfter(Email));
        Assert.Null(attempts.RetryAfter("leo@example.com"));
    }
}
