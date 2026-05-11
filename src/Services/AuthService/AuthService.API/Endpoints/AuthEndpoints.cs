namespace AuthService.API.Endpoints;

using System.Security.Claims;
using AuthService.Application.Commands.Auth;
using MediatR;
using Shared.Constants;
using Shared.Results;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithOpenApi();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithOpenApi();

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .WithOpenApi();

        group.MapPost("/verify-email", VerifyEmailAsync)
            .WithName("VerifyEmail")
            .WithOpenApi();

        group.MapPost("/request-password-reset", RequestPasswordResetAsync)
            .WithName("RequestPasswordReset")
            .WithOpenApi();

        group.MapPost("/reset-password", ResetPasswordAsync)
            .WithName("ResetPassword")
            .WithOpenApi();

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .RequireAuthorization()
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, ISender sender, HttpContext context)
    {
        var command = new RegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            IpAddress = GetClientIpAddress(context),
            UserAgent = GetUserAgent(context)
        };

        return (await sender.Send(command)).ToHttpResult();
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, ISender sender, HttpContext context)
    {
        var command = new LoginCommand
        {
            Email = request.Email,
            Password = request.Password,
            IpAddress = GetClientIpAddress(context),
            UserAgent = GetUserAgent(context)
        };

        var result = await sender.Send(command);

        if (result.IsSuccess && result.Value is not null)
        {
            SetRefreshTokenCookie(context, result.Value.RefreshToken);
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> RefreshAsync(RefreshTokenRequest request, ISender sender, HttpContext context)
    {
        var refreshToken = request.RefreshToken ?? GetRefreshTokenFromCookie(context);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Results.Unauthorized();
        }

        var command = new RefreshTokenCommand
        {
            RefreshToken = refreshToken,
            IpAddress = GetClientIpAddress(context),
            UserAgent = GetUserAgent(context)
        };

        var result = await sender.Send(command);

        if (result.IsSuccess && result.Value is not null)
        {
            SetRefreshTokenCookie(context, result.Value.RefreshToken);
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> VerifyEmailAsync(VerifyEmailRequest request, ISender sender)
    {
        var command = new VerifyEmailCommand
        {
            UserId = request.UserId,
            Email = request.Email,
            Token = request.Token
        };

        return (await sender.Send(command)).ToHttpResult();
    }

    private static async Task<IResult> RequestPasswordResetAsync(RequestPasswordResetRequest request, ISender sender, HttpContext context)
    {
        var command = new RequestPasswordResetCommand
        {
            Email = request.Email,
            IpAddress = GetClientIpAddress(context)
        };

        return (await sender.Send(command)).ToHttpResult();
    }

    private static async Task<IResult> ResetPasswordAsync(ResetPasswordRequest request, ISender sender)
    {
        var command = new ResetPasswordCommand
        {
            UserId = request.UserId,
            Token = request.Token,
            NewPassword = request.NewPassword
        };

        return (await sender.Send(command)).ToHttpResult();
    }

    private static async Task<IResult> LogoutAsync(LogoutRequest request, ClaimsPrincipal user, ISender sender, HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Results.Unauthorized();
        }

        var refreshToken = request.RefreshToken ?? GetRefreshTokenFromCookie(context);

        var command = new LogoutCommand
        {
            UserId = userId,
            RefreshToken = refreshToken,
            RefreshTokenId = request.RefreshTokenId,
            Reason = request.Reason
        };

        var result = await sender.Send(command);

        if (result.IsSuccess)
        {
            ClearRefreshTokenCookie(context);
        }

        return result.ToHttpResult();
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var subject = principal.FindFirstValue(AuthConstants.JwtClaimSubject);
        return Guid.TryParse(subject, out userId);
    }

    private static string? GetClientIpAddress(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString();

    private static string? GetUserAgent(HttpContext context)
    {
        var userAgent = context.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
    }

    private static string? GetRefreshTokenFromCookie(HttpContext context)
        => context.Request.Cookies.TryGetValue(AuthConstants.RefreshTokenCookieName, out var token) ? token : null;

    private static void SetRefreshTokenCookie(HttpContext context, string refreshToken)
    {
        context.Response.Cookies.Append(AuthConstants.RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = AuthConstants.RefreshTokenCookiePath,
            Expires = DateTimeOffset.UtcNow.AddDays(AuthConstants.RefreshTokenExpirationDays),
            MaxAge = TimeSpan.FromDays(AuthConstants.RefreshTokenExpirationDays),
            IsEssential = true
        });
    }

    private static void ClearRefreshTokenCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(AuthConstants.RefreshTokenCookieName, new CookieOptions
        {
            Path = AuthConstants.RefreshTokenCookiePath,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax
        });
    }

    private static IResult ToHttpResult(this Result result)
    {
        return result.IsSuccess ? Results.NoContent() : ToProblem(result.Error!);
    }

    private static IResult ToHttpResult<T>(this Result<T> result)
    {
        return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);
    }

    private static IResult ToProblem(Error error)
    {
        var statusCode = error.Code switch
        {
            "VALIDATION_ERROR" or "BAD_REQUEST" => StatusCodes.Status400BadRequest,
            "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "CONFLICT" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: statusCode);
    }

    private sealed record RegisterRequest(string Email, string Password);
    private sealed record LoginRequest(string Email, string Password);
    private sealed record RefreshTokenRequest(string? RefreshToken);
    private sealed record VerifyEmailRequest(Guid UserId, string Email, string Token);
    private sealed record RequestPasswordResetRequest(string Email);
    private sealed record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);
    private sealed record LogoutRequest(string? RefreshToken, Guid? RefreshTokenId, string? Reason);
}