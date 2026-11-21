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

## Phase 5 - Study content and personal notes

Dependencies: Phase 4

Build:

- tags/categories
- study notes
- personal notes
- content revision/publish workflow

## Phase 6 - Digital library

Dependencies: Phase 4 and storage provider decision

Build:

- resource metadata
- secure upload/download
- local storage first or approved cloud provider
- books and chapters
- collections
- bookmarks
- reading progress
- resource personal notes
- topic associations

## Phase 7 - MCQ foundation

Dependencies: Phases 4 and 5

Build:

- question/options
- answer explanations
- topic filtering
- practice sessions
- attempt snapshots
- attempt history
- weak-topic calculation

## Phase 8 - Mock exams

Dependencies: Phase 7

Build:

- exam templates
- sections
- timed attempts
- randomization
- server-authoritative submission
- review
- score trends

## Phase 9 - Written and viva

Dependencies: Phases 4 and 5

Build written:

- written questions
- answer submission
- personal evaluation
- model answers and references
- answer history

Build viva:

- viva questions
- topic/profile/category filters
- mock sessions
- personal answer notes
- ratings and follow-up

## Phase 10 - Planner and progress

Dependencies: Phases 4, 5, and 7

Build:

- daily/weekly goals
- study sessions
- topic progress
- revision queue
- mistake notebook
- real database dashboard
- recent activity

## Phase 11 - Current affairs and government resources

Dependencies: Phases 4 and 6

Build:

- current-affairs workflow
- categories/tags/sources
- publication dates
- syllabus and question links
- government resource directory
- official external-link management

## Phase 12 - Hardening

Dependencies: all selected modules

Build:

- security review
- rate limiting
- upload scanning integration if approved
- backup/restore procedure
- performance tuning
- accessibility pass
- automated and end-to-end tests
- CI/CD
- deployment configuration

## Module dependency matrix

```text
Identity --------> every authenticated module
Syllabus ---------> topics, progress, questions, resources
Content ----------> notes, syllabus
Library ----------> syllabus, books, current affairs
MCQ --------------> syllabus, attempt analytics
Mock Exam --------> MCQ, exam templates
Written ----------> syllabus, personal answer state
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

### Future modules

- [ ] Add in dependency order only
- [ ] Review database impact before each migration
- [ ] Keep module scope to one vertical slice
