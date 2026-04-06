namespace Shared.Contracts.Events.Auth;

/// <summary>
/// Published when a user successfully registers in the system.
/// Consumed by: UserManagementService, AuditService, DeviceService
/// </summary>
public class UserRegisteredEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public DateTime RegisteredAt { get; init; }
}

/// <summary>
/// Published when a user successfully logs in.
/// Consumed by: AuditService, DeviceService, MFAService
/// </summary>
public class UserLoggedInEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public string UserAgent { get; init; } = string.Empty;
    public DateTime LoginAt { get; init; }
}

/// <summary>
/// Published when a refresh token is rotated (security best practice).
/// Consumed by: AuditService, DeviceService
/// </summary>
public class RefreshTokenRotatedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public Guid TokenId { get; init; }
    public DateTime RotatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Published when a user's session is revoked (logout or suspicious activity detected).
/// Consumed by: AuditService, DeviceService, UserManagementService
/// </summary>
public class SessionRevokedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public Guid? TokenId { get; init; } // If null, all tokens revoked
    public string Reason { get; init; } = string.Empty; // logout, suspicious_activity, etc.
    public DateTime RevokedAt { get; init; }
}

/// <summary>
/// Published when email verification fails or succeeds.
/// Consumed by: AuditService
/// </summary>
public class EmailVerificationAttemptedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public bool Success { get; init; }
    public DateTime AttemptedAt { get; init; }
}
