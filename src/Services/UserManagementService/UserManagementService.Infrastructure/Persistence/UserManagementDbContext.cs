namespace UserManagementService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Shared.Constants;

/// <summary>
/// Entity Framework Core DbContext for UserManagementService.
/// Manages UserProfiles, Roles, and UserRoles.
/// </summary>
public class UserManagementDbContext : DbContext
{
    private const string DefaultSchema = "user_management";

    public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema(DefaultSchema);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserManagementDbContext).Assembly);

        // Seed default roles
        SeedDefaultRoles(modelBuilder);
    }

    private static void SeedDefaultRoles(ModelBuilder modelBuilder)
    {
        var seedTimestamp = DateTime.UnixEpoch;

        modelBuilder.Entity<Role>().HasData(
            new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = RoleNames.Admin,
                Description = "Administrator with full system access",
                IsSystem = true,
                CreatedAt = seedTimestamp,
                UpdatedAt = seedTimestamp
            },
            new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = RoleNames.User,
                Description = "Regular user with standard permissions",
                IsSystem = true,
                CreatedAt = seedTimestamp,
                UpdatedAt = seedTimestamp
            },
            new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Name = RoleNames.Moderator,
                Description = "Moderator with content management permissions",
                IsSystem = true,
                CreatedAt = seedTimestamp,
                UpdatedAt = seedTimestamp
            });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
        // Add audit tracking or domain event publishing here

        return base.SaveChangesAsync(cancellationToken);
    }
}
