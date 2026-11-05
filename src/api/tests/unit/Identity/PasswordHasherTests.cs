using BudTack.Identity.Internal.Application;
using Xunit;

namespace BudTack.Unit.Tests.Identity;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new(iterations: 1_000);

    [Fact]
    public void Hash_aPassword_isNeverThePlaintext()
    {
        var stored = _hasher.Hash("Str0ngPass!23");

        Assert.DoesNotContain("Str0ngPass!23", stored, StringComparison.Ordinal);
        Assert.Matches(@"^pbkdf2-sha256\$1000\$[A-Za-z0-9+/=]{24}\$[A-Za-z0-9+/=]{44}$", stored);
    }

    [Fact]
    public void Hash_samePasswordTwice_usesADifferentSaltEachTime()
    {
        Assert.NotEqual(_hasher.Hash("Str0ngPass!23"), _hasher.Hash("Str0ngPass!23"));
    }

    [Fact]
    public void Verify_rightPassword_isTrue()
    {
        Assert.True(PasswordHasher.Verify("Str0ngPass!23", _hasher.Hash("Str0ngPass!23")));
    }

    [Fact]
    public void Verify_wrongPassword_isFalse()
    {
        Assert.False(PasswordHasher.Verify("str0ngPass!23", _hasher.Hash("Str0ngPass!23")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plaintext")]
    [InlineData("md5$1$abc$def")]
    [InlineData("pbkdf2-sha256$0$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$x$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$10$not-base64!$AAAA")]
    public void Verify_storedValueNotOurs_isFalse(string stored)
    {
        Assert.False(PasswordHasher.Verify("anything", stored));
    }

    [Fact]
    public void Default_usesOwaspIterations()
    {
        Assert.StartsWith("pbkdf2-sha256$600000$", new PasswordHasher().Hash("x"), StringComparison.Ordinal);
    }
}
