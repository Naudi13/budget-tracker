using BudTack.Transactions.Api.Contracts;

namespace BudTack.Transactions.Internal.Domain;

/// <summary>
/// The shape of a new transaction (transactions.yaml) and the business rules
/// on its lines: each line's type comes from its subcategory's category
/// (BR-01), every line shares one type (BR-02), and only active subcategories of
/// active categories can be used (BR-05). Messages are for developers; the
/// portal words them for the user in Spanish.
/// </summary>
internal static class MovementRules
{
    public const int DescriptionMaxLength = 200;

    /// <summary>The largest amount numeric(14,2) holds.</summary>
    public const decimal MaxAmount = 999_999_999_999.99m;

    public static (DateOnly Date, string Description, IReadOnlyList<(Guid SubcategoryId, decimal Amount)> Lines) Validate(
        NewTransaction request)
    {
        var errors = new List<FieldError>();
        if (request.TransactionDate is null)
        {
            errors.Add(new FieldError("transactionDate", "is required"));
        }

        var description = request.Description?.Trim() ?? string.Empty;
        if (description.Length == 0)
        {
            errors.Add(new FieldError("description", "is required"));
        }
        else if (description.Length > DescriptionMaxLength)
        {
            errors.Add(new FieldError("description", $"must be at most {DescriptionMaxLength} characters"));
        }

        var lines = new List<(Guid, decimal)>();
        if (request.Details is not { Count: > 0 })
        {
            errors.Add(new FieldError("details", "must have at least one line"));
        }
        else
        {
            for (var i = 0; i < request.Details.Count; i++)
            {
                var line = request.Details[i];
                if (line?.SubcategoryId is not { } subcategoryId || subcategoryId == Guid.Empty)
                {
                    errors.Add(new FieldError($"details[{i}].subcategoryId", "is required"));
                }

                var amountError = AmountError(line?.Amount);
                if (amountError is not null)
                {
                    errors.Add(new FieldError($"details[{i}].amount", amountError));
                }

                if (line?.SubcategoryId is { } id && line.Amount is { } amount)
                {
                    lines.Add((id, amount));
                }
            }
        }

        return errors.Count == 0
            ? (request.TransactionDate!.Value, description, lines)
            : throw new TransactionsValidationException(errors);
    }

    /// <summary>
    /// Gives each line its type and enforces BR-02 and BR-05. A subcategory missing
    /// from <paramref name="uses"/> does not exist or belongs to someone else.
    /// </summary>
    public static IReadOnlyList<MovementLine> Classify(
        IReadOnlyList<(Guid SubcategoryId, decimal Amount)> lines, IReadOnlyDictionary<Guid, SubcategoryUse> uses)
    {
        var unknown = new List<FieldError>();
        var inactive = new List<FieldError>();
        for (var i = 0; i < lines.Count; i++)
        {
            if (!uses.TryGetValue(lines[i].SubcategoryId, out var use))
            {
                unknown.Add(new FieldError($"details[{i}].subcategoryId", "does not exist"));
            }
            else if (!use.IsUsable)
            {
                inactive.Add(new FieldError($"details[{i}].subcategoryId", "is inactive, or its category is"));
            }
        }

        if (unknown.Count > 0)
        {
            throw new TransactionsValidationException(unknown);
        }

        if (inactive.Count > 0)
        {
            throw new TransactionsBusinessRuleException("A line uses an inactive subcategory or category.", inactive);
        }

        var classified = lines
            .Select(line => new MovementLine(Guid.CreateVersion7(), line.SubcategoryId, line.Amount, uses[line.SubcategoryId].Type))
            .ToList();

        var first = classified[0].Type;
        var mixed = classified
            .Select((line, index) => (line, index))
            .Where(pair => pair.line.Type != first)
            .Select(pair => new FieldError($"details[{pair.index}].subcategoryId", $"is an {pair.line.Type} subcategory"))
            .ToList();
        if (mixed.Count > 0)
        {
            throw new TransactionsBusinessRuleException("A transaction cannot mix income and expense details.", mixed);
        }

        return classified;
    }

    private static string? AmountError(decimal? amount) => amount switch
    {
        null => "is required",
        <= 0 => "must be greater than zero",
        > MaxAmount => "must be below 1000000000000",
        _ when decimal.Round(amount.Value, 2) != amount.Value => "must have at most two decimals",
        _ => null,
    };
}
