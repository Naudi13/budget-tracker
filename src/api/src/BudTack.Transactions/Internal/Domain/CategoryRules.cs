using BudTack.Transactions.Api.Contracts;

namespace BudTack.Transactions.Internal.Domain;

/// <summary>
/// What a category, a subcategory, an activation change and a page request must
/// carry (categories.yaml, 02-domain/entities-and-rules.md). Every problem is
/// reported at once. Messages are for developers; the portal words them for the
/// user in Spanish.
/// </summary>
internal static class CategoryRules
{
    public const int NameMaxLength = 100;
    public const string Income = "income";
    public const string Expense = "expense";

    public static (string Name, string Type) Validate(NewCategory request)
    {
        var errors = new List<FieldError>();
        var name = Name(request.Name, errors);
        var type = request.Type is Income or Expense ? request.Type : null;
        if (type is null)
        {
            errors.Add(new FieldError("type", "must be income or expense"));
        }

        return errors.Count == 0 ? (name, type!) : throw new TransactionsValidationException(errors);
    }

    public static string Validate(NewSubcategory request)
    {
        var errors = new List<FieldError>();
        var name = Name(request.Name, errors);
        return errors.Count == 0 ? name : throw new TransactionsValidationException(errors);
    }

    /// <summary>What a category's <c>PATCH</c> changes: at least one field, and a valid name if it brings one.</summary>
    public static (string? Name, bool? IsActive) Validate(CategoryChanges request)
    {
        var errors = new List<FieldError>();
        if (request.Name is null && request.IsActive is null)
        {
            errors.Add(new FieldError("body", "send name or isActive"));
        }

        var name = request.Name is null ? null : Name(request.Name, errors);
        return errors.Count == 0 ? (name, request.IsActive) : throw new TransactionsValidationException(errors);
    }

    public static bool Validate(ActivationUpdate request) =>
        request.IsActive
        ?? throw new TransactionsValidationException([new FieldError("isActive", "is required: true or false")]);

    /// <summary>An optional type filter for the category list.</summary>
    public static string? ValidateFilter(string? type) =>
        type is null or Income or Expense
            ? type
            : throw new TransactionsValidationException([new FieldError("type", "must be income or expense")]);

    public static Paging Validate(int? page, int? limit) => Paging.From(page, limit);

    private static string Name(string? raw, List<FieldError> errors)
    {
        var name = raw?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            errors.Add(new FieldError("name", "is required"));
        }
        else if (name.Length > NameMaxLength)
        {
            errors.Add(new FieldError("name", $"must be at most {NameMaxLength} characters"));
        }

        return name;
    }
}
