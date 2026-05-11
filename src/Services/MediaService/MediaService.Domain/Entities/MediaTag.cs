namespace MediaService.Domain.Entities;

/// <summary>
/// Represents a tag that can be attached to one or more media items.
/// </summary>
public class MediaTag
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public List<MediaTagAssignment> TagAssignments { get; } = [];

    public MediaTag()
    {
    }

    public MediaTag(Guid id, Guid ownerId, string name, bool isSystem = false)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        IsSystem = isSystem;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}