namespace BudTack.Identity.Api.Contracts;

/// <summary>One field the request got wrong, and how to fix it, in the words of the screens.</summary>
public sealed record FieldError(string Field, string Message);
