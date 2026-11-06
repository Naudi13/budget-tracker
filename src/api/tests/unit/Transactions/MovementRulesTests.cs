using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Domain;
using Xunit;

namespace BudTack.Unit.Tests.Transactions;

public sealed class MovementRulesTests
{
    private static readonly Guid Supplies = Guid.Parse("00000000-0000-7000-8000-0000000000d1");
    private static readonly Guid Rent = Guid.Parse("00000000-0000-7000-8000-0000000000d2");
    private static readonly Guid Sales = Guid.Parse("00000000-0000-7000-8000-0000000000d3");
    private static readonly DateOnly Today = new(2026, 11, 6);

    private static NewTransaction New(params NewTransactionDetail?[] lines) =>
        new(Today, "Printer paper", lines);

    [Fact]
    public void Validate_validTransaction_trimsTheDescriptionAndKeepsTheLines()
    {
        var (date, description, lines) = MovementRules.Validate(
            new NewTransaction(Today, "  Printer paper ", [new NewTransactionDetail(Supplies, 85.50m)]));

        Assert.Equal(Today, date);
        Assert.Equal("Printer paper", description);
        Assert.Equal([(Supplies, 85.50m)], lines);
    }

    [Fact]
    public void Validate_emptyTransaction_reportsDateDescriptionAndDetails()
    {
        var error = Assert.Throws<TransactionsValidationException>(() => MovementRules.Validate(new NewTransaction(null, " ", null)));

        Assert.Equal(["transactionDate", "description", "details"], error.Errors.Select(e => e.Field));
    }

    [Fact]
    public void Validate_noLines_isRefused()
    {
        var error = Assert.Throws<TransactionsValidationException>(() => MovementRules.Validate(New()));

        Assert.Equal(new FieldError("details", "must have at least one line"), Assert.Single(error.Errors));
    }

    [Fact]
    public void Validate_description201Characters_isRefused()
    {
        var error = Assert.Throws<TransactionsValidationException>(
            () => MovementRules.Validate(new NewTransaction(Today, new string('a', 201), [new NewTransactionDetail(Supplies, 1)])));

        Assert.Equal("description", Assert.Single(error.Errors).Field);
    }

    [Theory]
    [InlineData(null, "is required")]
    [InlineData("0", "must be greater than zero")]
    [InlineData("-5", "must be greater than zero")]
    [InlineData("10.005", "must have at most two decimals")]
    [InlineData("1000000000000", "must be below 1000000000000")]
    public void Validate_badAmount_namesTheLine(string? amount, string message)
    {
        var value = amount is null ? (decimal?)null : decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        var error = Assert.Throws<TransactionsValidationException>(
            () => MovementRules.Validate(New(new NewTransactionDetail(Supplies, 1), new NewTransactionDetail(Supplies, value))));

        Assert.Equal(new FieldError("details[1].amount", message), Assert.Single(error.Errors));
    }

    [Fact]
    public void Validate_largestAmountAndOneCent_areAccepted()
    {
        var (_, _, lines) = MovementRules.Validate(
            New(new NewTransactionDetail(Supplies, MovementRules.MaxAmount), new NewTransactionDetail(Supplies, 0.01m)));

        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void Validate_lineWithoutSubcategory_orNull_isRefused()
    {
        var error = Assert.Throws<TransactionsValidationException>(
            () => MovementRules.Validate(New(new NewTransactionDetail(null, 5), null)));

        Assert.Equal(
            ["details[0].subcategoryId", "details[1].subcategoryId", "details[1].amount"],
            error.Errors.Select(e => e.Field));
    }

    private static readonly Dictionary<Guid, SubcategoryUse> Uses = new()
    {
        [Supplies] = new SubcategoryUse(Supplies, "expense", IsUsable: true),
        [Rent] = new SubcategoryUse(Rent, "expense", IsUsable: false),
        [Sales] = new SubcategoryUse(Sales, "income", IsUsable: true),
    };

    [Fact]
    public void Classify_linesOfOneType_takeTheirSubcategorysType() // BR-01
    {
        var lines = MovementRules.Classify([(Supplies, 85.50m), (Supplies, 10m)], Uses);

        Assert.All(lines, line => Assert.Equal("expense", line.Type));
        Assert.Equal([85.50m, 10m], lines.Select(l => l.Amount));
    }

    [Fact]
    public void Classify_incomeAndExpense_isABusinessRuleViolation_namingTheOddLine() // BR-02, TC-004
    {
        var error = Assert.Throws<TransactionsBusinessRuleException>(
            () => MovementRules.Classify([(Sales, 100m), (Supplies, 5m)], Uses));

        Assert.Equal(new FieldError("details[1].subcategoryId", "is an expense subcategory"), Assert.Single(error.Errors));
    }

    [Fact]
    public void Classify_inactiveSubcategoryOrCategory_isABusinessRuleViolation() // BR-05
    {
        var error = Assert.Throws<TransactionsBusinessRuleException>(
            () => MovementRules.Classify([(Supplies, 1m), (Rent, 2m)], Uses));

        Assert.Equal("details[1].subcategoryId", Assert.Single(error.Errors).Field);
    }

    [Fact]
    public void Classify_unknownSubcategory_isAValidationError()
    {
        var error = Assert.Throws<TransactionsValidationException>(
            () => MovementRules.Classify([(Guid.NewGuid(), 1m)], Uses));

        Assert.Equal(new FieldError("details[0].subcategoryId", "does not exist"), Assert.Single(error.Errors));
    }
}
