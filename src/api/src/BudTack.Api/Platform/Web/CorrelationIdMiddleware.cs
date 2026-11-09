using BudTack.Api.Platform.Logging;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// First middleware of the pipeline: fixes the request's correlation id, echoes
/// it in the <c>X-Correlation-Id</c> response header, and opens the logging
/// scope every later log line of the request reads it from.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationScope.Resolve(context.Request.Headers[CorrelationScope.HeaderName]);
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationScope.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object?> { [CorrelationScope.Key] = correlationId }))
        {
            await next(context);
        }
    }
}
