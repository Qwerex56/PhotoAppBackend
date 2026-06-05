namespace MediaService.Infrastructure.Services;

using System.IO;
using System.Security;
using MediaService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public sealed class LocalMediaStorageService : IMediaStorageService
{
    private readonly string _rootPath;

    public LocalMediaStorageService(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredPath = configuration["MediaStorage:RootPath"];
        _rootPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "media-storage")
            : configuredPath;

        Directory.CreateDirectory(_rootPath);
    }

    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePath(storageKey);
        var directory = Path.GetDirectoryName(filePath);

        if (directory is not null)
            Directory.CreateDirectory(directory);

        if (content.CanSeek)
            content.Position = 0;

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var filePath = ResolvePath(storageKey);
        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    private string ResolvePath(string storageKey)
    {
        if (storageKey.Contains("..", StringComparison.Ordinal))
            throw new SecurityException("Storage key contains invalid path segments.");

        var segments = storageKey.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        return Path.Combine(_rootPath, Path.Combine(segments));
    }
}
