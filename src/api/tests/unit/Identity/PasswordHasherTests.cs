using BudTack.Identity.Internal.Application;
using Xunit;

namespace BudTack.Unit.Tests.Identity;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new(workFactor: 4);

    [Fact]
    public void Hash_aPassword_isNeverThePlaintext()
    {
        var stored = _hasher.Hash("Str0ngPass!23");

        Assert.DoesNotContain("Str0ngPass!23", stored, StringComparison.Ordinal);
        Assert.Matches(@"^\$2a\$04\$[./A-Za-z0-9]{53}$", stored);
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

    [Fact]
    public void Verify_passwordsThatDifferAfter72Bytes_areDifferent() // ADR-008
    {
        var stored = _hasher.Hash(new string('a', 100) + "1");

        Assert.False(PasswordHasher.Verify(new string('a', 100) + "2", stored));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plaintext")]
    [InlineData("md5$1$abc$def")]
    [InlineData("pbkdf2-sha256$600000$AAAA$AAAA")]
    [InlineData("$2a$")]
    public void Verify_storedValueNotOurs_isFalse(string stored)
    {
        Assert.False(PasswordHasher.Verify("anything", stored));
    }

    [Fact]
    public void Default_usesCost12()
    {
        Assert.StartsWith("$2a$12$", new PasswordHasher().Hash("x"), StringComparison.Ordinal);
    }
}
