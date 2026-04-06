namespace UserManagementService.Domain.Entities;

/// <summary>
/// Represents a role in the system (Admin, User, Moderator, etc.).
/// </summary>
public class Role
{
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Role name (e.g., "Admin", "User", "Moderator").
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    
    /// <summary>
    /// Description of the role's purpose.
    /// </summary>
    public string? Description { get; private set; }
    
    /// <summary>
    /// Is this a system role that cannot be deleted?
    /// </summary>
    public bool IsSystem { get; private set; }
    
    /// <summary>
    /// When the role was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// When the role was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; private set; }
    
    /// <summary>
    /// Users assigned to this role.
    /// </summary>
    public List<UserRole> UserRoles { get; } = [];

    public Role()
    {
    }

    public Role(string name, string? description = null, bool isSystem = false)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        IsSystem = isSystem;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates role information.
    /// </summary>
    public void Update(string? description = null)
    {
        if (description != null)
            Description = description;

        UpdatedAt = DateTime.UtcNow;
    }
}
