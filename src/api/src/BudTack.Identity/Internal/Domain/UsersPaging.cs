using BudTack.Identity.Api.Contracts;

namespace BudTack.Identity.Internal.Domain;

/// <summary>A valid <c>?page=&amp;limit=</c> (07-api/rest-conventions.md): page from 1, limit 1 to 100, 20 by default.</summary>
internal readonly record struct UsersPaging(int Page, int Limit)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    public int Offset => (Page - 1) * Limit;

    public static UsersPaging From(int? page, int? limit)
    {
        var errors = new List<FieldError>();
        if (page < 1)
        {
            errors.Add(new FieldError("page", "must be 1 or more"));
        }

        if (limit is < 1 or > MaxLimit)
        {
            errors.Add(new FieldError("limit", $"must be between 1 and {MaxLimit}"));
        }

        return errors.Count == 0
            ? new UsersPaging(page ?? 1, limit ?? DefaultLimit)
            : throw new IdentityValidationException(errors);
    }
}
