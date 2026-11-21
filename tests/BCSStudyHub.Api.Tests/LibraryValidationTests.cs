using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Library;
using BCSStudyHub.Domain.Entities;

namespace BCSStudyHub.Api.Tests;

public sealed class LibraryValidationTests
{
    [Fact]
    public void ExternalLinksRequireASafeUrl()
    {
        Assert.False(IsValid(new LibraryResourceInput { Kind = "ExternalLink", Title = "Budget speech" }));
        Assert.False(IsValid(new LibraryResourceInput { Kind = "ExternalLink", Title = "x", Url = "javascript:alert(1)" }));
        Assert.True(IsValid(new LibraryResourceInput { Kind = "ExternalLink", Title = "x", Url = "https://mof.gov.bd/budget" }));
    }

    [Fact]
    public void NotesRequireContent()
    {
        Assert.False(IsValid(new LibraryResourceInput { Kind = "Note", Title = "Constitution articles" }));
        Assert.True(IsValid(new LibraryResourceInput { Kind = "Note", Title = "Constitution articles", NoteContent = "Article 7" }));
    }

    [Fact]
    public void BookDetailsAreOnlyAcceptedForBooksAndIsbnMustBeValid()
    {
        Assert.False(IsValid(new LibraryResourceInput { Kind = "File", Title = "x", Book = new LibraryBookInput() }));
        Assert.False(IsValid(new LibraryResourceInput { Kind = "Book", Title = "x", Book = new LibraryBookInput { Isbn = "978-0-306-40615-8" } }));
        Assert.True(IsValid(new LibraryResourceInput { Kind = "Book", Title = "x", Book = new LibraryBookInput { Isbn = "978-0-306-40615-7" } }));
    }

    [Theory]
    [InlineData("Kind", "Video")]
    [InlineData("Kind", "1")]
    [InlineData("Category", "Gossip")]
    [InlineData("RightsBasis", "Pirated")]
    [InlineData("SourceType", "Rumour")]
    public void RejectsUnsupportedOrNumericEnumValues(string field, string value)
    {
        var input = new LibraryResourceInput { Kind = "File", Title = "x" };
        input = field switch
        {
            "Kind" => input with { Kind = value },
            "Category" => input with { Category = value },
            "RightsBasis" => input with { RightsBasis = value },
            _ => input with { SourceType = value },
        };

        Assert.False(IsValid(input));
    }

    [Fact]
    public void RejectsDuplicateMappingsAndTags()
    {
        var topic = Guid.NewGuid();
        Assert.False(IsValid(new LibraryResourceInput { Kind = "File", Title = "x", SyllabusItemIds = [topic, topic] }));
        Assert.False(IsValid(new LibraryResourceInput { Kind = "File", Title = "x", SubjectIds = [Guid.Empty] }));
        Assert.False(IsValid(new LibraryResourceInput { Kind = "File", Title = "x", Tags = ["GK", "gk"] }));
    }

    [Fact]
    public void ChapterPagesMustBeOrdered()
    {
        Assert.False(IsValid(new LibraryChapterInput { ChapterNumber = 1, Title = "Intro", StartPage = 20, EndPage = 10 }));
        Assert.False(IsValid(new LibraryChapterInput { ChapterNumber = 0, Title = "Intro" }));
        Assert.True(IsValid(new LibraryChapterInput { ChapterNumber = 1, Title = "Intro", StartPage = 1, EndPage = 10 }));
    }

    [Fact]
    public void ProgressCannotPassTheLastPage()
    {
        Assert.False(IsValid(new LibraryProgressInput { Status = "Reading", CurrentPage = 300, TotalPages = 200 }));
        Assert.False(IsValid(new LibraryProgressInput { Status = "Skimmed" }));
        Assert.True(IsValid(new LibraryProgressInput { Status = "Reading", CurrentPage = 50, TotalPages = 200 }));
    }

    [Fact]
    public void ProgressPercentIsDerivedFromPagesAndForcedOnCompletion()
    {
        var input = new LibraryProgressInput { Status = "Reading", CurrentPage = 50, TotalPages = 200 };

        Assert.Equal(25, input.ResolvePercent(LibraryReadingStatus.Reading));
        Assert.Equal(100, input.ResolvePercent(LibraryReadingStatus.Completed));
        Assert.Equal(40, (input with { PercentComplete = 40 }).ResolvePercent(LibraryReadingStatus.Reading));
    }

    private static bool IsValid(object instance)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }
}
