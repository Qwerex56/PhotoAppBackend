namespace MediaService.Infrastructure.Repositories;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence;

public sealed class AlbumRepository : IAlbumRepository
{
    private readonly MediaServiceDbContext _context;

    public AlbumRepository(MediaServiceDbContext context)
    {
        _context = context;
    }

    public async Task<Album> CreateAsync(Album album, CancellationToken cancellationToken = default)
    {
        await _context.Albums.AddAsync(album, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return album;
    }

    public async Task<Album?> GetByIdAsync(Guid albumId, CancellationToken cancellationToken = default)
    {
        return await _context.Albums
            .FirstOrDefaultAsync(x => x.Id == albumId, cancellationToken);
    }

    public async Task<List<Album>> GetOwnedByUserAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Albums
            .Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Album>> GetSharedWithUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Albums
            .Where(x => x.Shares.Any(share =>
                share.SharedWithUserId == userId &&
                share.RevokedAt == null &&
                (share.ExpiresAt == null || share.ExpiresAt > DateTime.UtcNow)))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Album> UpdateAsync(Album album, CancellationToken cancellationToken = default)
    {
        _context.Albums.Update(album);
        await _context.SaveChangesAsync(cancellationToken);
        return album;
    }

    public async Task DeleteAsync(Guid albumId, CancellationToken cancellationToken = default)
    {
        var album = await GetByIdAsync(albumId, cancellationToken);
        if (album is null)
            return;

        _context.Albums.Remove(album);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByOwnerAndNameAsync(Guid ownerId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Albums
            .AnyAsync(x => x.OwnerId == ownerId && x.Name == name, cancellationToken);
    }
}

public sealed class MediaAssetRepository : IMediaAssetRepository
{
    private readonly MediaServiceDbContext _context;

    public MediaAssetRepository(MediaServiceDbContext context)
    {
        _context = context;
    }

    public async Task<MediaAsset> CreateAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        await _context.MediaAssets.AddAsync(mediaAsset, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return mediaAsset;
    }

    public async Task<MediaAsset?> GetByIdAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .Include(x => x.TagAssignments)
                .ThenInclude(x => x.MediaTag)
            .FirstOrDefaultAsync(x => x.Id == mediaId, cancellationToken);
    }

    public async Task<List<MediaAsset>> GetByAlbumAsync(Guid albumId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .Where(x => x.AlbumId == albumId)
            .OrderByDescending(x => x.UploadedAt)
            .Include(x => x.TagAssignments)
                .ThenInclude(x => x.MediaTag)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MediaAsset>> GetOwnedByUserAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MediaAsset> UpdateAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        _context.MediaAssets.Update(mediaAsset);
        await _context.SaveChangesAsync(cancellationToken);
        return mediaAsset;
    }

    public async Task DeleteAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var mediaAsset = await GetByIdAsync(mediaId, cancellationToken);
        if (mediaAsset is null)
            return;

        _context.MediaAssets.Remove(mediaAsset);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByAlbumAsync(Guid albumId, CancellationToken cancellationToken = default)
    {
        var mediaAssets = await _context.MediaAssets
            .Where(x => x.AlbumId == albumId)
            .ToListAsync(cancellationToken);

        _context.MediaAssets.RemoveRange(mediaAssets);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var mediaAssets = await _context.MediaAssets
            .Where(x => x.OwnerId == ownerId)
            .ToListAsync(cancellationToken);

        _context.MediaAssets.RemoveRange(mediaAssets);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsInAlbumWithDisplayNameAsync(Guid albumId, string displayName, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .AnyAsync(x => x.AlbumId == albumId && x.DisplayName == displayName, cancellationToken);
    }
}

public sealed class AlbumShareRepository : IAlbumShareRepository
{
    private readonly MediaServiceDbContext _context;

    public AlbumShareRepository(MediaServiceDbContext context)
    {
        _context = context;
    }

    public async Task<AlbumShare> CreateAsync(AlbumShare share, CancellationToken cancellationToken = default)
    {
        await _context.AlbumShares.AddAsync(share, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return share;
    }

    public async Task<AlbumShare?> GetActiveShareAsync(Guid albumId, Guid sharedWithUserId, CancellationToken cancellationToken = default)
    {
        return await _context.AlbumShares
            .FirstOrDefaultAsync(x =>
                x.AlbumId == albumId &&
                x.SharedWithUserId == sharedWithUserId &&
                x.RevokedAt == null &&
                (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow), cancellationToken);
    }

    public async Task<List<AlbumShare>> GetActiveSharesAsync(Guid albumId, CancellationToken cancellationToken = default)
    {
        return await _context.AlbumShares
            .Where(x =>
                x.AlbumId == albumId &&
                x.RevokedAt == null &&
                (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow))
            .OrderBy(x => x.SharedWithUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(Guid albumId, Guid sharedWithUserId, SharePermission minimumPermission, CancellationToken cancellationToken = default)
    {
        return await _context.AlbumShares
            .AnyAsync(x =>
                x.AlbumId == albumId &&
                x.SharedWithUserId == sharedWithUserId &&
                x.RevokedAt == null &&
                (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow) &&
                x.Permission >= minimumPermission,
                cancellationToken);
    }

    public async Task RevokeAsync(Guid albumId, Guid sharedWithUserId, CancellationToken cancellationToken = default)
    {
        var share = await GetActiveShareAsync(albumId, sharedWithUserId, cancellationToken);
        if (share is null)
            return;

        share.Revoke("revoked");
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForAlbumAsync(Guid albumId, CancellationToken cancellationToken = default)
    {
        var shares = await _context.AlbumShares
            .Where(x => x.AlbumId == albumId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var share in shares)
        {
            share.Revoke("album deleted");
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed class MediaShareRepository : IMediaShareRepository
{
    private readonly MediaServiceDbContext _context;

    public MediaShareRepository(MediaServiceDbContext context)
    {
        _context = context;
    }

    public async Task<MediaShare> CreateAsync(MediaShare share, CancellationToken cancellationToken = default)
    {
        await _context.MediaShares.AddAsync(share, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return share;
    }

    public async Task<MediaShare?> GetActiveShareAsync(Guid mediaId, Guid sharedWithUserId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaShares
            .FirstOrDefaultAsync(x =>
                x.MediaAssetId == mediaId &&
                x.SharedWithUserId == sharedWithUserId &&
                x.RevokedAt == null &&
                (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow), cancellationToken);
    }

    public async Task<List<MediaShare>> GetActiveSharesAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaShares
            .Where(x =>
                x.MediaAssetId == mediaId &&
                x.RevokedAt == null &&
                (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow))
            .OrderBy(x => x.SharedWithUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(Guid mediaId, Guid sharedWithUserId, SharePermission minimumPermission, CancellationToken cancellationToken = default)
    {
        return await _context.MediaShares
            .AnyAsync(x =>
                x.MediaAssetId == mediaId &&
                x.SharedWithUserId == sharedWithUserId &&
                x.RevokedAt == null &&
                (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow) &&
                x.Permission >= minimumPermission,
                cancellationToken);
    }

    public async Task RevokeAsync(Guid mediaId, Guid sharedWithUserId, CancellationToken cancellationToken = default)
    {
        var share = await GetActiveShareAsync(mediaId, sharedWithUserId, cancellationToken);
        if (share is null)
            return;

        share.Revoke("revoked");
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForMediaAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var shares = await _context.MediaShares
            .Where(x => x.MediaAssetId == mediaId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var share in shares)
        {
            share.Revoke("media deleted");
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed class MediaTagRepository : IMediaTagRepository
{
    private readonly MediaServiceDbContext _context;

    public MediaTagRepository(MediaServiceDbContext context)
    {
        _context = context;
    }

    public async Task<MediaTag> CreateAsync(MediaTag tag, CancellationToken cancellationToken = default)
    {
        await _context.MediaTags.AddAsync(tag, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task<MediaTag?> GetByOwnerAndNameAsync(Guid ownerId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.MediaTags
            .FirstOrDefaultAsync(x => x.OwnerId == ownerId && x.Name == name, cancellationToken);
    }

    public async Task<MediaTag> UpdateAsync(MediaTag tag, CancellationToken cancellationToken = default)
    {
        _context.MediaTags.Update(tag);
        await _context.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task<List<MediaTag>> GetByMediaIdAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaTagAssignments
            .Where(x => x.MediaAssetId == mediaId)
            .Select(x => x.MediaTag)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<MediaTagAssignment> AssignToMediaAsync(MediaTagAssignment assignment, CancellationToken cancellationToken = default)
    {
        await _context.MediaTagAssignments.AddAsync(assignment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return assignment;
    }

    public async Task RemoveFromMediaAsync(Guid mediaId, string tagName, CancellationToken cancellationToken = default)
    {
        var assignments = await _context.MediaTagAssignments
            .Include(x => x.MediaTag)
            .Where(x => x.MediaAssetId == mediaId && x.MediaTag.Name == tagName)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
            return;

        _context.MediaTagAssignments.RemoveRange(assignments);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasTagOnMediaAsync(Guid mediaId, string tagName, CancellationToken cancellationToken = default)
    {
        return await _context.MediaTagAssignments
            .AnyAsync(x => x.MediaAssetId == mediaId && x.MediaTag.Name == tagName, cancellationToken);
    }
}

public sealed class MediaUnitOfWork : IMediaUnitOfWork
{
    private readonly MediaServiceDbContext _context;
    private IAlbumRepository? _albumRepository;
    private IMediaAssetRepository? _mediaAssetRepository;
    private IMediaTagRepository? _mediaTagRepository;
    private IAlbumShareRepository? _albumShareRepository;
    private IMediaShareRepository? _mediaShareRepository;

    public MediaUnitOfWork(MediaServiceDbContext context)
    {
        _context = context;
    }

    public IAlbumRepository Albums => _albumRepository ??= new AlbumRepository(_context);
    public IMediaAssetRepository MediaAssets => _mediaAssetRepository ??= new MediaAssetRepository(_context);
    public IMediaTagRepository MediaTags => _mediaTagRepository ??= new MediaTagRepository(_context);
    public IAlbumShareRepository AlbumShares => _albumShareRepository ??= new AlbumShareRepository(_context);
    public IMediaShareRepository MediaShares => _mediaShareRepository ??= new MediaShareRepository(_context);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _context.Database.BeginTransactionAsync(cancellationToken);

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

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => _context.Database.RollbackTransactionAsync(cancellationToken);

    public void Dispose()
    {
        _context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}