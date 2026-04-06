namespace UserManagementService.Domain.Repositories;

using Entities;

/// <summary>
/// Repository interface for UserProfile entity.
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>
    /// Creates a new user profile.
    /// </summary>
    Task<UserProfile> CreateAsync(UserProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user profile by ID.
    /// </summary>
    Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user profile by email.
    /// </summary>
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all user profiles (paginated for large result sets).
    /// </summary>
    Task<List<UserProfile>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user profile.
    /// </summary>
    Task<UserProfile> UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a user profile.
    /// </summary>
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a profile with given email exists.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Role entity.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Creates a new role.
    /// </summary>
    Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a role by ID.
    /// </summary>
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a role by name.
    /// </summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles.
    /// </summary>
    Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a role.
    /// </summary>
    Task<Role> UpdateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role (only if not system role).
    /// </summary>
    Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role with given name exists.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for UserRole entity.
/// </summary>
public interface IUserRoleRepository
{
    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    Task<UserRole> AssignRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles for a user.
    /// </summary>
    Task<List<UserRole>> GetUserRolesAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active roles for a user (not expired).
    /// </summary>
    Task<List<UserRole>> GetActiveUserRolesAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task RemoveRoleAsync(Guid userProfileId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all roles from a user.
    /// </summary>
    Task RemoveAllRolesAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific role.
    /// </summary>
    Task<bool> UserHasRoleAsync(Guid userProfileId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a role by name.
    /// </summary>
    Task<bool> UserHasRoleByNameAsync(Guid userProfileId, string roleName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work pattern for USER MANAGEMENT SERVICE data access.
/// </summary>
public interface IUserManagementUnitOfWork : IDisposable, IAsyncDisposable
{
    IUserProfileRepository UserProfiles { get; }
    IRoleRepository Roles { get; }
    IUserRoleRepository UserRoles { get; }

    /// <summary>
    /// Saves all changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a transaction.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
