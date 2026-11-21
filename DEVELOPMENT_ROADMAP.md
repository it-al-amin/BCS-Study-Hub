# Development Roadmap

## Dependency principle

Every later module depends on the base platform. Build a vertical slice rather than an entire entity layer. Add migrations only when the current vertical slice needs persistence.

## Phase 1 - Architecture and planning

Status: complete

Deliverables:

- architecture, database, API, and roadmap documentation
- proposed repository structure
- setup instructions
- detailed implementation checklist
- no code or migrations

Exit criteria:

- architecture is approved
- initial vertical slice is selected
- setup decisions are resolved

## Phase 2 - React frontend foundation

Dependencies: none

Build:

- React, TypeScript, Vite, Tailwind, and React Router
- responsive application shell and reusable UI components
- initial route map and typed API client
- accessible loading, empty, error, and responsive navigation states

The frontend foundation and requested initial routes are implemented. The syllabus UI is now connected to the Phase 4 APIs.

## Phase 3 - ASP.NET Core backend and MySQL foundation

Dependencies: Phase 2

Build:

- Clean Architecture solution and project references
- EF Core/MySQL, Identity, JWT authentication, roles, permissions, and audit baseline
- validation, Problem Details, logging, Swagger, health checks, and backend tests

The backend foundation is implemented. No question-bank, library, or other future-module tables are included.

## Phase 4 - Complete dynamic BCS syllabus management

Dependencies: Phases 2 and 3

Build:

- versioned Preliminary, Written (General and Technical/Professional tracks), and Viva syllabus outlines
- subject, optional paper, topic/subtopic, source-reference, and user-owned progress schema
- public published tree/search and authenticated progress APIs
- permission-protected draft management and publish/archive lifecycle
- responsive learner explorer and syllabus admin UI
- official-source register; never infer or present unverified content as official

The verified outline/API/UI vertical slice is implemented. Detailed topic text remains unverified and is explicitly marked as such.

## Phase 5 - Preliminary preparation and MCQ practice engine

Dependencies: Phase 4

The API, data model, migration, administration, and learner UI are implemented. The .NET build/tests and frontend build pass.

Build:

- sourced Preliminary MCQ authoring and browsing with topic/subtopic mapping and filters
- topic, subject, mixed, and mock-exam sets with editable count, timing, and scoring configuration
- server-authoritative answer validation, timeout handling, scoring, and attempt snapshots
- history, review, bookmarks, mistakes, and performance analytics

No sample questions or detailed topics are seeded. Publishing requires verified question sources and a verified topic in a published Preliminary version. Multiple-answer scoring uses exact-set matching; BPSC rules are not hardcoded.

## Phase 6 - Written preparation

Dependencies: Phase 4

The Written question, answer-writing, study-note, goal, and reporting vertical slice is implemented with source verification and user ownership. It has no seeded question content and performs no automatic/AI evaluation.

Build:

- Written syllabus exploration with source and verification indicators
- descriptive, analytical, essay, composition, and translation questions where verified
- model answers, references, study notes, topic/resource mapping, and admin management
- personal answer drafts, submission/revision history, word/time tracking, bookmarks, and self-assessment rubric
- written progress, weak-topic tracking, goals, and writing reports

Do not fabricate official content or imply automatic AI evaluation.

## Phase 7 - Complete Viva preparation

Dependencies: Phase 4

Build:

- editable Viva categories and source-aware questions with topic/category/search filters
- private bookmarks, answer notes/revisions, question history, and profile preparation
- ordered mock sessions, answer notes, completion, self-evaluation, feedback, and weak-topic tracking
- permission-protected category/question administration

Categories are application taxonomy, not an official BPSC topic list. No questions are seeded; voice recording and AI evaluation are not implemented.

## Phase 8 - Digital library

Dependencies: Phase 4 and storage provider decision (local filesystem behind `IFileStorage` for now)

Build:

- resource metadata, secure upload/download, books and chapters, collections, bookmarks, reading progress, personal notes, and topic associations
- private (owner-only) and shared (`library.manage`, draft/publish/archive) resources with a recorded rights basis

Copyright, access control and file security were reviewed before implementation; see [LIBRARY_SECURITY_REVIEW.md](LIBRARY_SECURITY_REVIEW.md). No library content is seeded. Malware scanning and a cloud storage provider are deferred.

## Phase 9 - Current affairs and official resource center (implemented)

Dependencies: Phases 4, 5, 6, 7, and 8

Build:

- curated current-affairs workflow with 19 categories, tags, event/source publication dates, and a verified-source publication gate
- daily, weekly (ISO), monthly, and yearly collections, archive, date/category/tag/topic filters, and search
- syllabus subject/topic, MCQ/Written/Viva question, publisher-reference, and shared-library links
- private bookmarks, study notes, spaced revision, and printable summaries
- official resource directory with manual, dated URL verification (see [OFFICIAL_SOURCES.md](OFFICIAL_SOURCES.md))

Reordered ahead of the planner at the project owner's request. No scraping; no current-affairs content is seeded.

## Phase 10 - Planner, revision, and progress tracking (implemented)

Dependencies: Phases 4–9

Build:

- daily/weekly/monthly goals computed from recorded activity, tasks with priorities and planned/actual time, recurring schedules, a session timer and manual logs, and a calendar
- topic completion and mastery, subject and stage progress, study time, streaks, and practice performance
- a revision queue with user-defined intervals and outcome rules; imports from bookmarks, mistakes, weak topics, and notes; history; current affairs on the same policy
- a mistake notebook with categories, correct-answer references, review dates, history, and topic mapping
- daily, weekly, and monthly reports and a database-backed dashboard

## Phase 11 - Admin and content management system (implemented)

Dependencies: all content modules

- one administration console over the existing module services (no duplicated CRUD): an overview dashboard, bulk publish/archive, cross-module tags, a categories overview, an audit history UI and per-record history panels, site settings with server-enforced registration control, a Markdown editor with safe preview, hierarchical topic selection, and a reusable resource picker
- server-side authorization verified by reflection tests

## Phase 12 - Audit, hardening, and deployment readiness (implemented; see AUDIT_REPORT.md)

Dependencies: all selected modules

Done: security and performance fixes, HTTP-pipeline security tests, Docker/nginx/Compose, CI, backup/restore, and deployment documentation. Not done: running against MySQL, end-to-end tests, malware scanning, accessibility tooling.

Original scope:

- security review
- rate limiting
- upload scanning integration if approved
- backup/restore procedure
- performance tuning
- accessibility pass
- automated and end-to-end tests
- CI/CD
- deployment configuration

## Phase 13 - Integration verification and session hardening (next)

- MySQL Testcontainers integration suite over all migrations and module APIs
- Playwright smoke and axe accessibility tests
- refresh-token sessions with revocation
- malware scanning, object storage, and a distributed rate-limit store when scaling out

## Module dependency matrix

```text
Identity --------> every authenticated module
Syllabus ---------> topics, progress, questions, resources
MCQ --------------> verified Preliminary syllabus items, attempt analytics
Written ----------> syllabus, personal answer state
Library ----------> syllabus, books, current affairs
Viva -------------> syllabus, personal session state
Planner ----------> syllabus, sessions, revision
Dashboard --------> progress, attempts, planner, revision
Current Affairs --> syllabus, library, questions
Admin ------------> every editable entity
```

## Detailed implementation checklist

### Phase 1

- [x] Analyze complete requirements
- [x] Define Clean Architecture
- [x] Define frontend architecture
- [x] Define MySQL architecture
- [x] Define initial entities
- [x] Define API conventions
- [x] Define authentication model
- [x] Define content lifecycle
- [x] Define storage model
- [x] Define syllabus hierarchy
- [x] Define study/practice module boundaries
- [x] Define testing strategy
- [x] Define implementation phases
- [x] Obtain approval for initial foundation stack and versions

### Foundation

- [x] Confirm SDK/package versions (.NET 10 / EF Core 10)
- [x] Create solution and React workspace
- [x] Add EF Core/MySQL
- [x] Add auth and permissions
- [x] Add audit
- [x] Add frontend shell
- [x] Add health/config/OpenAPI
- [x] Add test projects
- [x] Run baseline build/tests

### Syllabus

- [x] Add entities and migrations
- [x] Add seed permissions and stage/source-verified outline data
- [x] Add CRUD/use cases
- [x] Add publish/archive
- [x] Add tree/search/progress endpoints
- [x] Add admin UI
- [x] Add request validation and build/tests

### Phase 5 - MCQ

- [x] Add Preliminary-only MCQ entities, source references, tags, exam sets, attempt snapshots, bookmarks, and mistakes
- [x] Add permission-protected question/set management and publication validation
- [x] Add question bank, practice/mock attempt interface, review/history, bookmarks, mistakes, and analytics
- [x] Add server-side answer validation and configurable scoring tests
- [x] Generate migration and idempotent SQL script
- [ ] Apply migrations and run MySQL integration tests when a configured database is available

### Phase 6 - Written

- [x] Add source-aware Written question, model answer/reference, resource, and topic-mapping entities/APIs/admin workflow
- [x] Add private answer drafts, server word count, self-assessment, submissions, immutable revisions, and bookmarks
- [x] Add Written study notes, writing goals, and topic-level progress reports
- [x] Add responsive Written syllabus/question browser, answer-writing workspace, history, notes, bookmarks, and admin UI
- [x] Generate and inspect the Written migration
- [ ] Apply migrations and run MySQL integration tests when a configured database is available

### Phase 8 - Digital library

- [x] Review copyright, access control and file security before implementation
- [x] Add resource, book, file, chapter, mapping, tag, collection, bookmark, reading-progress, and personal-note entities
- [x] Add `IFileStorage`, a local provider, verified uploads, quotas, rate limits, and secured streaming downloads
- [x] Add private/shared visibility, `library.manage` administration, and rights-basis enforcement for shared files
- [x] Add library dashboard, catalogs, detail/viewer, editor, collections, reading/bookmarks, and admin UI
- [x] Add file-security, validation, and service-level access-control tests
- [x] Generate and inspect the `AddDigitalLibrary` migration
- [ ] Apply migrations and run MySQL/HTTP integration tests when a configured database is available
- [ ] Add malware scanning and an object-storage provider when infrastructure is approved

### Phase 9 - Current affairs

- [x] Add entry, tag, subject/topic, question, reference, library-resource, bookmark, study-note, revision, and official-source entities
- [x] Add digests (day/week/month/year), archive, filters, search, and the verified-source publication gate
- [x] Add the official directory with manual verification records, URL-change invalidation, and a 365-day freshness rule
- [x] Seed only directory URLs whose landing page identified the organisation; seed failed checks as drafts
- [x] Add learner, printable digest, revision, directory, and admin UI
- [x] Add rule, validation, and service-level tests; generate and inspect the migration
- [ ] Apply migrations and run MySQL/HTTP integration tests when a configured database is available
- [ ] Verify the seven draft directory URLs in a browser

### Future modules

- [x] Implement Phase 7 Viva categories, questions, private practice state, ordered sessions, profile preparation, and admin
- [x] Implement Phase 8 digital library
- [x] Implement Phase 9 current affairs and official resource center
- [x] Implement Phase 10 planner, revision, mistake notebook, tracking, reports, and dashboard
- [ ] Add remaining modules in dependency order only
- [ ] Review database impact before each migration
- [ ] Keep module scope to one vertical slice
