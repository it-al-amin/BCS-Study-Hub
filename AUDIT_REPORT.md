# BCS Study Hub: System Audit Report (Phase 12)

Date: 2026-11-21. Scope: the whole repository after Phases 1–11. This is a code, configuration, and automated-test audit; it is **not** a production penetration test.

## Executive summary

| | |
|---|---|
| Size | 105 C# files (≈22.8k lines, excluding migrations), 93 TypeScript files (≈12.1k lines), 9 EF Core migrations, 80 tables |
| Build | Release, `-warnaserror`: **0 warnings, 0 errors** (run) |
| Backend tests | **188 passed, 0 failed** (run, Release) |
| Frontend | Production build **passes**; lint exits 0 with **33 warnings**, all in pre-Phase-8 pages (run) |
| Dependency scans | NuGet `--vulnerable --include-transitive`: **none**; `npm audit`: **0 vulnerabilities** (run) |
| Migrations vs model | `has-pending-model-changes`: **none** (run) |
| Database | **Never run against a real MySQL server.** No MySQL or Docker is available in this environment. Every migration has been generated and its SQL reviewed, but none has been applied. |
| Deployment | Dockerfiles, Compose, nginx, CI, and backup/restore scripts are **written but not executed** (no Docker here). |

**Biggest risk:** the nine migrations and every database-backed request path have only been tested through EF Core's in-memory provider. MySQL-specific behaviour (query translation, collation-sensitive unique indexes, cascade ordering, `DateOnly` mapping) is unverified. Run the staging procedure in [DEPLOYMENT.md](DEPLOYMENT.md#first-deployment-on-staging) before any production use.

## What was actually checked

1. Static review of controllers, services, DTOs, entity configurations, middleware, and frontend routes and services.
2. `dotnet build -c Release -warnaserror` and `dotnet test`.
3. `dotnet ef migrations has-pending-model-changes`, and idempotent migration SQL generation for each phase.
4. `dotnet list package --vulnerable --include-transitive` and `npm audit`.
5. Pattern scans for raw SQL (`FromSqlRaw`/`ExecuteSqlRaw`), `dangerouslySetInnerHTML`, `eval`, token storage in `localStorage`, secrets in `appsettings*.json`, and missing authorization attributes.
6. **New automated checks added this phase:**
   - Reflection tests prove every `api/v1/admin/*` controller carries a registered permission policy and every `api/v1/me/*` controller requires authentication.
   - In-memory HTTP-server tests prove the security headers, the per-IP auth rate limit (11th request → 429), and that unhandled exceptions do not leak messages or secrets.

## Findings and resolutions

Severity: **High** means exploitable, or likely to cause data loss or outage; **Medium** is meaningful risk or inefficiency; **Low** is hygiene.

### Resolved in this phase

| # | Area | Severity | Finding | Resolution |
|---|---|---|---|---|
| R1 | Security | High | No rate limit on `/auth/login` or `/auth/register`; only per-account lockout existed, so credential stuffing across accounts and sign-up abuse were unthrottled. | Per-IP fixed-window limit (10/min) on both endpoints via a shared `SecurityLimits` config. Verified by an HTTP test. |
| R2 | Security | Medium | No browser security headers and no HSTS. | `SecurityHeadersMiddleware` (nosniff, frame DENY, no-referrer, Permissions-Policy, CSP `default-src 'none'` for JSON; endpoints keep their stricter file CSP) and HSTS outside Development. Verified by HTTP tests. |
| R3 | Security / deployment | Medium | Behind a proxy the API would see the proxy's IP, so per-IP rate limits would treat all users as one client, and the scheme would be wrong. | Opt-in `ReverseProxy:Enabled` with `ForwardedHeaders` (limit 1 hop, optional `KnownProxies`). |
| R4 | API correctness | Medium | Every error response claimed `application/problem+json` but was sent as `application/json`, because `WriteAsJsonAsync` overwrote the header. Found by the new HTTP test. | All 10 error paths now pass the content type explicitly. Verified by test. |
| R5 | Performance | Medium | Planner and dashboard analytics loaded **every MCQ answer row** a user ever submitted into memory on each dashboard, report, weak-topic, or progress request. | Aggregated per topic in SQL (`GROUP BY` with conditional counts). Results are bounded by topics practised; all 188 tests unchanged and passing. |
| R6 | Performance | Medium | The initial JS bundle was 470 kB (126 kB gzip) because 25 feature pages were imported eagerly. | Route-level lazy loading: **317 kB (98 kB gzip)**. Measured from build output. |
| R7 | Deployment | Medium | No way to create or upgrade the schema from the deployed artifact; the API crashes at startup on an empty database because seeders run first. | Opt-in `Database:ApplyMigrationsOnStartup` (default off), documented with the reviewed-script alternative. |
| R8 | Deployment | Medium | No container, proxy, CI, or backup tooling. | Added API and web Dockerfiles, nginx SPA/proxy config with CSP, `docker-compose.yml`, `.env` template, GitHub Actions CI, and backup/restore scripts. **Not executed here.** |
| R9 | Authorization (Phase 11) | Medium | Policies were registered by hand, one per permission, so a new permission could ship without a policy. | Policies generated from `PermissionCodes`; a test verifies every code has an authenticated claim policy. |

### Critical issues outstanding

| # | Issue | Why it matters | Action |
|---|---|---|---|
| C1 | **Migrations never applied to MySQL.** | Schema or translation errors would surface only in production. | Follow the staging procedure in DEPLOYMENT.md; run the full smoke checklist. |
| C2 | **No end-to-end tests against a real database or browser.** | In-memory EF does not enforce foreign keys, unique indexes, or SQL translation; UI flows are untested. | Add Testcontainers MySQL integration tests and Playwright smoke tests (next phase). |

### Remaining issues (accepted or deferred)

| # | Area | Severity | Finding | Recommendation |
|---|---|---|---|---|
| M1 | Authentication | Medium | Access tokens live in memory only. No refresh token, so users sign in again on reload or after 30 minutes, and there is no server-side revocation or logout. | Add rotating refresh tokens in an httpOnly, SameSite=strict cookie with a revocation table, plus CSRF protection for that cookie endpoint. |
| M2 | Security | Medium | Rate limits and lockout counters are in-memory per API instance. | Fine for one instance; use a distributed store (Redis) before scaling out. |
| M3 | Files | Medium | Uploads are signature- and structure-checked but not malware-scanned; only local-disk storage exists. | Add a scanning step (e.g. ClamAV) before `SaveAsync`; add an object-storage `IFileStorage`. |
| M4 | Accessibility | Medium | Labels, roles and keyboard focus were reviewed by reading the code only; no automated accessibility tool (axe) or screen reader was run. | Add axe checks to the Playwright suite. |
| L1 | Security | Low | Registration returns 409 for an existing email, so account existence can be enumerated. | Accept, or return a generic response and send email verification instead. |
| L2 | Maintainability | Low | `ExceptionHandlingMiddleware` has 10 near-identical catch blocks (one per module exception type), and validation/`Parse<T>` helpers are repeated per service. | Introduce one `IProblemException` interface and a shared validation helper. |
| L3 | Maintainability | Low | 33 lint warnings (30 `set-state-in-effect`) in 14 pre-Phase-8 pages (MCQ, Written, syllabus, ThemeProvider). Phases 8–11 code has none. | Apply the derived-state pattern used in Phases 8–11. |
| L4 | Maintainability | Low | Some older pages put whole layouts on single very long JSX lines; `queryString` helpers are duplicated across frontend services. | Reformat when those pages are next changed; extract one helper. |
| L5 | Performance | Low | Text search uses `LIKE '%term%'` (full scan). Fine at current scale. | Add MySQL FULLTEXT indexes for questions, library, and current affairs when content grows. |
| L6 | Data model | Low | `McqMistakes` (automatic per-question tracker) and `MistakeEntries` (annotated notebook) overlap by design; an import links them. | Keep; documented. |
| L7 | Data model | Low | Deletion semantics differ: published content is archived (status), while drafts can be hard-deleted. There is no global soft-delete. User deletion is blocked by `Restrict` FKs where the user owns shared or private content. | Documented; add an explicit account-deletion workflow if needed. |

## Area reviews

**Architecture.** Dependencies point inward (Domain ← Application ← Infrastructure ← Api) with no reverse references, and the module boundaries are clear. Complexity is proportionate: no repositories wrap EF needlessly, and services are per module. Weak spots are the duplication noted in L2, and the fact that Application contracts own validation through data annotations while services re-validate. That re-validation is deliberate defence in depth.

**Backend.** Controllers are thin. Ownership is enforced in services, and another user's IDs return 404. Pagination is capped at 100 per page in one shared `PageRequest`. There is no raw SQL, so all queries are parameterized. Problem Details are used for errors (now with the correct type), the 500 response is generic with a correlation ID, and Swagger is enabled only in Development. Audit logs cover admin, publication, file, verification, settings, and tag actions.

**Database.** Every relationship has an explicit `OnDelete`. Indexes exist for every owner-scoped list (`UserId` + date or status) and every foreign-key filter reviewed. Uniqueness is enforced where business rules require it (one note per user and entry, schedule-date idempotency, file role per resource, directory URL). Snapshot tables (MCQ attempts, Viva sessions, revision titles) are intentionally denormalized for history stability. Migrations are additive. Phase 10 added three columns with defaults to existing tables; nothing is dropped.

**Frontend.** Every route renders loading, error, and empty states. Personal data requires sign-in, with server enforcement. Forms validate client-side and display server Problem Details. The JWT stays in memory and nothing renders raw HTML: the Markdown renderer builds React elements and allows only http(s) and mailto links. External links use `rel="noopener noreferrer nofollow"`. Protected files are fetched with the bearer token into short-lived object URLs.

**Security (OWASP view).**
- **Injection:** EF parameterization throughout.
- **Broken access control:** policy per permission, verified by reflection test; owner scoping in services.
- **Cryptographic failures:** Identity password hashing, an HS256 key of at least 32 bytes validated at startup, HTTPS redirection, and HSTS.
- **Security misconfiguration:** secrets only from environment or user-secrets; `AllowedHosts` must be set in production (template provided).
- **Vulnerable components:** scans clean.
- **Identification and authentication failures:** 12-character complex passwords, lockout after 5 failures, and the new IP rate limit; see M1 and L1.
- **SSRF:** the server never fetches user-supplied URLs; directory verification is manual.
- **XSS:** no HTML injection; CSP in nginx and the API.
- **CSRF:** not applicable while authentication uses bearer headers rather than cookies. Revisit with M1.

**Uploads.** Extension allowlist plus magic-byte, Office-structure and UTF-8 checks; macro and ActiveX rejection; size and quota limits; per-user upload rate limit; random storage keys with a path-traversal guard; SHA-256 hashes; served with `nosniff`, a sandboxing CSP and `no-store`. The nginx `client_max_body_size` is aligned with the API ceiling.

## Functional test coverage (automated, in-memory)

| Area | Covered by | Not covered |
|---|---|---|
| Authentication | Password hashing, request validation, rate limit (HTTP), registration-closed setting | Full login/register against Identity + MySQL |
| Permissions | Reflection coverage of all admin and personal controllers; policy registration; module-scoped tags and bulk | Live JWT round-trip |
| Syllabus and topics | Request validation | Publish workflow against MySQL |
| MCQ, mock exams, attempts | Scoring and validation | Timed attempt flow end to end |
| Written, Viva | Validation | Answer and session flows end to end |
| Library and uploads | 17 service tests (ownership, rights, signatures, quotas, replacement, deletion) plus file-security tests | Real multipart upload through Kestrel |
| Current affairs and directory | Rules, validation, 10 service tests, seeder | — |
| Planner, revision, reports | Policy engine, streaks, validation, 9 service tests | — |
| Admin | Settings, tags, audit query, overview scoping, bulk input validation | Bulk endpoint against real module services and MySQL |

## Recommended next phase

**Phase 13: Integration verification and session hardening.**

1. Run a MySQL 8.4 Testcontainers suite: apply all migrations, then exercise each module's main API flows through `WebApplicationFactory`. This addresses C1 and C2.
2. Add Playwright smoke tests for sign-in, MCQ attempt, Written answer, library upload and preview, planner timer, revision review, and admin publish, with axe accessibility checks (M4).
3. Add refresh-token sessions with revocation (M1).
4. Add malware scanning and an object-storage provider (M3); add Redis if running more than one API instance (M2).
5. Clean up the duplication and older-page lint debt (L2–L4).
