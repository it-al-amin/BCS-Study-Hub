# Development Roadmap

## Dependency principle

Every later module depends on the base platform. Build a vertical slice rather than an entire entity layer. Add migrations only when the current vertical slice needs persistence.

## Phase 1 - Architecture and planning

Status: current

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

## Phase 2 - Development foundation

Dependencies: none

Build:

- solution/workspace scaffold
- shared logging, configuration, health, CORS, validation, and Problem Details
- EF Core/MySQL baseline
- Identity or equivalent authentication
- role/permission/audit baseline
- frontend shell, route map, API client, authentication guard
- Docker Compose if approved

Do not build full syllabus or study modules here.

## Phase 3 - Versioned syllabus foundation

Dependencies: Phase 2

Build:

- `ExamStage`, `SyllabusVersion`, `Subject`, `Paper`, `Topic`, `Subtopic`
- admin CRUD
- publish/archive transitions
- syllabus tree read endpoint
- minimal React syllabus/admin pages

This phase proves hierarchy, versioning, permissions, and audit behavior.

## Phase 4 - Study content and personal notes

Dependencies: Phase 3

Build:

- tags/categories
- study notes
- personal notes
- source attribution
- content revision/publish workflow

## Phase 5 - Digital library

Dependencies: Phase 3 and storage provider decision

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

## Phase 6 - MCQ foundation

Dependencies: Phases 3 and 4

Build:

- question/options
- answer explanations
- topic filtering
- practice sessions
- attempt snapshots
- attempt history
- weak-topic calculation

## Phase 7 - Mock exams

Dependencies: Phase 6

Build:

- exam templates
- sections
- timed attempts
- randomization
- server-authoritative submission
- review
- score trends

## Phase 8 - Written and viva

Dependencies: Phases 3 and 4

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

## Phase 9 - Planner and progress

Dependencies: Phases 3, 4, and 6

Build:

- daily/weekly goals
- study sessions
- topic progress
- revision queue
- mistake notebook
- real database dashboard
- recent activity

## Phase 10 - Current affairs and government resources

Dependencies: Phases 3 and 5

Build:

- current-affairs workflow
- categories/tags/sources
- publication dates
- syllabus and question links
- government resource directory
- official external-link management

## Phase 11 - Hardening

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
- [ ] Obtain approval for initial foundation stack and versions

### Foundation

- [ ] Confirm SDK/package versions
- [ ] Create solution and React workspace
- [ ] Add EF Core/MySQL
- [ ] Add auth and permissions
- [ ] Add audit
- [ ] Add frontend shell
- [ ] Add health/config/OpenAPI
- [ ] Add test projects
- [ ] Run baseline build/tests

### Syllabus

- [ ] Add entities and migrations
- [ ] Add seed permissions and test administrator only
- [ ] Add CRUD/use cases
- [ ] Add publish/archive
- [ ] Add tree endpoint
- [ ] Add admin UI
- [ ] Add API and UI tests

### Future modules

- [ ] Add in dependency order only
- [ ] Review database impact before each migration
- [ ] Keep module scope to one vertical slice
