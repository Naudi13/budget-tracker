using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Application;
using Xunit;

namespace BudTack.Unit.Tests.Identity;

/// <summary>HU-09: the account list, paged and ordered by email.</summary>
public sealed class ListUsersTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly InMemoryUsers _users = new();

    private ListUsers ListUsers => new(_users);

    private void Add(string email, string role = Roles.User) =>
        _users.Rows.Add(new BudTack.Identity.Internal.Domain.User(Guid.NewGuid(), "Ana", email, "hash", role));

    [Fact]
    public async Task HandleAsync_someAccounts_listsThemByEmailWithTheirRole()
    {
        Add("zoe@example.com");
        Add("admin@example.com", Roles.Admin);

        var page = await ListUsers.HandleAsync(null, null, Ct);

        Assert.Equal(["admin@example.com", "zoe@example.com"], page.Data.Select(user => user.Email));
        Assert.Equal(Roles.Admin, page.Data[0].Role);
        Assert.Equal(new UserPageMeta(1, 20, 2), page.Meta);
    }

    [Fact]
    public async Task HandleAsync_secondPage_skipsTheFirst()
    {
        Add("a@example.com");
        Add("b@example.com");

        var page = await ListUsers.HandleAsync(2, 1, Ct);

        Assert.Equal("b@example.com", Assert.Single(page.Data).Email);
    }

    [Theory]
    [InlineData(0, null, "page")]
    [InlineData(null, 0, "limit")]
    [InlineData(null, 101, "limit")]
    public async Task HandleAsync_pagingOutOfBounds_namesTheField(int? page, int? limit, string field)
    {
        var error = await Assert.ThrowsAsync<IdentityValidationException>(() => ListUsers.HandleAsync(page, limit, Ct));

        Assert.Equal(field, Assert.Single(error.Errors).Field);
    }
}
