namespace MediaService.API.Endpoints;

using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;
using MediaService.Application.Commands.Media;
using MediaService.Domain.Entities;
using MediaService.Domain.Enums;
using MediaService.Domain.Repositories;
using Shared.Constants;
using Shared.Results;

public static class MediaEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(MediaConstants.AccessCacheMinutes);

    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var albumGroup = app.MapGroup("/api/albums")
            .WithTags("Media")
            .RequireAuthorization();

        var mediaGroup = app.MapGroup("/api/media")
            .WithTags("Media")
            .RequireAuthorization();

        albumGroup.MapPost(string.Empty, CreateAlbumAsync)
            .WithName("CreateAlbum")
            .WithOpenApi();

        albumGroup.MapGet(string.Empty, GetAlbumsAsync)
            .WithName("GetAlbums")
            .WithOpenApi();

        albumGroup.MapGet("/{albumId:guid}", GetAlbumAsync)
            .WithName("GetAlbum")
            .WithOpenApi();

        albumGroup.MapPut("/{albumId:guid}", RenameAlbumAsync)
            .WithName("RenameAlbum")
            .WithOpenApi();

        albumGroup.MapDelete("/{albumId:guid}", DeleteAlbumAsync)
            .WithName("DeleteAlbum")
            .WithOpenApi();

        albumGroup.MapPost("/{albumId:guid}/share", ShareAlbumAsync)
            .WithName("ShareAlbum")
            .WithOpenApi();

        albumGroup.MapGet("/{albumId:guid}/media", GetAlbumMediaAsync)
            .WithName("GetAlbumMedia")
            .WithOpenApi();

        albumGroup.MapPost("/{albumId:guid}/media", UploadMediaAsync)
            .Accepts<UploadMediaRequest>("multipart/form-data")
            .WithName("UploadMedia")
            .WithOpenApi();

        mediaGroup.MapGet("/{mediaId:guid}", GetMediaAsync)
            .WithName("GetMedia")
            .WithOpenApi();

        mediaGroup.MapPut("/{mediaId:guid}", RenameMediaAsync)
            .WithName("RenameMedia")
            .WithOpenApi();

        mediaGroup.MapPatch("/{mediaId:guid}/move", MoveMediaAsync)
            .WithName("MoveMedia")
            .WithOpenApi();

        mediaGroup.MapDelete("/{mediaId:guid}", DeleteMediaAsync)
            .WithName("DeleteMedia")
            .WithOpenApi();

        mediaGroup.MapPost("/{mediaId:guid}/share", ShareMediaAsync)
            .WithName("ShareMedia")
            .WithOpenApi();

        mediaGroup.MapPost("/{mediaId:guid}/tags", AddTagAsync)
            .WithName("AddMediaTag")
            .WithOpenApi();

        mediaGroup.MapDelete("/{mediaId:guid}/tags/{tagName}", RemoveTagAsync)
            .WithName("RemoveMediaTag")
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> CreateAlbumAsync(
        CreateAlbumRequest request,
        ClaimsPrincipal user,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var command = new CreateAlbumCommand
        {
            OwnerId = userId,
            ActorUserId = userId,
            Name = request.Name,
            Description = request.Description
        };

        var result = await sender.Send(command);
        if (result.IsSuccess)
            await InvalidateUserLibraryAsync(cache, userId, context.RequestAborted);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAlbumsAsync(
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var ownedAlbums = await unitOfWork.Albums.GetOwnedByUserAsync(userId, context.RequestAborted);
        var sharedAlbums = await unitOfWork.Albums.GetSharedWithUserAsync(userId, context.RequestAborted);

        var ownedResponses = ownedAlbums.Select(album => new AlbumSummaryResponse(
            album.Id,
            album.OwnerId,
            album.Name,
            album.Description,
            album.CreatedAt,
            album.UpdatedAt,
            "owner"));

        var sharedResponses = sharedAlbums.Select(album => new AlbumSummaryResponse(
            album.Id,
            album.OwnerId,
            album.Name,
            album.Description,
            album.CreatedAt,
            album.UpdatedAt,
            "shared"));

        return Results.Ok(new AlbumsResponse(ownedResponses.ToArray(), sharedResponses.ToArray()));
    }

    private static async Task<IResult> GetAlbumAsync(
        Guid albumId,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var album = await unitOfWork.Albums.GetByIdAsync(albumId, context.RequestAborted);
        if (album is null)
            return Results.NotFound();

        if (!await CanViewAlbumAsync(unitOfWork, album, userId, context.RequestAborted))
            return Results.Forbid();

        var cacheKey = CacheKeys.MediaAlbum(albumId);
        var cached = await GetCachedAsync<AlbumDetailsResponse>(cache, cacheKey, context.RequestAborted);
        if (cached is not null)
            return Results.Ok(cached);

        var mediaCount = (await unitOfWork.MediaAssets.GetByAlbumAsync(albumId, context.RequestAborted)).Count;
        var response = new AlbumDetailsResponse(
            album.Id,
            album.OwnerId,
            album.Name,
            album.Description,
            album.CreatedAt,
            album.UpdatedAt,
            mediaCount,
            album.IsDeleted);

        await SetCachedAsync(cache, cacheKey, response, context.RequestAborted);
        return Results.Ok(response);
    }

    private static async Task<IResult> RenameAlbumAsync(
        Guid albumId,
        RenameAlbumRequest request,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var album = await unitOfWork.Albums.GetByIdAsync(albumId, context.RequestAborted);
        if (album is null)
            return Results.NotFound();

        var command = new RenameAlbumCommand
        {
            AlbumId = albumId,
            OwnerId = album.OwnerId,
            ActorUserId = userId,
            Name = request.Name,
            Description = request.Description
        };

        var result = await sender.Send(command);
        if (result.IsSuccess)
            await InvalidateAlbumCacheAsync(cache, albumId, context.RequestAborted);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteAlbumAsync(
        Guid albumId,
        [FromBody] DeleteAlbumRequest request,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var album = await unitOfWork.Albums.GetByIdAsync(albumId, context.RequestAborted);
        if (album is null)
            return Results.NotFound();

        var mediaIds = await unitOfWork.MediaAssets.GetByAlbumAsync(albumId, context.RequestAborted);

        var command = new DeleteAlbumCommand
        {
            AlbumId = albumId,
            OwnerId = album.OwnerId,
            ActorUserId = userId,
            Reason = request.Reason
        };

        var result = await sender.Send(command);
        if (result.IsSuccess)
        {
            await InvalidateAlbumCacheAsync(cache, albumId, context.RequestAborted);
            foreach (var mediaAsset in mediaIds)
            {
                await InvalidateMediaCacheAsync(cache, mediaAsset.Id, context.RequestAborted);
            }
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> ShareAlbumAsync(
        Guid albumId,
        ShareRequest request,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var album = await unitOfWork.Albums.GetByIdAsync(albumId, context.RequestAborted);
        if (album is null)
            return Results.NotFound();

        var command = new ShareAlbumCommand
        {
            AlbumId = albumId,
            OwnerId = album.OwnerId,
            ActorUserId = userId,
            SharedWithUserId = request.SharedWithUserId,
            Permission = request.Permission,
            ExpiresAt = request.ExpiresAt
        };

        var result = await sender.Send(command);
        if (result.IsSuccess)
            await InvalidateAlbumCacheAsync(cache, albumId, context.RequestAborted);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAlbumMediaAsync(
        Guid albumId,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var album = await unitOfWork.Albums.GetByIdAsync(albumId, context.RequestAborted);
        if (album is null)
            return Results.NotFound();

        if (!await CanViewAlbumAsync(unitOfWork, album, userId, context.RequestAborted))
            return Results.Forbid();

        var mediaItems = await unitOfWork.MediaAssets.GetByAlbumAsync(albumId, context.RequestAborted);
        var response = mediaItems.Select(ToMediaSummary).ToArray();
        return Results.Ok(response);
    }

    private static async Task<IResult> UploadMediaAsync(
        Guid albumId,
        [FromForm] UploadMediaRequest request,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        if (request.File is null || request.File.Length == 0)
            return Results.BadRequest(new { error = "File is required." });

        var album = await unitOfWork.Albums.GetByIdAsync(albumId, context.RequestAborted);
        if (album is null)
            return Results.NotFound();

        using var content = request.File.OpenReadStream();
        var command = new UploadMediaCommand
        {
            AlbumId = albumId,
            OwnerId = album.OwnerId,
            ActorUserId = userId,
            DisplayName = request.DisplayName,
            OriginalFileName = request.File.FileName,
            ContentType = request.File.ContentType,
            FileSize = request.File.Length,
            Content = content,
            Checksum = request.Checksum
        };

        var result = await sender.Send(command, context.RequestAborted);
        if (result.IsSuccess)
            await InvalidateAlbumCacheAsync(cache, albumId, context.RequestAborted);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetMediaAsync(
        Guid mediaId,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var mediaAsset = await unitOfWork.MediaAssets.GetByIdAsync(mediaId, context.RequestAborted);
        if (mediaAsset is null)
            return Results.NotFound();

        if (!await CanViewMediaAsync(unitOfWork, mediaAsset, userId, context.RequestAborted))
            return Results.Forbid();

        var cacheKey = CacheKeys.MediaAsset(mediaId);
        var cached = await GetCachedAsync<MediaDetailsResponse>(cache, cacheKey, context.RequestAborted);
        if (cached is not null)
            return Results.Ok(cached);

        var response = ToMediaDetails(mediaAsset);
        await SetCachedAsync(cache, cacheKey, response, context.RequestAborted);
        return Results.Ok(response);
    }

    private static async Task<IResult> RenameMediaAsync(
        Guid mediaId,
        RenameMediaRequest request,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var mediaAsset = await unitOfWork.MediaAssets.GetByIdAsync(mediaId, context.RequestAborted);
        if (mediaAsset is null)
            return Results.NotFound();

        var command = new RenameMediaCommand
        {
            MediaId = mediaId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = userId,
            DisplayName = request.DisplayName
        };

        var result = await sender.Send(command);
        if (result.IsSuccess)
            await InvalidateMediaCacheAsync(cache, mediaId, context.RequestAborted);

        return result.ToHttpResult();
    }

    private static async Task<IResult> MoveMediaAsync(
        Guid mediaId,
        MoveMediaRequest request,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var mediaAsset = await unitOfWork.MediaAssets.GetByIdAsync(mediaId, context.RequestAborted);
        if (mediaAsset is null)
            return Results.NotFound();

        var command = new MoveMediaCommand
        {
            MediaId = mediaId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = userId,
            TargetAlbumId = request.TargetAlbumId
        };

        var result = await sender.Send(command);
        if (result.IsSuccess)
        {
            await InvalidateMediaCacheAsync(cache, mediaId, context.RequestAborted);
            await InvalidateAlbumCacheAsync(cache, mediaAsset.AlbumId, context.RequestAborted);
            await InvalidateAlbumCacheAsync(cache, request.TargetAlbumId, context.RequestAborted);
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteMediaAsync(
        Guid mediaId,
        [FromBody] DeleteMediaRequest request,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var mediaAsset = await unitOfWork.MediaAssets.GetByIdAsync(mediaId, context.RequestAborted);
        if (mediaAsset is null)
            return Results.NotFound();

        var command = new DeleteMediaCommand
        {
            MediaId = mediaId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = userId,
            Reason = request.Reason
        };

        var result = await sender.Send(command);
        if (result.IsSuccess)
        {
            await InvalidateMediaCacheAsync(cache, mediaId, context.RequestAborted);
            await InvalidateAlbumCacheAsync(cache, mediaAsset.AlbumId, context.RequestAborted);
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> ShareMediaAsync(
        Guid mediaId,
        ShareRequest request,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var mediaAsset = await unitOfWork.MediaAssets.GetByIdAsync(mediaId, context.RequestAborted);
        if (mediaAsset is null)
            return Results.NotFound();

        var command = new ShareMediaCommand
        {
            MediaId = mediaId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = userId,
            SharedWithUserId = request.SharedWithUserId,
            Permission = request.Permission,
            ExpiresAt = request.ExpiresAt
        };

        var result = await sender.Send(command);
        if (result.IsSuccess)
            await InvalidateMediaCacheAsync(cache, mediaId, context.RequestAborted);

        return result.ToHttpResult();
    }

    private static async Task<IResult> AddTagAsync(
        Guid mediaId,
        AddTagRequest request,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var mediaAsset = await unitOfWork.MediaAssets.GetByIdAsync(mediaId, context.RequestAborted);
        if (mediaAsset is null)
            return Results.NotFound();

        var command = new AddMediaTagCommand
        {
            MediaId = mediaId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = userId,
            TagName = request.TagName
        };

        var result = await sender.Send(command);
        if (result.IsSuccess)
            await InvalidateMediaCacheAsync(cache, mediaId, context.RequestAborted);

        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveTagAsync(
        Guid mediaId,
        string tagName,
        ClaimsPrincipal user,
        IMediaUnitOfWork unitOfWork,
        ISender sender,
        IDistributedCache cache,
        HttpContext context)
    {
        if (!TryGetUserId(user, out var userId))
            return Results.Unauthorized();

        var mediaAsset = await unitOfWork.MediaAssets.GetByIdAsync(mediaId, context.RequestAborted);
        if (mediaAsset is null)
            return Results.NotFound();

        var command = new RemoveMediaTagCommand
        {
            MediaId = mediaId,
            OwnerId = mediaAsset.OwnerId,
            ActorUserId = userId,
            TagName = tagName
        };

        var result = await sender.Send(command);
        if (result.IsSuccess)
            await InvalidateMediaCacheAsync(cache, mediaId, context.RequestAborted);

        return result.ToHttpResult();
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var subject = principal.FindFirstValue(AuthConstants.JwtClaimSubject);
        return Guid.TryParse(subject, out userId);
    }

    private static async Task<bool> CanViewAlbumAsync(IMediaUnitOfWork unitOfWork, Album album, Guid userId, CancellationToken cancellationToken)
    {
        if (album.OwnerId == userId)
            return true;

        return await unitOfWork.AlbumShares.HasPermissionAsync(album.Id, userId, SharePermission.View, cancellationToken);
    }

    private static async Task<bool> CanViewMediaAsync(IMediaUnitOfWork unitOfWork, MediaAsset mediaAsset, Guid userId, CancellationToken cancellationToken)
    {
        if (mediaAsset.OwnerId == userId)
            return true;

        if (await unitOfWork.MediaShares.HasPermissionAsync(mediaAsset.Id, userId, SharePermission.View, cancellationToken))
            return true;

        return await unitOfWork.AlbumShares.HasPermissionAsync(mediaAsset.AlbumId, userId, SharePermission.View, cancellationToken);
    }

    private static MediaSummaryResponse ToMediaSummary(MediaAsset mediaAsset)
        => new(
            mediaAsset.Id,
            mediaAsset.AlbumId,
            mediaAsset.OwnerId,
            mediaAsset.DisplayName,
            mediaAsset.OriginalFileName,
            mediaAsset.ContentType,
            mediaAsset.FileSize,
            mediaAsset.Checksum,
            mediaAsset.Kind.ToString().ToLowerInvariant(),
            mediaAsset.SafetyStatus.ToString().ToLowerInvariant(),
            mediaAsset.UploadedAt,
            mediaAsset.UpdatedAt,
            mediaAsset.TagAssignments.Select(x => x.MediaTag.Name).ToArray());

    private static MediaDetailsResponse ToMediaDetails(MediaAsset mediaAsset)
        => new(
            mediaAsset.Id,
            mediaAsset.AlbumId,
            mediaAsset.OwnerId,
            mediaAsset.DisplayName,
            mediaAsset.OriginalFileName,
            mediaAsset.StorageKey,
            mediaAsset.ContentType,
            mediaAsset.FileSize,
            mediaAsset.Checksum,
            mediaAsset.Kind.ToString().ToLowerInvariant(),
            mediaAsset.SafetyStatus.ToString().ToLowerInvariant(),
            mediaAsset.UploadedAt,
            mediaAsset.UpdatedAt,
            mediaAsset.TagAssignments.Select(x => x.MediaTag.Name).ToArray());

    private static async Task<T?> GetCachedAsync<T>(IDistributedCache cache, string key, CancellationToken cancellationToken)
    {
        var cachedBytes = await cache.GetAsync(key, cancellationToken);
        return cachedBytes is null ? default : JsonSerializer.Deserialize<T>(cachedBytes, JsonOptions);
    }

    private static Task SetCachedAsync<T>(IDistributedCache cache, string key, T value, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        return cache.SetAsync(key, payload, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        }, cancellationToken);
    }

    private static Task InvalidateAlbumCacheAsync(IDistributedCache cache, Guid albumId, CancellationToken cancellationToken)
        => cache.RemoveAsync(CacheKeys.MediaAlbum(albumId), cancellationToken);

    private static Task InvalidateMediaCacheAsync(IDistributedCache cache, Guid mediaId, CancellationToken cancellationToken)
        => cache.RemoveAsync(CacheKeys.MediaAsset(mediaId), cancellationToken);

    private static Task InvalidateUserLibraryAsync(IDistributedCache cache, Guid userId, CancellationToken cancellationToken)
        => cache.RemoveAsync(CacheKeys.MediaLibrary(userId), cancellationToken);

    private static IResult ToHttpResult(this Result result)
        => result.IsSuccess ? Results.NoContent() : ToProblem(result.Error!);

    private static IResult ToHttpResult<T>(this Result<T> result)
        => result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);

    private static IResult ToProblem(Error error)
    {
        var statusCode = error.Code switch
        {
            ErrorCodes.ValidationFailed or ErrorCodes.MediaInvalidFileType or ErrorCodes.MediaUnsafeFile => StatusCodes.Status400BadRequest,
            ErrorCodes.MediaPermissionInvalid => StatusCodes.Status400BadRequest,
            ErrorCodes.MediaAccessDenied => StatusCodes.Status403Forbidden,
            ErrorCodes.MediaAlbumNotFound or ErrorCodes.MediaAssetNotFound or ErrorCodes.MediaTagNotFound or ErrorCodes.MediaShareNotFound => StatusCodes.Status404NotFound,
            ErrorCodes.MediaAlbumAlreadyExists or ErrorCodes.MediaAssetAlreadyExists or ErrorCodes.MediaTagAlreadyExists => StatusCodes.Status409Conflict,
            ErrorCodes.MediaFileTooLarge => StatusCodes.Status413PayloadTooLarge,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: statusCode);
    }

    private sealed record CreateAlbumRequest(string Name, string? Description);
    private sealed record RenameAlbumRequest(string Name, string? Description);
    private sealed record DeleteAlbumRequest(string Reason);
    private sealed record ShareRequest(Guid SharedWithUserId, string Permission, DateTime? ExpiresAt);
    private sealed record UploadMediaRequest(IFormFile File, string DisplayName, string? Checksum);
    private sealed record RenameMediaRequest(string DisplayName);
    private sealed record MoveMediaRequest(Guid TargetAlbumId);
    private sealed record DeleteMediaRequest(string Reason);
    private sealed record AddTagRequest(string TagName);

    private sealed record AlbumsResponse(AlbumSummaryResponse[] Owned, AlbumSummaryResponse[] Shared);
    private sealed record AlbumSummaryResponse(
        Guid AlbumId,
        Guid OwnerId,
        string Name,
        string? Description,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string AccessMode);

    private sealed record AlbumDetailsResponse(
        Guid AlbumId,
        Guid OwnerId,
        string Name,
        string? Description,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int MediaCount,
        bool IsDeleted);

    private sealed record MediaSummaryResponse(
        Guid MediaId,
        Guid AlbumId,
        Guid OwnerId,
        string DisplayName,
        string OriginalFileName,
        string ContentType,
        long FileSize,
        string Checksum,
        string Kind,
        string SafetyStatus,
        DateTime UploadedAt,
        DateTime UpdatedAt,
        string[] Tags);

    private sealed record MediaDetailsResponse(
        Guid MediaId,
        Guid AlbumId,
        Guid OwnerId,
        string DisplayName,
        string OriginalFileName,
        string StorageKey,
        string ContentType,
        long FileSize,
        string Checksum,
        string Kind,
        string SafetyStatus,
        DateTime UploadedAt,
        DateTime UpdatedAt,
        string[] Tags);
}