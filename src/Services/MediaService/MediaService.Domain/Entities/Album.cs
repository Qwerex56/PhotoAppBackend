namespace MediaService.Domain.Entities;

/// <summary>
/// Represents a collection of media items.
/// </summary>
public class Album
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public List<MediaAsset> MediaAssets { get; } = [];
    public List<AlbumShare> Shares { get; } = [];

    public Album()
    {
    }

    public Album(Guid id, Guid ownerId, string name, string? description = null)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string name, string? description = null)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}