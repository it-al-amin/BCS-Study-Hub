using BCSStudyHub.Application.Common;
using BCSStudyHub.Application.Syllabus;
using BCSStudyHub.Domain.Entities;
using BCSStudyHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BCSStudyHub.Infrastructure.Syllabus;

internal sealed class SyllabusService(
    ApplicationDbContext dbContext,
    IHttpContextAccessor httpContextAccessor) : ISyllabusService
{
    private const int MinimumSearchLength = 2;

    public async Task<IReadOnlyList<ExamStageDto>> GetStagesAsync(CancellationToken cancellationToken) =>
        await dbContext.ExamStages.AsNoTracking()
            .Where(stage => stage.IsActive)
            .OrderBy(stage => stage.SortOrder)
            .Select(stage => new ExamStageDto(
                stage.Id,
                stage.Code,
                stage.Name,
                stage.LocalizedName,
                stage.SortOrder,
                stage.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<PagedResponse<SyllabusVersionDto>> GetVersionsAsync(
        SyllabusVersionQuery query,
        bool includeUnpublished,
        CancellationToken cancellationToken)
    {
        var versions = dbContext.SyllabusVersions.AsNoTracking()
            .Include(version => version.ExamStage)
            .AsQueryable();

        if (!includeUnpublished)
        {
            versions = versions.Where(version => version.Status == SyllabusVersionStatus.Published);
        }

        if (!string.IsNullOrWhiteSpace(query.StageCode))
        {
            var stageCode = query.StageCode.Trim();
            versions = versions.Where(version => version.ExamStage.Code == stageCode);
        }

        if (!string.IsNullOrWhiteSpace(query.TrackCode))
        {
            var trackCode = query.TrackCode.Trim();
            versions = versions.Where(version => version.TrackCode == trackCode);
        }

        if (includeUnpublished && !string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<SyllabusVersionStatus>(query.Status, true, out var requestedStatus))
            {
                throw Invalid("The requested syllabus status is not supported.");
            }

            versions = versions.Where(version => version.Status == requestedStatus);
        }

        versions = query.SortBy.ToLowerInvariant() switch
        {
            "updated" => versions.OrderByDescending(version => version.UpdatedAt),
            "label" => versions.OrderBy(version => version.VersionLabel),
            "status" => versions.OrderBy(version => version.Status),
            "stage" => versions.OrderBy(version => version.ExamStage.SortOrder)
                .ThenBy(version => version.TrackCode)
                .ThenByDescending(version => version.UpdatedAt),
            _ => throw Invalid("SortBy must be one of: stage, updated, label, status."),
        };

        var totalCount = await versions.LongCountAsync(cancellationToken);
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var rows = await versions
            .Skip(page.Offset)
            .Take(page.EffectivePageSize)
            .Select(version => new
            {
                Version = version,
                SubjectCount = version.Subjects.Count,
                SourceCount = version.SourceReferences.Count,
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<SyllabusVersionDto>(
            rows.Select(row => ToVersionDto(row.Version, row.SubjectCount, row.SourceCount)).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            totalCount);
    }

    public async Task<PagedResponse<SubjectDto>> GetSubjectsAsync(
        string? search,
        bool includeInactive,
        int pageNumber,
        int pageSize,
        string sortBy,
        CancellationToken cancellationToken)
    {
        var subjects = dbContext.Subjects.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            subjects = subjects.Where(subject => subject.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            subjects = subjects.Where(subject =>
                subject.Code.Contains(value) || subject.Name.Contains(value));
        }

        subjects = sortBy.ToLowerInvariant() switch
        {
            "code" => subjects.OrderBy(subject => subject.Code),
            "name" => subjects.OrderBy(subject => subject.Name),
            _ => throw Invalid("SortBy must be one of: code, name."),
        };

        var total = await subjects.LongCountAsync(cancellationToken);
        var page = new PageRequest(pageNumber, pageSize);
        var rows = await subjects.Skip(page.Offset).Take(page.EffectivePageSize).ToListAsync(cancellationToken);
        return new PagedResponse<SubjectDto>(
            rows.Select(ToSubjectDto).ToArray(),
            page.EffectivePageNumber,
            page.EffectivePageSize,
            total);
    }

    public async Task<IReadOnlyList<PaperDto>> GetPapersAsync(
        Guid syllabusSubjectId,
        bool includeUnpublished,
        CancellationToken cancellationToken)
    {
        var mappingExists = await dbContext.SyllabusSubjects.AnyAsync(
            mapping => mapping.Id == syllabusSubjectId &&
                       (includeUnpublished ||
                        mapping.SyllabusVersion.Status == SyllabusVersionStatus.Published),
            cancellationToken);
        if (!mappingExists)
        {
            throw NotFound("The published syllabus subject was not found.");
        }

        var papers = dbContext.Papers.AsNoTracking()
            .Where(paper => paper.SyllabusSubjectId == syllabusSubjectId);
        if (!includeUnpublished)
        {
            papers = papers.Where(paper =>
                paper.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published);
        }

        var rows = await papers
            .OrderBy(paper => paper.SortOrder)
            .ToListAsync(cancellationToken);
        return rows.Select(ToPaperDto).ToArray();
    }

    public async Task<SyllabusTreeDto?> GetTreeAsync(
        Guid versionId,
        Guid? userId,
        bool includeUnpublished,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.SyllabusVersions.AsNoTracking()
            .Include(candidate => candidate.ExamStage)
            .SingleOrDefaultAsync(candidate => candidate.Id == versionId, cancellationToken);
        if (version is null || (!includeUnpublished && version.Status != SyllabusVersionStatus.Published))
        {
            return null;
        }

        var mappings = await dbContext.SyllabusSubjects.AsNoTracking()
            .Include(mapping => mapping.Subject)
            .Include(mapping => mapping.Papers)
            .Where(mapping => mapping.SyllabusVersionId == versionId)
            .OrderBy(mapping => mapping.SortOrder)
            .ToListAsync(cancellationToken);
        var mappingIds = mappings.Select(mapping => mapping.Id).ToArray();
        var syllabusItems = await dbContext.SyllabusItems.AsNoTracking()
            .Where(item => mappingIds.Contains(item.SyllabusSubjectId))
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, bool>? progressItems = null;
        if (userId is Guid authenticatedUserId)
        {
            progressItems = await dbContext.TopicProgress.AsNoTracking()
                .Where(progress => progress.UserId == authenticatedUserId)
                .Where(progress => mappingIds.Contains(progress.SyllabusItem.SyllabusSubjectId))
                .ToDictionaryAsync(
                    progress => progress.SyllabusItemId,
                    progress => progress.IsCompleted,
                    cancellationToken);
        }

        var sourceRows = await dbContext.SyllabusVersionSources.AsNoTracking()
            .Where(mapping => mapping.SyllabusVersionId == versionId)
            .Select(mapping => mapping.SourceReference)
            .OrderBy(source => source.Title)
            .ToListAsync(cancellationToken);
        var sources = sourceRows.Select(ToSourceDto).ToArray();
        var itemsByParent = syllabusItems
            .Where(item => item.ParentId.HasValue)
            .GroupBy(item => item.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var rootItems = syllabusItems.Where(item => item.ParentId is null).ToArray();

        var subjectDtos = mappings.Select(mapping =>
        {
            return new SyllabusSubjectDto(
                mapping.Id,
                mapping.SubjectId,
                mapping.Subject.Code,
                mapping.Subject.Name,
                mapping.SortOrder,
                mapping.Marks,
                mapping.IsRequired,
                mapping.Papers.OrderBy(paper => paper.SortOrder).Select(ToPaperDto).ToArray(),
                rootItems
                    .Where(item => item.SyllabusSubjectId == mapping.Id)
                    .Select(item => ToItemDto(item, mapping.SubjectId, progressItems, itemsByParent))
                    .ToArray());
        }).ToArray();

        return new SyllabusTreeDto(
            ToVersionDto(version, mappings.Count, sources.Length),
            subjectDtos,
            sources);
    }

    public async Task<PagedResponse<SyllabusSearchHitDto>> SearchAsync(
        SyllabusSearchQuery query,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var text = query.Query.Trim();
        if (text.Length < MinimumSearchLength)
        {
            throw Invalid($"Search text must be at least {MinimumSearchLength} characters.");
        }

        var items = dbContext.SyllabusItems.AsNoTracking()
            .Where(item => item.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published)
            .Where(item => item.Title.Contains(text) || (item.Description != null && item.Description.Contains(text)));

        if (query.VersionId is Guid versionId)
        {
            items = items.Where(item => item.SyllabusSubject.SyllabusVersionId == versionId);
        }

        if (query.SubjectId is Guid subjectId)
        {
            items = items.Where(item => item.SyllabusSubject.SubjectId == subjectId);
        }

        if (!string.IsNullOrWhiteSpace(query.StageCode))
        {
            var stageCode = query.StageCode.Trim();
            items = items.Where(item => item.SyllabusSubject.SyllabusVersion.ExamStage.Code == stageCode);
        }

        if (!string.IsNullOrWhiteSpace(query.Kind))
        {
            if (!Enum.TryParse<SyllabusItemKind>(query.Kind, true, out var kind))
            {
                throw Invalid("The requested syllabus item type is not supported.");
            }

            items = items.Where(item => item.Kind == kind);
        }

        items = query.SortBy.ToLowerInvariant() switch
        {
            "subject" => items.OrderBy(item => item.SyllabusSubject.Subject.Name).ThenBy(item => item.SortOrder),
            "sequence" => items.OrderBy(item => item.SortOrder).ThenBy(item => item.Title),
            "title" => items.OrderBy(item => item.Title),
            _ => throw Invalid("SortBy must be one of: title, subject, sequence."),
        };

        var total = await items.LongCountAsync(cancellationToken);
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var hits = await items.Skip(page.Offset).Take(page.EffectivePageSize)
            .Select(item => new SyllabusSearchHitDto(
                item.Id,
                item.SyllabusSubject.SyllabusVersionId,
                item.SyllabusSubject.SyllabusVersion.ExamStage.Code,
                item.SyllabusSubject.SyllabusVersion.ExamStage.Name,
                item.SyllabusSubject.SyllabusVersion.VersionLabel,
                item.SyllabusSubject.SubjectId,
                item.SyllabusSubject.Subject.Name,
                item.Kind.ToString(),
                item.Title,
                item.Description,
                item.VerificationStatus.ToString()))
            .ToListAsync(cancellationToken);
        return new PagedResponse<SyllabusSearchHitDto>(hits, page.EffectivePageNumber, page.EffectivePageSize, total);
    }

    public async Task<PagedResponse<SyllabusItemDto>> GetItemsAsync(
        SyllabusItemQuery query,
        bool includeUnpublished,
        CancellationToken cancellationToken)
    {
        var items = dbContext.SyllabusItems.AsNoTracking()
            .Where(item => item.SyllabusSubject.SyllabusVersionId == query.VersionId);
        if (!includeUnpublished)
        {
            items = items.Where(item => item.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published);
        }

        if (query.SubjectId is Guid subjectId)
        {
            items = items.Where(item => item.SyllabusSubject.SubjectId == subjectId);
        }

        if (query.ParentId is Guid parentId)
        {
            items = items.Where(item => item.ParentId == parentId);
        }
        else
        {
            items = items.Where(item => item.ParentId == null);
        }

        if (!string.IsNullOrWhiteSpace(query.Kind))
        {
            if (!Enum.TryParse<SyllabusItemKind>(query.Kind, true, out var kind))
            {
                throw Invalid("The requested syllabus item type is not supported.");
            }

            items = items.Where(item => item.Kind == kind);
        }

        var total = await items.LongCountAsync(cancellationToken);
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var rows = await items.OrderBy(item => item.SortOrder).ThenBy(item => item.Title)
            .Skip(page.Offset).Take(page.EffectivePageSize)
            .Select(item => new
            {
                Item = item,
                item.SyllabusSubject.SubjectId,
            })
            .ToListAsync(cancellationToken);
        var result = rows.Select(row => ToFlatItemDto(row.Item, row.SubjectId)).ToArray();
        return new PagedResponse<SyllabusItemDto>(result, page.EffectivePageNumber, page.EffectivePageSize, total);
    }

    public async Task<TopicProgressDto> GetProgressAsync(
        Guid userId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.SyllabusVersions.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == versionId, cancellationToken);
        if (version is null || version.Status != SyllabusVersionStatus.Published)
        {
            throw NotFound("The published syllabus version was not found.");
        }

        var mappingIds = await dbContext.SyllabusSubjects.AsNoTracking()
            .Where(mapping => mapping.SyllabusVersionId == versionId)
            .Select(mapping => mapping.Id)
            .ToArrayAsync(cancellationToken);
        var itemIds = await dbContext.SyllabusItems.AsNoTracking()
            .Where(item => mappingIds.Contains(item.SyllabusSubjectId))
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
        var completed = await dbContext.TopicProgress.AsNoTracking()
            .Where(progress => progress.UserId == userId && itemIds.Contains(progress.SyllabusItemId))
            .ToDictionaryAsync(progress => progress.SyllabusItemId, progress => progress.IsCompleted, cancellationToken);
        return new TopicProgressDto(versionId, itemIds.Length, completed.Count(pair => pair.Value), completed);
    }

    public async Task<bool> SetProgressAsync(
        Guid userId,
        Guid itemId,
        bool isCompleted,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.SyllabusItems
            .Include(candidate => candidate.SyllabusSubject)
            .ThenInclude(mapping => mapping.SyllabusVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == itemId, cancellationToken);
        if (item is null || item.SyllabusSubject.SyllabusVersion.Status != SyllabusVersionStatus.Published)
        {
            return false;
        }

        var progress = await dbContext.TopicProgress.SingleOrDefaultAsync(
            candidate => candidate.UserId == userId && candidate.SyllabusItemId == itemId,
            cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (progress is null)
        {
            progress = new TopicProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SyllabusItemId = itemId,
            };
            dbContext.TopicProgress.Add(progress);
        }

        progress.IsCompleted = isCompleted;
        progress.CompletedAt = isCompleted ? now : null;
        progress.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task UpdateStageAsync(
        Guid id,
        UpdateExamStageRequest request,
        CancellationToken cancellationToken)
    {
        var stage = await dbContext.ExamStages.FindAsync([id], cancellationToken);
        if (stage is null)
        {
            throw NotFound("The exam stage was not found.");
        }

        stage.Name = request.Name.Trim();
        stage.LocalizedName = request.LocalizedName.Trim();
        stage.SortOrder = request.SortOrder;
        stage.IsActive = request.IsActive;
        AddAuditLog("stage.updated", nameof(ExamStage), id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SyllabusVersionDto> CreateVersionAsync(
        Guid stageId,
        CreateSyllabusVersionRequest request,
        CancellationToken cancellationToken)
    {
        var stage = await dbContext.ExamStages.SingleOrDefaultAsync(
            candidate => candidate.Id == stageId && candidate.IsActive,
            cancellationToken);
        if (stage is null)
        {
            throw NotFound("The active exam stage was not found.");
        }

        ValidateDateRange(request.EffectiveFrom, request.EffectiveTo);
        if (!Enum.TryParse<SyllabusCoverage>(request.Coverage, true, out var coverage))
        {
            throw Invalid("The syllabus coverage value is not supported.");
        }

        var trackCode = NormalizeCode(request.TrackCode);
        await EnsureVersionUniqueAsync(stageId, trackCode, request.VersionLabel, cancellationToken: cancellationToken);
        var version = new SyllabusVersion
        {
            Id = Guid.NewGuid(),
            ExamStageId = stageId,
            VersionLabel = request.VersionLabel.Trim(),
            TrackCode = trackCode,
            Summary = request.Summary.Trim(),
            Coverage = coverage,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Status = SyllabusVersionStatus.Draft,
        };
        dbContext.SyllabusVersions.Add(version);
        AddAuditLog("version.created", nameof(SyllabusVersion), version.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToVersionDto(version, 0, 0, stage);
    }

    public async Task<SyllabusVersionDto?> UpdateVersionAsync(
        Guid id,
        UpdateSyllabusVersionRequest request,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.SyllabusVersions.Include(candidate => candidate.ExamStage)
            .Include(candidate => candidate.Subjects)
            .Include(candidate => candidate.SourceReferences)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (version is null)
        {
            return null;
        }

        EnsureDraft(version);
        ValidateDateRange(request.EffectiveFrom, request.EffectiveTo);
        if (!Enum.TryParse<SyllabusCoverage>(request.Coverage, true, out var coverage))
        {
            throw Invalid("The syllabus coverage value is not supported.");
        }

        var trackCode = NormalizeCode(request.TrackCode);
        await EnsureVersionUniqueAsync(
            version.ExamStageId,
            trackCode,
            request.VersionLabel,
            id,
            cancellationToken);
        version.VersionLabel = request.VersionLabel.Trim();
        version.TrackCode = trackCode;
        version.Summary = request.Summary.Trim();
        version.Coverage = coverage;
        version.EffectiveFrom = request.EffectiveFrom;
        version.EffectiveTo = request.EffectiveTo;
        AddAuditLog("version.updated", nameof(SyllabusVersion), version.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToVersionDto(version, version.Subjects.Count, version.SourceReferences.Count);
    }

    public async Task<SyllabusVersionDto?> SetVersionStatusAsync(
        Guid id,
        string status,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.SyllabusVersions.Include(candidate => candidate.ExamStage)
            .Include(candidate => candidate.Subjects)
            .Include(candidate => candidate.SourceReferences)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (version is null)
        {
            return null;
        }

        if (!Enum.TryParse<SyllabusVersionStatus>(status, true, out var targetStatus))
        {
            throw Invalid("The requested syllabus status is not supported.");
        }

        if (version.Status == SyllabusVersionStatus.Published)
        {
            if (targetStatus != SyllabusVersionStatus.Archived)
            {
                throw Conflict("A published version is immutable. Create a new draft to make changes.");
            }
        }
        else if (version.Status == SyllabusVersionStatus.Archived || targetStatus == SyllabusVersionStatus.Draft)
        {
            throw Conflict("The requested syllabus status transition is not allowed.");
        }

        if (targetStatus == SyllabusVersionStatus.Published)
        {
            var hasVerifiedSource = await dbContext.SyllabusVersionSources
                .AnyAsync(mapping =>
                    mapping.SyllabusVersionId == id &&
                    mapping.SourceReference.VerificationStatus == SourceVerificationStatus.Verified,
                    cancellationToken);
            if (!hasVerifiedSource)
            {
                throw Invalid("Attach at least one source marked verified before publishing this syllabus version.");
            }

            if (version.Coverage == SyllabusCoverage.FullTopics)
            {
                var hasItems = await dbContext.SyllabusItems.AnyAsync(
                    item => item.SyllabusSubject.SyllabusVersionId == id,
                    cancellationToken);
                var hasUnverifiedItems = await dbContext.SyllabusItems.AnyAsync(
                    item => item.SyllabusSubject.SyllabusVersionId == id &&
                            (item.VerificationStatus != SyllabusVerificationStatus.Verified ||
                             !item.SourceReferences.Any(source =>
                                 source.SourceReference.VerificationStatus == SourceVerificationStatus.Verified)),
                    cancellationToken);
                if (!hasItems || hasUnverifiedItems)
                {
                    throw Invalid("Full topic coverage requires at least one syllabus item and a verified source for every topic and subtopic.");
                }
            }

            version.PublishedAt = DateTimeOffset.UtcNow;
        }

        version.Status = targetStatus;
        AddAuditLog($"version.{targetStatus.ToString().ToLowerInvariant()}", nameof(SyllabusVersion), version.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToVersionDto(version, version.Subjects.Count, version.SourceReferences.Count);
    }

    public async Task<SubjectDto> CreateSubjectAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var code = NormalizeCode(request.Code);
        if (await dbContext.Subjects.AnyAsync(subject => subject.Code == code, cancellationToken))
        {
            throw Conflict("A subject with this code already exists.");
        }

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            Description = NormalizeNullable(request.Description),
        };
        dbContext.Subjects.Add(subject);
        AddAuditLog("subject.created", nameof(Subject), subject.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSubjectDto(subject);
    }

    public async Task<SubjectDto?> UpdateSubjectAsync(
        Guid id,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var subject = await dbContext.Subjects.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (subject is null)
        {
            return null;
        }

        if (await dbContext.SyllabusSubjects.AnyAsync(
                mapping => mapping.SubjectId == id && mapping.SyllabusVersion.Status != SyllabusVersionStatus.Draft,
                cancellationToken))
        {
            throw Conflict("Subjects in published or archived versions are immutable. Create another subject for revised content.");
        }

        subject.Name = request.Name.Trim();
        subject.Description = NormalizeNullable(request.Description);
        subject.IsActive = request.IsActive;
        AddAuditLog("subject.updated", nameof(Subject), subject.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSubjectDto(subject);
    }

    public async Task<SyllabusSubjectDto> AssignSubjectAsync(
        Guid versionId,
        AssignSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var version = await GetDraftVersionAsync(versionId, cancellationToken);
        var subject = await dbContext.Subjects.SingleOrDefaultAsync(
            candidate => candidate.Id == request.SubjectId && candidate.IsActive,
            cancellationToken);
        if (subject is null)
        {
            throw NotFound("The active subject was not found.");
        }

        if (await dbContext.SyllabusSubjects.AnyAsync(
                mapping => mapping.SyllabusVersionId == versionId && mapping.SubjectId == request.SubjectId,
                cancellationToken))
        {
            throw Conflict("This subject is already assigned to the syllabus version.");
        }

        var mapping = new SyllabusSubject
        {
            Id = Guid.NewGuid(),
            SyllabusVersionId = version.Id,
            SubjectId = subject.Id,
            SortOrder = request.SortOrder,
            Marks = request.Marks,
            IsRequired = request.IsRequired,
        };
        dbContext.SyllabusSubjects.Add(mapping);
        AddAuditLog("subject.assigned", nameof(SyllabusSubject), mapping.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSyllabusSubjectDto(mapping, subject, [], []);
    }

    public async Task<SyllabusSubjectDto?> UpdateSyllabusSubjectAsync(
        Guid id,
        UpdateSyllabusSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var mapping = await dbContext.SyllabusSubjects
            .Include(candidate => candidate.Subject)
            .Include(candidate => candidate.SyllabusVersion)
            .Include(candidate => candidate.Papers)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (mapping is null)
        {
            return null;
        }

        EnsureDraft(mapping.SyllabusVersion);
        mapping.SortOrder = request.SortOrder;
        mapping.Marks = request.Marks;
        mapping.IsRequired = request.IsRequired;
        AddAuditLog("subject-placement.updated", nameof(SyllabusSubject), mapping.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSyllabusSubjectDto(
            mapping,
            mapping.Subject,
            mapping.Papers.Select(ToPaperDto),
            []);
    }

    public async Task<PaperDto> CreatePaperAsync(
        Guid syllabusSubjectId,
        CreatePaperRequest request,
        CancellationToken cancellationToken)
    {
        var mapping = await dbContext.SyllabusSubjects
            .Include(candidate => candidate.SyllabusVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == syllabusSubjectId, cancellationToken);
        if (mapping is null)
        {
            throw NotFound("The syllabus subject was not found.");
        }

        EnsureDraft(mapping.SyllabusVersion);
        var code = NormalizeCode(request.Code);
        if (await dbContext.Papers.AnyAsync(
                paper => paper.SyllabusSubjectId == syllabusSubjectId && paper.Code == code,
                cancellationToken))
        {
            throw Conflict("A paper with this code already exists for the syllabus subject.");
        }

        var paper = new Paper
        {
            Id = Guid.NewGuid(),
            SyllabusSubjectId = syllabusSubjectId,
            Code = code,
            Name = request.Name.Trim(),
            SortOrder = request.SortOrder,
            MaxMarks = request.MaxMarks,
        };
        dbContext.Papers.Add(paper);
        AddAuditLog("paper.created", nameof(Paper), paper.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPaperDto(paper);
    }

    public async Task<PaperDto?> UpdatePaperAsync(
        Guid id,
        UpdatePaperRequest request,
        CancellationToken cancellationToken)
    {
        var paper = await dbContext.Papers.Include(candidate => candidate.SyllabusSubject)
            .ThenInclude(mapping => mapping.SyllabusVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (paper is null)
        {
            return null;
        }

        EnsureDraft(paper.SyllabusSubject.SyllabusVersion);
        paper.Name = request.Name.Trim();
        paper.SortOrder = request.SortOrder;
        paper.MaxMarks = request.MaxMarks;
        AddAuditLog("paper.updated", nameof(Paper), paper.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPaperDto(paper);
    }

    public async Task<bool> DeletePaperAsync(Guid id, CancellationToken cancellationToken)
    {
        var paper = await dbContext.Papers.Include(candidate => candidate.SyllabusSubject)
            .ThenInclude(mapping => mapping.SyllabusVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (paper is null)
        {
            return false;
        }

        EnsureDraft(paper.SyllabusSubject.SyllabusVersion);
        if (await dbContext.SyllabusItems.AnyAsync(item => item.PaperId == id, cancellationToken))
        {
            throw Conflict("A paper with syllabus topics cannot be deleted. Remove or move its topics first.");
        }

        dbContext.Papers.Remove(paper);
        AddAuditLog("paper.deleted", nameof(Paper), paper.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<SyllabusItemDto> CreateItemAsync(
        SyllabusItemKindRequest kind,
        CreateSyllabusItemRequest request,
        CancellationToken cancellationToken)
    {
        var mapping = await dbContext.SyllabusSubjects
            .Include(candidate => candidate.SyllabusVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == request.SyllabusSubjectId, cancellationToken);
        if (mapping is null)
        {
            throw NotFound("The syllabus subject was not found.");
        }

        EnsureDraft(mapping.SyllabusVersion);
        if (!Enum.TryParse<SyllabusVerificationStatus>(request.VerificationStatus, true, out var verificationStatus))
        {
            throw Invalid("The item verification status is not supported.");
        }

        await ValidateItemPlacementAsync(
            kind,
            mapping.Id,
            request.PaperId,
            request.ParentId,
            cancellationToken);
        await EnsureItemVerificationAllowedAsync(null, verificationStatus, cancellationToken);
        var item = new SyllabusItem
        {
            Id = Guid.NewGuid(),
            SyllabusSubjectId = mapping.Id,
            PaperId = request.PaperId,
            ParentId = request.ParentId,
            Kind = kind switch
            {
                SyllabusItemKindRequest.Topic => SyllabusItemKind.Topic,
                SyllabusItemKindRequest.Subtopic => SyllabusItemKind.Subtopic,
                _ => throw Invalid("The syllabus item type is not supported."),
            },
            Title = request.Title.Trim(),
            Description = NormalizeNullable(request.Description),
            SortOrder = request.SortOrder,
            VerificationStatus = verificationStatus,
        };
        dbContext.SyllabusItems.Add(item);
        AddAuditLog($"item.{item.Kind.ToString().ToLowerInvariant()}.created", nameof(SyllabusItem), item.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToFlatItemDto(item, mapping.SubjectId);
    }

    public async Task<SyllabusItemDto?> UpdateItemAsync(
        Guid id,
        UpdateSyllabusItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.SyllabusItems
            .Include(candidate => candidate.SyllabusSubject)
            .ThenInclude(mapping => mapping.SyllabusVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        EnsureDraft(item.SyllabusSubject.SyllabusVersion);
        if (!Enum.TryParse<SyllabusVerificationStatus>(request.VerificationStatus, true, out var verificationStatus))
        {
            throw Invalid("The item verification status is not supported.");
        }

        await ValidateItemPlacementAsync(
            (SyllabusItemKindRequest)item.Kind,
            item.SyllabusSubjectId,
            request.PaperId,
            request.ParentId,
            cancellationToken,
            id);
        await EnsureItemVerificationAllowedAsync(id, verificationStatus, cancellationToken);
        item.PaperId = request.PaperId;
        item.ParentId = request.ParentId;
        item.Title = request.Title.Trim();
        item.Description = NormalizeNullable(request.Description);
        item.SortOrder = request.SortOrder;
        item.VerificationStatus = verificationStatus;
        AddAuditLog("item.updated", nameof(SyllabusItem), item.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToFlatItemDto(item, item.SyllabusSubject.SubjectId);
    }

    public async Task<bool> DeleteItemAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.SyllabusItems
            .Include(candidate => candidate.SyllabusSubject)
            .ThenInclude(mapping => mapping.SyllabusVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        EnsureDraft(item.SyllabusSubject.SyllabusVersion);
        if (await dbContext.SyllabusItems.AnyAsync(candidate => candidate.ParentId == id, cancellationToken) ||
            await dbContext.TopicProgress.AnyAsync(progress => progress.SyllabusItemId == id, cancellationToken))
        {
            throw Conflict("A syllabus item with child items or progress records cannot be deleted.");
        }

        dbContext.SyllabusItems.Remove(item);
        AddAuditLog("item.deleted", nameof(SyllabusItem), item.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<SyllabusSourceDto>> GetSourcesAsync(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var versionExists = await dbContext.SyllabusVersions.AnyAsync(version => version.Id == versionId, cancellationToken);
        if (!versionExists)
        {
            throw NotFound("The syllabus version was not found.");
        }

        var sources = await dbContext.SyllabusVersionSources.AsNoTracking()
            .Where(mapping => mapping.SyllabusVersionId == versionId)
            .Select(mapping => mapping.SourceReference)
            .OrderBy(source => source.Title)
            .ToListAsync(cancellationToken);
        return sources.Select(ToSourceDto).ToArray();
    }

    public async Task<IReadOnlyList<SyllabusSourceDto>> GetItemSourcesAsync(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var itemExists = await dbContext.SyllabusItems.AnyAsync(
            item => item.Id == itemId &&
                    item.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published,
            cancellationToken);
        if (!itemExists)
        {
            throw NotFound("The published syllabus item was not found.");
        }

        var sources = await dbContext.SyllabusItemSources.AsNoTracking()
            .Where(mapping => mapping.SyllabusItemId == itemId)
            .Select(mapping => mapping.SourceReference)
            .OrderBy(source => source.Title)
            .ToListAsync(cancellationToken);
        return sources.Select(ToSourceDto).ToArray();
    }

    public async Task<SyllabusSourceDto?> GetSourceAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await dbContext.SyllabusSourceReferences.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return source is null ? null : ToSourceDto(source);
    }

    public async Task<SyllabusSourceDto> CreateSourceAsync(
        CreateSyllabusSourceRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SourceVerificationStatus>(request.VerificationStatus, true, out var status))
        {
            throw Invalid("The source verification status is not supported.");
        }

        ValidateSourceVerification(status, request.LastVerifiedOn);
        var url = ValidateSourceUrl(request.Url);
        if (await dbContext.SyllabusSourceReferences.AnyAsync(source => source.Url == url, cancellationToken))
        {
            throw Conflict("A syllabus source with this URL already exists.");
        }

        var source = new SyllabusSourceReference
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Publisher = request.Publisher.Trim(),
            Url = url,
            SourceType = request.SourceType.Trim(),
            PublishedOn = request.PublishedOn,
            LastVerifiedOn = request.LastVerifiedOn,
            VerificationStatus = status,
            Notes = NormalizeNullable(request.Notes),
        };
        dbContext.SyllabusSourceReferences.Add(source);
        AddAuditLog("source.created", nameof(SyllabusSourceReference), source.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSourceDto(source);
    }

    public async Task<SyllabusSourceDto?> UpdateSourceAsync(
        Guid id,
        UpdateSyllabusSourceRequest request,
        CancellationToken cancellationToken)
    {
        var source = await dbContext.SyllabusSourceReferences.SingleOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);
        if (source is null)
        {
            return null;
        }

        if (await dbContext.SyllabusVersionSources.AnyAsync(
                mapping => mapping.SourceReferenceId == id &&
                           mapping.SyllabusVersion.Status == SyllabusVersionStatus.Published,
                cancellationToken) ||
            await dbContext.SyllabusItemSources.AnyAsync(
                mapping => mapping.SourceReferenceId == id &&
                           mapping.SyllabusItem.SyllabusSubject.SyllabusVersion.Status == SyllabusVersionStatus.Published,
                cancellationToken))
        {
            throw Conflict("A source linked to a published version is immutable. Add a revised source reference instead.");
        }

        if (!Enum.TryParse<SourceVerificationStatus>(request.VerificationStatus, true, out var status))
        {
            throw Invalid("The source verification status is not supported.");
        }

        ValidateSourceVerification(status, request.LastVerifiedOn);
        var url = ValidateSourceUrl(request.Url);
        if (await dbContext.SyllabusSourceReferences.AnyAsync(
                candidate => candidate.Id != id && candidate.Url == url,
                cancellationToken))
        {
            throw Conflict("A syllabus source with this URL already exists.");
        }

        source.Title = request.Title.Trim();
        source.Publisher = request.Publisher.Trim();
        source.Url = url;
        source.SourceType = request.SourceType.Trim();
        source.PublishedOn = request.PublishedOn;
        source.LastVerifiedOn = request.LastVerifiedOn;
        source.VerificationStatus = status;
        source.Notes = NormalizeNullable(request.Notes);
        AddAuditLog("source.updated", nameof(SyllabusSourceReference), source.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSourceDto(source);
    }

    public async Task<bool> LinkVersionSourceAsync(
        Guid versionId,
        Guid sourceId,
        LinkSyllabusSourceRequest request,
        CancellationToken cancellationToken)
    {
        var version = await GetDraftVersionAsync(versionId, cancellationToken);
        var sourceExists = await dbContext.SyllabusSourceReferences.AnyAsync(
            source => source.Id == sourceId,
            cancellationToken);
        if (!sourceExists)
        {
            throw NotFound("The syllabus source was not found.");
        }

        if (await dbContext.SyllabusVersionSources.AnyAsync(
                mapping => mapping.SyllabusVersionId == version.Id &&
                           mapping.SourceReferenceId == sourceId,
                cancellationToken))
        {
            return true;
        }

        dbContext.SyllabusVersionSources.Add(new SyllabusVersionSource
        {
            SyllabusVersionId = versionId,
            SourceReferenceId = sourceId,
            Notes = NormalizeNullable(request.Notes),
        });
        AddAuditLog("source.linked", nameof(SyllabusVersion), versionId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnlinkVersionSourceAsync(
        Guid versionId,
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        var version = await GetDraftVersionAsync(versionId, cancellationToken);
        var mapping = await dbContext.SyllabusVersionSources.SingleOrDefaultAsync(
            candidate => candidate.SyllabusVersionId == version.Id &&
                         candidate.SourceReferenceId == sourceId,
            cancellationToken);
        if (mapping is null)
        {
            return false;
        }

        dbContext.SyllabusVersionSources.Remove(mapping);
        AddAuditLog("source.unlinked", nameof(SyllabusVersion), versionId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> LinkItemSourceAsync(
        Guid itemId,
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        var item = await GetDraftItemAsync(itemId, cancellationToken);
        var sourceExists = await dbContext.SyllabusSourceReferences.AnyAsync(
            source => source.Id == sourceId,
            cancellationToken);
        if (!sourceExists)
        {
            throw NotFound("The syllabus source was not found.");
        }

        if (await dbContext.SyllabusItemSources.AnyAsync(
                mapping => mapping.SyllabusItemId == item.Id &&
                           mapping.SourceReferenceId == sourceId,
                cancellationToken))
        {
            return true;
        }

        dbContext.SyllabusItemSources.Add(new SyllabusItemSource
        {
            SyllabusItemId = itemId,
            SourceReferenceId = sourceId,
        });
        if (item.VerificationStatus != SyllabusVerificationStatus.Verified)
        {
            item.VerificationStatus = SyllabusVerificationStatus.NeedsReview;
        }

        AddAuditLog("source.linked", nameof(SyllabusItem), itemId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnlinkItemSourceAsync(
        Guid itemId,
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        _ = await GetDraftItemAsync(itemId, cancellationToken);
        var mapping = await dbContext.SyllabusItemSources.SingleOrDefaultAsync(
            candidate => candidate.SyllabusItemId == itemId &&
                         candidate.SourceReferenceId == sourceId,
            cancellationToken);
        if (mapping is null)
        {
            return false;
        }

        dbContext.SyllabusItemSources.Remove(mapping);
        AddAuditLog("source.unlinked", nameof(SyllabusItem), itemId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<SyllabusVersion> GetDraftVersionAsync(Guid id, CancellationToken cancellationToken)
    {
        var version = await dbContext.SyllabusVersions.SingleOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);
        if (version is null)
        {
            throw NotFound("The syllabus version was not found.");
        }

        EnsureDraft(version);
        return version;
    }

    private async Task<SyllabusItem> GetDraftItemAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.SyllabusItems
            .Include(candidate => candidate.SyllabusSubject)
            .ThenInclude(mapping => mapping.SyllabusVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null)
        {
            throw NotFound("The syllabus item was not found.");
        }

        EnsureDraft(item.SyllabusSubject.SyllabusVersion);
        return item;
    }

    private async Task ValidateItemPlacementAsync(
        SyllabusItemKindRequest kind,
        Guid syllabusSubjectId,
        Guid? paperId,
        Guid? parentId,
        CancellationToken cancellationToken,
        Guid? itemId = null)
    {
        if (paperId is Guid selectedPaperId &&
            !await dbContext.Papers.AnyAsync(
                paper => paper.Id == selectedPaperId && paper.SyllabusSubjectId == syllabusSubjectId,
                cancellationToken))
        {
            throw Invalid("The selected paper does not belong to this syllabus subject.");
        }

        if (kind == SyllabusItemKindRequest.Topic && parentId.HasValue)
        {
            throw Invalid("A topic cannot have a parent item; create subtopics under a topic.");
        }

        if (kind == SyllabusItemKindRequest.Subtopic)
        {
            if (!parentId.HasValue)
            {
                throw Invalid("A subtopic must have a parent topic.");
            }

            var parent = await dbContext.SyllabusItems.SingleOrDefaultAsync(
                item => item.Id == parentId && item.SyllabusSubjectId == syllabusSubjectId,
                cancellationToken);
            if (parent is null || parent.Kind != SyllabusItemKind.Topic || parent.Id == itemId)
            {
                throw Invalid("A subtopic must belong to a topic in the same syllabus subject.");
            }

            if (parent.PaperId != paperId)
            {
                throw Invalid("A subtopic must belong to the same paper as its parent topic.");
            }
        }
    }

    private async Task EnsureItemVerificationAllowedAsync(
        Guid? itemId,
        SyllabusVerificationStatus verificationStatus,
        CancellationToken cancellationToken)
    {
        if (verificationStatus == SyllabusVerificationStatus.Verified &&
            (itemId is null ||
             !await dbContext.SyllabusItemSources.AnyAsync(
                 mapping => mapping.SyllabusItemId == itemId &&
                            mapping.SourceReference.VerificationStatus == SourceVerificationStatus.Verified,
                 cancellationToken)))
        {
            throw Invalid("A syllabus item can be marked verified only after it has a linked source marked verified.");
        }
    }

    private async Task EnsureVersionUniqueAsync(
        Guid stageId,
        string trackCode,
        string versionLabel,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedLabel = versionLabel.Trim();
        if (await dbContext.SyllabusVersions.AnyAsync(version =>
                version.ExamStageId == stageId &&
                version.TrackCode == trackCode &&
                version.VersionLabel == normalizedLabel &&
                version.Id != excludingId,
                cancellationToken))
        {
            throw Conflict("A version with this label and track already exists for the exam stage.");
        }
    }

    private static void EnsureDraft(SyllabusVersion version)
    {
        if (version.Status != SyllabusVersionStatus.Draft)
        {
            throw Conflict("Only draft syllabus versions can be edited.");
        }
    }

    private static void ValidateDateRange(DateOnly? from, DateOnly? to)
    {
        if (from.HasValue && to.HasValue && to < from)
        {
            throw Invalid("The syllabus end date cannot be earlier than its start date.");
        }
    }

    private static string ValidateSourceUrl(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw Invalid("Syllabus source URLs must use HTTPS.");
        }

        return uri.AbsoluteUri;
    }

    private static void ValidateSourceVerification(SourceVerificationStatus status, DateOnly? lastVerifiedOn)
    {
        if (status == SourceVerificationStatus.Verified && !lastVerifiedOn.HasValue)
        {
            throw Invalid("A verified source must include the date it was last checked.");
        }
    }

    private void AddAuditLog(string action, string entityType, Guid entityId)
    {
        var claimValue = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? actorId = Guid.TryParse(claimValue, out var userId) ? userId : null;
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = $"syllabus.{action}",
            EntityType = entityType,
            EntityId = entityId.ToString(),
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = httpContextAccessor.HttpContext?.TraceIdentifier,
        });
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SyllabusVersionDto ToVersionDto(
        SyllabusVersion version,
        int subjectCount,
        int sourceCount,
        ExamStage? stage = null)
    {
        stage ??= version.ExamStage;
        return new SyllabusVersionDto(
            version.Id,
            version.ExamStageId,
            stage.Code,
            stage.Name,
            version.VersionLabel,
            version.TrackCode,
            version.Summary,
            version.Coverage.ToString(),
            version.Status.ToString(),
            version.EffectiveFrom,
            version.EffectiveTo,
            version.PublishedAt,
            subjectCount,
            sourceCount);
    }

    private static SubjectDto ToSubjectDto(Subject subject) =>
        new(subject.Id, subject.Code, subject.Name, subject.Description, subject.IsActive);

    private static PaperDto ToPaperDto(Paper paper) =>
        new(paper.Id, paper.SyllabusSubjectId, paper.Code, paper.Name, paper.SortOrder, paper.MaxMarks);

    private static SyllabusSubjectDto ToSyllabusSubjectDto(
        SyllabusSubject mapping,
        Subject subject,
        IEnumerable<PaperDto> papers,
        IEnumerable<SyllabusItemDto> items) =>
        new(
            mapping.Id,
            mapping.SubjectId,
            subject.Code,
            subject.Name,
            mapping.SortOrder,
            mapping.Marks,
            mapping.IsRequired,
            papers.ToArray(),
            items.ToArray());

    private static SyllabusItemDto ToFlatItemDto(SyllabusItem item, Guid subjectId) =>
        new(
            item.Id,
            item.SyllabusSubjectId,
            subjectId,
            item.PaperId,
            item.ParentId,
            item.Kind.ToString(),
            item.Title,
            item.Description,
            item.SortOrder,
            item.VerificationStatus.ToString(),
            null,
            []);

    private static SyllabusItemDto ToItemDto(
        SyllabusItem item,
        Guid subjectId,
        IReadOnlyDictionary<Guid, bool>? progressItems,
        IReadOnlyDictionary<Guid, SyllabusItem[]> itemsByParent)
    {
        itemsByParent.TryGetValue(item.Id, out var children);
        return new SyllabusItemDto(
            item.Id,
            item.SyllabusSubjectId,
            subjectId,
            item.PaperId,
            item.ParentId,
            item.Kind.ToString(),
            item.Title,
            item.Description,
            item.SortOrder,
            item.VerificationStatus.ToString(),
            progressItems is not null && progressItems.TryGetValue(item.Id, out var isCompleted)
                ? isCompleted
                : null,
            (children ?? []).Select(child => ToItemDto(child, subjectId, progressItems, itemsByParent)).ToArray());
    }

    private static SyllabusSourceDto ToSourceDto(SyllabusSourceReference source) =>
        new(
            source.Id,
            source.Title,
            source.Publisher,
            source.Url,
            source.SourceType,
            source.PublishedOn,
            source.LastVerifiedOn,
            source.VerificationStatus.ToString(),
            source.Notes);

    private static SyllabusException Invalid(string detail) =>
        new(StatusCodes.Status400BadRequest, "Invalid syllabus request", detail);

    private static SyllabusException NotFound(string detail) =>
        new(StatusCodes.Status404NotFound, "Syllabus content not found", detail);

    private static SyllabusException Conflict(string detail) =>
        new(StatusCodes.Status409Conflict, "Syllabus content conflict", detail);
}
