namespace Shared.Constants;

/// <summary>
/// Authentication-related constants.
/// </summary>
public static class AuthConstants
{
    public const string AuthenticationScheme = "Bearer";
    public const int AccessTokenExpirationMinutes = 15;
    public const int RefreshTokenExpirationDays = 7;
    public const string JwtClaimSubject = "sub";
    public const string JwtClaimEmail = "email";
    public const string JwtClaimRoles = "roles";
    public const string JwtClaimTokenId = "jti";
    
    // Security settings (OWASP recommendations)
    public const int MinimumPasswordLength = 12;
    public const int PasswordHashIterations = 4; // For Argon2id
    public const int PasswordHashMemorySize = 65536; // 64 MB
    public const int PasswordHashParallelism = 2;
}

/// <summary>
/// Common error codes used across services.
/// </summary>
public static class ErrorCodes
{
    // Auth errors
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string UserNotFound = "AUTH_USER_NOT_FOUND";
    public const string UserAlreadyExists = "AUTH_USER_ALREADY_EXISTS";
    public const string InvalidToken = "AUTH_INVALID_TOKEN";
    public const string TokenExpired = "AUTH_TOKEN_EXPIRED";
    public const string RefreshTokenReuse = "AUTH_REFRESH_TOKEN_REUSE";
    public const string InvalidRefreshToken = "AUTH_INVALID_REFRESH_TOKEN";
    public const string EmailNotVerified = "AUTH_EMAIL_NOT_VERIFIED";
    public const string WeakPassword = "AUTH_WEAK_PASSWORD";

    // User errors
    public const string UserProfileNotFound = "USER_PROFILE_NOT_FOUND";
    public const string InvalidRole = "USER_INVALID_ROLE";
    public const string RoleAlreadyAssigned = "USER_ROLE_ALREADY_ASSIGNED";
    public const string CannotRemoveAdminRole = "USER_CANNOT_REMOVE_ADMIN_ROLE";

    // MFA errors
    public const string MfaRequired = "MFA_REQUIRED";
    public const string InvalidMfaCode = "MFA_INVALID_CODE";
    public const string MfaAlreadyEnabled = "MFA_ALREADY_ENABLED";
    public const string MfaNotEnabled = "MFA_NOT_ENABLED";

    // General errors
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InternalError = "INTERNAL_ERROR";
    public const string Forbidden = "FORBIDDEN";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
}

/// <summary>
/// Cache key patterns used across services.
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "photoapp:";
    
    public static string User(Guid userId) => $"{Prefix}user:{userId}";
    public static string UserByEmail(string email) => $"{Prefix}user:email:{email}";
    public static string RefreshTokenBlacklist(Guid tokenId) => $"{Prefix}refresh_token_blacklist:{tokenId}";
    public static string MfaSecret(Guid userId) => $"{Prefix}mfa_secret:{userId}";
    public static string EmailVerificationToken(string email) => $"{Prefix}email_verification:{email}";
    public static string PasswordResetToken(string email) => $"{Prefix}password_reset:{email}";
    public static string Roles(Guid userId) => $"{Prefix}roles:{userId}";
    public static string SessionCount(Guid userId) => $"{Prefix}session_count:{userId}";
}

/// <summary>
/// Role names used in the system.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Moderator = "Moderator";
}

/// <summary>
/// Message queue names and topics.
/// </summary>
public static class MessageQueues
{
    public const string AuthServiceQueue = "photo-app.auth-service";
    public const string UserServiceQueue = "photo-app.user-management-service";
    public const string MediaServiceQueue = "photo-app.media-service";
    public const string AuditServiceQueue = "photo-app.audit-service";
    
    // Event topics
    public const string AuthEventsTopic = "photo-app.auth.events";
    public const string UserEventsTopic = "photo-app.user.events";
    public const string MediaEventsTopic = "photo-app.media.events";
}

/// <summary>
/// Policy names for authorization.
/// </summary>
public static class PolicyNames
{
    public const string RequireAuthentication = "RequireAuthentication";
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireEmailVerified = "RequireEmailVerified";
    public const string RequireMfa = "RequireMfa";
}
