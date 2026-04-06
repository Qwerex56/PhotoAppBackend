namespace UserManagementService.Domain.Entities;

/// <summary>
/// Represents the assignment of a role to a user.
/// Many-to-many relationship between User and Role.
/// </summary>
public class UserRole
{
    public Guid Id { get; private set; }
    
    /// <summary>
    /// User profile ID.
    /// </summary>
    public Guid UserProfileId { get; private set; }
    
    /// <summary>
    /// User profile navigation property.
    /// </summary>
    public UserProfile UserProfile { get; private set; } = null!;
    
    /// <summary>
    /// Role ID.
    /// </summary>
    public Guid RoleId { get; private set; }
    
    /// <summary>
    /// Role navigation property.
    /// </summary>
    public Role Role { get; private set; } = null!;
    
    /// <summary>
    /// When the role was assigned (UTC).
    /// </summary>
    public DateTime AssignedAt { get; private set; }
    
    /// <summary>
    /// When the role assignment expires (UTC). Null = never expires.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    public UserRole()
    {
    }

    public UserRole(Guid userProfileId, Guid roleId, DateTime? expiresAt = null)
    {
        Id = Guid.NewGuid();
        UserProfileId = userProfileId;
        RoleId = roleId;
        AssignedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Checks if the role assignment is still active.
    /// </summary>
    public bool IsActive => ExpiresAt == null || DateTime.UtcNow < ExpiresAt;
}
