namespace AuthService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;

/// <summary>
/// Entity Framework Core DbContext for AuthService.
/// Manages Users, RefreshTokens, EmailVerificationTokens, and PasswordResetTokens.
/// </summary>
public class AuthServiceDbContext : DbContext
{
    public const string DefaultSchema = "auth";

    public AuthServiceDbContext(DbContextOptions<AuthServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema for all entities
        modelBuilder.HasDefaultSchema(DefaultSchema);

        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthServiceDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
        // Add audit tracking, domain event publishing, or other pre-save operations here

        return base.SaveChangesAsync(cancellationToken);
    }
}
