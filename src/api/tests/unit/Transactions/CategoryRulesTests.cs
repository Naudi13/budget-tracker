using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Domain;
using Xunit;

namespace BudTack.Unit.Tests.Transactions;

public sealed class CategoryRulesTests
{
    [Theory]
    [InlineData("income")]
    [InlineData("expense")]
    public void Validate_category_trimsTheNameAndKeepsTheType(string type)
    {
        Assert.Equal(("Oficina", type), CategoryRules.Validate(new NewCategory("  Oficina ", type)));
    }

    [Fact]
    public void Validate_emptyCategory_reportsNameAndType()
    {
        var error = Assert.Throws<TransactionsValidationException>(() => CategoryRules.Validate(new NewCategory(null, null)));

        Assert.Equal(["name", "type"], error.Errors.Select(e => e.Field));
    }

    [Theory]
    [InlineData("Income")]
    [InlineData("savings")]
    [InlineData("")]
    public void Validate_typeOtherThanIncomeOrExpense_isRefused(string type)
    {
        var error = Assert.Throws<TransactionsValidationException>(() => CategoryRules.Validate(new NewCategory("x", type)));

        Assert.Equal("type", Assert.Single(error.Errors).Field);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    public void Validate_blankName_isRefused(string name)
    {
        var error = Assert.Throws<TransactionsValidationException>(() => CategoryRules.Validate(new NewSubcategory(name)));

        Assert.Equal(new FieldError("name", "is required"), Assert.Single(error.Errors));
    }

    [Fact]
    public void Validate_nameOf100Characters_isAccepted_and101_isRefused()
    {
        Assert.Equal(100, CategoryRules.Validate(new NewSubcategory(new string('a', 100))).Length);

        var error = Assert.Throws<TransactionsValidationException>(
            () => CategoryRules.Validate(new NewSubcategory(new string('a', 101))));
        Assert.Equal("must be at most 100 characters", Assert.Single(error.Errors).Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_activation_returnsTheValue(bool isActive)
    {
        Assert.Equal(isActive, CategoryRules.Validate(new ActivationUpdate(isActive)));
    }

    [Fact]
    public void Validate_activationWithoutIsActive_isRefused()
    {
        var error = Assert.Throws<TransactionsValidationException>(() => CategoryRules.Validate(new ActivationUpdate(null)));

        Assert.Equal("isActive", Assert.Single(error.Errors).Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("income")]
    [InlineData("expense")]
    public void ValidateFilter_noneOrAType_isKept(string? type)
    {
        Assert.Equal(type, CategoryRules.ValidateFilter(type));
    }

    [Fact]
    public void ValidateFilter_unknownType_isRefused()
    {
        Assert.Throws<TransactionsValidationException>(() => CategoryRules.ValidateFilter("all"));
    }

    [Fact]
    public void Validate_noPaging_isPage1Of20()
    {
        var paging = CategoryRules.Validate(page: null, limit: null);

        Assert.Equal(new Paging(1, 20), paging);
        Assert.Equal(0, paging.Offset);
    }

    [Fact]
    public void Validate_page3Of10_skips20()
    {
        Assert.Equal(20, CategoryRules.Validate(3, 10).Offset);
    }

    [Theory]
    [InlineData(0, 20, "page")]
    [InlineData(1, 0, "limit")]
    [InlineData(1, 101, "limit")]
    public void Validate_pagingOutOfRange_isRefused(int page, int limit, string field)
    {
        var error = Assert.Throws<TransactionsValidationException>(() => CategoryRules.Validate(page, limit));

        Assert.Equal(field, Assert.Single(error.Errors).Field);
    }
}
