namespace MediaService.Infrastructure.Persistence.Configurations;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.ToTable("albums");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => new { x.OwnerId, x.Name }).IsUnique();
        builder.HasIndex(x => x.OwnerId);

        builder.HasMany(x => x.MediaAssets)
            .WithOne(x => x.Album)
            .HasForeignKey(x => x.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Shares)
            .WithOne(x => x.Album)
            .HasForeignKey(x => x.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AlbumId).IsRequired();
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(512);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.FileSize).IsRequired();
        builder.Property(x => x.Checksum).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Kind).IsRequired();
        builder.Property(x => x.SafetyStatus).IsRequired();
        builder.Property(x => x.UploadedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => x.AlbumId);
        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => new { x.AlbumId, x.DisplayName }).IsUnique();

        builder.HasMany(x => x.TagAssignments)
            .WithOne(x => x.MediaAsset)
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Shares)
            .WithOne(x => x.MediaAsset)
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class AlbumShareConfiguration : IEntityTypeConfiguration<AlbumShare>
{
    public void Configure(EntityTypeBuilder<AlbumShare> builder)
    {
        builder.ToTable("album_shares");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AlbumId).IsRequired();
        builder.Property(x => x.SharedByUserId).IsRequired();
        builder.Property(x => x.SharedWithUserId).IsRequired();
        builder.Property(x => x.Permission).IsRequired();
        builder.Property(x => x.SharedAt).IsRequired();
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.RevocationReason).HasMaxLength(250);

        builder.HasIndex(x => new { x.AlbumId, x.SharedWithUserId }).IsUnique();
        builder.HasIndex(x => x.SharedWithUserId);
    }
}

internal sealed class MediaShareConfiguration : IEntityTypeConfiguration<MediaShare>
{
    public void Configure(EntityTypeBuilder<MediaShare> builder)
    {
        builder.ToTable("media_shares");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.MediaAssetId).IsRequired();
        builder.Property(x => x.SharedByUserId).IsRequired();
        builder.Property(x => x.SharedWithUserId).IsRequired();
        builder.Property(x => x.Permission).IsRequired();
        builder.Property(x => x.SharedAt).IsRequired();
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.RevocationReason).HasMaxLength(250);

        builder.HasIndex(x => new { x.MediaAssetId, x.SharedWithUserId }).IsUnique();
        builder.HasIndex(x => x.SharedWithUserId);
    }
}

internal sealed class MediaTagConfiguration : IEntityTypeConfiguration<MediaTag>
{
    public void Configure(EntityTypeBuilder<MediaTag> builder)
    {
        builder.ToTable("media_tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(64);
        builder.Property(x => x.IsSystem).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => new { x.OwnerId, x.Name }).IsUnique();
        builder.HasIndex(x => x.OwnerId);

        builder.HasMany(x => x.TagAssignments)
            .WithOne(x => x.MediaTag)
            .HasForeignKey(x => x.MediaTagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class MediaTagAssignmentConfiguration : IEntityTypeConfiguration<MediaTagAssignment>
{
    public void Configure(EntityTypeBuilder<MediaTagAssignment> builder)
    {
        builder.ToTable("media_tag_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.MediaAssetId).IsRequired();
        builder.Property(x => x.MediaTagId).IsRequired();
        builder.Property(x => x.AssignedByUserId).IsRequired();
        builder.Property(x => x.AssignedAt).IsRequired();

        builder.HasIndex(x => new { x.MediaAssetId, x.MediaTagId }).IsUnique();
        builder.HasIndex(x => x.MediaAssetId);
        builder.HasIndex(x => x.MediaTagId);
    }
}