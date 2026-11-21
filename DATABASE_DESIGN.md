# Database Design

## Purpose

MySQL is the system of record for identities, syllabus versions, content metadata, relationships, attempts, progress, and audit records. Binary files are stored outside MySQL.

## Implemented schema

`InitialIdentityAndAudit` creates ASP.NET Core Identity tables, `Permissions`, `RolePermissions`, and `AuditLogs`. `AddVersionedSyllabus` adds the versioned examination/syllabus hierarchy, official source references, and user-owned topic progress. Users and roles use GUID keys. Permissions are assigned to database-managed roles; the seeded baseline is Candidate, ContentManager, and Administrator.

`AddMcqPracticeEngine` adds the Preliminary MCQ question bank, dynamic practice-set configurations, answer snapshots, attempts, bookmarks, and mistake tracking. `AddWrittenPreparation` adds source-aware Written questions/model answers, user-owned answer revisions, bookmarks, notes, goals, and reports. `AddVivaPreparation` adds editable Viva categories and questions/references, user-owned notes/revisions/bookmarks/question history, ordered mock sessions and answers, and private profile preparation.

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
McqQuestion
  - Id
  - SyllabusItemId (Topic or Subtopic)
  - Prompt
  - Explanation
  - ImageUrl (HTTPS reference only)
  - Type (SingleAnswer, MultipleAnswer)
  - Difficulty (Easy, Medium, Hard)
  - Status (Draft, Published, Archived)
  - BcsExamYear nullable
  - ExamNumber nullable
  - ExamReference nullable
  - CreatedById
  - timestamps

McqQuestionOption
  - Id
  - QuestionId
  - Text
  - SortOrder
  - IsCorrect (never exposed in an active attempt)

McqQuestionSourceReference
  - Id
  - QuestionId
  - Title
  - Publisher
  - Url
  - Citation
  - PublishedOn
  - VerificationStatus

McqQuestionTag (QuestionId, Tag)

McqExamSet
  - Id
  - SyllabusVersionId
  - SubjectId nullable
  - SyllabusItemId nullable
  - Mode (Topic, Subject, Mixed, MockExam)
  - QuestionCount
  - DurationSeconds nullable
  - BcsExamYear nullable
  - Difficulty nullable
  - MarksCorrect
  - MarksIncorrect
  - MarksUnanswered
  - ShowFeedbackImmediately
  - Status

McqAttempt
  - Id
  - UserId
  - ExamSetId
  - scoring/mode/duration snapshots
  - StartedAt
  - ExpiresAt nullable
  - SubmittedAt nullable
  - IsTimedOut
  - Score nullable
  - CorrectCount / IncorrectCount / UnansweredCount

McqAttemptQuestion
  - Id
  - AttemptId
  - QuestionId
  - prompt/topic/source snapshots
  - Position

McqAttemptOption
  - Id
  - AttemptQuestionId
  - SourceOptionId
  - TextSnapshot
  - IsCorrect

McqAttemptAnswer
  - AttemptQuestionId
  - SelectedOptionIdsJson
  - IsMarkedForReview
  - IsCorrect nullable
  - MarksAwarded nullable
  - UpdatedAt

McqBookmark (UserId, QuestionId)
McqMistake (UserId, QuestionId, LastAttemptId, Status, Notes, timestamps)
```

Questions map directly to one topic/subtopic in a published Preliminary syllabus version. Publishing a question requires verified question-source references and a verified syllabus item; the system does not seed invented or unverified question content. Attempt question/option, scoring, topic, and source snapshots preserve submitted history if editorial content or set configuration later changes. The server validates selected option IDs and calculates results from the stored correct-option snapshot; client-submitted scores and correctness are not accepted. Multiple-answer questions use exact-set matching, with no assumed partial credit. Each exam set stores editable scoring and timing configuration; no BPSC negative-marking rule is hardcoded.

### Written preparation

```text
WrittenQuestion
  - Id
  - SyllabusItemId (verified topic/subtopic in a Written version)
  - Prompt
  - Type (Descriptive, Analytical, Essay, Composition, Translation)
  - Status (Draft, Published, Archived)
  - BcsExamYear / ExamNumber / ExamReference
  - Marks nullable
  - AnswerGuidance
  - CreatedById / timestamps

WrittenQuestionSourceReference
  - QuestionId
  - Title / Publisher / HTTPS Url / Citation / PublishedOn
  - VerificationStatus

WrittenQuestionResource
  - QuestionId
  - Title / HTTPS Url

WrittenQuestionTag (QuestionId, Tag)

WrittenModelAnswer
  - QuestionId (one per question)
  - Content
  - Status (Draft, Published, Archived)
  - UpdatedById / UpdatedAt

WrittenModelAnswerReference
  - ModelAnswerId
  - Title / Publisher / HTTPS Url / Citation
  - VerificationStatus

WrittenAnswer
  - Id
  - UserId
  - QuestionId
  - Status (Draft, Submitted)
  - Current Content / server-calculated WordCount
  - SelfAssessmentJson (personal only)
  - TimeSpentSeconds (self-reported)
  - StartedAt / UpdatedAt / SubmittedAt

WrittenAnswerRevision
  - Id
  - AnswerId / RevisionNumber
  - Kind (DraftSave, Submission, RevisionStarted)
  - Content snapshot / WordCount / TimeSpentSeconds
  - SelfAssessmentJson / CreatedAt

WrittenBookmark (UserId, QuestionId)

WrittenGoal
  - Id
  - UserId
  - StartDate / EndDate
  - TargetSubmissions / TargetWords

WrittenStudyNote
  - Id
  - UserId / SyllabusItemId
  - Title / Content
  - CreatedAt / UpdatedAt
```

Only source-attributed questions mapped to verified items in published Written syllabus versions can be published. Model answers have their own publication status and require verified references. Personal answers, notes, goals, bookmarks, and revision history are scoped to their owner. Word count is server-calculated; time is explicitly self-reported. Self-assessment is not an official score or automated/AI evaluation.

### Viva preparation

```text
VivaQuestion
  - Id
  - CategoryId
  - Topic nullable (editor-defined)
  - Prompt
  - Status (Draft / Published / Archived)
  - Optional BCS exam year/number/reference

VivaCategory
  - Id
  - Slug / Name / Description
  - DisplayOrder / IsActive

VivaQuestionReference
  - Id
  - VivaQuestionId
  - Type (QuestionSource / SuggestedAnswer)
  - Title / Publisher / HTTPS URL / Citation
  - VerificationStatus (editor-entered)

VivaBookmark (UserId, VivaQuestionId)
VivaQuestionHistory (UserId, VivaQuestionId)

VivaAnswerNote
  - Id
  - UserId
  - VivaQuestionId
  - Content / CreatedAt / UpdatedAt

VivaAnswerNoteRevision
  - VivaAnswerNoteId
  - RevisionNumber / Content / CreatedAt

VivaMockSession
  - Id
  - UserId / Title / Status
  - StartedAt / CompletedAt
  - Optional 1-5 self-ratings
  - FeedbackNotes / ImprovementAreas

VivaMockSessionQuestion
  - SessionId / VivaQuestionId / Sequence
  - PromptSnapshot / CategorySnapshot / TopicSnapshot
  - AnswerNotes / NeedsRevision

VivaProfilePreparation
  - UserId
  - SelfIntroduction / AcademicBackground / UniversityAndSubject
  - HomeDistrict / ProfessionalExperience / CadrePreferences
  - InterestsAndHobbies / StrengthsAndDevelopmentAreas
  - UpdatedAt
```

Viva categories are seeded from the project-requested labels and remain editable. They are application taxonomy only, not a BPSC-published topic list. No Viva questions or model answers are seeded. User notes, sessions, history, bookmarks, evaluations, and profile fields are owner-scoped. Session question text/order is snapshotted for history stability. Ratings and weak-topic flags are personal reflections, not official or automated evaluation; no voice or AI data is stored.

### Library (implemented in `AddDigitalLibrary`)

```text
LibraryResources
  - Id, OwnerId -> AspNetUsers (restrict)
  - Kind: Book | File | ExternalLink | Note (fixed at creation)
  - Category: General | Textbook | StudyNote | GovernmentPublication | Report | NewspaperArticle | Reference | ChapterMaterial | Other
  - Visibility: Private | Shared;  Status: Draft | Published | Archived
  - Title, Description, Authors, SourceName, SourceType, Website, Url, PublicationDate (date)
  - RightsBasis: NotApplicable | PersonalCopy | OwnWork | PublicDomain | GovernmentPublication | OpenLicense | PermissionGranted
  - RightsNote, NoteContent (longtext), CreatedAt, UpdatedAt, PublishedAt
  - IX (Visibility, Status, Kind), (OwnerId, Visibility), Title, UpdatedAt

LibraryBookDetails            1:1 with a Book resource (PK = ResourceId, cascade)
  - Publisher, Edition, PublicationYear, Isbn (normalised ISBN-10/13, indexed), PageCount, Language

LibraryFiles                  metadata only; bytes live in IFileStorage
  - Id, ResourceId (cascade), Role: Primary | Cover, Format: Pdf | Image | Document | Text
  - OriginalFileName (sanitised), ContentType (server-detected), SizeBytes, Sha256 char(64)
  - StorageProvider, StorageKey (unique), UploadedById -> AspNetUsers (restrict), UploadedAt
  - UX (ResourceId, Role)

LibraryBookChapters           Id, ResourceId (cascade), ChapterNumber, Title, Summary, StartPage, EndPage; UX (ResourceId, ChapterNumber)
LibraryChapterTopics          PK (ChapterId, SyllabusItemId); SyllabusItem restrict
LibraryChapterResources       PK (ChapterId, ResourceId); chapter-wise resources, both cascade
LibraryResourceSubjects       PK (ResourceId, SubjectId); Subject restrict
LibraryResourceTopics         PK (ResourceId, SyllabusItemId); SyllabusItem restrict
LibraryResourceTags           PK (ResourceId, Tag)

LibraryCollections            Id, OwnerId (cascade), Name, Description; UX (OwnerId, Name)
LibraryCollectionItems        PK (CollectionId, ResourceId), SortOrder, AddedAt
LibraryBookmarks              Id, UserId, ResourceId (cascade), ChapterId (set null), Page, Label, CreatedAt
LibraryReadingProgress        PK (UserId, ResourceId), Status, CurrentChapterId (set null), CurrentPage, TotalPages,
                              PercentComplete, StartedAt, CompletedAt, UpdatedAt
LibraryPersonalNotes          Id, UserId, ResourceId (cascade), ChapterId (set null), Page, Content (longtext)
```

Design notes:

- Books are resources, so bookmarks, progress, notes, collections, tags and syllabus mappings apply uniformly to books, files, links and notes without parallel tables.
- Large files are never stored in MySQL. `StorageKey` is a random provider key (`yyyy/MM/{guid}` for the local provider), never derived from the uploaded file name.
- Resource owners are `restrict`, so an account with private resources cannot be deleted until its files are removed through the API, which also deletes the blobs. Personal tracking rows (bookmarks, progress, notes, collections) cascade with their user and with their resource.
- Topic links may reference a syllabus item of any stage (Preliminary, Written, Viva) as long as its version is published. The `restrict` behaviour keeps mapped syllabus items from being deleted silently.
- Visibility is enforced in the application layer (`LibraryService.VisibleTo` / `ManagedBy`); see [LIBRARY_SECURITY_REVIEW.md](LIBRARY_SECURITY_REVIEW.md).
- No library rows are seeded. The seeder adds only the `library.manage` permission, mapped to Administrator and ContentManager.

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
- `McqQuestion *-1 SyllabusItem` for its versioned Preliminary topic/subtopic association
- `McqQuestion 1-* McqQuestionOption / McqQuestionSourceReference / McqQuestionTag`
- `McqExamSet 1-* McqAttempt`
- `McqAttempt 1-* McqAttemptQuestion 1-* McqAttemptOption`
- `McqAttemptQuestion 1-0..1 McqAttemptAnswer`
- `User *-* McqQuestion` through bookmarks and mistake records
- `WrittenQuestion *-1 SyllabusItem`; questions have source, resource, and tag children
- `WrittenQuestion 1-0..1 WrittenModelAnswer 1-* WrittenModelAnswerReference`
- `User 1-* WrittenAnswer 1-* WrittenAnswerRevision`
- `User *-* WrittenQuestion` through Written bookmarks
- `User 1-* WrittenGoal / WrittenStudyNote`; notes target a Written syllabus item
- `VivaCategory 1-* VivaQuestion 1-* VivaQuestionReference`
- `User *-* VivaQuestion` through bookmarks and question-history rows
- `User 1-* VivaAnswerNote 1-* VivaAnswerNoteRevision`
- `User 1-* VivaMockSession 1-* VivaMockSessionQuestion`; each session item snapshots one Viva question
- `User 1-0..1 VivaProfilePreparation`
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
- MCQ question status/difficulty/year and syllabus-item association
- Question option order, source references, and tag lookup
- MCQ exam-set status/mode
- Attempt user/start/status and attempt-question position/question
- Unique user/question bookmark and mistake records
- Written-question status/type/year and source/item association
- Written answer user/status/updated date and revision answer/number
- Written bookmark user/question and Written goal/note ownership
- Unique Viva category slug and category/question/status
- Unique Viva user/question bookmark, history, and note
- Unique Viva note revision number and mock-session question sequence
- Viva session user/start time and session question weak-topic lookup
- Study progress user/version/topic/subtopic
- Revision next review date by user
- Resource status/type/uploader
- Reading progress user/resource
- Current affairs publication/event dates and category/status

## Migration strategy

Migrations are application phase deliverables. The Phase 4 `AddVersionedSyllabus`, Phase 5 `AddMcqPracticeEngine`, Phase 6 `AddWrittenPreparation`, and Phase 7 `AddVivaPreparation` migrations have been generated and reviewed; all must be applied to the configured MySQL database before the current API can start. Before execution:

1. Update entity configurations and DbContext.
2. Generate an EF Core migration.
3. Review it for MySQL-specific types and destructive operations.
4. Run against a disposable local database when available.
5. Run integration tests.
6. Apply only to the explicitly configured database.

Use the exact `dotnet ef database update` command in the [Development Guide](DEVELOPMENT_GUIDE.md). Do not apply migrations until the connection string points to the intended disposable or managed MySQL database.
