using System.Security.Cryptography;
using FEMS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FEMS.Infrastructure.Services;

/// <summary>
/// Local on-prem disk storage under `App:StorageRoot` (defaults to ./App_Data/uploads,
/// which should be moved outside the IIS site's web root in production and locked down
/// via NTFS permissions to the app pool identity only).
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly string _root;

    public FileStorageService(IConfiguration configuration)
    {
        _root = configuration["App:StorageRoot"] ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads");
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string subfolder, CancellationToken ct = default)
    {
        var safeSubfolder = subfolder.Replace("..", string.Empty).Trim('/', '\\');
        var directory = Path.Combine(_root, safeSubfolder);
        Directory.CreateDirectory(directory);

        var uniqueName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(directory, uniqueName);

        using var sha256 = SHA256.Create();
        await using (var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
        await using (var hashingStream = new CryptoStream(fileStream, sha256, CryptoStreamMode.Write))
        {
            await content.CopyToAsync(hashingStream, ct);
        }

        var hash = Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>());
        var sizeBytes = new FileInfo(fullPath).Length;
        var relativePath = Path.Combine(safeSubfolder, uniqueName).Replace('\\', '/');

        return new StoredFile(relativePath, hash, sizeBytes);
    }

    public Stream OpenRead(string relativePath)
    {
        var safePath = relativePath.Replace("..", string.Empty).TrimStart('/', '\\');
        var fullPath = Path.Combine(_root, safePath);
        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }
}
