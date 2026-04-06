namespace AuthService.Application.Services;

using Shared.Results;

/// <summary>
/// Handles password hashing and verification using Argon2id.
/// Implements OWASP recommendations for secure password storage.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain password using Argon2id with secure parameters.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plain password against a hash.
    /// </summary>
    bool VerifyPassword(string password, string hash);
}

/// <summary>
/// Generates and manages JWT tokens for authentication.
/// Handles both access and refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a short-lived JWT access token.
    /// </summary>
    string GenerateAccessToken(Guid userId, string email, List<string> roles);

    /// <summary>
    /// Generates a refresh token value (secure random string).
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates and extracts claims from a JWT token.
    /// Returns null if validation fails.
    /// </summary>
    Dictionary<string, string>? ValidateAndExtractClaims(string token);

    /// <summary>
    /// Validates a JWT token without extracting claims.
    /// </summary>
    bool ValidateToken(string token);
}

/// <summary>
/// Manages email token generation and validation.
/// Uses secure random tokens for email verification.
/// </summary>
public interface IEmailTokenService
{
    /// <summary>
    /// Generates a random token for email verification.
    /// </summary>
    string GenerateToken();

    /// <summary>
    /// Generates a token hash for secure storage.
    /// </summary>
    string HashToken(string token);

    /// <summary>
    /// Verifies a token against a hash.
    /// </summary>
    bool VerifyToken(string token, string hash);
}

/// <summary>
/// Sends emails for authentication events (verification, password reset, etc.).
/// TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification link to the user.
    /// </summary>
    Task<Result> SendEmailVerificationAsync(string email, string verificationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset link to the user.
    /// </summary>
    Task<Result> SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification of a new login from an unusual location/device.
    /// </summary>
    Task<Result> SendLoginAlertAsync(string email, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validates password security requirements (OWASP guidelines).
/// </summary>
public interface IPasswordValidator
{
    /// <summary>
    /// Validates a password against security requirements.
    /// </summary>
    Result ValidatePassword(string password);

    /// <summary>
    /// Gets a detailed list of unmet password requirements.
    /// </summary>
    List<string> GetUnmetRequirements(string password);
}
