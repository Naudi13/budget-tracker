using System.Globalization;
using System.Text.Json;
using BudTack.Api.Platform.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudTack.Unit.Tests.Platform;

public sealed class JsonLogFormatterTests
{
    [Theory]
    [InlineData(LogLevel.Debug, "DEBUG")]
    [InlineData(LogLevel.Information, "INFO")]
    [InlineData(LogLevel.Warning, "WARN")]
    [InlineData(LogLevel.Error, "ERROR")]
    [InlineData(LogLevel.Critical, "ERROR")]
    public void ToLevel_mapsToTheObservabilityLevels(LogLevel level, string expected)
    {
        Assert.Equal(expected, JsonLogFormatter.ToLevel(level));
    }

    [Theory]
    [InlineData("BudTack.Identity.Internal.Application.SignUp", "identity")]
    [InlineData("BudTack.Transactions.Api.TransactionsFacade", "transactions")]
    [InlineData("BudTack.Dashboard", "dashboard")]
    [InlineData("BudTack.Api.Platform.Web.CorrelationIdMiddleware", "platform")]
    [InlineData("Microsoft.AspNetCore.Hosting.Diagnostics", "platform")]
    public void ToModule_readsTheModuleFromTheCategory(string category, string expected)
    {
        Assert.Equal(expected, JsonLogFormatter.ToModule(category));
    }

    [Fact]
    public void Write_insideACorrelationScope_emitsEveryRequiredField()
    {
        var scopes = new LoggerExternalScopeProvider();
        using var scope = scopes.Push(new Dictionary<string, object?> { [CorrelationScope.Key] = "req-42" });

        using var line = Format(LogLevel.Warning, "BudTack.Transactions.Internal.Something", "budget saved", scopes);

        var root = line.RootElement;
        Assert.True(DateTimeOffset.TryParse(
            root.GetProperty("timestamp").GetString(), CultureInfo.InvariantCulture, out _));
        Assert.Equal("WARN", root.GetProperty("level").GetString());
        Assert.Equal("req-42", root.GetProperty("correlationId").GetString());
        Assert.Equal("transactions", root.GetProperty("module").GetString());
        Assert.Equal("budget saved", root.GetProperty("message").GetString());
        Assert.False(root.TryGetProperty("exception", out _));
    }

    [Fact]
    public void Write_withAnException_includesIt()
    {
        using var line = Format(
            LogLevel.Error, "BudTack.Api", "boom", new LoggerExternalScopeProvider(), new InvalidOperationException("broken"));

        Assert.Contains("broken", line.RootElement.GetProperty("exception").GetString(), StringComparison.Ordinal);
        Assert.Equal(JsonValueKind.Null, line.RootElement.GetProperty("correlationId").ValueKind);
    }

    private static JsonDocument Format(
        LogLevel level, string category, string message, IExternalScopeProvider scopes, Exception? exception = null)
    {
        var entry = new LogEntry<string>(level, category, default, message, exception, (state, _) => state);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        new JsonLogFormatter().Write(entry, scopes, writer);
        return JsonDocument.Parse(writer.ToString());
    }
}
