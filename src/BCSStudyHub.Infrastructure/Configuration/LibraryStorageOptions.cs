using BCSStudyHub.Application.Library;

namespace BCSStudyHub.Infrastructure.Configuration;

public sealed class LibraryStorageOptions
{
    public const string SectionName = "LibraryStorage";

    /// <summary>
    /// Root directory for the local provider. Relative paths resolve against the API content root.
    /// It must not be served as static content.
    /// </summary>
    public string RootPath { get; set; } = "App_Data/library-files";

    public long MaxFileSizeBytes { get; set; } = 50L * 1024 * 1024;

    /// <summary>Total bytes a user may store in private resources.</summary>
    public long UserQuotaBytes { get; set; } = 2L * 1024 * 1024 * 1024;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RootPath))
        {
            throw new InvalidOperationException("LibraryStorage:RootPath is required.");
        }

        if (MaxFileSizeBytes <= 0 || MaxFileSizeBytes > LibraryFileRules.AbsoluteMaxUploadBytes)
        {
            throw new InvalidOperationException(
                $"LibraryStorage:MaxFileSizeBytes must be between 1 and {LibraryFileRules.AbsoluteMaxUploadBytes}.");
        }

        if (UserQuotaBytes < MaxFileSizeBytes)
        {
            throw new InvalidOperationException("LibraryStorage:UserQuotaBytes must be at least the maximum file size.");
        }
    }
}
