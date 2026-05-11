namespace MediaService.Domain.Enums;

/// <summary>
/// Describes the media type.
/// </summary>
public enum MediaKind
{
    Photo = 1,
    Video = 2
}

/// <summary>
/// Permission level for media and album sharing.
/// </summary>
public enum SharePermission
{
    View = 1,
    Edit = 2
}

/// <summary>
/// File safety result for uploads.
/// </summary>
public enum MediaSafetyStatus
{
    PendingScan = 1,
    Approved = 2,
    Rejected = 3,
    Quarantined = 4
}