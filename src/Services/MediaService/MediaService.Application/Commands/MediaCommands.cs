namespace MediaService.Application.Commands.Media;

using Shared.Contracts.Commands;
using Shared.Results;

public sealed class CreateAlbumCommand : Command<Result<CreateAlbumCommandResponse>>
{
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed class CreateAlbumCommandResponse
{
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class RenameAlbumCommand : Command<Result<RenameAlbumCommandResponse>>
{
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed class RenameAlbumCommandResponse
{
    public Guid AlbumId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class DeleteAlbumCommand : Command<Result>
{
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed class ShareAlbumCommand : Command<Result<ShareAlbumCommandResponse>>
{
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public Guid SharedWithUserId { get; init; }
    public string Permission { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
}

public sealed class ShareAlbumCommandResponse
{
    public Guid AlbumId { get; init; }
    public Guid SharedWithUserId { get; init; }
    public string Permission { get; init; } = string.Empty;
    public DateTime SharedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public sealed class UploadMediaCommand : Command<Result<UploadMediaCommandResponse>>
{
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public Stream Content { get; init; } = Stream.Null;
    public string? Checksum { get; init; }
}

public sealed class UploadMediaCommandResponse
{
    public Guid MediaId { get; init; }
    public Guid AlbumId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string StorageKey { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string Checksum { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}

public sealed class RenameMediaCommand : Command<Result<RenameMediaCommandResponse>>
{
    public Guid MediaId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class RenameMediaCommandResponse
{
    public Guid MediaId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}

public sealed class MoveMediaCommand : Command<Result<MoveMediaCommandResponse>>
{
    public Guid MediaId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public Guid TargetAlbumId { get; init; }
}

public sealed class MoveMediaCommandResponse
{
    public Guid MediaId { get; init; }
    public Guid FromAlbumId { get; init; }
    public Guid TargetAlbumId { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class DeleteMediaCommand : Command<Result>
{
    public Guid MediaId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed class ShareMediaCommand : Command<Result<ShareMediaCommandResponse>>
{
    public Guid MediaId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public Guid SharedWithUserId { get; init; }
    public string Permission { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
}

public sealed class ShareMediaCommandResponse
{
    public Guid MediaId { get; init; }
    public Guid SharedWithUserId { get; init; }
    public string Permission { get; init; } = string.Empty;
    public DateTime SharedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public sealed class AddMediaTagCommand : Command<Result<AddMediaTagCommandResponse>>
{
    public Guid MediaId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string TagName { get; init; } = string.Empty;
}

public sealed class AddMediaTagCommandResponse
{
    public Guid MediaId { get; init; }
    public string TagName { get; init; } = string.Empty;
    public DateTime AssignedAt { get; init; }
}

public sealed class RemoveMediaTagCommand : Command<Result>
{
    public Guid MediaId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string TagName { get; init; } = string.Empty;
}