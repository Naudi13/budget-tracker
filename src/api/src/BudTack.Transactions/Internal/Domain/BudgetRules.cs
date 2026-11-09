using BudTack.Transactions.Api.Contracts;

namespace BudTack.Transactions.Internal.Domain;

/// <summary>
/// The Budget invariants (02-domain/entities-and-rules.md, budgets.yaml): an
/// amount above zero with at most two decimals and below 10^12, a period that
/// ends after it starts, and an active category of the same owner. A new
/// budget and a changed one are held to the same rules (BR-06). Shape problems
/// are a 400; a period that ends too early or an inactive category is a 422.
/// </summary>
internal static class BudgetRules
{
    public static (Guid CategoryId, decimal Amount, DateOnly Start, DateOnly End) Validate(NewBudget request)
    {
        var errors = new List<FieldError>();
        if (request.CategoryId is not { } categoryId || categoryId == Guid.Empty)
        {
            errors.Add(new FieldError("categoryId", "is required"));
        }

        AddAmountError(request.Amount, required: true, errors);
        if (request.PeriodStart is null)
        {
            errors.Add(new FieldError("periodStart", "is required"));
        }

        if (request.PeriodEnd is null)
        {
            errors.Add(new FieldError("periodEnd", "is required"));
        }

        if (errors.Count > 0)
        {
            throw new TransactionsValidationException(errors);
        }

        var result = (request.CategoryId!.Value, request.Amount!.Value, request.PeriodStart!.Value, request.PeriodEnd!.Value);
        RequireOrderedPeriod(result.Item3, result.Item4);
        return result;
    }

    /// <summary>The budget as it would be after <paramref name="changes"/>, held to every invariant.</summary>
    public static BudgetRow Apply(BudgetRow current, BudgetChanges changes)
    {
        if (changes is { CategoryId: null, Amount: null, PeriodStart: null, PeriodEnd: null })
        {
            throw new TransactionsValidationException([new FieldError("body", "must change at least one field")]);
        }

        var errors = new List<FieldError>();
        if (changes.CategoryId == Guid.Empty)
        {
            errors.Add(new FieldError("categoryId", "is required"));
        }

        AddAmountError(changes.Amount, required: false, errors);
        if (errors.Count > 0)
        {
            throw new TransactionsValidationException(errors);
        }

        var changed = current with
        {
            CategoryId = changes.CategoryId ?? current.CategoryId,
            Amount = changes.Amount ?? current.Amount,
            PeriodStart = changes.PeriodStart ?? current.PeriodStart,
            PeriodEnd = changes.PeriodEnd ?? current.PeriodEnd,
        };
        RequireOrderedPeriod(changed.PeriodStart, changed.PeriodEnd);
        return changed;
    }

    /// <summary>BR-05: a budget limits an active category of its owner; null when the category is unknown.</summary>
    public static void RequireUsable(Category? category)
    {
        if (category is null)
        {
            throw new TransactionsValidationException([new FieldError("categoryId", "does not exist")]);
        }

        if (!category.IsActive)
        {
            throw new TransactionsBusinessRuleException(
                "A budget needs an active category.", [new FieldError("categoryId", "is inactive")]);
        }
    }

    private static void RequireOrderedPeriod(DateOnly start, DateOnly end)
    {
        if (end <= start)
        {
            throw new TransactionsBusinessRuleException(
                "A budget's period must end after it starts.", [new FieldError("periodEnd", "must be after periodStart")]);
        }
    }

    private static void AddAmountError(decimal? amount, bool required, List<FieldError> errors)
    {
        var message = amount switch
        {
            null when required => "is required",
            null => null,
            <= 0 => "must be greater than zero",
            > MovementRules.MaxAmount => "must be below 1000000000000",
            _ when decimal.Round(amount.Value, 2) != amount.Value => "must have at most two decimals",
            _ => null,
        };
        if (message is not null)
        {
            errors.Add(new FieldError("amount", message));
        }
    }
}
