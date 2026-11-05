using BudTack.Identity.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BudTack.Api.Platform.Errors;

/// <summary>
/// Turns the errors a module's facade declares into the status and error code
/// of 07-api/rest-conventions.md. Anything else stays an unhandled error (500).
/// </summary>
internal sealed class ModuleErrorFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var result = context.Exception switch
        {
            IdentityValidationException e => Respond(
                StatusCodes.Status400BadRequest,
                new ApiError(
                    ErrorCodes.ValidationError,
                    e.Message,
                    [.. e.Errors.Select(error => new ApiErrorDetail(error.Field, error.Message))])),
            EmailAlreadyInUseException e => Respond(
                StatusCodes.Status409Conflict, new ApiError(ErrorCodes.Conflict, e.Message)),
            InvalidCredentialsException e => Respond(
                StatusCodes.Status401Unauthorized, new ApiError(ErrorCodes.Unauthorized, e.Message)),
            _ => null,
        };

        if (result is not null)
        {
            context.Result = result;
            context.ExceptionHandled = true;
        }
    }

    // The same serializer settings as every other error: no "details": null.
    private static JsonResult Respond(int status, ApiError error) =>
        new(error, ErrorResponses.Json) { StatusCode = status, ContentType = "application/json" };
}
