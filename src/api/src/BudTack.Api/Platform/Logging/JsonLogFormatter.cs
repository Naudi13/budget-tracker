using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace BudTack.Api.Platform.Logging;

/// <summary>
/// Writes every log line as one JSON object with the fields that
/// 13-operations/observability.md requires: <c>timestamp</c>, <c>level</c>,
/// <c>correlationId</c>, <c>module</c> and <c>message</c>, plus
/// <c>exception</c> when there is one (NFR-09).
/// </summary>
internal sealed class JsonLogFormatter : ConsoleFormatter
{
    public const string FormatterName = "bud-tack-json";

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly TimeProvider _clock;

    public JsonLogFormatter()
        : this(TimeProvider.System)
    {
    }

    public JsonLogFormatter(TimeProvider clock)
        : base(FormatterName)
    {
        _clock = clock;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (string.IsNullOrEmpty(message) && logEntry.Exception is null)
        {
            return;
        }

        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, WriterOptions))
        {
            json.WriteStartObject();
            json.WriteString("timestamp", _clock.GetUtcNow().ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            json.WriteString("level", ToLevel(logEntry.LogLevel));
            json.WriteString("correlationId", FindCorrelationId(scopeProvider));
            json.WriteString("module", ToModule(logEntry.Category));
            json.WriteString("message", message);
            if (logEntry.Exception is not null)
            {
                json.WriteString("exception", logEntry.Exception.ToString());
            }

            json.WriteEndObject();
        }

        textWriter.WriteLine(System.Text.Encoding.UTF8.GetString(buffer.ToArray()));
    }

    /// <summary>Maps .NET log levels to the three levels the observability doc names.</summary>
    public static string ToLevel(LogLevel level) => level switch
    {
        LogLevel.Trace or LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARN",
        _ => "ERROR",
    };

    /// <summary>
    /// The module that emitted the line, from the logger category:
    /// <c>BudTack.Identity.*</c> is <c>identity</c>, and so on; anything else,
    /// including the framework, is <c>platform</c>.
    /// </summary>
    public static string ToModule(string category)
    {
        const string prefix = "BudTack.";
        if (!category.StartsWith(prefix, StringComparison.Ordinal))
        {
            return "platform";
        }

        var rest = category[prefix.Length..];
        var end = rest.IndexOf('.', StringComparison.Ordinal);
        var name = (end < 0 ? rest : rest[..end]).ToLowerInvariant();
        return name is "identity" or "transactions" or "dashboard" ? name : "platform";
    }

    private static string? FindCorrelationId(IExternalScopeProvider? scopeProvider)
    {
        var found = new Found();
        scopeProvider?.ForEachScope(
            static (scope, found) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> pairs)
                {
                    foreach (var pair in pairs)
                    {
                        if (pair.Key == CorrelationScope.Key && pair.Value is string value)
                        {
                            found.Value = value;
                        }
                    }
                }
            },
            found);
        return found.Value;
    }

    private sealed class Found
    {
        public string? Value { get; set; }
    }
}
