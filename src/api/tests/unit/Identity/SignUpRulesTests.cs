using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Domain;
using Xunit;

namespace BudTack.Unit.Tests.Identity;

public sealed class SignUpRulesTests
{
    [Fact]
    public void Validate_validSignUp_trimsTheNameAndLowercasesTheEmail()
    {
        var (name, email, password) = SignUpRules.Validate(new NewUser("  Ana Owner ", " Owner@Example.COM ", "12345678"));

        Assert.Equal("Ana Owner", name);
        Assert.Equal("owner@example.com", email);
        Assert.Equal("12345678", password);
    }

    [Fact]
    public void Validate_emptySignUp_reportsEveryFieldAtOnce()
    {
        var error = Assert.Throws<IdentityValidationException>(() => SignUpRules.Validate(new NewUser(null, null, null)));

        Assert.Equal(
            [
                new FieldError("name", "Enter your name or your business's name."),
                new FieldError("email", "Enter an email, for example owner@example.com."),
                new FieldError("password", "Use at least 8 characters."),
            ],
            error.Errors);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    public void Validate_blankName_isRefused(string name)
    {
        AssertOnlyField("name", () => SignUpRules.Validate(new NewUser(name, "a@b.co", "12345678")));
    }

    [Fact]
    public void Validate_nameOver100Characters_isRefused()
    {
        AssertOnlyField("name", () => SignUpRules.Validate(new NewUser(new string('a', 101), "a@b.co", "12345678")));
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
        AssertOnlyField("email", () => SignUpRules.Validate(new NewUser("Ana", email, "12345678")));
    }

    [Fact]
    public void Validate_emailOver254Characters_isRefused()
    {
        var email = new string('a', 250) + "@b.co";
        AssertOnlyField("email", () => SignUpRules.Validate(new NewUser("Ana", email, "12345678")));
    }

    [Fact]
    public void Validate_sevenCharacterPassword_isRefused()
    {
        AssertOnlyField("password", () => SignUpRules.Validate(new NewUser("Ana", "a@b.co", "1234567")));
    }

    [Fact]
    public void Validate_passwordOver128Characters_isRefused()
    {
        var error = Assert.Throws<IdentityValidationException>(
            () => SignUpRules.Validate(new NewUser("Ana", "a@b.co", new string('p', 129))));

        Assert.Equal([new FieldError("password", "Use at most 128 characters.")], error.Errors);
    }

    [Fact]
    public void Validate_passwordWithSpaces_isKeptAsTyped()
    {
        var (_, _, password) = SignUpRules.Validate(new NewUser("Ana", "a@b.co", " pass word "));

        Assert.Equal(" pass word ", password);
    }

    [Fact]
    public void Validate_validCredentials_lowercaseTheEmail()
    {
        var (email, password) = SignUpRules.Validate(new Credentials("Owner@Example.com", "x"));

        Assert.Equal("owner@example.com", email);
        Assert.Equal("x", password);
    }

    [Fact]
    public void Validate_emptyCredentials_reportBothFields()
    {
        var error = Assert.Throws<IdentityValidationException>(() => SignUpRules.Validate(new Credentials(null, "")));

        Assert.Equal(
            [
                new FieldError("email", "Enter the email you signed up with, for example owner@example.com."),
                new FieldError("password", "Enter your password."),
            ],
            error.Errors);
    }

    private static void AssertOnlyField(string field, Action validate)
    {
        var error = Assert.Throws<IdentityValidationException>(validate);
        Assert.Equal(field, Assert.Single(error.Errors).Field);
    }
}
