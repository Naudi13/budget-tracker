using BudTack.Api.Platform.Logging;
using Xunit;

namespace BudTack.Unit.Tests.Platform;

public sealed class CorrelationScopeTests
{
    [Theory]
    [InlineData("abc-123")]
    [InlineData("0f8fad5b_d9cb_469f")]
    public void Resolve_withWellFormedHeader_keepsIt(string incoming)
    {
        Assert.Equal(incoming, CorrelationScope.Resolve(incoming));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("has spaces")]
    [InlineData("line\nbreak")]
    [InlineData("{\"json\":1}")]
    public void Resolve_withMissingOrUnsafeHeader_generatesANewId(string? incoming)
    {
        var resolved = CorrelationScope.Resolve(incoming);

        Assert.NotEqual(incoming, resolved);
        Assert.True(Guid.TryParseExact(resolved, "N", out _));
    }

    [Fact]
    public void Resolve_withOverlongHeader_generatesANewId()
    {
        var incoming = new string('a', 65);

        Assert.NotEqual(incoming, CorrelationScope.Resolve(incoming));
    }
}
