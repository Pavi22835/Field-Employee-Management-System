namespace FEMS.Application.Common.Interfaces;

public record StoredFile(string StoragePath, string FileHash, long SizeBytes);

/// <summary>
/// Section 5.4/11: files (photos, documents) are stored on the on-prem server's disk,
/// not a cloud bucket. Implementation lives in Infrastructure and writes under a
/// configurable root directory outside the web root.
/// </summary>
public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(Stream content, string fileName, string subfolder, CancellationToken ct = default);

    /// <summary>Opens a previously-saved file for reading, by the relative path returned in <see cref="StoredFile"/>.</summary>
    Stream OpenRead(string relativePath);
}
