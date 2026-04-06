namespace AuthService.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

/// <summary>
/// Entity Framework Core configuration for User entity.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.PasswordHash)
            .IsRequired();

        builder.Property(x => x.RegisteredAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.PasswordChangedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.EmailVerifiedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LockedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastLoginAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.EmailVerified)
            .IsRequired();

        builder.Property(x => x.IsLocked)
            .IsRequired();

        builder.Property(x => x.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        // One-to-Many relationship with RefreshToken
        builder.HasMany(x => x.RefreshTokens)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for email lookup
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("idx_user_email");

        // Index for finding used tokens
        builder.HasIndex(x => x.IsLocked)
            .HasDatabaseName("idx_user_locked_status");
    }
}

/// <summary>
/// Entity Framework Core configuration for RefreshToken entity.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(512); // Hashes are longer

        builder.Property(x => x.IssuedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiresAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevocationReason)
            .HasMaxLength(100);

        builder.Property(x => x.IssuedFromIpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.IssuedFromUserAgent)
            .HasMaxLength(500);

        builder.Property(x => x.RotatedToTokenId);

        // Foreign key to User (configured in User configuration)
        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for finding tokens
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_refresh_token_user_id");

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("idx_refresh_token_hash");

        builder.HasIndex(x => new { x.UserId, x.RevokedAt })
            .HasDatabaseName("idx_refresh_token_active");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_refresh_token_expires");
    }
}

/// <summary>
/// Entity Framework Core configuration for EmailVerificationToken entity.
/// </summary>
public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiresAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.VerifiedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RequestedFromIpAddress)
            .HasMaxLength(45);

        // Foreign key to User
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("idx_email_verification_token_hash");

        builder.HasIndex(x => new { x.UserId, x.Email, x.VerifiedAt })
            .HasDatabaseName("idx_email_verification_pending");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_email_verification_expires");
    }
}

/// <summary>
/// Entity Framework Core configuration for PasswordResetToken entity.
/// </summary>
public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiresAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UsedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RequestedFromIpAddress)
            .HasMaxLength(45);

        builder.Property(x => x.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Foreign key to User
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("idx_password_reset_token_hash");

        builder.HasIndex(x => new { x.UserId, x.UsedAt })
            .HasDatabaseName("idx_password_reset_active");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_password_reset_expires");
    }
}
