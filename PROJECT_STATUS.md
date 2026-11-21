# Project Status

## Last updated

2026-11-21

## Current phase

Phase 8 - Digital Library and Resource Management. Books, files, external links, study notes, chapters, collections, bookmarks, reading progress, personal notes, syllabus mappings, the storage abstraction with secure upload/download, learner/admin UI, migration, and tests are implemented and build-verified. MySQL migration application and database-backed integration remain pending.

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
- [x] Preliminary MCQ question/options, source references, tags, exam-set configuration, attempt snapshots, bookmarks, and mistake tracking
- [x] Permission-protected question and practice-set administration with verified-source/topic publication checks
- [x] Learner question bank, practice/mock attempts, answer review, history, bookmarks, mistakes, and analytics
- [x] Server-validated answers, immutable attempt snapshots, configurable scoring/timing, and focused scoring tests
- [x] `AddMcqPracticeEngine` migration and idempotent migration-script generation
- [x] Written question bank, verified-source references, syllabus mapping, and model answers
- [x] User-owned drafts, submissions, answer revisions, bookmarks, study notes, writing goals, and progress reports
- [x] Permission-protected Written question and model-answer administration with publication validation
- [x] Responsive Written syllabus/question explorer, answer-writing workspace, notes, history, and progress UI
- [x] Self-assessment rubric and model-answer comparison clearly identified as manual; no AI evaluation is claimed
- [x] `AddWrittenPreparation` migration and idempotent migration-script generation
- [x] Editable Viva categories, searchable questions, optional topic labels, source/suggested-answer references, and permission-protected administration
- [x] User-owned bookmarks, answer notes/revisions, question history, profile preparation, and ordered mock-session snapshots
- [x] Mock-session answer notes, completion, self-evaluation, feedback, revision flags, history, and weak-topic report
- [x] Viva learner and admin routes, API services, `viva.manage` policy, and seeded editable category labels (no questions seeded)
- [x] `AddVivaPreparation` migration generated and reviewed; API/request validation tests added
- [x] Copyright, access-control and file-security review recorded in `LIBRARY_SECURITY_REVIEW.md` before implementation
- [x] Library resources (books, files, external links, study notes) with category, source type, rights basis, subjects, topics, tags, and publication metadata
- [x] Book details (publisher, edition, year, validated ISBN, pages, language), covers, chapters with page ranges, chapter topics, and chapter-wise resources
- [x] `IFileStorage` abstraction with a local filesystem provider; random storage keys, SHA-256 hashes, and metadata-only MySQL rows
- [x] Upload validation: extension allowlist, magic-byte/Office-structure/UTF-8 checks, macro rejection, size limit, per-user quota, and rate limiting
- [x] Private (owner-only) and shared (`library.manage`, draft/publish/archive) resources; shared files limited to redistributable rights bases
- [x] Owner-scoped bookmarks (resource/chapter/page), reading progress, personal notes, and collections
- [x] Library dashboard, resource catalog with search/filters, book catalog, book/resource detail with chapter navigation, PDF/image/text viewer, editor, collections, reading/bookmarks, and admin management UI
- [x] `AddDigitalLibrary` migration generated and reviewed; file-security, validation, and service-level access-control tests added

## Known limitations

- No local MySQL server was available. The syllabus, MCQ, Written, Viva, and library migrations are generated and reviewed but have not been applied; database/API integration smoke tests could not run.
- BPSC's detailed Preliminary and Written PDF topic text has not been transcribed or verified. Seeded versions are marked `OutlineOnly`; no topic-level official text is invented.
- The inspected BPSC Viva source states the oral exam marks/pass mark, but does not publish a topic-by-topic Viva syllabus. No Viva topics are seeded.
- The 50th BCS 2025 Preliminary notice is exam-specific and is not applied as a universal version.
- No administrator user is created or promoted automatically. Content-manager/admin provisioning remains an operator task.
- Access JWTs are kept in browser memory and cleared on reload/expiry; refresh-token, logout revocation, and account-recovery flows are not implemented.
- No detailed verified Preliminary topics or MCQs are seeded. Authorized content managers must add verified syllabus items and source-attributed questions before publishing questions or practice sets.
- MCQ images are HTTPS references only; file upload is not implemented. Written answers and notes are private to their owner; answer-writing time is self-reported.
- The Written module does not provide automatic or AI evaluation. Model answers and self-assessment are for manual comparison only.
- Viva categories are editable application taxonomy based on requested labels, not an official BPSC topic list. No Viva questions are seeded; reference verification is editor-entered. Voice recording and AI evaluation are not implemented.
- Library uploads are not malware-scanned (no scanner is available); signature checks block disguised executables and active content only. Only the local filesystem storage provider exists, and the storage root needs its own backup.
- Library rights declarations are self-reported by the uploader and audited; the system cannot verify copyright status. Commercial books should be catalogued without files in the shared library.
- Planner and current-affairs modules remain out of scope.

## Pending

- [ ] Apply migrations and verify seed/API behavior against a configured disposable MySQL database
- [ ] Review and import detailed official syllabus topics into a new version after source verification
- [ ] Add automated API integration and frontend end-to-end tests when a test database/runtime is available
- [ ] Implement the remaining planner, current-affairs, and deployment modules in roadmap order
- [ ] Add malware scanning and a cloud object-storage provider for library files when infrastructure is approved

## Testing status

Backend solution build passed with 0 warnings/errors; all 116 backend tests passed, including 80 library cases: file-signature/sanitisation/storage-key, validation, and 17 service-level access-control tests that run the real library service against an EF Core in-memory database and temporary local storage. Frontend production build passed with Viva and library screens in lazy-loaded chunks. Frontend lint completed successfully with non-blocking warnings in existing ThemeProvider, MCQ, Written, and syllabus screens; no Viva- or library-specific warnings remain. Idempotent migration SQL was generated and inspected with a design-time placeholder. The migration has not been applied because local MySQL is unavailable.
