using System.Text.RegularExpressions;
using BCSStudyHub.Application.Library;

namespace BCSStudyHub.Infrastructure.Library;

/// <summary>
/// Stores files on the local filesystem under random keys (<c>yyyy/MM/{guid}</c>, no extension).
/// Keys are validated against a strict pattern and resolved paths are confined to the root,
/// so stored names can never be influenced by user input or escape the storage directory.
/// </summary>
public sealed partial class LocalFileStorage : IFileStorage
{
    private readonly string root;

    public LocalFileStorage(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
    }

    public string ProviderName => "Local";

    public string CreateKey()
    {
        var now = DateTime.UtcNow;
        return $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}";
    }

    public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".uploading";
        try
        {
            await using (var target = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous))
            {
                await content.CopyToAsync(target, cancellationToken);
            }

            File.Move(temporaryPath, path, overwrite: false);
        }
        catch
        {
            TryDelete(temporaryPath);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        TryDelete(ResolvePath(key));
        return Task.CompletedTask;
    }

    internal string ResolvePath(string key)
    {
        if (string.IsNullOrEmpty(key) || !KeyPattern().IsMatch(key))
        {
            throw new ArgumentException("The storage key is not valid.", nameof(key));
        }

        var path = Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("The storage key resolves outside the storage root.", nameof(key));
        }

        return path;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best effort: an orphaned blob is preferable to failing the caller's operation.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [GeneratedRegex("^[0-9]{4}/[0-9]{2}/[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}
