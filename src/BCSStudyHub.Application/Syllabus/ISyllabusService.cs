using BCSStudyHub.Application.Common;

namespace BCSStudyHub.Application.Syllabus;

public interface ISyllabusService
{
    Task<IReadOnlyList<ExamStageDto>> GetStagesAsync(CancellationToken cancellationToken);
    Task<PagedResponse<SyllabusVersionDto>> GetVersionsAsync(
        SyllabusVersionQuery query,
        bool includeUnpublished,
        CancellationToken cancellationToken);
    Task<PagedResponse<SubjectDto>> GetSubjectsAsync(
        string? search,
        bool includeInactive,
        int pageNumber,
        int pageSize,
        string sortBy,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<PaperDto>> GetPapersAsync(
        Guid syllabusSubjectId,
        bool includeUnpublished,
        CancellationToken cancellationToken);
    Task<SyllabusTreeDto?> GetTreeAsync(Guid versionId, Guid? userId, bool includeUnpublished, CancellationToken cancellationToken);
    Task<PagedResponse<SyllabusSearchHitDto>> SearchAsync(
        SyllabusSearchQuery query,
        Guid? userId,
        CancellationToken cancellationToken);
    Task<PagedResponse<SyllabusItemDto>> GetItemsAsync(
        SyllabusItemQuery query,
        bool includeUnpublished,
        CancellationToken cancellationToken);
    Task<TopicProgressDto> GetProgressAsync(Guid userId, Guid versionId, CancellationToken cancellationToken);
    Task<bool> SetProgressAsync(Guid userId, Guid itemId, bool isCompleted, CancellationToken cancellationToken);

    Task UpdateStageAsync(Guid id, UpdateExamStageRequest request, CancellationToken cancellationToken);
    Task<SyllabusVersionDto> CreateVersionAsync(Guid stageId, CreateSyllabusVersionRequest request, CancellationToken cancellationToken);
    Task<SyllabusVersionDto?> UpdateVersionAsync(Guid id, UpdateSyllabusVersionRequest request, CancellationToken cancellationToken);
    Task<SyllabusVersionDto?> SetVersionStatusAsync(Guid id, string status, CancellationToken cancellationToken);
    Task<SubjectDto> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken cancellationToken);
    Task<SubjectDto?> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request, CancellationToken cancellationToken);
    Task<SyllabusSubjectDto> AssignSubjectAsync(Guid versionId, AssignSubjectRequest request, CancellationToken cancellationToken);
    Task<SyllabusSubjectDto?> UpdateSyllabusSubjectAsync(Guid id, UpdateSyllabusSubjectRequest request, CancellationToken cancellationToken);
    Task<PaperDto> CreatePaperAsync(Guid syllabusSubjectId, CreatePaperRequest request, CancellationToken cancellationToken);
    Task<PaperDto?> UpdatePaperAsync(Guid id, UpdatePaperRequest request, CancellationToken cancellationToken);
    Task<bool> DeletePaperAsync(Guid id, CancellationToken cancellationToken);
    Task<SyllabusItemDto> CreateItemAsync(
        SyllabusItemKindRequest kind,
        CreateSyllabusItemRequest request,
        CancellationToken cancellationToken);
    Task<SyllabusItemDto?> UpdateItemAsync(
        Guid id,
        UpdateSyllabusItemRequest request,
        CancellationToken cancellationToken);
    Task<bool> DeleteItemAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<SyllabusSourceDto>> GetSourcesAsync(Guid versionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SyllabusSourceDto>> GetItemSourcesAsync(Guid itemId, CancellationToken cancellationToken);
    Task<SyllabusSourceDto?> GetSourceAsync(Guid id, CancellationToken cancellationToken);
    Task<SyllabusSourceDto> CreateSourceAsync(CreateSyllabusSourceRequest request, CancellationToken cancellationToken);
    Task<SyllabusSourceDto?> UpdateSourceAsync(Guid id, UpdateSyllabusSourceRequest request, CancellationToken cancellationToken);
    Task<bool> LinkVersionSourceAsync(Guid versionId, Guid sourceId, LinkSyllabusSourceRequest request, CancellationToken cancellationToken);
    Task<bool> UnlinkVersionSourceAsync(Guid versionId, Guid sourceId, CancellationToken cancellationToken);
    Task<bool> LinkItemSourceAsync(Guid itemId, Guid sourceId, CancellationToken cancellationToken);
    Task<bool> UnlinkItemSourceAsync(Guid itemId, Guid sourceId, CancellationToken cancellationToken);
}

public enum SyllabusItemKindRequest
{
    Topic,
    Subtopic,
}
