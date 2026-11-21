using System.IO.Compression;
using System.Text;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Application.Library;

public sealed record FileInspectionResult(
    bool IsValid,
    LibraryFileFormat Format,
    string ContentType,
    string Extension,
    string? Error)
{
    public static FileInspectionResult Reject(string error) =>
        new(false, default, string.Empty, string.Empty, error);
}

/// <summary>
/// Validates uploads against an allowlist using the file's actual content, not the client-declared
/// MIME type. Active content (SVG, HTML, macro-enabled or legacy Office formats, executables) is rejected.
/// </summary>
public static class LibraryFileRules
{
    /// <summary>Hard ceiling for request bodies on upload endpoints. Configured limits must not exceed it.</summary>
    public const long AbsoluteMaxUploadBytes = 100L * 1024 * 1024;
    public const long MaxTextFileBytes = 5L * 1024 * 1024;
    public const int MaxFileNameLength = 200;
    private const long MaxOfficeUncompressedBytes = 512L * 1024 * 1024;
    private const int MaxOfficeEntries = 10_000;

    private sealed record AllowedType(LibraryFileFormat Format, string ContentType);

    private static readonly IReadOnlyDictionary<string, AllowedType> Allowed =
        new Dictionary<string, AllowedType>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new(LibraryFileFormat.Pdf, "application/pdf"),
            [".png"] = new(LibraryFileFormat.Image, "image/png"),
            [".jpg"] = new(LibraryFileFormat.Image, "image/jpeg"),
            [".jpeg"] = new(LibraryFileFormat.Image, "image/jpeg"),
            [".webp"] = new(LibraryFileFormat.Image, "image/webp"),
            [".gif"] = new(LibraryFileFormat.Image, "image/gif"),
            [".docx"] = new(LibraryFileFormat.Document, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            [".xlsx"] = new(LibraryFileFormat.Document, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            [".pptx"] = new(LibraryFileFormat.Document, "application/vnd.openxmlformats-officedocument.presentationml.presentation"),
            [".txt"] = new(LibraryFileFormat.Text, "text/plain; charset=utf-8"),
            [".md"] = new(LibraryFileFormat.Text, "text/markdown; charset=utf-8"),
            [".csv"] = new(LibraryFileFormat.Text, "text/csv; charset=utf-8"),
        };

    public static IReadOnlyCollection<string> AllowedExtensions { get; } =
        Allowed.Keys.OrderBy(extension => extension, StringComparer.Ordinal).ToArray();

    /// <summary>PDFs, images and UTF-8 text can be previewed inline; Office documents are download-only.</summary>
    public static bool CanPreviewInline(LibraryFileFormat format) =>
        format is LibraryFileFormat.Pdf or LibraryFileFormat.Image or LibraryFileFormat.Text;

    /// <summary>
    /// Inspects a seekable stream. The stream position is restored to zero afterwards.
    /// </summary>
    public static FileInspectionResult Inspect(string fileName, Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanSeek || !content.CanRead)
        {
            throw new ArgumentException("File inspection requires a readable, seekable stream.", nameof(content));
        }

        var extension = Path.GetExtension(SanitizeFileName(fileName));
        if (string.IsNullOrEmpty(extension) || !Allowed.TryGetValue(extension, out var allowed))
        {
            return FileInspectionResult.Reject(
                $"This file type is not supported. Allowed types: {string.Join(", ", AllowedExtensions)}.");
        }

        if (content.Length == 0)
        {
            return FileInspectionResult.Reject("The uploaded file is empty.");
        }

        try
        {
            var error = allowed.Format switch
            {
                LibraryFileFormat.Pdf => CheckSignature(content, extension),
                LibraryFileFormat.Image => CheckSignature(content, extension),
                LibraryFileFormat.Document => CheckOfficeDocument(content, extension),
                LibraryFileFormat.Text => CheckText(content),
                _ => "This file type is not supported.",
            };
            return error is null
                ? new FileInspectionResult(true, allowed.Format, allowed.ContentType, extension.ToLowerInvariant(), null)
                : FileInspectionResult.Reject(error);
        }
        finally
        {
            content.Position = 0;
        }
    }

    /// <summary>
    /// Produces a display-safe file name: strips directories, control and reserved characters, and limits length.
    /// The result is metadata only; it is never used as a storage path.
    /// </summary>
    public static string SanitizeFileName(string? fileName)
    {
        var name = (fileName ?? string.Empty).Replace('\\', '/');
        name = name[(name.LastIndexOf('/') + 1)..];
        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            if (char.IsControl(character) || character is '<' or '>' or ':' or '"' or '|' or '?' or '*' or '/')
            {
                continue;
            }

            builder.Append(character);
        }

        name = builder.ToString().Trim().Trim('.').Trim();
        if (name.Length > MaxFileNameLength)
        {
            var extension = Path.GetExtension(name);
            if (extension.Length > 16)
            {
                extension = string.Empty;
            }

            name = name[..(MaxFileNameLength - extension.Length)].TrimEnd() + extension;
        }

        return string.IsNullOrEmpty(name) ? "file" : name;
    }

    private static string? CheckSignature(Stream content, string extension)
    {
        Span<byte> header = stackalloc byte[16];
        content.Position = 0;
        var read = content.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        header = header[..read];
        var matches = extension.ToLowerInvariant() switch
        {
            ".pdf" => header.StartsWith("%PDF-"u8),
            ".png" => header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
            ".gif" => header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8),
            ".webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            _ => false,
        };
        return matches ? null : "The file content does not match its extension.";
    }

    private static string? CheckOfficeDocument(Stream content, string extension)
    {
        Span<byte> header = stackalloc byte[4];
        content.Position = 0;
        if (content.ReadAtLeast(header, 4, throwOnEndOfStream: false) < 4 ||
            !header.SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
        {
            return "The file content does not match its extension.";
        }

        content.Position = 0;
        var requiredPart = extension.ToLowerInvariant() switch
        {
            ".docx" => "word/document.xml",
            ".xlsx" => "xl/workbook.xml",
            ".pptx" => "ppt/presentation.xml",
            _ => null,
        };
        if (requiredPart is null)
        {
            return "This file type is not supported.";
        }

        try
        {
            using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count > MaxOfficeEntries)
            {
                return "The document contains too many internal parts.";
            }

            long uncompressed = 0;
            var hasContentTypes = false;
            var hasRequiredPart = false;
            foreach (var entry in archive.Entries)
            {
                uncompressed += entry.Length;
                if (uncompressed > MaxOfficeUncompressedBytes)
                {
                    return "The document expands beyond the supported size.";
                }

                var name = entry.FullName.Replace('\\', '/');
                if (name.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("/activeX/", StringComparison.OrdinalIgnoreCase))
                {
                    return "Documents containing macros or ActiveX controls are not accepted.";
                }

                hasContentTypes |= string.Equals(name, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase);
                hasRequiredPart |= string.Equals(name, requiredPart, StringComparison.OrdinalIgnoreCase);
            }

            return hasContentTypes && hasRequiredPart
                ? null
                : "The file content does not match its extension.";
        }
        catch (InvalidDataException)
        {
            return "The document is damaged or is not a valid Office file.";
        }
    }

    private static string? CheckText(Stream content)
    {
        if (content.Length > MaxTextFileBytes)
        {
            return $"Text files cannot exceed {MaxTextFileBytes / (1024 * 1024)} MB.";
        }

        content.Position = 0;
        var bytes = new byte[content.Length];
        content.ReadExactly(bytes);
        if (Array.IndexOf(bytes, (byte)0) >= 0)
        {
            return "Text files must not contain binary data.";
        }

        try
        {
            _ = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
            return null;
        }
        catch (DecoderFallbackException)
        {
            return "Text files must be UTF-8 encoded.";
        }
    }
}
