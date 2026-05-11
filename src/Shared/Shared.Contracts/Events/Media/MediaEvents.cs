namespace Shared.Contracts.Events.Media;

/// <summary>
/// Published when a new album is created.
/// </summary>
public class AlbumCreatedEvent : DomainEvent
{
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Published when an album is renamed.
/// </summary>
public class AlbumRenamedEvent : DomainEvent
{
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Published when an album is deleted.
/// </summary>
public class AlbumDeletedEvent : DomainEvent
{
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public DateTime DeletedAt { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Published when an album is shared.
/// </summary>
public class AlbumSharedEvent : DomainEvent
{
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public Guid SharedWithUserId { get; init; }
    public string Permission { get; init; } = string.Empty;
    public DateTime SharedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Published when a media asset is uploaded.
/// </summary>
public class MediaUploadedEvent : DomainEvent
{
    public Guid MediaId { get; init; }
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string StorageKey { get; init; } = string.Empty;
    public string Checksum { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}

/// <summary>
/// Published when a media asset is renamed.
/// </summary>
public class MediaRenamedEvent : DomainEvent
{
    public Guid MediaId { get; init; }
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Published when a media asset is moved between albums.
/// </summary>
public class MediaMovedEvent : DomainEvent
{
    public Guid MediaId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public Guid FromAlbumId { get; init; }
    public Guid ToAlbumId { get; init; }
    public DateTime MovedAt { get; init; }
}

/// <summary>
/// Published when a media asset is deleted.
/// </summary>
public class MediaDeletedEvent : DomainEvent
{
    public Guid MediaId { get; init; }
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public DateTime DeletedAt { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Published when a media asset is shared.
/// </summary>
public class MediaSharedEvent : DomainEvent
{
    public Guid MediaId { get; init; }
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public Guid SharedWithUserId { get; init; }
    public string Permission { get; init; } = string.Empty;
    public DateTime SharedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Published when a tag is assigned to a media asset.
/// </summary>
public class MediaTagAssignedEvent : DomainEvent
{
    public Guid MediaId { get; init; }
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string TagName { get; init; } = string.Empty;
    public DateTime AssignedAt { get; init; }
}

/// <summary>
/// Published when a tag is removed from a media asset.
/// </summary>
public class MediaTagRemovedEvent : DomainEvent
{
    public Guid MediaId { get; init; }
    public Guid AlbumId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid ActorUserId { get; init; }
    public string TagName { get; init; } = string.Empty;
    public DateTime RemovedAt { get; init; }
}