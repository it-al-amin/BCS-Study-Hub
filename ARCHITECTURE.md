# BCS Study Hub Architecture

## 1. System overview

The platform uses a modular monolith instead of microservices. A browser frontend communicates with one ASP.NET Core Web API, backed by MySQL and an external file-storage abstraction. Logical modules remain independently organized so they can be developed and tested without introducing distributed-system complexity.

```text
React / TypeScript / Vite
         |
         | REST / OpenAPI
         v
ASP.NET Core Web API
         |
         v
Application services + interfaces
         |
         v
EF Core
         |
         v
MySQL

File API -> Storage abstraction -> local filesystem / object storage
```

## 2. Clean Architecture

### Domain

Contains entities, enums, value objects, domain events, and domain invariants. It must not depend on EF Core, ASP.NET Core, or frontend contracts.

### Application

Contains use cases, command/query handlers or application services, DTO contracts, validation, authorization policies, mapping rules, interfaces, and orchestration. It must not depend on ASP.NET infrastructure or MySQL implementation details.

### Infrastructure

Contains EF Core DbContext and migrations, repositories only where they add value, MySQL configuration, identity/JWT integration, storage providers, audit logging, email/background-worker integrations, and external source adapters.

### API

Contains thin controllers or minimal API endpoints, middleware, Swagger/OpenAPI, request bindings, authentication setup, file-download endpoints, health endpoints, and global exception handling.

## 3. Core domain entities

### Identity and authorization

- `User`: id, identity fields, profile, status, timestamps
- `Role`: id, name, normalized name, description
- `Permission`: id, code, description
- `RolePermission`: role and permission many-to-many
- `UserRole`: user and role many-to-many
- `RefreshToken`: user, token hash, expiry, revocation, replacement metadata
- `AuditLog`: actor, action, entity type/id, old/new values or changes, timestamp

Roles and permissions are database-configurable. The UI and API must authorize against permission keys, not a growing set of hardcoded role checks.

### Examination model

- `ExamStage`: preliminary, written, viva
- `SyllabusVersion`: stage, version label, candidate track, effective period, draft/published/archived status, coverage scope, source references
- `Subject`: code, name, description, status
- `Paper`: subject, title, sequence, optional pass/marks metadata
- `SyllabusSubject`: version-to-subject mapping, order, marks, required flag
- `SyllabusItem`: one versioned hierarchy table with a `Topic`/`Subtopic` discriminator, optional paper, parent topic, order, verification status
- `SyllabusSourceReference`: official source metadata and verification status; linked to versions and individual items
- `TopicProgress`: user-owned completion state referencing a syllabus item

The hierarchy is `ExamStage -> SyllabusVersion -> SyllabusSubject -> Paper (optional) -> Topic -> Subtopic`. `SyllabusItem` stores topic and subtopic nodes once, avoiding duplicated topic/subtopic tables. Revisions of syllabus content receive separate versions and are not silently overwritten. A written version is track-specific (`General` or `TechnicalProfessional`); common stages use `Common`.

### Syllabus and publication metadata

- `SourceAttribution`: name, URL, publisher, document, publication date
- `SyllabusVersionSource`: links syllabus versions to official sources
- `ContentStatus`: draft, published, archived
- `Tag`
- `EntityTag`: polymorphic-like ownership through a normalized tagged-item table
- `Category`

### Study content

- `StudyNote`: title, content, topic/subtopic associations, status, version metadata
- `PersonalNote`: user-owned note, target entity, content, status
- `RevisionItem`: user, content target, interval state, next review date
- `TopicProgress`: user, topic/subtopic, status, completion, time spent
- `StudySession`: user, start/end, duration, target, note
- `StudyGoal`: user, scope, target, period, status
- `Bookmark`: user, target entity
- `MistakeEntry`: user, question/content target, user reason, review state

### MCQ system

- `Question`: polymorphic stage/paper/topic associations, question type, prompt, explanation, difficulty, status
- `McqQuestion`: option or discriminator entity if a dedicated subtype is required
- `McqOption`: question, text, correctness, sequence, explanation
- `QuestionTag`
- `QuestionAttempt`: user, question, selected option, score, timing, timestamp
- `Exam`
- `ExamSection`
- `ExamQuestion`
- `ExamAttempt`
- `ExamAttemptAnswer`

Randomized questions are selected at attempt creation and stored against the attempt so later question changes cannot alter the submitted result.

### Written system

- `WrittenQuestion`: prompt, format, stage/paper/topics, marks, difficulty, source
- `WrittenAnswer`: user, question, submitted content, word/character count, status
- `AnswerEvaluation`: answer, personal score or rubric, feedback
- `ModelAnswer`
- `ModelAnswerReference`

### Viva system

- `VivaQuestion`: prompt, category, expected topics, difficulty, status
- `MockVivaSession`: user, planned/completed times, notes, overall result
- `VivaSessionAnswer`: session, question, answer summary/notes, timing, evaluation
- `CadreResearchProfile`: optional cadre-specific questions and resources

### Digital library

- `Book`: title, authors, publisher, edition, publication year, description, cover resource
- `BookChapter`: book, title, sequence, resource/topic associations
- `Resource`: type, title, description, original name, MIME type, size, storage key, status
- `ResourceFile`: resource, provider key, checksum, size, content type
- `ExternalLink`: URL, title, source attribution, status
- `ResourceCollection`
- `ResourceCollectionItem`
- `ResourceBookmark`
- `ResourceReadingProgress`: user, resource, position, completion, total pages if available
- `ResourcePersonalNote`
- `ResourceTopicLink`
- `ResourceSource`

Large files remain outside MySQL. Metadata and checksums are relational records.

### Current affairs

- `CurrentAffair`: title, summary, content, event date, publication date, category, status
- `CurrentAffairTopicLink`
- `CurrentAffairQuestionLink`
- `CurrentAffairResourceLink`
- `GovernmentResource`: ministry/agency, title, URL, jurisdiction, status
- `Publication`: title, publisher, publication date, resource, source URL

## 4. Relationship rules

- A subject can belong to one or more syllabus versions through `SyllabusSubject`.
- A paper belongs to a subject but is optional.
- Topics and subtopics belong to a syllabus version through `SyllabusSubject`; subtopics have one topic parent in the same mapping and paper.
- Published versions are immutable. Draft-to-published publication requires a verified source. `FullTopics` versions also require every topic and subtopic to have a verified item-level source.
- User progress is unique per user and syllabus item and is never returned across user boundaries.
- Questions, notes, resources, and current-affair entries associate with syllabus nodes through many-to-many link tables.
- A resource can belong to subjects, topics, subtopics, books, collections, or current-affair entries.
- Users own personal notes, progress, attempts, answers, goals, bookmarks, and mistake entries.
- Published content remains immutable except through an explicit content revision; drafts and revisions are auditable.
- Soft deletion or archival is preferred for user-visible content.

## 5. Indexing and integrity

Use database constraints and indexes for:

- Unique normalized names and codes where relevant
- Version/content status and publication date
- Foreign keys and junction-table uniqueness
- Hierarchy ordering
- Question filtering by stage, paper, subject, topic, difficulty, and status
- Attempt/user/timestamp queries
- Revision queue by user and next-review date
- Resource name, type, status, and collection membership
- Full-text search only after MySQL and EF provider support is confirmed

## 6. Frontend architecture

```text
src/
  app/                 routes, providers, layout, guards
  components/          generic UI, accessible reusable components
  features/
    auth/
    dashboard/
    roadmap/
    syllabus/
    preliminary/
    written/
    viva/
    subjects/
    questions/
    exams/
    library/
    current-affairs/
    planner/
    revision/
    progress/
    notes/
    admin/
  services/            typed API clients
  types/               shared API/domain DTO types
  hooks/
  stores/              only where shared state cannot be server state
  utils/
```

### Route structure

- `/dashboard`
- `/roadmap`
- `/syllabus`
- `/preliminary`
- `/written`
- `/viva`
- `/subjects/:subjectId`
- `/subjects/:subjectId/topics/:topicId`
- `/questions/mcq`
- `/questions/written`
- `/questions/viva`
- `/exams`
- `/exams/:examId`
- `/exams/:examAttemptId/review`
- `/library`
- `/library/books/:bookId`
- `/library/resources/:resourceId`
- `/current-affairs`
- `/planner`
- `/revision`
- `/progress`
- `/notes`
- `/admin/*`

The `admin/*` area is nested behind permission routes. Public and self-service routes must not be inferred from the presence of UI controls.

## 7. Backend API module structure

Suggested module folders:

- `Auth`
- `Users`
- `RolesPermissions`
- `Syllabi`
- `Subjects`
- `Topics`
- `Notes`
- `Questions`
- `Exams`
- `WrittenPractice`
- `Viva`
- `Library`
- `CurrentAffairs`
- `GovernmentResources`
- `Planner`
- `Revision`
- `Progress`
- `Dashboard`
- `Admin`
- `Audit`

Controllers remain thin. Queries and mutations use application services or command/query handlers with FluentValidation, DTO mapping, and authorization policies.

## 8. Authentication and authorization

- Short-lived access JWT
- Refresh token rotation with secure HTTP-only cookie or other approved secure transport
- Password hashing through ASP.NET Core Identity-compatible APIs or a reviewed password-hasher implementation
- Permission claims and/or database-backed policies
- Admin and content-manager roles are permissions, not hardcoded controller logic
- Account lockout, failed-login protection, password reset, and secure logout
- CSRF protection where cookies are used
- CORS allowlists
- Audit logs for admin, publication, role, file, and authentication actions

No password, JWT signing key, database credential, or storage secret belongs in source control.

## 9. File storage architecture

Create `IFileStorage` with provider-independent operations:

- open/write/delete
- metadata validation
- streaming download
- unique object key generation
- checksum calculation

Implement local and cloud provider adapters behind that interface. The API validates:

- allowlisted extensions
- verified content signatures and MIME where practical
- maximum file size
- malware scanning before publication if infrastructure is available
- collision-resistant storage keys
- signed, short-lived download URLs for private resources

Store original name, storage key, MIME type, size, checksum, privacy level, status, uploader, and timestamps in MySQL. Never use database blobs for normal uploaded files.

## 10. API conventions

- Pagination: `pageNumber`, `pageSize`
- Sorting: explicit, allowlisted fields
- Filtering: typed filter DTOs
- Stable response envelopes
- RFC 7807 problem details
- Correlation IDs
- Audit logging for critical mutations
- Serilog or equivalent structured logging
- Health and readiness endpoints
- Swagger/OpenAPI only on approved environments

## 11. Development phases

### Phase 1 - Architecture and planning

- Approve architecture, initial technology stack, and implementation roadmap

### Phase 2 - React frontend foundation

- Responsive React/TypeScript application shell, reusable UI, and initial routes

### Phase 3 - ASP.NET Core backend and MySQL foundation

- Clean Architecture, EF Core/MySQL, Identity/JWT, permissions, audit, validation, and health

### Phase 4 - Dynamic BCS syllabus management

- Versioned stage/subject/paper/topic hierarchy, sources, publication, and user progress
- Responsive learner explorer and permission-protected administration
- Import only content verified against official sources

### Phase 5 - Study content and personal notes

- Notes and personal notes
- Tags/categories and content revision workflow

### Phase 6 - Digital library

- Book/resource metadata
- Storage abstraction and secure upload/download
- Collections and topic associations

### Phase 7 - MCQ practice

- Question and option CRUD
- Topic associations
- Practice sessions
- Attempt history
- Explanations and weak-topic calculations

### Phase 8 - Mock exams

- Exam templates
- Sections and question selection
- Timed attempts
- Randomization
- Review and score analytics

### Phase 9 - Written and viva

- Written questions and answers
- Personal evaluation and model answers
- Viva questions and mock sessions
- Profile and cadre research

### Phase 10 - Planner, revision, and dashboard

- Daily/weekly goals
- Study sessions
- Revision scheduling
- Progress dashboard
- Real performance trends

### Phase 11 - Current affairs and government resources

- Curated content workflow
- Official-source metadata
- Date/category/tag filtering
- Resource and topic associations

### Phase 12 - Hardening and delivery

- Docker Compose
- CI/CD
- Security testing
- Integration and end-to-end tests
- Backup and restore testing
- Performance tuning

## 12. Architectural risks

- Syllabus changes can invalidate old questions or become ambiguous: solve with versioned syllabus relationships.
- File storage coupling can become expensive: isolate behind `IFileStorage`.
- MySQL full-text behavior and search requirements differ by deployment: start with relational filtering and add the smallest justified search layer later.
- Over-normalization makes routine screens complex: keep core relational structure normalized and provide read-optimized application DTOs.
- Hardcoded roles or stage names fragment authorization: use permission policies and database-managed taxonomy.
- A "MCQ" or "written" question hierarchy added too late may require redesign: use a reusable question base plus explicit type-specific content.
- Large assessments can generate many child rows: use bounded transactions and explicit attempt snapshot tables.
- Content provenance and copyright: require attribution, publication metadata, and review status; do not import at scale without permission.

## 13. Initial implementation checklist

- [x] Create solution and frontend workspace
- [x] Set up configuration and secret separation
- [x] Establish database and EF Core conventions
- [x] Add base migrations for identity, roles, permissions, and audit
- [x] Add global validation and problem-details middleware
- [x] Implement authentication and authorization
- [x] Implement versioned syllabus hierarchy, source references, and user-owned topic progress
- [x] Add permission-protected draft/publish administration and published tree/search APIs
- [x] Add frontend API integration, sign-in for progress/admin, and responsive syllabus explorer
- [x] Build and test the first syllabus endpoint/UI flow
- [ ] Add storage abstraction and one provider
- [ ] Expand content modules in approved phases

The Phase 4 syllabus implementation publishes only BPSC-verified stage/subject/marks outlines. Detailed topic lists remain clearly marked as not yet verified; see [SYLLABUS_SOURCES.md](SYLLABUS_SOURCES.md).

## 14. Decisions requiring later approval

- Production storage provider and deployment configuration
- Registration policy for production
- Search provider after core CRUD needs are known
