namespace MediaService.Domain.Entities;

/// <summary>
/// Links a tag to a media asset.
/// </summary>
public class MediaTagAssignment
{
    public Guid Id { get; private set; }
    public Guid MediaAssetId { get; private set; }
    public MediaAsset MediaAsset { get; private set; } = null!;
    public Guid MediaTagId { get; private set; }
    public MediaTag MediaTag { get; private set; } = null!;
    public Guid AssignedByUserId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public MediaTagAssignment()
    {
    }

    public MediaTagAssignment(Guid id, Guid mediaAssetId, Guid mediaTagId, Guid assignedByUserId)
    {
        Id = id;
        MediaAssetId = mediaAssetId;
        MediaTagId = mediaTagId;
        AssignedByUserId = assignedByUserId;
        AssignedAt = DateTime.UtcNow;
    }
}