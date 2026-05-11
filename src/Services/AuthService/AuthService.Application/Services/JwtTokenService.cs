namespace AuthService.Application.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Shared.Constants;

public sealed class JwtTokenService : ITokenService
{
    private const string DefaultIssuer = "PhotoAppBackend.AuthService";
    private const string DefaultAudience = "PhotoAppBackend";
    private const int RefreshTokenSizeBytes = 64;

    private readonly string _issuer;
    private readonly string _audience;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtTokenService()
    {
        _issuer = Environment.GetEnvironmentVariable("AUTH_JWT_ISSUER") ?? DefaultIssuer;
        _audience = Environment.GetEnvironmentVariable("AUTH_JWT_AUDIENCE") ?? DefaultAudience;

        var signingKey = Environment.GetEnvironmentVariable("AUTH_JWT_SIGNING_KEY")
            ?? throw new InvalidOperationException("AUTH_JWT_SIGNING_KEY is required for JwtTokenService.");

        if (signingKey.Length < 32)
        {
            throw new InvalidOperationException("AUTH_JWT_SIGNING_KEY must be at least 32 characters long.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = securityKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    public string GenerateAccessToken(Guid userId, string email, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new(AuthConstants.JwtClaimSubject, userId.ToString()),
            new(AuthConstants.JwtClaimEmail, email),
            new(AuthConstants.JwtClaimTokenId, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(AuthConstants.JwtClaimRoles, role)));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(AuthConstants.AccessTokenExpirationMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenSizeBytes));

    public Dictionary<string, string>? ValidateAndExtractClaims(string token)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, _tokenValidationParameters, out _);

            var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var claim in principal.Claims)
            {
                if (claims.TryGetValue(claim.Type, out var existingValue))
                {
                    if (!string.IsNullOrWhiteSpace(claim.Value) && !existingValue.Contains(claim.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        claims[claim.Type] = $"{existingValue},{claim.Value}";
                    }

                    continue;
                }

                claims[claim.Type] = claim.Value;
            }

            return claims;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public bool ValidateToken(string token)
    {
        try
        {
            new JwtSecurityTokenHandler().ValidateToken(token, _tokenValidationParameters, out _);
            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}