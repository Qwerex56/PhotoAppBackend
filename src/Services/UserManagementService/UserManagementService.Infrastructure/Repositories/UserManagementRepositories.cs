namespace UserManagementService.Infrastructure.Repositories;

using Domain.Entities;
using Domain.Repositories;
using Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementation of IUserProfileRepository.
/// </summary>
public class UserProfileRepository : IUserProfileRepository
{
    private readonly UserManagementDbContext _context;

    public UserProfileRepository(UserManagementDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile> CreateAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await _context.UserProfiles.AddAsync(profile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles
            .Include(p => p.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(p => p.Id == userId && !p.IsDeleted, cancellationToken);
    }

    public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles
            .Include(p => p.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(p => p.Email == email && !p.IsDeleted, cancellationToken);
    }

    public async Task<List<UserProfile>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserProfile> UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        _context.UserProfiles.Update(profile);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await GetByIdAsync(userId, cancellationToken);
        if (profile == null)
            return;

        profile.Delete();
        await UpdateAsync(profile, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles
            .AnyAsync(p => p.Email == email && !p.IsDeleted, cancellationToken);
    }
}

/// <summary>
/// Implementation of IRoleRepository.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly UserManagementDbContext _context;

    public RoleRepository(UserManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Role> UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await GetByIdAsync(roleId, cancellationToken);
        if (role == null || role.IsSystem)
            return;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AnyAsync(r => r.Name == name, cancellationToken);
    }
}

/// <summary>
/// Implementation of IUserRoleRepository.
/// </summary>
public class UserRoleRepository : IUserRoleRepository
{
    private readonly UserManagementDbContext _context;

    public UserRoleRepository(UserManagementDbContext context)
    {
        _context = context;
    }

    public async Task<UserRole> AssignRoleAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        await _context.UserRoles.AddAsync(userRole, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return userRole;
    }

    public async Task<List<UserRole>> GetUserRolesAsync(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserProfileId == userProfileId)
            .OrderBy(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserRole>> GetActiveUserRolesAsync(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserProfileId == userProfileId && (ur.ExpiresAt == null || ur.ExpiresAt > now))
            .OrderBy(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveRoleAsync(Guid userProfileId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserProfileId == userProfileId && ur.RoleId == roleId, cancellationToken);

        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveAllRolesAsync(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);

        _context.UserRoles.RemoveRange(userRoles);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UserHasRoleAsync(Guid userProfileId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.UserRoles
            .AnyAsync(ur => ur.UserProfileId == userProfileId &&
                           ur.RoleId == roleId &&
                           (ur.ExpiresAt == null || ur.ExpiresAt > now),
                cancellationToken);
    }

    public async Task<bool> UserHasRoleByNameAsync(Guid userProfileId, string roleName, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.UserRoles
            .AsNoTracking()
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserProfileId == userProfileId &&
                           ur.Role.Name == roleName &&
                           (ur.ExpiresAt == null || ur.ExpiresAt > now),
                cancellationToken);
    }
}

/// <summary>
/// Unit of Work implementation for UserManagementService.
/// </summary>
public class UserManagementUnitOfWork : IUserManagementUnitOfWork
{
    private readonly UserManagementDbContext _context;
    private IUserProfileRepository? _userProfileRepository;
    private IRoleRepository? _roleRepository;
    private IUserRoleRepository? _userRoleRepository;

    public UserManagementUnitOfWork(UserManagementDbContext context)
    {
        _context = context;
    }

    public IUserProfileRepository UserProfiles => _userProfileRepository ??= new UserProfileRepository(_context);
    public IRoleRepository Roles => _roleRepository ??= new RoleRepository(_context);
    public IUserRoleRepository UserRoles => _userRoleRepository ??= new UserRoleRepository(_context);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _context.Database.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.RollbackTransactionAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
    }
}
