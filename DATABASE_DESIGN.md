# Database Design

## Purpose

MySQL is the system of record for identities, syllabus versions, content metadata, relationships, attempts, progress, and audit records. Binary files are stored outside MySQL.

## Implemented schema

`InitialIdentityAndAudit` creates ASP.NET Core Identity tables, `Permissions`, `RolePermissions`, and `AuditLogs`. `AddVersionedSyllabus` adds the versioned examination/syllabus hierarchy, official source references, and user-owned topic progress. Users and roles use GUID keys. Permissions are assigned to database-managed roles; the seeded baseline is Candidate, ContentManager, and Administrator.

Question-bank, library, planner, written-question, and viva-question tables remain out of scope.

The initial audit records cover successful registration and login and exclude credentials and token values. Additional audited mutations will be added with their respective modules.

## Conventions

- Use `long`/`Guid` values for public identities; choose UUID or BIGINT per entity during implementation
- Use UTC `DateTimeOffset` values and snake_case? No: EF-generated MySQL names follow a documented convention
- Prefer a shared `Id`, `CreatedAt`, `CreatedById`, `UpdatedAt`, `UpdatedById`, `IsArchived` pattern only where required
- Use soft archives for published content and hard deletion only for disposable drafts or private records
- Define delete behavior explicitly: restrict for protected references, cascade only for owned children
- Store enum values by stable integer or string according to query/report needs
- Use MySQL `datetime(6)`, `longtext`, `varchar` lengths, decimal precision, and UTF-8mb4
- Every junction table has a composite unique key
- Every public mutable entity has row version/concurrency handling where simultaneous admin edits are plausible

## Entity groups

### Identity

```text
User
  - Id
  - Email
  - PasswordHash
  - DisplayName
  - Status
  - FailedLoginCount
  - LockoutEnd
  - timestamps

Role
  - Id
  - Name
  - NormalizedName
  - Description

Permission
  - Id
  - Code
  - Description

RolePermission (RoleId, PermissionId)
UserRole (UserId, RoleId)
RefreshToken
  - Id
  - UserId
  - TokenHash
  - ExpiresAt
  - RevokedAt
  - ReplacedByTokenId
  - CreatedByIp

AuditLog
  - Id
  - ActorId nullable
  - Action
  - EntityType
  - EntityId
  - ChangeJson
  - Timestamp
  - CorrelationId
  - IpAddress
```

Identity is permission-oriented: a role is a bundle of permissions, and authorization uses permission keys in application policies.

### Syllabus

```text
ExamStage
  - Id
  - Code
  - Name
  - LocalizedName
  - SortOrder
  - IsActive

SyllabusVersion
  - Id
  - ExamStageId
  - VersionLabel
  - TrackCode (Common, General, TechnicalProfessional)
  - Summary
  - Coverage (OutlineOnly, FullTopics)
  - Status (Draft, Published, Archived)
  - EffectiveFrom nullable
  - EffectiveTo nullable
  - PublishedAt nullable
  - CreatedAt
  - UpdatedAt

Subject
  - Id
  - Code
  - Name
  - Description
  - IsActive

SyllabusSubject
  - Id
  - SyllabusVersionId
  - SubjectId
  - SortOrder
  - Marks nullable
  - IsRequired

Paper
  - Id
  - SyllabusSubjectId
  - Code
  - Name
  - SortOrder
  - MaxMarks nullable

SyllabusItem
  - Id
  - SyllabusSubjectId
  - PaperId nullable
  - ParentId nullable
  - Kind (Topic, Subtopic)
  - Title
  - Description nullable
  - SortOrder
  - VerificationStatus (Unverified, Verified, NeedsReview)

SyllabusSourceReference
  - Id
  - Title
  - Publisher
  - Url
  - SourceType
  - PublishedOn nullable
  - LastVerifiedOn nullable
  - VerificationStatus (Unverified, Verified, Superseded)
  - Notes nullable

SyllabusVersionSource (SyllabusVersionId, SourceReferenceId)
SyllabusItemSource (SyllabusItemId, SourceReferenceId)

TopicProgress
  - Id
  - UserId
  - SyllabusItemId
  - IsCompleted
  - CompletedAt nullable
  - UpdatedAt
```

`SyllabusItem` is the single persisted hierarchy for topic and subtopic nodes; the `Kind` discriminator and application validation prevent deeper or cross-subject parent links. A subtopic must have a topic parent in the same version/subject mapping and paper. This avoids duplicate topic/subtopic storage while retaining separate topic and subtopic API routes. `SyllabusSubject` resolves a shared subject catalog record to a specific version and stores version-specific marks/order.

Versions are immutable after publication. A verified version source is required to publish; a `FullTopics` version additionally requires every topic/subtopic to be marked verified and linked to a verified source. `OutlineOnly` versions explicitly state their coverage limits. Topic progress has a unique `(UserId, SyllabusItemId)` key and is only accessible by the owning user.

### Content metadata

```text
Category
Tag
TagAssignment
Source
ContentRevision
StudyNote
PersonalNote
NoteSyllabusLink
MistakeEntry
```

`TagAssignment` uses `EntityType` plus `EntityId` or dedicated junction tables. Dedicated junction tables are preferred for important domains because they provide integrity and clear indexes.

### Questions and attempts

```text
Question
  - Id
  - QuestionType
  - Prompt
  - AnswerExplanation
  - Difficulty
  - Status
  - SourceId nullable
  - PublishedAt

QuestionSyllabusLink
  - QuestionId
  - SyllabusVersionId
  - TopicId nullable
  - SubtopicId nullable

McqOption
  - Id
  - QuestionId
  - Text
  - IsCorrect
  - Explanation
  - SortOrder

PreviousQuestion
  - QuestionId
  - ExamYear
  - ExamSession
  - OfficialReference

ExamTemplate
  - Id
  - Name
  - ExamStageId
  - SyllabusVersionId
  - DurationSeconds
  - Instructions
  - Status

ExamSection
  - Id
  - ExamTemplateId
  - Name
  - SortOrder
  - QuestionCount

ExamTemplateQuestion
  - ExamTemplateId
  - SectionId nullable
  - QuestionId
  - Marks
  - SortOrder nullable

ExamAttempt
  - Id
  - UserId
  - ExamTemplateId
  - StartedAt
  - SubmittedAt nullable
  - Score
  - Status
  - SnapshotJson

ExamAttemptAnswer
  - Id
  - ExamAttemptId
  - QuestionId
  - SelectedOptionId nullable
  - IsCorrect nullable
  - MarksAwarded
  - TimeSpentSeconds
  - AnswerSnapshotJson
```

The attempt snapshots prompt/options/scores at attempt start so later edits do not rewrite completed history.

### Written preparation

```text
WrittenQuestion
  - Id
  - QuestionId
  - Format
  - SuggestedMarks
  - WordLimitMin nullable
  - WordLimitMax nullable

WrittenAnswer
  - Id
  - UserId
  - WrittenQuestionId
  - Content
  - SubmittedAt
  - Status

AnswerEvaluation
  - Id
  - WrittenAnswerId
  - EvaluatorUserId nullable
  - RubricJson
  - Score
  - Feedback

ModelAnswer
  - Id
  - WrittenQuestionId
  - Content
  - Status
  - SourceId nullable
```

### Viva preparation

```text
VivaQuestion
  - Id
  - QuestionId
  - Category
  - PromptAreas

MockVivaSession
  - Id
  - UserId
  - StartedAt
  - CompletedAt nullable
  - OverallRating
  - PersonalNotes

VivaSessionAnswer
  - Id
  - MockVivaSessionId
  - VivaQuestionId
  - AnswerNote
  - Rating

CadreResearchProfile
  - Id
  - UserId
  - CadreInterest
  - ProfileJson
```

### Library

```text
Book
  - Id
  - Title
  - AuthorsJson
  - Publisher
  - Edition
  - PublicationYear
  - Description
  - CoverResourceId nullable

BookChapter
  - Id
  - BookId
  - Title
  - ChapterNumber
  - ResourceId nullable

Resource
  - Id
  - ResourceType
  - Title
  - OriginalFileName nullable
  - StorageProvider
  - StorageKey nullable
  - ExternalUrl nullable
  - MimeType
  - SizeBytes
  - Checksum
  - Visibility
  - Status
  - UploaderId

ResourceCollection
ResourceCollectionItem
ResourceSyllabusLink
ResourceBookmark
ResourceReadingProgress
ResourcePersonalNote
SourceAttribution
ResourceSource
```

### Planner and progress

```text
StudySession
  - Id
  - UserId
  - SyllabusVersionId
  - TargetType
  - TargetId
  - StartedAt
  - EndedAt
  - DurationSeconds
  - Note

StudyGoal
  - Id
  - UserId
  - PeriodType
  - PeriodStart
  - TargetJson
  - Status

TopicProgress
  - Id
  - UserId
  - SyllabusVersionId
  - TopicId nullable
  - SubtopicId nullable
  - Status
  - CompletionPercentage
  - LastStudiedAt
  - TotalStudySeconds

RevisionItem
  - Id
  - UserId
  - TargetType
  - TargetId
  - Ease
  - IntervalDays
  - NextReviewAt
  - LastReviewedAt

Bookmark
MistakeEntry
```

`TopicProgress` and `StudySession` are normalized, while dashboard values are aggregated by application queries or later materialized views. Calculated values must not be stored in multiple places without ownership and invalidation rules.

### Current affairs

```text
CurrentAffair
  - Id
  - Title
  - Summary
  - Content
  - EventDate
  - PublicationDate
  - CategoryId
  - Status

CurrentAffairSyllabusLink
CurrentAffairQuestionLink
CurrentAffairResourceLink
GovernmentResource
Publication
```

## Key relationships

- `ExamStage 1-* SyllabusVersion`
- `SyllabusVersion *-* Subject` through `SyllabusSubject`
- `SyllabusSubject 1-* Paper`
- `SyllabusSubject 1-* SyllabusItem`
- `SyllabusItem 1-* SyllabusItem` for the one-level Topic-to-Subtopic hierarchy, validated by the application
- `SyllabusVersion *-* SyllabusSourceReference` through `SyllabusVersionSource`
- `SyllabusItem *-* SyllabusSourceReference` through `SyllabusItemSource`
- `User 1-* TopicProgress`; each progress row targets one syllabus item
- `Question *-* Topic/Subtopic` through versioned links
- `ExamTemplate 1-* ExamSection 1-* ExamTemplateQuestion`
- `ExamAttempt 1-* ExamAttemptAnswer`
- `User 1-*` personal attempts, answers, notes, goals, progress, and revisions
- `Resource *-* SyllabusNode/Book/Collection/CurrentAffair`
- `Source *-*` content that uses it

## Important indexes

- Unique normalized user email and role/permission codes
- Unique refresh-token hash
- Unique stage/version/track labels, subject codes, and paper codes within a syllabus subject
- Unique version-to-subject and source/item junction pairs
- Topic order by syllabus subject, kind, and sort order; progress by user and item
- Unique junction-table pairs
- Question status/type/difficulty
- Question-syllabus topic/subtopic
- Exam attempt user/start/status
- Attempt answer attempt/question
- Study progress user/version/topic/subtopic
- Revision next review date by user
- Resource status/type/uploader
- Reading progress user/resource
- Current affairs publication/event dates and category/status

## Migration strategy

Migrations are application phase deliverables. The Phase 4 `AddVersionedSyllabus` migration has been generated and reviewed; it must be applied to the configured MySQL database before the API can start. Before execution:

1. Update entity configurations and DbContext.
2. Generate an EF Core migration.
3. Review it for MySQL-specific types and destructive operations.
4. Run against a disposable local database when available.
5. Run integration tests.
6. Apply only to the explicitly configured database.

The exact migration command will use the established startup/Infrastructure project names.
