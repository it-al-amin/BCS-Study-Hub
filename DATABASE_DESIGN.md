# Database Design

## Purpose

MySQL is the system of record for identities, syllabus versions, content metadata, relationships, attempts, progress, and audit records. Binary files are stored outside MySQL.

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
  - SortOrder
  - Status

SyllabusVersion
  - Id
  - ExamStageId
  - Title
  - EffectiveFrom
  - EffectiveTo nullable
  - Status
  - SourceSummary
  - PublishedAt

Subject
  - Id
  - Code
  - Name
  - Description
  - Status

SyllabusSubject
  - Id
  - SyllabusVersionId
  - SubjectId
  - SortOrder
  - MarksInfoJson nullable

Paper
  - Id
  - SyllabusSubjectId
  - Name
  - Code
  - SortOrder
  - MarksInfoJson nullable

Topic
  - Id
  - SyllabusVersionId
  - SubjectId
  - PaperId nullable
  - ParentTopicId nullable
  - Title
  - Description
  - SortOrder
  - Status

Subtopic
  - Id
  - TopicId
  - Title
  - Description
  - SortOrder
  - Status
```

To keep the hierarchy flexible, a topic may contain child topics. Subtopics are separately modeled for an explicit UI leaf and for direct progress/resource associations. `SyllabusSubject` resolves a subject to a version; `Subject` may also be reused across versions.

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
- `SyllabusVersion 1-* Topic`
- `Topic 1-* Subtopic`
- `Topic 1-* Topic` for child topics, no cycles in application validation
- `Question *-* Topic/Subtopic` through versioned links
- `ExamTemplate 1-* ExamSection 1-* ExamTemplateQuestion`
- `ExamAttempt 1-* ExamAttemptAnswer`
- `User 1-*` personal attempts, answers, notes, goals, progress, and revisions
- `Resource *-* SyllabusNode/Book/Collection/CurrentAffair`
- `Source *-*` content that uses it

## Important indexes

- Unique normalized user email and role/permission codes
- Unique refresh-token hash
- Unique syllabus subject/paper/topic sort keys
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

Migrations are application phase deliverables, not Phase 1 deliverables. Before execution:

1. Update entity configurations and DbContext.
2. Generate an EF Core migration.
3. Review it for MySQL-specific types and destructive operations.
4. Run against a disposable local database.
5. Run integration tests.
6. Run migration against the user's explicitly approved database.

The exact migration command will use the established startup/Infrastructure project names.
