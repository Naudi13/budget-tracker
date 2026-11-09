using System.Globalization;
using BudTack.Identity.Api.Contracts;
using TransactionsContracts = BudTack.Transactions.Api.Contracts;
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
            AccountStateConflictException e => Respond(
                StatusCodes.Status409Conflict, new ApiError(ErrorCodes.Conflict, e.Message)),
            UserNotFoundException e => Respond(
                StatusCodes.Status404NotFound, new ApiError(ErrorCodes.NotFound, e.Message)),
            InvalidCredentialsException e => Respond(
                StatusCodes.Status401Unauthorized, new ApiError(ErrorCodes.Unauthorized, e.Message)),
            TooManySignInAttemptsException e => TooManyRequests(context, e),
            TransactionsContracts.TransactionsValidationException e => Respond(
                StatusCodes.Status400BadRequest,
                new ApiError(
                    ErrorCodes.ValidationError,
                    e.Message,
                    [.. e.Errors.Select(error => new ApiErrorDetail(error.Field, error.Message))])),
            TransactionsContracts.TransactionsForbiddenException e => Respond(
                StatusCodes.Status403Forbidden, new ApiError(ErrorCodes.Forbidden, e.Message)),
            TransactionsContracts.TransactionsNotFoundException e => Respond(
                StatusCodes.Status404NotFound, new ApiError(ErrorCodes.NotFound, e.Message)),
            TransactionsContracts.TransactionsBusinessRuleException e => Respond(
                StatusCodes.Status422UnprocessableEntity,
                new ApiError(
                    ErrorCodes.BusinessRuleViolated,
                    e.Message,
                    [.. e.Errors.Select(error => new ApiErrorDetail(error.Field, error.Message))])),
            TransactionsContracts.TransactionsConflictException e => Respond(
                StatusCodes.Status409Conflict, new ApiError(ErrorCodes.Conflict, e.Message)),
            _ => null,
        };

        if (result is not null)
        {
            context.Result = result;
            context.ExceptionHandled = true;
        }
    }

    // RNF-02: Retry-After says, in whole seconds, when the email may try again.
    private static JsonResult TooManyRequests(ExceptionContext context, TooManySignInAttemptsException e)
    {
        var seconds = Math.Max(1, (int)Math.Ceiling(e.RetryAfter.TotalSeconds));
        context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        return Respond(StatusCodes.Status429TooManyRequests, new ApiError(ErrorCodes.TooManyRequests, e.Message));
    }

    // The same serializer settings as every other error: no "details": null.
    private static JsonResult Respond(int status, ApiError error) =>
        new(error, ErrorResponses.Json) { StatusCode = status, ContentType = "application/json" };
}
