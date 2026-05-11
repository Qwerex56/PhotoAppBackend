namespace MediaService.Application.Services;

using System.Security.Cryptography;
using System.Text;
using Shared.Constants;
using Shared.Results;

public interface IMediaSecurityService
{
    Result ValidateUpload(string fileName, string contentType, long fileSize, string? checksum);
    string GetMediaKind(string contentType, string fileName);
    string NormalizeDisplayName(string displayName);
    string CreateStorageKey(Guid ownerId, Guid albumId, Guid mediaId, string fileName);
    string ComputeChecksum(Stream content);
    bool IsSafeText(string value);
}

public sealed class MediaSecurityService : IMediaSecurityService
{
    public Result ValidateUpload(string fileName, string contentType, long fileSize, string? checksum)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure(Error.Create(ErrorCodes.ValidationFailed, "File name is required"));

        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
            return Result.Failure(Error.Create(ErrorCodes.MediaInvalidFileType, "File extension is required"));

        if (MediaConstants.IsForbiddenExtension(extension))
            return Result.Failure(Error.Create(ErrorCodes.MediaUnsafeFile, "The file extension is not allowed"));

        if (!MediaConstants.IsAllowedExtension(extension))
            return Result.Failure(Error.Create(ErrorCodes.MediaInvalidFileType, "Unsupported file extension"));

        if (!MediaConstants.IsAllowedContentType(contentType))
            return Result.Failure(Error.Create(ErrorCodes.MediaInvalidFileType, "Unsupported content type"));

        if (fileSize <= 0)
            return Result.Failure(Error.Create(ErrorCodes.ValidationFailed, "File size must be greater than zero"));

        if (fileSize > MediaConstants.MaximumUploadSizeBytes)
            return Result.Failure(Error.Create(ErrorCodes.MediaFileTooLarge, "File is too large"));

        if (!string.IsNullOrWhiteSpace(checksum) && checksum.Length < 16)
            return Result.Failure(Error.Create(ErrorCodes.ValidationFailed, "Checksum is invalid"));

        return Result.Success();
    }

    public string GetMediaKind(string contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (MediaConstants.IsAllowedVideoContentType(contentType) || MediaConstants.IsAllowedVideoExtension(extension))
            return "video";

        return "photo";
    }

    public string NormalizeDisplayName(string displayName)
    {
        var normalized = displayName.Trim();
        return normalized.Length == 0 ? throw new ArgumentException("Display name cannot be empty", nameof(displayName)) : normalized;
    }

    public string CreateStorageKey(Guid ownerId, Guid albumId, Guid mediaId, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return $"media/{ownerId}/{albumId}/{mediaId}{extension}";
    }

    public string ComputeChecksum(Stream content)
    {
        if (content.CanSeek)
            content.Position = 0;

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(content);

        if (content.CanSeek)
            content.Position = 0;

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool IsSafeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return !value.Contains("<script", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("data:text/html", StringComparison.OrdinalIgnoreCase)
            && !value.Contains('\0');
    }
}