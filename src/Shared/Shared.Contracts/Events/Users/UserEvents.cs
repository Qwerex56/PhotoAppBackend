namespace Shared.Contracts.Events.Users;

/// <summary>
/// Published when user profile is updated.
/// Consumed by: AuditService, DeviceService, UserManagementService
/// </summary>
public class UserProfileUpdatedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string? FullName { get; init; }
    public string? Avatar { get; init; }
    public string? Bio { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Published when a user is deleted from the system.
/// Consumed by: AuditService, MediaService, DeviceService, AuthService
/// </summary>
public class UserDeletedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime DeletedAt { get; init; }
    public string? Reason { get; init; } // self-delete, admin-delete, etc.
}

/// <summary>
/// Published when a user's MFA settings change.
/// Consumed by: AuditService, UserManagementService
/// </summary>
public class UserMfaChangedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string Method { get; init; } = string.Empty; // webauthn, totp, etc.
    public bool Enabled { get; init; }
    public DateTime ChangedAt { get; init; }
}

/// <summary>
/// Published when a user's role is assigned or removed.
/// Consumed by: UserManagementService, AuditService
/// </summary>
public class UserRoleChangedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public bool Assigned { get; init; } // true = assigned, false = removed
    public DateTime ChangedAt { get; init; }
}
