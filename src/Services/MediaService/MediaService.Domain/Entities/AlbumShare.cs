namespace MediaService.Domain.Entities;

using Enums;

/// <summary>
/// Represents an album sharing grant.
/// </summary>
public class AlbumShare
{
    public Guid Id { get; private set; }
    public Guid AlbumId { get; private set; }
    public Album Album { get; private set; } = null!;
    public Guid SharedByUserId { get; private set; }
    public Guid SharedWithUserId { get; private set; }
    public SharePermission Permission { get; private set; }
    public DateTime SharedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevocationReason { get; private set; }

    public AlbumShare()
    {
    }

    public AlbumShare(
        Guid id,
        Guid albumId,
        Guid sharedByUserId,
        Guid sharedWithUserId,
        SharePermission permission,
        DateTime? expiresAt = null)
    {
        Id = id;
        AlbumId = albumId;
        SharedByUserId = sharedByUserId;
        SharedWithUserId = sharedWithUserId;
        Permission = permission;
        SharedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsActive => RevokedAt == null && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);

    public void UpdatePermission(SharePermission permission, DateTime? expiresAt = null)
    {
        Permission = permission;
        ExpiresAt = expiresAt;
        RevokedAt = null;
        RevocationReason = null;
    }

    public void Revoke(string reason)
    {
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
    }
}