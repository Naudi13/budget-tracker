using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Domain;
using Xunit;

namespace BudTack.Unit.Tests.Identity;

public sealed class AccountRulesTests
{
    [Fact]
    public void Validate_validAccount_trimsTheNameAndLowercasesTheEmail()
    {
        var (name, email, role) = AccountRules.Validate(new NewUser("  Ana Owner ", " Owner@Example.COM ", Roles.Admin));

        Assert.Equal("Ana Owner", name);
        Assert.Equal("owner@example.com", email);
        Assert.Equal(Roles.Admin, role);
    }

    [Fact]
    public void Validate_emptyAccount_reportsEveryFieldAtOnce()
    {
        var error = Assert.Throws<IdentityValidationException>(() => AccountRules.Validate(new NewUser(null, null, null)));

        Assert.Equal(["name", "email", "role"], error.Errors.Select(e => e.Field));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    public void Validate_blankName_isRefused(string name)
    {
        AssertOnlyField("name", () => AccountRules.Validate(new NewUser(name, "a@b.co", Roles.User)));
    }

    [Fact]
    public void Validate_nameOver100Characters_isRefused()
    {
        AssertOnlyField("name", () => AccountRules.Validate(new NewUser(new string('a', 101), "a@b.co", Roles.User)));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("owner@")]
    [InlineData("@example.com")]
    [InlineData("owner@example")]
    [InlineData("own er@example.com")]
    [InlineData("a@b@c.com")]
    public void Validate_malformedEmail_isRefused(string email)
    {
        AssertOnlyField("email", () => AccountRules.Validate(new NewUser("Ana", email, Roles.User)));
    }

    [Theory]
    [InlineData("OWNER")]
    [InlineData("user")]
    [InlineData("")]
    public void Validate_aRoleThatDoesNotExist_isRefused(string role)
    {
        AssertOnlyField("role", () => AccountRules.Validate(new NewUser("Ana", "a@b.co", role)));
    }

    [Fact]
    public void Validate_sevenCharacterPassword_isRefused()
    {
        AssertOnlyField("password", () => AccountRules.Validate(new LinkConfirmation("t", "1234567")));
    }

    [Fact]
    public void Validate_passwordOver128Characters_isRefused()
    {
        var error = Assert.Throws<IdentityValidationException>(
            () => AccountRules.Validate(new LinkConfirmation("t", new string('p', 129))));

        Assert.Equal([new FieldError("password", "Use at most 128 characters.")], error.Errors);
    }

    [Fact]
    public void Validate_passwordWithSpaces_isKeptAsTyped()
    {
        Assert.Equal(" pass word ", AccountRules.Validate(new LinkConfirmation("t", " pass word ")).Password);
    }

    [Fact]
    public void Validate_noToken_answersLikeAnInvalidLink()
    {
        var error = Assert.Throws<IdentityValidationException>(() => AccountRules.Validate(new LinkConfirmation(" ", "12345678")));

        Assert.Equal([new FieldError("token", AccountRules.InvalidLink)], error.Errors);
    }

    [Fact]
    public void Validate_validCredentials_lowercaseTheEmail()
    {
        var (email, password) = AccountRules.Validate(new Credentials("Owner@Example.com", "x"));

        Assert.Equal("owner@example.com", email);
        Assert.Equal("x", password);
    }

    [Fact]
    public void Validate_emptyCredentials_reportBothFields()
    {
        var error = Assert.Throws<IdentityValidationException>(() => AccountRules.Validate(new Credentials(null, "")));

        Assert.Equal(["email", "password"], error.Errors.Select(e => e.Field));
    }

    private static void AssertOnlyField(string field, Action validate)
    {
        var error = Assert.Throws<IdentityValidationException>(validate);
        Assert.Equal(field, Assert.Single(error.Errors).Field);
    }
}
