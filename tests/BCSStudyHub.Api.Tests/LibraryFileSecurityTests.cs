using System.IO.Compression;
using System.Text;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Library;

namespace BCSStudyHub.Api.Tests;

public sealed class LibraryFileSecurityTests
{
    [Theory]
    [InlineData("notes.pdf", LibraryFileFormat.Pdf, "application/pdf")]
    [InlineData("MAP.PNG", LibraryFileFormat.Image, "image/png")]
    [InlineData("scan.jpeg", LibraryFileFormat.Image, "image/jpeg")]
    [InlineData("chart.webp", LibraryFileFormat.Image, "image/webp")]
    [InlineData("diagram.gif", LibraryFileFormat.Image, "image/gif")]
    public void AcceptsAllowlistedFilesWhoseContentMatchesTheExtension(string name, LibraryFileFormat format, string contentType)
    {
        using var stream = new MemoryStream(SampleFor(name));

        var result = LibraryFileRules.Inspect(name, stream);

        Assert.True(result.IsValid, result.Error);
        Assert.Equal(format, result.Format);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void RejectsExecutableRenamedAsPdf()
    {
        using var stream = new MemoryStream([0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00]);

        var result = LibraryFileRules.Inspect("syllabus.pdf", stream);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RejectsPdfContentRenamedAsImage()
    {
        using var stream = new MemoryStream(SampleFor("x.pdf"));

        Assert.False(LibraryFileRules.Inspect("photo.png", stream).IsValid);
    }

    [Theory]
    [InlineData("logo.svg")]
    [InlineData("page.html")]
    [InlineData("tool.exe")]
    [InlineData("legacy.doc")]
    [InlineData("macro.docm")]
    [InlineData("noextension")]
    public void RejectsActiveOrUnsupportedTypes(string name)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<svg onload=alert(1)>"));

        Assert.False(LibraryFileRules.Inspect(name, stream).IsValid);
    }

    [Fact]
    public void RejectsEmptyFiles()
    {
        using var stream = new MemoryStream();

        Assert.False(LibraryFileRules.Inspect("empty.pdf", stream).IsValid);
    }

    [Fact]
    public void AcceptsWellFormedOfficeDocument()
    {
        using var stream = OfficeDocument("word/document.xml");

        var result = LibraryFileRules.Inspect("essay.docx", stream);

        Assert.True(result.IsValid, result.Error);
        Assert.Equal(LibraryFileFormat.Document, result.Format);
        Assert.False(LibraryFileRules.CanPreviewInline(result.Format));
    }

    [Fact]
    public void RejectsOfficeDocumentContainingMacros()
    {
        using var stream = OfficeDocument("word/document.xml", "word/vbaProject.bin");

        Assert.False(LibraryFileRules.Inspect("essay.docx", stream).IsValid);
    }

    [Fact]
    public void RejectsArbitraryZipRenamedAsOfficeDocument()
    {
        using var stream = OfficeDocument("payload/readme.txt");

        Assert.False(LibraryFileRules.Inspect("sheet.xlsx", stream).IsValid);
    }

    [Fact]
    public void RejectsTextWithBinaryOrInvalidUtf8()
    {
        using var withNul = new MemoryStream([0x41, 0x00, 0x42]);
        using var invalidUtf8 = new MemoryStream([0x41, 0xC3, 0x28]);
        using var bangla = new MemoryStream(Encoding.UTF8.GetBytes("বাংলাদেশের সংবিধান"));

        Assert.False(LibraryFileRules.Inspect("a.txt", withNul).IsValid);
        Assert.False(LibraryFileRules.Inspect("a.txt", invalidUtf8).IsValid);
        Assert.True(LibraryFileRules.Inspect("a.md", bangla).IsValid);
    }

    [Theory]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("C:\\Users\\me\\Desktop\\notes.pdf", "notes.pdf")]
    [InlineData("bad\u0000na<me>?.pdf", "badname.pdf")]
    [InlineData("   ", "file")]
    [InlineData("...", "file")]
    public void SanitizesDisplayFileNames(string input, string expected)
    {
        Assert.Equal(expected, LibraryFileRules.SanitizeFileName(input));
    }

    [Fact]
    public void TruncatesLongFileNamesButKeepsTheExtension()
    {
        var name = LibraryFileRules.SanitizeFileName(new string('a', 500) + ".pdf");

        Assert.Equal(LibraryFileRules.MaxFileNameLength, name.Length);
        Assert.EndsWith(".pdf", name);
    }

    [Theory]
    [InlineData("../../../windows/system32")]
    [InlineData("2026/11/not-a-guid")]
    [InlineData("2026/11/0123456789abcdef0123456789abcdef/../../x")]
    [InlineData("/2026/11/0123456789abcdef0123456789abcdef")]
    [InlineData("")]
    public void LocalStorageRejectsKeysOutsideTheGeneratedPattern(string key)
    {
        var storage = new LocalFileStorage(Path.Combine(Path.GetTempPath(), "bcs-library-tests"));

        Assert.Throws<ArgumentException>(() => storage.ResolvePath(key));
    }

    [Fact]
    public async Task LocalStorageRoundTripsAndDeletesFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "bcs-library-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorage(root);
        try
        {
            var key = storage.CreateKey();
            await storage.SaveAsync(key, new MemoryStream([1, 2, 3]), CancellationToken.None);

            await using (var stream = await storage.OpenReadAsync(key, CancellationToken.None))
            {
                Assert.NotNull(stream);
                var copy = new MemoryStream();
                await stream.CopyToAsync(copy);
                Assert.Equal([1, 2, 3], copy.ToArray());
            }

            await Assert.ThrowsAsync<IOException>(() => storage.SaveAsync(key, new MemoryStream([9]), CancellationToken.None));
            await storage.DeleteAsync(key, CancellationToken.None);
            Assert.Null(await storage.OpenReadAsync(key, CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    internal static byte[] SampleFor(string name) =>
        Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".pdf" => Encoding.ASCII.GetBytes("%PDF-1.7\n%test\n"),
            ".png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D],
            ".jpg" or ".jpeg" => [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10],
            ".webp" => [.. "RIFF"u8.ToArray(), 0x24, 0, 0, 0, .. "WEBPVP8 "u8.ToArray()],
            ".gif" => [.. "GIF89a"u8.ToArray(), 1, 0, 1, 0],
            _ => Encoding.UTF8.GetBytes("plain text"),
        };

    internal static MemoryStream OfficeDocument(params string[] parts)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var part in new[] { "[Content_Types].xml" }.Concat(parts))
            {
                using var writer = new StreamWriter(archive.CreateEntry(part).Open());
                writer.Write("<xml/>");
            }
        }

        stream.Position = 0;
        return stream;
    }
}

public sealed class LibraryRulesTests
{
    [Theory]
    [InlineData("978-0-306-40615-7", true)]
    [InlineData("0-306-40615-2", true)]
    [InlineData("080442957X", true)]
    [InlineData("978-0-306-40615-8", false)]
    [InlineData("0-306-40615-3", false)]
    [InlineData("12345", false)]
    [InlineData("97803064061X7", false)]
    public void ValidatesIsbnCheckDigits(string isbn, bool expected)
    {
        Assert.Equal(expected, LibraryRules.IsValidIsbn(isbn));
    }

    [Theory]
    [InlineData("https://bpsc.gov.bd/notice", true)]
    [InlineData("http://www.bangladesh.gov.bd", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("ftp://files.example.org/a.pdf", false)]
    [InlineData("https://user:secret@example.org/", false)]
    [InlineData("/relative/path", false)]
    [InlineData("", false)]
    public void AcceptsOnlyCredentialFreeWebUrls(string url, bool expected)
    {
        Assert.Equal(expected, LibraryRules.IsSafeWebUrl(url));
    }

    [Fact]
    public void PersonalCopiesAreAllowedOnlyInPrivateResources()
    {
        Assert.Null(LibraryRules.ValidateFileRights(LibraryVisibility.Private, LibraryRightsBasis.PersonalCopy, null));
        Assert.NotNull(LibraryRules.ValidateFileRights(LibraryVisibility.Shared, LibraryRightsBasis.PersonalCopy, null));
    }

    [Fact]
    public void FilesRequireADeclaredRightsBasis()
    {
        Assert.NotNull(LibraryRules.ValidateFileRights(LibraryVisibility.Private, LibraryRightsBasis.NotApplicable, null));
        Assert.NotNull(LibraryRules.ValidateFileRights(LibraryVisibility.Shared, LibraryRightsBasis.NotApplicable, null));
    }

    [Theory]
    [InlineData(LibraryRightsBasis.GovernmentPublication)]
    [InlineData(LibraryRightsBasis.OpenLicense)]
    [InlineData(LibraryRightsBasis.PermissionGranted)]
    public void SharedLicensedFilesRequireAJustification(LibraryRightsBasis basis)
    {
        Assert.NotNull(LibraryRules.ValidateFileRights(LibraryVisibility.Shared, basis, " "));
        Assert.Null(LibraryRules.ValidateFileRights(LibraryVisibility.Shared, basis, "CC BY 4.0 per publisher page"));
    }

    [Fact]
    public void OwnWorkAndPublicDomainCanBeSharedWithoutANote()
    {
        Assert.Null(LibraryRules.ValidateFileRights(LibraryVisibility.Shared, LibraryRightsBasis.OwnWork, null));
        Assert.Null(LibraryRules.ValidateFileRights(LibraryVisibility.Shared, LibraryRightsBasis.PublicDomain, null));
    }

    [Fact]
    public void TagsMustBeUniqueIgnoringCase()
    {
        Assert.False(LibraryRules.TryNormalizeTags(["Economy", "economy"], out _));
        Assert.False(LibraryRules.TryNormalizeTags([" "], out _));
        Assert.True(LibraryRules.TryNormalizeTags([" Economy ", "Budget"], out var tags));
        Assert.Equal(["Economy", "Budget"], tags);
    }
}
