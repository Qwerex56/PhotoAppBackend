namespace AuthService.Application.Commands.Auth;

using Shared.Contracts.Commands;
using Shared.Results;

/// <summary>
/// Command to register a new user.
/// </summary>
public class RegisterCommand : Command<Result<RegisterCommandResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

/// <summary>
/// Response containing newly registered user information.
/// </summary>
public class RegisterCommandResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Message { get; init; } = "Registration successful. Please verify your email.";
}

/// <summary>
/// Command to log in a user and obtain tokens.
/// </summary>
public class LoginCommand : Command<Result<LoginCommandResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

/// <summary>
/// Response containing authentication tokens.
/// </summary>
public class LoginCommandResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int AccessTokenExpiresIn { get; init; } // Seconds
    public bool RequiresMfa { get; init; }
    public string? MfaChallenge { get; init; }
}

/// <summary>
/// Command to refresh an access token using a refresh token.
/// Implements token rotation for enhanced security.
/// </summary>
public class RefreshTokenCommand : Command<Result<RefreshTokenResponse>>
{
    public string RefreshToken { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

/// <summary>
/// Response containing new tokens after refresh.
/// </summary>
public class RefreshTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty; // New rotated token
    public int AccessTokenExpiresIn { get; init; }
}

/// <summary>
/// Command to verify user's email address using the token.
/// </summary>
public class VerifyEmailCommand : Command<Result>
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}

/// <summary>
/// Command to request password reset (sends email with token).
/// </summary>
public class RequestPasswordResetCommand : Command<Result>
{
    public string Email { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
}

/// <summary>
/// Command to reset password using the reset token.
/// </summary>
public class ResetPasswordCommand : Command<Result>
{
    public Guid UserId { get; init; }
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Command to logout (revoke current token).
/// </summary>
public class LogoutCommand : Command<Result>
{
    public Guid UserId { get; init; }
    public Guid? RefreshTokenId { get; init; } // If null, revoke all tokens
    public string? Reason { get; init; }
}
