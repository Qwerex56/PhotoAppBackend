namespace UserManagementService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;

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
        // SeedDefaultRoles(modelBuilder);
    }

    // private static void SeedDefaultRoles(ModelBuilder modelBuilder)
    // {
    //     var adminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    //     var userRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    //     var moderatorRoleId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    //
    //     modelBuilder.Entity<Role>().HasData(
    //         new Role("Admin", "Administrator with full system access", true) 
    //         { 
    //             Id = adminRoleId, 
    //             CreatedAt = DateTime.UtcNow,
    //             UpdatedAt = DateTime.UtcNow
    //         },
    //         new Role("User", "Regular user with standard permissions", true) 
    //         { 
    //             Id = userRoleId,
    //             CreatedAt = DateTime.UtcNow,
    //             UpdatedAt = DateTime.UtcNow
    //         },
    //         new Role("Moderator", "Moderator with content management permissions", true) 
    //         { 
    //             Id = moderatorRoleId,
    //             CreatedAt = DateTime.UtcNow,
    //             UpdatedAt = DateTime.UtcNow
    //         }
    //     );
    // }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
        // Add audit tracking or domain event publishing here

        return base.SaveChangesAsync(cancellationToken);
    }
}
