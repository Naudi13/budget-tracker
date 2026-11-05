using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BudTack.EndToEnd.Tests.Support;

/// <summary>Keeps every formatted log line, with its exception, in memory.</summary>
internal sealed class CapturingLoggerProvider(ConcurrentQueue<string> lines) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new Logger(lines);

    public void Dispose()
    {
    }

    private sealed class Logger(ConcurrentQueue<string> lines) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            lines.Enqueue(formatter(state, exception) + " " + exception);
    }
}
