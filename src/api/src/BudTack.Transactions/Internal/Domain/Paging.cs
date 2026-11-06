using BudTack.Transactions.Api.Contracts;

namespace BudTack.Transactions.Internal.Domain;

/// <summary>A valid <c>?page=&amp;limit=</c> (07-api/rest-conventions.md): page from 1, limit 1 to 100, 20 by default.</summary>
internal readonly record struct Paging(int Page, int Limit)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;

    public int Offset => (Page - 1) * Limit;

    /// <summary>A page request from the query string, or a validation error naming page and limit.</summary>
    public static Paging From(int? page, int? limit)
    {
        var errors = new List<FieldError>();
        if (page is < 1)
        {
            errors.Add(new FieldError("page", "must be 1 or more"));
        }

        if (limit is < 1 or > MaxLimit)
        {
            errors.Add(new FieldError("limit", $"must be between 1 and {MaxLimit}"));
        }

        return errors.Count == 0
            ? new Paging(page ?? 1, limit ?? DefaultLimit)
            : throw new TransactionsValidationException(errors);
    }
}
