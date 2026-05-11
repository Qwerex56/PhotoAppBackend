namespace MediaService.Application.Services;

using MediaService.Domain.Enums;
using MediaService.Domain.Repositories;

public interface IMediaAccessService
{
    Task<bool> CanViewAlbumAsync(Guid albumId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanEditAlbumAsync(Guid albumId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanViewMediaAsync(Guid mediaId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanEditMediaAsync(Guid mediaId, Guid userId, CancellationToken cancellationToken = default);
}

public sealed class MediaAccessService : IMediaAccessService
{
    private readonly IMediaUnitOfWork _unitOfWork;

    public MediaAccessService(IMediaUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> CanViewAlbumAsync(Guid albumId, Guid userId, CancellationToken cancellationToken = default)
    {
        var album = await _unitOfWork.Albums.GetByIdAsync(albumId, cancellationToken);
        if (album is null || album.IsDeleted)
            return false;

        if (album.OwnerId == userId)
            return true;

        return await _unitOfWork.AlbumShares.HasPermissionAsync(albumId, userId, SharePermission.View, cancellationToken);
    }

    public async Task<bool> CanEditAlbumAsync(Guid albumId, Guid userId, CancellationToken cancellationToken = default)
    {
        var album = await _unitOfWork.Albums.GetByIdAsync(albumId, cancellationToken);
        if (album is null || album.IsDeleted)
            return false;

        if (album.OwnerId == userId)
            return true;

        return await _unitOfWork.AlbumShares.HasPermissionAsync(albumId, userId, SharePermission.Edit, cancellationToken);
    }

    public async Task<bool> CanViewMediaAsync(Guid mediaId, Guid userId, CancellationToken cancellationToken = default)
    {
        var mediaAsset = await _unitOfWork.MediaAssets.GetByIdAsync(mediaId, cancellationToken);
        if (mediaAsset is null || mediaAsset.IsDeleted)
            return false;

        if (mediaAsset.OwnerId == userId)
            return true;

        if (await _unitOfWork.MediaShares.HasPermissionAsync(mediaId, userId, SharePermission.View, cancellationToken))
            return true;

        return await _unitOfWork.AlbumShares.HasPermissionAsync(mediaAsset.AlbumId, userId, SharePermission.View, cancellationToken);
    }

    public async Task<bool> CanEditMediaAsync(Guid mediaId, Guid userId, CancellationToken cancellationToken = default)
    {
        var mediaAsset = await _unitOfWork.MediaAssets.GetByIdAsync(mediaId, cancellationToken);
        if (mediaAsset is null || mediaAsset.IsDeleted)
            return false;

        if (mediaAsset.OwnerId == userId)
            return true;

        if (await _unitOfWork.MediaShares.HasPermissionAsync(mediaId, userId, SharePermission.Edit, cancellationToken))
            return true;

        return await _unitOfWork.AlbumShares.HasPermissionAsync(mediaAsset.AlbumId, userId, SharePermission.Edit, cancellationToken);
    }
}