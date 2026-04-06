namespace UserManagementService.Domain.Entities;

/// <summary>
/// Represents a user profile in the system.
/// Owned by UserManagementService. Authentication data is in AuthService.
/// </summary>
public class UserProfile
{
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Email associated with this profile (from AuthService, for reference only).
    /// </summary>
    public string Email { get; private set; } = string.Empty;
    
    /// <summary>
    /// Full name of the user.
    /// </summary>
    public string? FullName { get; private set; }
    
    /// <summary>
    /// Biography or user description.
    /// </summary>
    public string? Bio { get; private set; }
    
    /// <summary>
    /// URL to user's avatar image.
    /// </summary>
    public string? AvatarUrl { get; private set; }
    
    /// <summary>
    /// When the profile was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// When the profile was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; private set; }
    
    /// <summary>
    /// Is the profile deleted (soft delete)?
    /// </summary>
    public bool IsDeleted { get; private set; }
    
    /// <summary>
    /// When the profile was deleted (UTC). Null if not deleted.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }
    
    /// <summary>
    /// User's roles.
    /// </summary>
    public List<UserRole> UserRoles { get; } = [];

    public UserProfile()
    {
    }

    public UserProfile(Guid id, string email, string? fullName = null)
    {
        Id = id;
        Email = email;
        FullName = fullName;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// Updates user profile information.
    /// </summary>
    public void Update(string? fullName = null, string? bio = null, string? avatarUrl = null)
    {
        if (fullName != null)
            FullName = fullName;

        if (bio != null)
            Bio = bio;

        if (avatarUrl != null)
            AvatarUrl = avatarUrl;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft-deletes the profile.
    /// </summary>
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
