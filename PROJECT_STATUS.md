# Project Status

## Last updated

2026-11-21

## Current phase

Phase 4 - Dynamic BCS Syllabus Management. The versioned syllabus foundation, public APIs, admin workflow, learner explorer, and user-owned completion tracking are implemented.

## Completed

- [x] Architecture, database design, API conventions, implementation roadmap, and setup documentation
- [x] React/TypeScript/Vite/Tailwind responsive application shell and initial routes
- [x] .NET 10 Clean Architecture solution and MySQL EF Core foundation
- [x] ASP.NET Core Identity registration/login, password hashing, JWT and permission-based authorization
- [x] Audit log foundation, validation, Problem Details, Swagger, CORS, and health endpoints
- [x] Versioned syllabus schema for exam stages, track-specific versions, subjects, papers, topic/subtopic items, sources, and topic progress
- [x] `AddVersionedSyllabus` migration generated and reviewed
- [x] Idempotent seeding for BPSC stage structure, official subject/marks outlines, source links, and explicit coverage notes
- [x] Permission-protected syllabus stage/version/subject/paper/item/source management and publish/archive lifecycle
- [x] Published syllabus tree/search/source APIs and user-scoped topic progress API
- [x] Responsive API-backed syllabus explorer, source/verification labels, search, details, and progress controls
- [x] Syllabus administration screens and in-memory sign-in/registration flow for authenticated actions
- [x] Official source register documenting verified facts, source URLs, and content not yet verified
- [x] Focused backend request-validation, password-hashing, pagination, and syllabus request tests

## Known limitations

- No local MySQL server was available. The migration is generated and reviewed but has not been applied; database/API integration smoke tests could not run.
- BPSC's detailed Preliminary and Written PDF topic text has not been transcribed or verified. Seeded versions are marked `OutlineOnly`; no topic-level official text is invented.
- The inspected BPSC Viva source states the oral exam marks/pass mark, but does not publish a topic-by-topic Viva syllabus. No Viva topics are seeded.
- The 50th BCS 2025 Preliminary notice is exam-specific and is not applied as a universal version.
- No administrator user is created or promoted automatically. Content-manager/admin provisioning remains an operator task.
- Access JWTs are kept in browser memory and cleared on reload/expiry; refresh-token, logout revocation, and account-recovery flows are not implemented.
- Topic notes are navigation-only in this phase; no notes are saved. MCQ, library, planner, and other study modules are out of scope.

## Pending

- [ ] Apply migrations and verify seed/API behavior against a configured disposable MySQL database
- [ ] Review and import detailed official syllabus topics into a new version after source verification
- [ ] Add automated API integration and frontend end-to-end tests when a test database/runtime is available
- [ ] Implement the remaining study, question-bank, library, planner, current-affairs, and deployment modules in roadmap order

## Testing status

See the final task report for the latest backend build/tests and frontend build/lint results. Database migration application remains blocked by the unavailable local MySQL service.
