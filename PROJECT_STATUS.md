# Project Status

## Last updated

2026-11-21

## Current phase

Phase 12 - System Audit and Deployment Readiness. Phases 1–11 are implemented and build-verified; Phase 12 fixed the security and performance issues it found and added deployment tooling. **Not yet verified:** migrations and API flows against a real MySQL server, end-to-end UI tests, and the Docker/CI files themselves, because no MySQL or Docker was available. See AUDIT_REPORT.md and DEPLOYMENT.md.

## Knowledge collection platform (new track)

Phase 1 done: content-source registry (`ContentSources`, `ContentSourceScans`), `sources.manage` permission, admin API and `/admin/content-sources` UI, SSRF-safe URL rules, 18 new tests. Phase 2 done: `SafeHttpFetcher` (DNS/IP checks on every connection, redirect and size limits, per-host rate limit), robots.txt service, RSS/Atom, sitemap and HTML adapters, a background scan worker, `DiscoveredContents` and `DiscoveredContentVersions` tables with SHA-256 change detection and versioning, a read-only discovered-content API and admin page, 27 new tests, and a live scan of a public RSS feed against MariaDB. No daily scheduler yet. See SOURCE_MONITORING_GUIDE.md, CRAWLING_POLICY.md and CONTENT_INGESTION_GUIDE.md. Next: Phase 3 (documents/PDF) and Phase 4 (scheduler).

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
- [x] Current-affairs entries with 19 categories, summary/content, event and source dates, tags, cited source, editor notes, publisher references, and draft/published/archived status
- [x] Links to syllabus subjects/topics, MCQ/Written/Viva questions (validated per bank), and shared published library files
- [x] Daily, ISO-weekly, monthly, and yearly collections, archive, date/category/tag/topic filters, and search
- [x] Private bookmarks, study notes, and spaced revision (Again/Hard/Good/Easy); printable entries and collections
- [x] Official resource directory with manual, dated verification records; URL changes and failed checks unpublish; 365-day freshness rule
- [x] 26 directory URLs checked on 2026-11-21: 19 seeded as verified and published, 7 seeded as drafts pending browser verification (`OFFICIAL_SOURCES.md`)
- [x] `AddCurrentAffairsAndOfficialSources` migration generated and reviewed; rule, validation, and service-level tests added
- [x] Study goals (daily/weekly/monthly, seven computed metrics), prioritised tasks with planned/actual time, recurring schedules, a session timer and manual logs, and a calendar
- [x] Topic mastery ratings, completed/pending topics, subject and stage progress, study time, streaks, and MCQ/Written/Viva practice performance
- [x] Revision queue with a user-defined interval ladder and outcome rules (no fixed intervals), imports from bookmarks/mistakes/weak topics/notes, suspension, retirement, and history; current-affairs revision migrated to the same policy
- [x] Mistake notebook with categories, personal explanation, correct-answer reference, review date, topic mapping, history, and MCQ-mistake import
- [x] Daily, weekly, and monthly reports, weak subjects, revision statistics, and the dashboard computed from real records (mock dashboard, planner, progress, and revision data removed)
- [x] `AddStudyPlannerAndRevision` migration generated and reviewed; policy, streak, validation, and service-level tests added

- [x] Phase 11 admin console: overview dashboard, bulk publish/archive, cross-module tags, categories overview, audit history (page and per-record panel), site settings with server-enforced registration control, Markdown editor with safe renderer, hierarchical topic selector, reusable resource picker, and functional topic notes; `AddSiteSettings` migration; reflection tests for server-side authorization coverage
- [x] Phase 12 audit: per-IP auth rate limiting, security headers and HSTS, forwarded headers, Problem Details content-type fix, SQL-side MCQ aggregation, lazy-loaded routes (main bundle 470 kB to 317 kB), opt-in startup migrations, Docker/Compose/nginx, CI workflow, backup/restore scripts, AUDIT_REPORT.md, and DEPLOYMENT.md

## Known limitations

- No local MySQL server was available. The syllabus, MCQ, Written, Viva, library, current-affairs, and planner migrations are generated and reviewed but have not been applied; database/API integration smoke tests could not run.
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
- No current-affairs content is seeded, and there is no feed or scraper by design; entries are curated by editors. Source verification for entries is an editor attestation.
- Seven official-directory URLs (Planning Commission, BBS, Parliament, PID, Election Commission, UN, UNDP) failed the automated check (TLS-chain or 403 responses) and remain unpublished drafts until an editor verifies them in a browser.
- Related MCQ links open the question bank; MCQ has no single-question learner page.
- Existing current-affairs revision rows start at step 0 of the user's ladder after the Phase 10 migration; their next review re-places them.
- Study time is self-recorded (timer or manual log); the app cannot verify that a running timer reflects real study.

## Pending

- [ ] Apply migrations and verify seed/API behavior against a configured disposable MySQL database
- [ ] Review and import detailed official syllabus topics into a new version after source verification
- [ ] Add automated API integration and frontend end-to-end tests when a test database/runtime is available
- [ ] Implement the hardening and deployment phase
- [ ] Verify the seven draft official-directory URLs in a browser and publish them
- [ ] Add malware scanning and a cloud object-storage provider for library files when infrastructure is approved

## Testing status

**Latest run (2026-11-21, Phase 12):** Release build with `-warnaserror`: 0 warnings, 0 errors. Backend tests: **188 passed, 0 failed**, including HTTP-pipeline security tests (headers, auth rate limit, no error-detail leakage) and reflection-based authorization coverage. Frontend production build passes; lint exits 0 with 33 warnings, all in pre-Phase-8 pages. NuGet and npm vulnerability scans: none. Migrations match the model. Not run: MySQL integration, end-to-end browser tests, Docker or CI execution.

Earlier record: Backend solution build passed with 0 warnings/errors; all 173 backend tests passed, including 24 Phase 10 cases (user-defined revision policy, streaks, schedule and Bangladesh-time rules, validation, and 9 service-level tests over planner, revision, mistakes, reports, and the dashboard); 80 library cases (file security, validation, and 17 service-level access-control tests) and 33 current-affairs cases (digest ranges across ISO-week, month, and leap-year boundaries, Bangladesh-time "today", the revision schedule, input validation, and 10 service-level tests with a controllable clock covering the publication gate, digests, the archive, owner-scoped personal data, directory verification rules, and seeder idempotency). Frontend production build passed with Viva, library, and current-affairs screens in lazy-loaded chunks. Frontend lint completed successfully with non-blocking warnings in existing ThemeProvider, MCQ, Written, and syllabus screens; no Viva-, library-, or current-affairs-specific warnings remain. Idempotent migration SQL was generated and inspected with a design-time placeholder. The migration has not been applied because local MySQL is unavailable.
