using System.Collections.Concurrent;

namespace BudTack.Identity.Internal.Application;

/// <summary>
/// RNF-02: after <see cref="MaxFailures"/> failed sign-ins with one email within
/// <see cref="Window"/>, that email is refused until the oldest of those failures
/// leaves the window. The count lives in memory: there is one API instance, and a
/// restart clears it.
/// </summary>
internal sealed class SignInAttempts(TimeProvider clock)
{
    public const int MaxFailures = 5;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    // Above this many tracked emails, the ones with no failure left in the window are dropped.
    private const int PruneAbove = 10_000;

    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _failures = new(StringComparer.Ordinal);

    /// <summary>How long until <paramref name="email"/> may try again, or null when it may try now.</summary>
    public TimeSpan? RetryAfter(string email)
    {
        if (!_failures.TryGetValue(email, out var failures))
        {
            return null;
        }

        lock (failures)
        {
            var now = clock.GetUtcNow();
            Expire(failures, now);
            return failures.Count >= MaxFailures ? failures.Peek() + Window - now : null;
        }
    }

    public void Failed(string email)
    {
        if (_failures.Count > PruneAbove)
        {
            Prune();
        }

        var failures = _failures.GetOrAdd(email, _ => new Queue<DateTimeOffset>());
        lock (failures)
        {
            failures.Enqueue(clock.GetUtcNow());
        }
    }

    public void Succeeded(string email) => _failures.TryRemove(email, out _);

    private static void Expire(Queue<DateTimeOffset> failures, DateTimeOffset now)
    {
        while (failures.Count > 0 && failures.Peek() <= now - Window)
        {
            failures.Dequeue();
        }
    }

    private void Prune()
    {
        var now = clock.GetUtcNow();
        foreach (var (email, failures) in _failures)
        {
            lock (failures)
            {
                Expire(failures, now);
                if (failures.Count == 0)
                {
                    _failures.TryRemove(email, out _);
                }
            }
        }
    }
}
