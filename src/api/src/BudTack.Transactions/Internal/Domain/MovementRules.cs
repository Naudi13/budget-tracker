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

        var description = ValidateDescription(request.Description, errors);
        var lines = ValidateDetails(request.Details, errors);

        return errors.Count == 0
            ? (request.TransactionDate!.Value, description, lines)
            : throw new TransactionsValidationException(errors);
    }

    /// <summary>The trimmed description; adds an error when it is empty or too long.</summary>
    private static string ValidateDescription(string? value, List<FieldError> errors)
    {
        var description = value?.Trim() ?? string.Empty;
        if (description.Length == 0)
        {
            errors.Add(new FieldError("description", "is required"));
        }
        else if (description.Length > DescriptionMaxLength)
        {
            errors.Add(new FieldError("description", $"must be at most {DescriptionMaxLength} characters"));
        }

        return description;
    }

    /// <summary>The complete lines of a transaction; adds an error for a missing list and for each faulty line.</summary>
    private static List<(Guid SubcategoryId, decimal Amount)> ValidateDetails(
        IReadOnlyList<NewTransactionDetail?>? details, List<FieldError> errors)
    {
        var lines = new List<(Guid, decimal)>();
        if (details is not { Count: > 0 })
        {
            errors.Add(new FieldError("details", "must have at least one line"));
            return lines;
        }

        for (var i = 0; i < details.Count; i++)
        {
            if (ValidateLine(details[i], i, errors) is { } line)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    /// <summary>One line, named by its position; null when its subcategory or amount is missing.</summary>
    private static (Guid SubcategoryId, decimal Amount)? ValidateLine(NewTransactionDetail? line, int index, List<FieldError> errors)
    {
        if (line?.SubcategoryId is not { } subcategoryId || subcategoryId == Guid.Empty)
        {
            errors.Add(new FieldError($"details[{index}].subcategoryId", "is required"));
        }

        var amountError = AmountError(line?.Amount);
        if (amountError is not null)
        {
            errors.Add(new FieldError($"details[{index}].amount", amountError));
        }

        return line?.SubcategoryId is { } id && line.Amount is { } amount ? (id, amount) : null;
    }

    public const int ReasonMinLength = 3;
    public const int ReasonMaxLength = 200;

    /// <summary>Why a transaction is deleted (BR-04), trimmed.</summary>
    public static string Validate(TransactionDeletion request)
    {
        var reason = request.Reason?.Trim() ?? string.Empty;
        return reason.Length is >= ReasonMinLength and <= ReasonMaxLength
            ? reason
            : throw new TransactionsValidationException(
                [new FieldError("reason", $"must be {ReasonMinLength} to {ReasonMaxLength} characters")]);
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
