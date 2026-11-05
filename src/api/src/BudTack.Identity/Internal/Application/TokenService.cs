using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BudTack.Identity.Internal.Application;

/// <summary>
/// Issues and checks the bearer token of <c>POST /sessions</c>: an HS256 JWT
/// whose <c>sub</c> is the user id. identity is the only place a token is
/// checked (security-policy.md); the host asks through the facade.
/// </summary>
internal sealed class TokenService
{
    public const string Issuer = "bud-tack-api";
    public const string Audience = "bud-tack";

    private readonly SymmetricSecurityKey _key;
    private readonly TimeSpan _lifetime;
    private readonly TimeProvider _clock;
    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    public TokenService(IdentitySettings settings, TimeProvider clock)
    {
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSecret));
        _lifetime = settings.TokenLifetime;
        _clock = clock;
    }

    public TimeSpan Lifetime => _lifetime;

    public string Issue(Guid userId)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now + _lifetime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256),
        });
    }

    /// <summary>The user id of a valid, unexpired token signed with our key; otherwise null.</summary>
    public async Task<Guid?> ValidateAsync(string token)
    {
        var result = await _handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = _key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        }).ConfigureAwait(false);

        return result.IsValid
            && result.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub)
            && Guid.TryParse(sub as string, out var userId)
                ? userId
                : null;
    }
}
