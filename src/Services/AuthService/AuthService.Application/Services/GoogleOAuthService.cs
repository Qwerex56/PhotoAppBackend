namespace AuthService.Application.Services;

using System.Data.Common;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using AuthService.Application.Commands.Auth;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Contracts.Events.Auth;
using Shared.Results;

public sealed class GoogleOAuthService : IGoogleOAuthService
{
    private const string DefaultAuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string DefaultTokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string DefaultUserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";

    private readonly HttpClient _httpClient;
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailTokenService _emailTokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;
    private readonly ILogger<GoogleOAuthService> _logger;

    public GoogleOAuthService(
        HttpClient httpClient,
        IAuthUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailTokenService emailTokenService,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration,
        ITokenService tokenService,
        ILogger<GoogleOAuthService> logger)
    {
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _emailTokenService = emailTokenService;
        _publishEndpoint = publishEndpoint;
        _configuration = configuration;
        _tokenService = tokenService;
        _logger = logger;
    }

    public Task<Result<string>> BuildAuthorizationUrlAsync(string? returnUrl, CancellationToken cancellationToken = default)
    {
        var clientId = _configuration["AuthOAuth:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Task.FromResult(Result<string>.Failure(Error.InternalError("Google OAuth client id is not configured.")));
        }

        var frontendCallbackUrl = _configuration["AuthOAuth:Google:RedirectUri"]
            ?? "https://app.localhost/oauth/google/callback";
        var scope = _configuration["AuthOAuth:Google:Scope"] ?? "openid email profile";

        var state = _tokenService.GenerateChallengeToken(new Dictionary<string, string>
        {
            ["purpose"] = "oauth_google",
            ["provider"] = "google",
            ["redirect_uri"] = frontendCallbackUrl,
            ["return_url"] = string.IsNullOrWhiteSpace(returnUrl) ? "/app" : returnUrl!,
            ["nonce"] = Guid.NewGuid().ToString("N")
        }, TimeSpan.FromMinutes(AuthConstants.OAuthStateExpirationMinutes));

        var authorizationEndpoint = _configuration["AuthOAuth:Google:AuthorizationEndpoint"] ?? DefaultAuthorizationEndpoint;
        var authorizationUrl = $"{authorizationEndpoint}?client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(frontendCallbackUrl)}&response_type=code&scope={Uri.EscapeDataString(scope)}&state={Uri.EscapeDataString(state)}&access_type=offline&prompt=consent";

        return Task.FromResult(Result<string>.Success(authorizationUrl));
    }

    public async Task<Result<GoogleOAuthProfile>> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken = default)
    {
        var stateClaims = _tokenService.ValidateAndExtractClaims(state);
        if (stateClaims is null)
        {
            return Result<GoogleOAuthProfile>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (!stateClaims.TryGetValue("purpose", out var purpose) || !string.Equals(purpose, "oauth_google", StringComparison.Ordinal))
        {
            return Result<GoogleOAuthProfile>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (!stateClaims.TryGetValue("redirect_uri", out var redirectUri) || string.IsNullOrWhiteSpace(redirectUri))
        {
            return Result<GoogleOAuthProfile>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        var clientId = _configuration["AuthOAuth:Google:ClientId"];
        var clientSecret = _configuration["AuthOAuth:Google:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return Result<GoogleOAuthProfile>.Failure(Error.InternalError("Google OAuth credentials are not configured."));
        }

        var tokenEndpoint = _configuration["AuthOAuth:Google:TokenEndpoint"] ?? DefaultTokenEndpoint;
        var userInfoEndpoint = _configuration["AuthOAuth:Google:UserInfoEndpoint"] ?? DefaultUserInfoEndpoint;

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri
            })
        };

        using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google token exchange failed with status {StatusCode}.", tokenResponse.StatusCode);
            return Result<GoogleOAuthProfile>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken);
        if (tokenPayload is null || string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
        {
            return Result<GoogleOAuthProfile>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
        userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenPayload.AccessToken);

        using var userInfoResponse = await _httpClient.SendAsync(userInfoRequest, cancellationToken);
        if (!userInfoResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google userinfo request failed with status {StatusCode}.", userInfoResponse.StatusCode);
            return Result<GoogleOAuthProfile>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfoResponse>(cancellationToken: cancellationToken);
        if (userInfo is null || string.IsNullOrWhiteSpace(userInfo.Email) || userInfo.EmailVerified is not true || string.IsNullOrWhiteSpace(userInfo.Subject))
        {
            return Result<GoogleOAuthProfile>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        return Result<GoogleOAuthProfile>.Success(new GoogleOAuthProfile(userInfo.Email, userInfo.FullName, userInfo.Subject));
    }

    public async Task<Result<LoginCommandResponse>> CompleteGoogleAuthorizationAsync(string code, string state, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var profileResult = await ExchangeCodeAsync(code, state, cancellationToken);
        if (profileResult.IsFailure || profileResult.Value is null)
        {
            return Result<LoginCommandResponse>.Failure(profileResult.Error ?? Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        var profile = profileResult.Value;
        var user = await _unitOfWork.Users.GetByEmailAsync(profile.Email, cancellationToken);
        var isNewUser = user is null;

        if (user is null)
        {
            var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            user = new User(Guid.NewGuid(), profile.Email, _passwordHasher.HashPassword(randomPassword));
        }

        user.VerifyEmail();

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, []);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken(
            user.Id,
            _emailTokenService.HashToken(refreshTokenValue),
            DateTime.UtcNow.AddDays(AuthConstants.RefreshTokenExpirationDays),
            ipAddress,
            userAgent);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            if (isNewUser)
            {
                await _unitOfWork.Users.CreateAsync(user, cancellationToken);
            }
            else
            {
                await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            }

            await _unitOfWork.RefreshTokens.CreateAsync(refreshToken, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (DbException e)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result<LoginCommandResponse>.Failure(Error.InternalError(e.Message));
        }

        if (isNewUser)
        {
            await _publishEndpoint.Publish(new UserRegisteredEvent
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = profile.FullName,
                RegisteredAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _publishEndpoint.Publish(new UserLoggedInEvent
        {
            UserId = user.Id,
            IpAddress = ipAddress ?? string.Empty,
            UserAgent = userAgent ?? string.Empty,
            LoginAt = DateTime.UtcNow
        }, cancellationToken);

        return Result<LoginCommandResponse>.Success(new LoginCommandResponse
        {
            UserId = user.Id,
            Email = user.Email,
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiresIn = AuthConstants.AccessTokenExpirationMinutes * 60,
            RequiresMfa = false,
            MfaChallenge = null,
            MfaMethod = null
        });
    }

    private sealed record GoogleTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] string Scope,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("id_token")] string? IdToken);

    private sealed record GoogleUserInfoResponse(
        [property: JsonPropertyName("sub")] string Subject,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("email_verified")] bool? EmailVerified,
        [property: JsonPropertyName("name")] string? FullName);
}