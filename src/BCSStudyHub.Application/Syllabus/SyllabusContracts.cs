using System.ComponentModel.DataAnnotations;
using BCSStudyHub.Application.Common;

namespace BCSStudyHub.Application.Syllabus;

public sealed record ExamStageDto(
    Guid Id,
    string Code,
    string Name,
    string LocalizedName,
    int SortOrder,
    bool IsActive);

public sealed record SyllabusVersionDto(
    Guid Id,
    Guid ExamStageId,
    string StageCode,
    string StageName,
    string VersionLabel,
    string TrackCode,
    string Summary,
    string Coverage,
    string Status,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    DateTimeOffset? PublishedAt,
    int SubjectCount,
    int SourceCount);

public sealed record SubjectDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive);

public sealed record PaperDto(
    Guid Id,
    Guid SyllabusSubjectId,
    string Code,
    string Name,
    int SortOrder,
    int? MaxMarks);

public sealed record SyllabusItemDto(
    Guid Id,
    Guid SyllabusSubjectId,
    Guid SubjectId,
    Guid? PaperId,
    Guid? ParentId,
    string Kind,
    string Title,
    string? Description,
    int SortOrder,
    string VerificationStatus,
    bool? IsCompleted,
    IReadOnlyList<SyllabusItemDto> Children);

public sealed record SyllabusSubjectDto(
    Guid Id,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    int SortOrder,
    int? Marks,
    bool IsRequired,
    IReadOnlyList<PaperDto> Papers,
    IReadOnlyList<SyllabusItemDto> Items);

public sealed record SyllabusSourceDto(
    Guid Id,
    string Title,
    string Publisher,
    string Url,
    string SourceType,
    DateOnly? PublishedOn,
    DateOnly? LastVerifiedOn,
    string VerificationStatus,
    string? Notes);

public sealed record SyllabusTreeDto(
    SyllabusVersionDto Version,
    IReadOnlyList<SyllabusSubjectDto> Subjects,
    IReadOnlyList<SyllabusSourceDto> Sources);

public sealed record SyllabusSearchHitDto(
    Guid Id,
    Guid VersionId,
    string StageCode,
    string StageName,
    string VersionLabel,
    Guid SubjectId,
    string SubjectName,
    string Kind,
    string Title,
    string? Description,
    string VerificationStatus);

public sealed record TopicProgressDto(
    Guid VersionId,
    int TotalItems,
    int CompletedItems,
    IReadOnlyDictionary<Guid, bool> Items);

public sealed record SyllabusVersionQuery(
    string? StageCode,
    string? TrackCode,
    string? Status,
    int PageNumber = 1,
    int PageSize = 20,
    string SortBy = "stage");

public sealed record SyllabusSearchQuery(
    string Query,
    string? StageCode,
    Guid? VersionId,
    Guid? SubjectId,
    string? Kind,
    int PageNumber = 1,
    int PageSize = 20,
    string SortBy = "title");

public sealed record SyllabusItemQuery(
    Guid VersionId,
    Guid? SubjectId,
    Guid? ParentId,
    string? Kind,
    int PageNumber = 1,
    int PageSize = 50);

public sealed record CreateSyllabusVersionRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string VersionLabel,
    [property: Required, StringLength(40, MinimumLength = 2)] string TrackCode,
    [property: Required, StringLength(2000, MinimumLength = 1)] string Summary,
    [property: Required, RegularExpression("^(OutlineOnly|FullTopics)$")] string Coverage,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record UpdateSyllabusVersionRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string VersionLabel,
    [property: Required, StringLength(40, MinimumLength = 2)] string TrackCode,
    [property: Required, StringLength(2000, MinimumLength = 1)] string Summary,
    [property: Required, RegularExpression("^(OutlineOnly|FullTopics)$")] string Coverage,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record UpdateExamStageRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    [property: Required, StringLength(160, MinimumLength = 2)] string LocalizedName,
    [property: Range(0, 1000)] int SortOrder,
    bool IsActive);

public sealed record CreateSubjectRequest(
    [property: Required, StringLength(60, MinimumLength = 2)] string Code,
    [property: Required, StringLength(240, MinimumLength = 2)] string Name,
    [property: StringLength(2000)] string? Description);

public sealed record UpdateSubjectRequest(
    [property: Required, StringLength(240, MinimumLength = 2)] string Name,
    [property: StringLength(2000)] string? Description,
    bool IsActive);

public sealed record AssignSubjectRequest(
    [property: Required] Guid SubjectId,
    [property: Range(0, 1000)] int SortOrder,
    [property: Range(0, 10000)] int? Marks,
    bool IsRequired);

public sealed record UpdateSyllabusSubjectRequest(
    [property: Range(0, 1000)] int SortOrder,
    [property: Range(0, 10000)] int? Marks,
    bool IsRequired);

public sealed record CreatePaperRequest(
    [property: Required, StringLength(60, MinimumLength = 2)] string Code,
    [property: Required, StringLength(240, MinimumLength = 2)] string Name,
    [property: Range(0, 1000)] int SortOrder,
    [property: Range(0, 10000)] int? MaxMarks);

public sealed record UpdatePaperRequest(
    [property: Required, StringLength(240, MinimumLength = 2)] string Name,
    [property: Range(0, 1000)] int SortOrder,
    [property: Range(0, 10000)] int? MaxMarks);

public sealed record CreateSyllabusItemRequest(
    [property: Required] Guid SyllabusSubjectId,
    Guid? PaperId,
    Guid? ParentId,
    [property: Required, StringLength(300, MinimumLength = 1)] string Title,
    [property: StringLength(4000)] string? Description,
    [property: Range(0, 10000)] int SortOrder,
    [property: Required, RegularExpression("^(Unverified|Verified|NeedsReview)$")] string VerificationStatus);

public sealed record UpdateSyllabusItemRequest(
    Guid? PaperId,
    Guid? ParentId,
    [property: Required, StringLength(300, MinimumLength = 1)] string Title,
    [property: StringLength(4000)] string? Description,
    [property: Range(0, 10000)] int SortOrder,
    [property: Required, RegularExpression("^(Unverified|Verified|NeedsReview)$")] string VerificationStatus);

public sealed record CreateSyllabusSourceRequest(
    [property: Required, StringLength(300, MinimumLength = 2)] string Title,
    [property: Required, StringLength(180, MinimumLength = 2)] string Publisher,
    [property: Required, Url, StringLength(512)] string Url,
    [property: Required, StringLength(40, MinimumLength = 2)] string SourceType,
    DateOnly? PublishedOn,
    DateOnly? LastVerifiedOn,
    [property: Required, RegularExpression("^(Unverified|Verified|Superseded)$")] string VerificationStatus,
    [property: StringLength(2000)] string? Notes);

public sealed record UpdateSyllabusSourceRequest(
    [property: Required, StringLength(300, MinimumLength = 2)] string Title,
    [property: Required, StringLength(180, MinimumLength = 2)] string Publisher,
    [property: Required, Url, StringLength(512)] string Url,
    [property: Required, StringLength(40, MinimumLength = 2)] string SourceType,
    DateOnly? PublishedOn,
    DateOnly? LastVerifiedOn,
    [property: Required, RegularExpression("^(Unverified|Verified|Superseded)$")] string VerificationStatus,
    [property: StringLength(2000)] string? Notes);

public sealed record LinkSyllabusSourceRequest([property: StringLength(1000)] string? Notes);

public sealed record SetTopicProgressRequest(bool IsCompleted);

public sealed record SyllabusItemPage(PagedResponse<SyllabusItemDto> Items);
