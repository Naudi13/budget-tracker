using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Errors;

/// <summary>
/// Makes every non-2xx response use <see cref="ApiError"/>: unhandled
/// exceptions (500), bodiless status codes such as an unknown route (404), and
/// model validation failures (400).
/// </summary>
internal static partial class ErrorResponses
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static IServiceCollection AddApiErrors(this IServiceCollection services)
    {
        // UseExceptionHandler() needs a ProblemDetails service to start, even
        // though UnhandledExceptionHandler always writes the ApiError body itself.
        services.AddProblemDetails();
        services.AddExceptionHandler<UnhandledExceptionHandler>();
        // PostConfigure: AddControllers() sets its own factory after this
        // method runs, and a plain Configure would lose to it.
        services.PostConfigure<ApiBehaviorOptions>(options =>
        {
            // 415 and the like reach UseStatusCodePages with no body, so they
            // get our format too, instead of ASP.NET's ProblemDetails.
            options.SuppressMapClientErrors = true;
            options.InvalidModelStateResponseFactory = context =>
            {
                var details = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiErrorDetail(
                        ToCamelCase(entry.Key),
                        string.IsNullOrEmpty(error.ErrorMessage) ? "is invalid" : error.ErrorMessage)))
                    .ToList();
                return new BadRequestObjectResult(
                    new ApiError(ErrorCodes.ValidationError, "The request is malformed.", details));
            };
        });
        return services;
    }

    public static IApplicationBuilder UseApiErrors(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            var error = response.StatusCode switch
            {
                StatusCodes.Status401Unauthorized =>
                    new ApiError(ErrorCodes.Unauthorized, "Sign in to continue."),
                StatusCodes.Status403Forbidden =>
                    new ApiError(ErrorCodes.Forbidden, "Your role may not do this."),
                StatusCodes.Status404NotFound or StatusCodes.Status405MethodNotAllowed =>
                    new ApiError(ErrorCodes.NotFound, "The resource does not exist."),
                >= 500 => new ApiError(ErrorCodes.InternalError, "The request could not be handled."),
                _ => new ApiError(ErrorCodes.ValidationError, "The request is malformed."),
            };
            if (response.StatusCode == StatusCodes.Status405MethodNotAllowed)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
            }

            await WriteAsync(response, error);
        });
        return app;
    }

    public static Task WriteAsync(HttpResponse response, ApiError error) =>
        response.WriteAsJsonAsync(error, Json, "application/json");

    private static string ToCamelCase(string key) =>
        string.IsNullOrEmpty(key) ? "body" : JsonNamingPolicy.CamelCase.ConvertName(key.TrimStart('$', '.'));

    private sealed partial class UnhandledExceptionHandler(ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // NFR-09: every unhandled error is logged, with the request's
            // correlationId (from the logging scope) and a timestamp.
            LogUnhandled(logger, exception, httpContext.Request.Method, httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await WriteAsync(
                httpContext.Response,
                new ApiError(
                    ErrorCodes.InternalError,
                    $"Something went wrong. Quote {httpContext.TraceIdentifier} when you report it."));
            return true;
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled error on {Method} {Path}")]
        private static partial void LogUnhandled(ILogger logger, Exception exception, string method, PathString path);
    }
}
