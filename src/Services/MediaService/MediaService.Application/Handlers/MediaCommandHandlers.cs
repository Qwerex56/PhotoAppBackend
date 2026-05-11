namespace MediaService.Application.Handlers.Media;

using MassTransit;
using MediatR;
using MediaService.Application.Commands.Media;
using MediaService.Application.Services;
using MediaService.Domain.Entities;
using MediaService.Domain.Enums;
using MediaService.Domain.Repositories;
using Shared.Constants;
using Shared.Contracts.Events.Media;
using Shared.Contracts.Events.Users;
using Shared.Results;

public sealed class CreateAlbumCommandHandler : IRequestHandler<CreateAlbumCommand, Result<CreateAlbumCommandResponse>>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateAlbumCommandHandler(IMediaUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<CreateAlbumCommandResponse>> Handle(CreateAlbumCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerId != request.ActorUserId)
            return Result<CreateAlbumCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Only the owner can create albums"));

        var normalizedName = request.Name.Trim();

        var exists = await _unitOfWork.Albums.ExistsByOwnerAndNameAsync(request.OwnerId, normalizedName, cancellationToken);
        if (exists)
            return Result<CreateAlbumCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAlbumAlreadyExists, $"Album '{normalizedName}' already exists"));

        var album = new Album(Guid.NewGuid(), request.OwnerId, normalizedName, request.Description);

        await _unitOfWork.Albums.CreateAsync(album, cancellationToken);

        await _publishEndpoint.Publish(new AlbumCreatedEvent
        {
            AlbumId = album.Id,
            OwnerId = album.OwnerId,
            ActorUserId = request.ActorUserId,
            Name = album.Name,
            Description = album.Description,
            CreatedAt = album.CreatedAt,
            Source = "MediaService.Application"
        }, cancellationToken);

        return Result<CreateAlbumCommandResponse>.Success(new CreateAlbumCommandResponse
        {
            AlbumId = album.Id,
            OwnerId = album.OwnerId,
            Name = album.Name,
            Description = album.Description,
            CreatedAt = album.CreatedAt
        });
    }
}

public sealed class RenameAlbumCommandHandler : IRequestHandler<RenameAlbumCommand, Result<RenameAlbumCommandResponse>>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaAccessService _mediaAccessService;
    private readonly IPublishEndpoint _publishEndpoint;

    public RenameAlbumCommandHandler(IMediaUnitOfWork unitOfWork, IMediaAccessService mediaAccessService, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _mediaAccessService = mediaAccessService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<RenameAlbumCommandResponse>> Handle(RenameAlbumCommand request, CancellationToken cancellationToken)
    {
        var album = await _unitOfWork.Albums.GetByIdAsync(request.AlbumId, cancellationToken);
        if (album is null || album.IsDeleted || album.OwnerId != request.OwnerId)
            return Result<RenameAlbumCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAlbumNotFound, "Album not found"));

        if (album.OwnerId != request.ActorUserId && !await _mediaAccessService.CanEditAlbumAsync(request.AlbumId, request.ActorUserId, cancellationToken))
            return Result<RenameAlbumCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Edit permission required"));

        var normalizedName = request.Name.Trim();

        album.Rename(normalizedName, request.Description);
        await _unitOfWork.Albums.UpdateAsync(album, cancellationToken);

        await _publishEndpoint.Publish(new AlbumRenamedEvent
        {
            AlbumId = album.Id,
            OwnerId = album.OwnerId,
            ActorUserId = request.ActorUserId,
            Name = album.Name,
            Description = album.Description,
            UpdatedAt = album.UpdatedAt,
            Source = "MediaService.Application"
        }, cancellationToken);

        return Result<RenameAlbumCommandResponse>.Success(new RenameAlbumCommandResponse
        {
            AlbumId = album.Id,
            Name = album.Name,
            Description = album.Description,
            UpdatedAt = album.UpdatedAt
        });
    }
}

public sealed class DeleteAlbumCommandHandler : IRequestHandler<DeleteAlbumCommand, Result>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public DeleteAlbumCommandHandler(IMediaUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result> Handle(DeleteAlbumCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerId != request.ActorUserId)
            return Result.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Only the owner can delete albums"));

        var album = await _unitOfWork.Albums.GetByIdAsync(request.AlbumId, cancellationToken);
        if (album is null || album.IsDeleted || album.OwnerId != request.OwnerId)
            return Result.Failure(Error.Create(ErrorCodes.MediaAlbumNotFound, "Album not found"));

        album.Delete();
        await _unitOfWork.Albums.UpdateAsync(album, cancellationToken);

        await _unitOfWork.MediaAssets.DeleteByAlbumAsync(album.Id, cancellationToken);
        await _unitOfWork.AlbumShares.RevokeAllForAlbumAsync(album.Id, cancellationToken);

        await _publishEndpoint.Publish(new AlbumDeletedEvent
        {
            AlbumId = album.Id,
            OwnerId = album.OwnerId,
            ActorUserId = request.ActorUserId,
            DeletedAt = album.DeletedAt ?? DateTime.UtcNow,
            Reason = request.Reason,
            Source = "MediaService.Application"
        }, cancellationToken);

        return Result.Success();
    }
}

public sealed class ShareAlbumCommandHandler : IRequestHandler<ShareAlbumCommand, Result<ShareAlbumCommandResponse>>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public ShareAlbumCommandHandler(IMediaUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<ShareAlbumCommandResponse>> Handle(ShareAlbumCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerId != request.ActorUserId)
            return Result<ShareAlbumCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Only the owner can share albums"));

        var album = await _unitOfWork.Albums.GetByIdAsync(request.AlbumId, cancellationToken);
        if (album is null || album.IsDeleted || album.OwnerId != request.OwnerId)
            return Result<ShareAlbumCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAlbumNotFound, "Album not found"));

        var permission = request.Permission.Equals("edit", StringComparison.OrdinalIgnoreCase)
            ? SharePermission.Edit
            : SharePermission.View;

        var share = await _unitOfWork.AlbumShares.GetActiveShareAsync(request.AlbumId, request.SharedWithUserId, cancellationToken);
        if (share is null)
        {
            share = new AlbumShare(Guid.NewGuid(), request.AlbumId, request.OwnerId, request.SharedWithUserId, permission, request.ExpiresAt);
            await _unitOfWork.AlbumShares.CreateAsync(share, cancellationToken);
        }
        else
        {
            share.UpdatePermission(permission, request.ExpiresAt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _publishEndpoint.Publish(new AlbumSharedEvent
        {
            AlbumId = album.Id,
            OwnerId = album.OwnerId,
            ActorUserId = request.ActorUserId,
            SharedWithUserId = request.SharedWithUserId,
            Permission = request.Permission,
            SharedAt = share.SharedAt,
            ExpiresAt = share.ExpiresAt,
            Source = "MediaService.Application"
        }, cancellationToken);

        return Result<ShareAlbumCommandResponse>.Success(new ShareAlbumCommandResponse
        {
            AlbumId = album.Id,
            SharedWithUserId = request.SharedWithUserId,
            Permission = request.Permission,
            SharedAt = share.SharedAt,
            ExpiresAt = share.ExpiresAt
        });
    }
}

public sealed class UploadMediaCommandHandler : IRequestHandler<UploadMediaCommand, Result<UploadMediaCommandResponse>>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaAccessService _mediaAccessService;
    private readonly IMediaSecurityService _mediaSecurityService;
    private readonly IPublishEndpoint _publishEndpoint;

    public UploadMediaCommandHandler(IMediaUnitOfWork unitOfWork, IMediaAccessService mediaAccessService, IMediaSecurityService mediaSecurityService, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _mediaAccessService = mediaAccessService;
        _mediaSecurityService = mediaSecurityService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<UploadMediaCommandResponse>> Handle(UploadMediaCommand request, CancellationToken cancellationToken)
    {
        var album = await _unitOfWork.Albums.GetByIdAsync(request.AlbumId, cancellationToken);
        if (album is null || album.IsDeleted || album.OwnerId != request.OwnerId)
            return Result<UploadMediaCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAlbumNotFound, "Album not found"));

        if (album.OwnerId != request.ActorUserId && !await _mediaAccessService.CanEditAlbumAsync(request.AlbumId, request.ActorUserId, cancellationToken))
            return Result<UploadMediaCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Edit permission required"));

        var validation = _mediaSecurityService.ValidateUpload(request.OriginalFileName, request.ContentType, request.FileSize, request.Checksum);
        if (validation.IsFailure)
            return Result<UploadMediaCommandResponse>.Failure(validation.Error!);

        var normalizedDisplayName = _mediaSecurityService.NormalizeDisplayName(request.DisplayName);

        if (!await _unitOfWork.MediaAssets.ExistsInAlbumWithDisplayNameAsync(request.AlbumId, normalizedDisplayName, cancellationToken))
        {
            var mediaId = Guid.NewGuid();
            var storageKey = _mediaSecurityService.CreateStorageKey(request.OwnerId, request.AlbumId, mediaId, request.OriginalFileName);
            var checksum = request.Checksum ?? _mediaSecurityService.ComputeChecksum(request.Content);
            var kind = _mediaSecurityService.GetMediaKind(request.ContentType, request.OriginalFileName) == "video"
                ? MediaKind.Video
                : MediaKind.Photo;

            var mediaAsset = new MediaAsset(
                mediaId,
                request.AlbumId,
                request.OwnerId,
                normalizedDisplayName,
                request.OriginalFileName,
                storageKey,
                request.ContentType,
                request.FileSize,
                checksum,
                kind,
                MediaSafetyStatus.PendingScan);

            await _unitOfWork.MediaAssets.CreateAsync(mediaAsset, cancellationToken);

            await _publishEndpoint.Publish(new MediaUploadedEvent
            {
                MediaId = mediaAsset.Id,
                AlbumId = mediaAsset.AlbumId,
                OwnerId = mediaAsset.OwnerId,
                ActorUserId = request.ActorUserId,
                DisplayName = mediaAsset.DisplayName,
                OriginalFileName = mediaAsset.OriginalFileName,
                ContentType = mediaAsset.ContentType,
                FileSize = mediaAsset.FileSize,
                StorageKey = mediaAsset.StorageKey,
                Checksum = mediaAsset.Checksum,
                Kind = mediaAsset.Kind.ToString().ToLowerInvariant(),
                UploadedAt = mediaAsset.UploadedAt,
                Source = "MediaService.Application"
            }, cancellationToken);

            return Result<UploadMediaCommandResponse>.Success(new UploadMediaCommandResponse
            {
                MediaId = mediaAsset.Id,
                AlbumId = mediaAsset.AlbumId,
                DisplayName = mediaAsset.DisplayName,
                OriginalFileName = mediaAsset.OriginalFileName,
                StorageKey = mediaAsset.StorageKey,
                ContentType = mediaAsset.ContentType,
                FileSize = mediaAsset.FileSize,
                Checksum = mediaAsset.Checksum,
                Kind = mediaAsset.Kind.ToString().ToLowerInvariant(),
                UploadedAt = mediaAsset.UploadedAt
            });
        }

        return Result<UploadMediaCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAssetAlreadyExists, $"Media '{normalizedDisplayName}' already exists in this album"));
    }
}

public sealed class RenameMediaCommandHandler : IRequestHandler<RenameMediaCommand, Result<RenameMediaCommandResponse>>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaAccessService _mediaAccessService;
    private readonly IMediaSecurityService _mediaSecurityService;
    private readonly IPublishEndpoint _publishEndpoint;

    public RenameMediaCommandHandler(IMediaUnitOfWork unitOfWork, IMediaAccessService mediaAccessService, IMediaSecurityService mediaSecurityService, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _mediaAccessService = mediaAccessService;
        _mediaSecurityService = mediaSecurityService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<RenameMediaCommandResponse>> Handle(RenameMediaCommand request, CancellationToken cancellationToken)
    {
        var mediaAsset = await _unitOfWork.MediaAssets.GetByIdAsync(request.MediaId, cancellationToken);
        if (mediaAsset is null || mediaAsset.IsDeleted || mediaAsset.OwnerId != request.OwnerId)
            return Result<RenameMediaCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAssetNotFound, "Media not found"));

        if (mediaAsset.OwnerId != request.ActorUserId && !await _mediaAccessService.CanEditMediaAsync(request.MediaId, request.ActorUserId, cancellationToken))
            return Result<RenameMediaCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Edit permission required"));

        var normalizedDisplayName = _mediaSecurityService.NormalizeDisplayName(request.DisplayName);

        mediaAsset.Rename(normalizedDisplayName);
        await _unitOfWork.MediaAssets.UpdateAsync(mediaAsset, cancellationToken);

        await _publishEndpoint.Publish(new MediaRenamedEvent
        {
            MediaId = mediaAsset.Id,
            AlbumId = mediaAsset.AlbumId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = request.ActorUserId,
            DisplayName = mediaAsset.DisplayName,
            UpdatedAt = mediaAsset.UpdatedAt,
            Source = "MediaService.Application"
        }, cancellationToken);

        return Result<RenameMediaCommandResponse>.Success(new RenameMediaCommandResponse
        {
            MediaId = mediaAsset.Id,
            DisplayName = mediaAsset.DisplayName,
            UpdatedAt = mediaAsset.UpdatedAt
        });
    }
}

public sealed class MoveMediaCommandHandler : IRequestHandler<MoveMediaCommand, Result<MoveMediaCommandResponse>>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public MoveMediaCommandHandler(IMediaUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<MoveMediaCommandResponse>> Handle(MoveMediaCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerId != request.ActorUserId)
            return Result<MoveMediaCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Only the owner can move media"));

        var mediaAsset = await _unitOfWork.MediaAssets.GetByIdAsync(request.MediaId, cancellationToken);
        var targetAlbum = await _unitOfWork.Albums.GetByIdAsync(request.TargetAlbumId, cancellationToken);

        if (mediaAsset is null || mediaAsset.IsDeleted || mediaAsset.OwnerId != request.OwnerId)
            return Result<MoveMediaCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAssetNotFound, "Media not found"));

        if (targetAlbum is null || targetAlbum.IsDeleted || targetAlbum.OwnerId != request.OwnerId)
            return Result<MoveMediaCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAlbumNotFound, "Album not found"));

        var sourceAlbumId = mediaAsset.AlbumId;
        mediaAsset.MoveTo(targetAlbum.Id);
        await _unitOfWork.MediaAssets.UpdateAsync(mediaAsset, cancellationToken);

        await _publishEndpoint.Publish(new MediaMovedEvent
        {
            MediaId = mediaAsset.Id,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = request.ActorUserId,
            FromAlbumId = sourceAlbumId,
            ToAlbumId = targetAlbum.Id,
            MovedAt = mediaAsset.UpdatedAt,
            Source = "MediaService.Application"
        }, cancellationToken);

        return Result<MoveMediaCommandResponse>.Success(new MoveMediaCommandResponse
        {
            MediaId = mediaAsset.Id,
            FromAlbumId = sourceAlbumId,
            TargetAlbumId = targetAlbum.Id,
            UpdatedAt = mediaAsset.UpdatedAt
        });
    }
}

public sealed class DeleteMediaCommandHandler : IRequestHandler<DeleteMediaCommand, Result>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public DeleteMediaCommandHandler(IMediaUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result> Handle(DeleteMediaCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerId != request.ActorUserId)
            return Result.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Only the owner can delete media"));

        var mediaAsset = await _unitOfWork.MediaAssets.GetByIdAsync(request.MediaId, cancellationToken);
        if (mediaAsset is null || mediaAsset.IsDeleted || mediaAsset.OwnerId != request.OwnerId)
            return Result.Failure(Error.Create(ErrorCodes.MediaAssetNotFound, "Media not found"));

        mediaAsset.Delete();
        await _unitOfWork.MediaAssets.UpdateAsync(mediaAsset, cancellationToken);
        await _unitOfWork.MediaShares.RevokeAllForMediaAsync(mediaAsset.Id, cancellationToken);

        await _publishEndpoint.Publish(new MediaDeletedEvent
        {
            MediaId = mediaAsset.Id,
            AlbumId = mediaAsset.AlbumId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = request.ActorUserId,
            DeletedAt = mediaAsset.DeletedAt ?? DateTime.UtcNow,
            Reason = request.Reason,
            Source = "MediaService.Application"
        }, cancellationToken);

        return Result.Success();
    }
}

public sealed class ShareMediaCommandHandler : IRequestHandler<ShareMediaCommand, Result<ShareMediaCommandResponse>>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public ShareMediaCommandHandler(IMediaUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<ShareMediaCommandResponse>> Handle(ShareMediaCommand request, CancellationToken cancellationToken)
    {
        if (request.OwnerId != request.ActorUserId)
            return Result<ShareMediaCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Only the owner can share media"));

        var mediaAsset = await _unitOfWork.MediaAssets.GetByIdAsync(request.MediaId, cancellationToken);
        if (mediaAsset is null || mediaAsset.IsDeleted || mediaAsset.OwnerId != request.OwnerId)
            return Result<ShareMediaCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAssetNotFound, "Media not found"));

        var permission = request.Permission.Equals("edit", StringComparison.OrdinalIgnoreCase)
            ? SharePermission.Edit
            : SharePermission.View;

        var share = await _unitOfWork.MediaShares.GetActiveShareAsync(request.MediaId, request.SharedWithUserId, cancellationToken);
        if (share is null)
        {
            share = new MediaShare(Guid.NewGuid(), request.MediaId, request.OwnerId, request.SharedWithUserId, permission, request.ExpiresAt);
            await _unitOfWork.MediaShares.CreateAsync(share, cancellationToken);
        }
        else
        {
            share.UpdatePermission(permission, request.ExpiresAt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _publishEndpoint.Publish(new MediaSharedEvent
        {
            MediaId = mediaAsset.Id,
            AlbumId = mediaAsset.AlbumId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = request.ActorUserId,
            SharedWithUserId = request.SharedWithUserId,
            Permission = request.Permission,
            SharedAt = share.SharedAt,
            ExpiresAt = share.ExpiresAt,
            Source = "MediaService.Application"
        }, cancellationToken);

        return Result<ShareMediaCommandResponse>.Success(new ShareMediaCommandResponse
        {
            MediaId = mediaAsset.Id,
            SharedWithUserId = request.SharedWithUserId,
            Permission = request.Permission,
            SharedAt = share.SharedAt,
            ExpiresAt = share.ExpiresAt
        });
    }
}

public sealed class AddMediaTagCommandHandler : IRequestHandler<AddMediaTagCommand, Result<AddMediaTagCommandResponse>>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaAccessService _mediaAccessService;
    private readonly IMediaSecurityService _mediaSecurityService;
    private readonly IPublishEndpoint _publishEndpoint;

    public AddMediaTagCommandHandler(IMediaUnitOfWork unitOfWork, IMediaAccessService mediaAccessService, IMediaSecurityService mediaSecurityService, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _mediaAccessService = mediaAccessService;
        _mediaSecurityService = mediaSecurityService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<AddMediaTagCommandResponse>> Handle(AddMediaTagCommand request, CancellationToken cancellationToken)
    {
        var mediaAsset = await _unitOfWork.MediaAssets.GetByIdAsync(request.MediaId, cancellationToken);
        if (mediaAsset is null || mediaAsset.IsDeleted || mediaAsset.OwnerId != request.OwnerId)
            return Result<AddMediaTagCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAssetNotFound, "Media not found"));

        if (mediaAsset.OwnerId != request.ActorUserId && !await _mediaAccessService.CanEditMediaAsync(request.MediaId, request.ActorUserId, cancellationToken))
            return Result<AddMediaTagCommandResponse>.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Edit permission required"));

        var tagName = request.TagName.Trim().ToLowerInvariant();

        var tag = await _unitOfWork.MediaTags.GetByOwnerAndNameAsync(request.OwnerId, tagName, cancellationToken)
            ?? await _unitOfWork.MediaTags.CreateAsync(new MediaTag(Guid.NewGuid(), request.OwnerId, tagName, tagName == MediaConstants.FavoriteTagName), cancellationToken);

        if (await _unitOfWork.MediaTags.HasTagOnMediaAsync(mediaAsset.Id, tag.Name, cancellationToken))
            return Result<AddMediaTagCommandResponse>.Failure(Error.Create(ErrorCodes.MediaTagAlreadyExists, $"Tag '{tag.Name}' is already assigned"));

        var assignment = new MediaTagAssignment(Guid.NewGuid(), mediaAsset.Id, tag.Id, request.ActorUserId);
        await _unitOfWork.MediaTags.AssignToMediaAsync(assignment, cancellationToken);

        await _publishEndpoint.Publish(new MediaTagAssignedEvent
        {
            MediaId = mediaAsset.Id,
            AlbumId = mediaAsset.AlbumId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = request.ActorUserId,
            TagName = tag.Name,
            AssignedAt = assignment.AssignedAt,
            Source = "MediaService.Application"
        }, cancellationToken);

        return Result<AddMediaTagCommandResponse>.Success(new AddMediaTagCommandResponse
        {
            MediaId = mediaAsset.Id,
            TagName = tag.Name,
            AssignedAt = assignment.AssignedAt
        });
    }
}

public sealed class RemoveMediaTagCommandHandler : IRequestHandler<RemoveMediaTagCommand, Result>
{
    private readonly IMediaUnitOfWork _unitOfWork;
    private readonly IMediaAccessService _mediaAccessService;
    private readonly IPublishEndpoint _publishEndpoint;

    public RemoveMediaTagCommandHandler(IMediaUnitOfWork unitOfWork, IMediaAccessService mediaAccessService, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _mediaAccessService = mediaAccessService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result> Handle(RemoveMediaTagCommand request, CancellationToken cancellationToken)
    {
        var mediaAsset = await _unitOfWork.MediaAssets.GetByIdAsync(request.MediaId, cancellationToken);
        if (mediaAsset is null || mediaAsset.IsDeleted || mediaAsset.OwnerId != request.OwnerId)
            return Result.Failure(Error.Create(ErrorCodes.MediaAssetNotFound, "Media not found"));

        if (mediaAsset.OwnerId != request.ActorUserId && !await _mediaAccessService.CanEditMediaAsync(request.MediaId, request.ActorUserId, cancellationToken))
            return Result.Failure(Error.Create(ErrorCodes.MediaAccessDenied, "Edit permission required"));

        await _unitOfWork.MediaTags.RemoveFromMediaAsync(mediaAsset.Id, request.TagName, cancellationToken);

        await _publishEndpoint.Publish(new MediaTagRemovedEvent
        {
            MediaId = mediaAsset.Id,
            AlbumId = mediaAsset.AlbumId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = request.ActorUserId,
            TagName = request.TagName,
            RemovedAt = DateTime.UtcNow,
            Source = "MediaService.Application"
        }, cancellationToken);

        return Result.Success();
    }
}