namespace MediaService.Domain.Repositories;

using Entities;
using Enums;

public interface IAlbumRepository
{
    Task<Album> CreateAsync(Album album, CancellationToken cancellationToken = default);
    Task<Album?> GetByIdAsync(Guid albumId, CancellationToken cancellationToken = default);
    Task<List<Album>> GetOwnedByUserAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<List<Album>> GetSharedWithUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Album> UpdateAsync(Album album, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid albumId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByOwnerAndNameAsync(Guid ownerId, string name, CancellationToken cancellationToken = default);
}

public interface IMediaAssetRepository
{
    Task<MediaAsset> CreateAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);
    Task<MediaAsset?> GetByIdAsync(Guid mediaId, CancellationToken cancellationToken = default);
    Task<List<MediaAsset>> GetByAlbumAsync(Guid albumId, CancellationToken cancellationToken = default);
    Task<List<MediaAsset>> GetOwnedByUserAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<MediaAsset> UpdateAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid mediaId, CancellationToken cancellationToken = default);
    Task DeleteByAlbumAsync(Guid albumId, CancellationToken cancellationToken = default);
    Task DeleteByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<bool> ExistsInAlbumWithDisplayNameAsync(Guid albumId, string displayName, CancellationToken cancellationToken = default);
}

public interface IAlbumShareRepository
{
    Task<AlbumShare> CreateAsync(AlbumShare share, CancellationToken cancellationToken = default);
    Task<AlbumShare?> GetActiveShareAsync(Guid albumId, Guid sharedWithUserId, CancellationToken cancellationToken = default);
    Task<List<AlbumShare>> GetActiveSharesAsync(Guid albumId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(Guid albumId, Guid sharedWithUserId, SharePermission minimumPermission, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid albumId, Guid sharedWithUserId, CancellationToken cancellationToken = default);
    Task RevokeAllForAlbumAsync(Guid albumId, CancellationToken cancellationToken = default);
}

public interface IMediaShareRepository
{
    Task<MediaShare> CreateAsync(MediaShare share, CancellationToken cancellationToken = default);
    Task<MediaShare?> GetActiveShareAsync(Guid mediaId, Guid sharedWithUserId, CancellationToken cancellationToken = default);
    Task<List<MediaShare>> GetActiveSharesAsync(Guid mediaId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(Guid mediaId, Guid sharedWithUserId, SharePermission minimumPermission, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid mediaId, Guid sharedWithUserId, CancellationToken cancellationToken = default);
    Task RevokeAllForMediaAsync(Guid mediaId, CancellationToken cancellationToken = default);
}

public interface IMediaTagRepository
{
    Task<MediaTag> CreateAsync(MediaTag tag, CancellationToken cancellationToken = default);
    Task<MediaTag?> GetByOwnerAndNameAsync(Guid ownerId, string name, CancellationToken cancellationToken = default);
    Task<MediaTag> UpdateAsync(MediaTag tag, CancellationToken cancellationToken = default);
    Task<List<MediaTag>> GetByMediaIdAsync(Guid mediaId, CancellationToken cancellationToken = default);
    Task<MediaTagAssignment> AssignToMediaAsync(MediaTagAssignment assignment, CancellationToken cancellationToken = default);
    Task RemoveFromMediaAsync(Guid mediaId, string tagName, CancellationToken cancellationToken = default);
    Task<bool> HasTagOnMediaAsync(Guid mediaId, string tagName, CancellationToken cancellationToken = default);
}

public interface IMediaUnitOfWork
{
    IAlbumRepository Albums { get; }
    IMediaAssetRepository MediaAssets { get; }
    IMediaTagRepository MediaTags { get; }
    IAlbumShareRepository AlbumShares { get; }
    IMediaShareRepository MediaShares { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}