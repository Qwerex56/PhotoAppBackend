namespace MediaService.Infrastructure.Persistence;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class MediaServiceDbContext : DbContext
{
    public const string DefaultSchema = "media";

    public MediaServiceDbContext(DbContextOptions<MediaServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Album> Albums => Set<Album>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<AlbumShare> AlbumShares => Set<AlbumShare>();
    public DbSet<MediaShare> MediaShares => Set<MediaShare>();
    public DbSet<MediaTag> MediaTags => Set<MediaTag>();
    public DbSet<MediaTagAssignment> MediaTagAssignments => Set<MediaTagAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaServiceDbContext).Assembly);
    }
}