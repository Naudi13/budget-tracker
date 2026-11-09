using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Domain;
using Xunit;
using Category = BudTack.Transactions.Internal.Domain.Category;

namespace BudTack.Unit.Tests.Transactions;

public sealed class BudgetRulesTests
{
    private static readonly Guid Office = Guid.Parse("00000000-0000-7000-8000-0000000000c1");
    private static readonly DateOnly Nov1 = new(2026, 11, 1);
    private static readonly DateOnly Nov30 = new(2026, 11, 30);

    private static readonly BudgetRow Current =
        new(Guid.Parse("00000000-0000-7000-8000-0000000000b1"), Office, 500m, Nov1, Nov30);

    [Fact]
    public void Validate_validBudget_keepsEveryField()
    {
        Assert.Equal((Office, 500m, Nov1, Nov30), BudgetRules.Validate(new NewBudget(Office, 500m, Nov1, Nov30)));
    }

    [Fact]
    public void Validate_emptyBudget_reportsEveryField()
    {
        var error = Assert.Throws<TransactionsValidationException>(() => BudgetRules.Validate(new NewBudget(null, null, null, null)));

        Assert.Equal(["categoryId", "amount", "periodStart", "periodEnd"], error.Errors.Select(e => e.Field));
    }

    [Theory]
    [InlineData("0", "must be greater than zero")]
    [InlineData("-1", "must be greater than zero")]
    [InlineData("10.005", "must have at most two decimals")]
    [InlineData("1000000000000", "must be below 1000000000000")]
    public void Validate_badAmount_isAValidationError(string amount, string message)
    {
        var value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        var error = Assert.Throws<TransactionsValidationException>(() => BudgetRules.Validate(new NewBudget(Office, value, Nov1, Nov30)));

        Assert.Equal(new FieldError("amount", message), Assert.Single(error.Errors));
    }

    [Theory]
    [InlineData(2026, 10, 31)]
    [InlineData(2026, 11, 1)]
    public void Validate_periodNotEndingAfterItStarts_isABusinessRuleViolation(int year, int month, int day) // TC-007
    {
        var error = Assert.Throws<TransactionsBusinessRuleException>(
            () => BudgetRules.Validate(new NewBudget(Office, 500m, Nov1, new DateOnly(year, month, day))));

        Assert.Equal("periodEnd", Assert.Single(error.Errors).Field);
    }

    [Fact]
    public void Apply_onlyTheFieldsSent_change() // BR-06
    {
        var changed = BudgetRules.Apply(Current, new BudgetChanges(null, 650m, null, null));

        Assert.Equal(Current with { Amount = 650m }, changed);
    }

    [Fact]
    public void Apply_aChangeThatBreaksThePeriod_isABusinessRuleViolation() // BR-06, TC-019
    {
        Assert.Throws<TransactionsBusinessRuleException>(
            () => BudgetRules.Apply(Current, new BudgetChanges(null, null, null, new DateOnly(2026, 10, 15))));
    }

    [Fact]
    public void Apply_nothingToChange_isAValidationError()
    {
        var error = Assert.Throws<TransactionsValidationException>(() => BudgetRules.Apply(Current, new BudgetChanges(null, null, null, null)));

        Assert.Equal("body", Assert.Single(error.Errors).Field);
    }

    [Fact]
    public void Apply_badAmountOrEmptyCategory_isAValidationError()
    {
        var error = Assert.Throws<TransactionsValidationException>(
            () => BudgetRules.Apply(Current, new BudgetChanges(Guid.Empty, 0m, null, null)));

        Assert.Equal(["categoryId", "amount"], error.Errors.Select(e => e.Field));
    }

    [Fact]
    public void RequireUsable_unknownCategory_isAValidationError_andAnInactiveOne_aBusinessRuleViolation() // BR-05
    {
        Assert.Throws<TransactionsValidationException>(() => BudgetRules.RequireUsable(null));
        Assert.Throws<TransactionsBusinessRuleException>(
            () => BudgetRules.RequireUsable(new Category(Office, "Oficina", "expense", IsActive: false)));
        BudgetRules.RequireUsable(new Category(Office, "Oficina", "expense", IsActive: true));
    }
}
