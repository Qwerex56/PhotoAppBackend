namespace Shared.Constants;

/// <summary>
/// Authentication-related constants.
/// </summary>
public static class AuthConstants
{
    public const string AuthenticationScheme = "Bearer";
    public const int AccessTokenExpirationMinutes = 15;
    public const int RefreshTokenExpirationDays = 7;
    public const string RefreshTokenCookieName = "photoapp_refresh_token";
    public const string RefreshTokenCookiePath = "/api/auth";
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

    // Media errors
    public const string MediaAlbumNotFound = "MEDIA_ALBUM_NOT_FOUND";
    public const string MediaAssetNotFound = "MEDIA_ASSET_NOT_FOUND";
    public const string MediaTagNotFound = "MEDIA_TAG_NOT_FOUND";
    public const string MediaShareNotFound = "MEDIA_SHARE_NOT_FOUND";
    public const string MediaAccessDenied = "MEDIA_ACCESS_DENIED";
    public const string MediaInvalidFileType = "MEDIA_INVALID_FILE_TYPE";
    public const string MediaFileTooLarge = "MEDIA_FILE_TOO_LARGE";
    public const string MediaUnsafeFile = "MEDIA_UNSAFE_FILE";
    public const string MediaStorageFailure = "MEDIA_STORAGE_FAILURE";
    public const string MediaAlbumAlreadyExists = "MEDIA_ALBUM_ALREADY_EXISTS";
    public const string MediaAssetAlreadyExists = "MEDIA_ASSET_ALREADY_EXISTS";
    public const string MediaTagAlreadyExists = "MEDIA_TAG_ALREADY_EXISTS";
    public const string MediaPermissionInvalid = "MEDIA_PERMISSION_INVALID";

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
    public static string MediaAlbum(Guid albumId) => $"{Prefix}media:album:{albumId}";
    public static string MediaAsset(Guid mediaId) => $"{Prefix}media:asset:{mediaId}";
    public static string MediaLibrary(Guid userId) => $"{Prefix}media:library:{userId}";
    public static string MediaAccess(string resourceType, Guid resourceId, Guid userId, string permission)
        => $"{Prefix}media:access:{resourceType}:{resourceId}:{userId}:{permission}";
}

/// <summary>
/// Media-specific constants used by the MediaService.
/// </summary>
public static class MediaConstants
{
    public const long MaximumUploadSizeBytes = 500L * 1024L * 1024L;
    public const int AccessCacheMinutes = 10;
    public const int DefaultShareExpirationDays = 30;
    public const string FavoriteTagName = "favorite";

    public static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    public static readonly string[] AllowedVideoExtensions = [".mp4", ".mov", ".m4v", ".webm"];
    public static readonly string[] AllowedImageContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    public static readonly string[] AllowedVideoContentTypes = ["video/mp4", "video/quicktime", "video/webm"];
    public static readonly string[] ForbiddenExtensions = [
        ".exe", ".dll", ".bat", ".cmd", ".ps1", ".sh", ".msi", ".jar", ".com", ".scr", ".vbs", ".js", ".svg"
    ];

    public static bool IsAllowedExtension(string extension)
        => IsAllowedImageExtension(extension) || IsAllowedVideoExtension(extension);

    public static bool IsAllowedImageExtension(string extension)
        => Array.Exists(AllowedImageExtensions, value => string.Equals(value, extension, StringComparison.OrdinalIgnoreCase));

    public static bool IsAllowedVideoExtension(string extension)
        => Array.Exists(AllowedVideoExtensions, value => string.Equals(value, extension, StringComparison.OrdinalIgnoreCase));

    public static bool IsAllowedContentType(string contentType)
        => IsAllowedImageContentType(contentType) || IsAllowedVideoContentType(contentType);

    public static bool IsAllowedImageContentType(string contentType)
        => Array.Exists(AllowedImageContentTypes, value => string.Equals(value, contentType, StringComparison.OrdinalIgnoreCase));

    public static bool IsAllowedVideoContentType(string contentType)
        => Array.Exists(AllowedVideoContentTypes, value => string.Equals(value, contentType, StringComparison.OrdinalIgnoreCase));

    public static bool IsForbiddenExtension(string extension)
        => Array.Exists(ForbiddenExtensions, value => string.Equals(value, extension, StringComparison.OrdinalIgnoreCase));
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
