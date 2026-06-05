namespace MediaService.Application.Services;

using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Results;

public interface IMediaSecurityService
{
    Result ValidateUpload(string fileName, string contentType, long fileSize, string? checksum);
    Task<Result> ScanForMalwareAsync(Stream content, CancellationToken cancellationToken = default);
    string GetMediaKind(string contentType, string fileName);
    string NormalizeDisplayName(string displayName);
    string CreateStorageKey(Guid ownerId, Guid albumId, Guid mediaId, string fileName);
    string ComputeChecksum(Stream content);
    bool IsSafeText(string value);
}

public sealed class MediaSecurityService : IMediaSecurityService
{
    private const int ChunkSize = 64 * 1024;

    private readonly ILogger<MediaSecurityService> _logger;
    private readonly bool _antimalwareEnabled;
    private readonly bool _failOpen;
    private readonly string _clamAvHost;
    private readonly int _clamAvPort;
    private readonly int _scanTimeoutSeconds;

    public MediaSecurityService(IConfiguration configuration, ILogger<MediaSecurityService> logger)
    {
        _logger = logger;
        _antimalwareEnabled = !bool.TryParse(configuration["MediaSecurity:Antimalware:Enabled"], out var enabled) || enabled;
        _failOpen = bool.TryParse(configuration["MediaSecurity:Antimalware:FailOpen"], out var failOpen) && failOpen;
        _clamAvHost = configuration["MediaSecurity:Antimalware:Host"] ?? "clamav";
        _clamAvPort = int.TryParse(configuration["MediaSecurity:Antimalware:Port"], out var port) ? port : 3310;
        _scanTimeoutSeconds = int.TryParse(configuration["MediaSecurity:Antimalware:TimeoutSeconds"], out var timeoutSeconds)
            ? timeoutSeconds
            : 30;
    }

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

    public async Task<Result> ScanForMalwareAsync(Stream content, CancellationToken cancellationToken = default)
    {
        if (!_antimalwareEnabled)
            return Result.Success();

        try
        {
            if (content.CanSeek)
                content.Position = 0;

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(_scanTimeoutSeconds));

            using var client = new TcpClient();
            await client.ConnectAsync(_clamAvHost, _clamAvPort, linkedCts.Token);

            await using var stream = client.GetStream();
            var command = Encoding.ASCII.GetBytes("zINSTREAM\0");
            await stream.WriteAsync(command, linkedCts.Token);

            var buffer = new byte[ChunkSize];
            while (true)
            {
                var bytesRead = await content.ReadAsync(buffer, linkedCts.Token);
                if (bytesRead <= 0)
                    break;

                var lengthPrefix = new byte[4];
                BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, bytesRead);
                await stream.WriteAsync(lengthPrefix, linkedCts.Token);
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead), linkedCts.Token);
            }

            var terminator = new byte[4];
            await stream.WriteAsync(terminator, linkedCts.Token);
            await stream.FlushAsync(linkedCts.Token);

            var response = await ReadClamAvResponseAsync(stream, linkedCts.Token);

            if (response.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Malware detected during upload scan. Response: {Response}", response);
                return Result.Failure(Error.Create(ErrorCodes.MediaUnsafeFile, "Malicious content detected in uploaded file"));
            }

            if (!response.Contains("OK", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Unexpected ClamAV scan response: {Response}", response);
                return HandleScannerFailure("Unexpected malware scanner response");
            }

            return Result.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Malware scan failed while processing uploaded file.");
            return HandleScannerFailure("Unable to complete malware scan");
        }
        finally
        {
            if (content.CanSeek)
                content.Position = 0;
        }
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

    private Result HandleScannerFailure(string message)
    {
        if (_failOpen)
        {
            _logger.LogWarning("Fail-open enabled for malware scanner. Upload will continue despite scan failure.");
            return Result.Success();
        }

        return Result.Failure(Error.Create(ErrorCodes.MediaUnsafeFile, message));
    }

    private static async Task<string> ReadClamAvResponseAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var responseBuffer = new List<byte>(256);
        var singleByte = new byte[1];

        while (true)
        {
            var read = await stream.ReadAsync(singleByte.AsMemory(0, 1), cancellationToken);
            if (read == 0)
                break;

            if (singleByte[0] == 0 || singleByte[0] == (byte)'\n')
                break;

            responseBuffer.Add(singleByte[0]);
        }

        return Encoding.UTF8.GetString(responseBuffer.ToArray());
    }
}