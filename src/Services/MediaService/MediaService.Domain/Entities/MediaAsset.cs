namespace MediaService.Domain.Entities;

using Enums;

/// <summary>
/// Represents a single photo or video stored by the service.
/// </summary>
public class MediaAsset
{
    public Guid Id { get; private set; }
    public Guid AlbumId { get; private set; }
    public Album Album { get; private set; } = null!;
    public Guid OwnerId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string Checksum { get; private set; } = string.Empty;
    public MediaKind Kind { get; private set; }
    public MediaSafetyStatus SafetyStatus { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public List<MediaTagAssignment> TagAssignments { get; } = [];
    public List<MediaShare> Shares { get; } = [];

    public MediaAsset()
    {
    }

    public MediaAsset(
        Guid id,
        Guid albumId,
        Guid ownerId,
        string displayName,
        string originalFileName,
        string storageKey,
        string contentType,
        long fileSize,
        string checksum,
        MediaKind kind,
        MediaSafetyStatus safetyStatus)
    {
        Id = id;
        AlbumId = albumId;
        OwnerId = ownerId;
        DisplayName = displayName;
        OriginalFileName = originalFileName;
        StorageKey = storageKey;
        ContentType = contentType;
        FileSize = fileSize;
        Checksum = checksum;
        Kind = kind;
        SafetyStatus = safetyStatus;
        UploadedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string displayName)
    {
        DisplayName = displayName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveTo(Guid albumId)
    {
        AlbumId = albumId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApproveSafety()
    {
        SafetyStatus = MediaSafetyStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}