# API Design

## Style

Use REST-style HTTPS endpoints, application services for business logic, RFC 7807 Problem Details, and typed DTOs. Controllers remain thin.

## Base conventions

- API prefix: `/api/v1`
- Health endpoints: `/health/live`, `/health/ready`
- OpenAPI endpoint: `/swagger` in development and only approved environments
- JSON field names: camelCase
- UTC timestamps use ISO 8601
- Path IDs use stable opaque/string or numeric IDs consistently per entity
- Long-running operations use a bounded background job contract rather than hidden synchronous work

## Response conventions

Successful collection response:

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

Mutations return a DTO or `204` for delete/archive operations. Use `201` for a created resource with `Location`.

Errors use Problem Details:

```json
{
  "type": "https://.../validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "instance": "/api/v1/resources",
  "errors": {
    "title": ["Title is required."]
  },
  "traceId": "..."
}
```

Never return database entities or raw provider exceptions.

## Common query parameters

- `pageNumber`
- `pageSize` with configured maximum
- `search`
- `sort`
- `status`
- `stageId`
- `syllabusVersionId`
- `subjectId`
- `paperId`
- `topicId`
- `subtopicId`
- `tagId`
- `categoryId`
- `from`
- `to`

Sort fields are allowlisted per endpoint. Unknown sort/filter fields produce validation errors or are ignored only if the API contract explicitly defines this behavior.

## Authentication endpoints

```text
POST /api/v1/auth/register
POST /api/v1/auth/login
GET  /api/v1/auth/me
```

The foundation exposes registration, login, and the authenticated current-user endpoint. Registration creates a Candidate account and returns a short-lived access JWT. Passwords must be at least 12 characters and include an uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character. Refresh, logout, password-reset, and change-password flows are not implemented yet.

## Syllabus endpoints

Public reads expose published versions only. Version and item reads support bounded pagination; search and version lists use allowlisted sort fields. Draft and archived versions are only available through the permission-protected admin routes.

```text
GET    /api/v1/syllabus/stages
GET    /api/v1/syllabus/versions?stageCode=&trackCode=&pageNumber=&pageSize=&sortBy=
GET    /api/v1/syllabus/versions/{versionId}/tree
GET    /api/v1/syllabus/versions/{versionId}/subjects
GET    /api/v1/syllabus/papers?syllabusSubjectId=
GET    /api/v1/syllabus/items?versionId=&subjectId=&parentId=&kind=
GET    /api/v1/syllabus/topics?versionId=&subjectId=
GET    /api/v1/syllabus/subtopics?versionId=&parentId=
GET    /api/v1/syllabus/search?q=&stageCode=&versionId=&subjectId=&kind=&pageNumber=&pageSize=&sortBy=
GET    /api/v1/syllabus/versions/{versionId}/sources
GET    /api/v1/syllabus/items/{itemId}/sources

GET    /api/v1/me/topic-progress?versionId=
PUT    /api/v1/me/topic-progress/{syllabusItemId}
```

`PUT /api/v1/me/topic-progress/{syllabusItemId}` accepts `{ "isCompleted": true }`; the user ID always comes from the authenticated JWT, never the request body. Public tree reads optionally include only the current authenticated user's completion flags.

Admin operations require the `syllabus.manage` permission:

```text
GET    /api/v1/admin/syllabus/stages
PUT    /api/v1/admin/syllabus/stages/{stageId}
GET    /api/v1/admin/syllabus/versions?stageCode=&trackCode=&status=&pageNumber=&pageSize=&sortBy=
POST   /api/v1/admin/syllabus/stages/{stageId}/versions
GET    /api/v1/admin/syllabus/versions/{versionId}/tree
PUT    /api/v1/admin/syllabus/versions/{versionId}
POST   /api/v1/admin/syllabus/versions/{versionId}/publish
POST   /api/v1/admin/syllabus/versions/{versionId}/archive
GET/POST /api/v1/admin/syllabus/subjects
PUT    /api/v1/admin/syllabus/subjects/{subjectId}
POST   /api/v1/admin/syllabus/versions/{versionId}/subjects
PUT    /api/v1/admin/syllabus/syllabus-subjects/{mappingId}
GET    /api/v1/admin/syllabus/papers?syllabusSubjectId=
POST   /api/v1/admin/syllabus/syllabus-subjects/{mappingId}/papers
PUT/DELETE /api/v1/admin/syllabus/papers/{paperId}
GET    /api/v1/admin/syllabus/items?versionId=&subjectId=&parentId=&kind=
POST   /api/v1/admin/syllabus/topics
POST   /api/v1/admin/syllabus/subtopics
PUT/DELETE /api/v1/admin/syllabus/items/{itemId}
GET    /api/v1/admin/syllabus/versions/{versionId}/sources
POST   /api/v1/admin/syllabus/sources
GET/PUT /api/v1/admin/syllabus/sources/{sourceId}
PUT/DELETE /api/v1/admin/syllabus/versions/{versionId}/sources/{sourceId}
PUT/DELETE /api/v1/admin/syllabus/items/{itemId}/sources/{sourceId}
```

Draft content is editable. Publishing requires at least one source marked verified. A `FullTopics` version additionally requires every topic/subtopic to be verified and linked to a verified source. Published versions are immutable and can only be archived; corrections use a new draft version. Admin mutations are written to `AuditLogs` without logging source content or secrets.

## Study content endpoints

```text
GET    /api/v1/topics/{topicId}/notes
POST   /api/v1/topics/{topicId}/notes
GET    /api/v1/notes/{id}
PUT    /api/v1/notes/{id}
POST   /api/v1/notes/{id}/publish
GET    /api/v1/me/notes
POST   /api/v1/me/notes
PUT    /api/v1/me/notes/{id}
DELETE /api/v1/me/notes/{id}
```

## Preliminary MCQ and practice endpoints

```text
GET    /api/v1/questions?search=&syllabusVersionId=&subjectId=&syllabusItemId=&bcsExamYear=&difficulty=&tag=&pageNumber=&pageSize=&sortBy=
GET    /api/v1/questions/{questionId}
GET    /api/v1/practice/mcq/sets?pageNumber=&pageSize=
POST   /api/v1/practice/mcq/attempts
GET    /api/v1/practice/mcq/attempts?pageNumber=&pageSize=
GET    /api/v1/practice/mcq/attempts/{attemptId}
PUT    /api/v1/practice/mcq/attempts/{attemptId}/questions/{attemptQuestionId}/answer
POST   /api/v1/practice/mcq/attempts/{attemptId}/submit
GET    /api/v1/practice/mcq/attempts/{attemptId}/review

GET    /api/v1/me/questions/bookmarks?pageNumber=&pageSize=
POST   /api/v1/me/questions/{questionId}/bookmark
DELETE /api/v1/me/questions/{questionId}/bookmark
GET    /api/v1/me/questions/mistakes?status=&pageNumber=&pageSize=
PUT    /api/v1/me/questions/{questionId}/mistake
GET    /api/v1/me/mcq/analytics

GET    /api/v1/admin/mcq/questions?search=&status=&pageNumber=&pageSize=
GET    /api/v1/admin/mcq/questions/{questionId}
POST   /api/v1/admin/mcq/questions
PUT    /api/v1/admin/mcq/questions/{questionId}
POST   /api/v1/admin/mcq/questions/{questionId}/publish
POST   /api/v1/admin/mcq/questions/{questionId}/archive
GET    /api/v1/admin/mcq/sets?pageNumber=&pageSize=
GET    /api/v1/admin/mcq/sets/{setId}
POST   /api/v1/admin/mcq/sets
PUT    /api/v1/admin/mcq/sets/{setId}
POST   /api/v1/admin/mcq/sets/{setId}/publish
POST   /api/v1/admin/mcq/sets/{setId}/archive
```

Public question-bank responses never include correct-option flags or explanations. Correctness is returned only after submission, except when the published set explicitly enables immediate feedback. Attempt answers accept selected option IDs and a review flag only; clients cannot submit scores, correctness values, or scoring rules. Scoring and time limits are enforced server-side from an immutable attempt snapshot.

Question and exam-set administration requires the `mcq.manage` permission. Questions link to a topic/subtopic in a published Preliminary syllabus version and require source attribution; publication requires a verified topic and verified question source. Image questions may reference an HTTPS image URL; file upload is not implemented. Exam-set configurations select a version, practice mode, optional subject/topic/year/difficulty filters, question count, duration, and configurable marks for correct/incorrect/unanswered responses. No BPSC negative-marking or other exam rule is hardcoded. Mock-exam sets require a configured duration. Multiple-answer questions are scored as exact option-set matches; partial credit is not assumed.

```text
POST   /api/v1/practice/mcq/attempts
PUT    /api/v1/practice/mcq/attempts/{attemptId}/questions/{attemptQuestionId}/answer
POST   /api/v1/practice/mcq/attempts/{attemptId}/submit
GET    /api/v1/practice/mcq/attempts/{attemptId}/review
```

## Written preparation endpoints

```text
GET    /api/v1/written/questions?search=&syllabusVersionId=&subjectId=&syllabusItemId=&type=&bcsExamYear=&tag=&pageNumber=&pageSize=&sortBy=
GET    /api/v1/written/questions/{questionId}
GET    /api/v1/written/questions/{questionId}/model-answer

GET    /api/v1/me/written/answers?pageNumber=&pageSize=
POST   /api/v1/me/written/questions/{questionId}/answers
GET    /api/v1/me/written/answers/{answerId}
PUT    /api/v1/me/written/answers/{answerId}
POST   /api/v1/me/written/answers/{answerId}/submit
POST   /api/v1/me/written/answers/{answerId}/revise
GET    /api/v1/me/written/answers/{answerId}/revisions

GET    /api/v1/me/written/bookmarks?pageNumber=&pageSize=
POST   /api/v1/me/written/questions/{questionId}/bookmark
DELETE /api/v1/me/written/questions/{questionId}/bookmark
GET    /api/v1/me/written/notes?syllabusItemId=&pageNumber=&pageSize=
POST   /api/v1/me/written/notes
PUT    /api/v1/me/written/notes/{noteId}
DELETE /api/v1/me/written/notes/{noteId}
GET    /api/v1/me/written/goals
POST   /api/v1/me/written/goals
PUT    /api/v1/me/written/goals/{goalId}
GET    /api/v1/me/written/progress?from=&to=
GET    /api/v1/admin/written/questions?search=&status=&pageNumber=&pageSize=
GET    /api/v1/admin/written/questions/{questionId}
POST   /api/v1/admin/written/questions
PUT    /api/v1/admin/written/questions/{questionId}
POST   /api/v1/admin/written/questions/{questionId}/publish
POST   /api/v1/admin/written/questions/{questionId}/archive
POST   /api/v1/admin/written/questions/{questionId}/model-answer/publish
POST   /api/v1/admin/written/questions/{questionId}/model-answer/archive
```

Written question and model-answer administration requires the `written.manage` permission. Published questions must map to a verified topic/subtopic in a published Written syllabus version and have verified source attribution. Model answers publish separately and require verified references. No question or answer content is seeded; unverified official topics are not invented. Personal answer updates and submissions are owner-scoped and create immutable content snapshots. The server calculates word count; time is user-reported. Self-assessment is private and is not an official score or AI evaluation. Study notes, goals, bookmarks, and writing reports are also user-scoped.

## Viva preparation endpoints

```text
GET    /api/v1/viva/categories
GET    /api/v1/viva/topics?categoryId=
GET    /api/v1/viva/questions?search=&categoryId=&topic=&bookmarkedOnly=&pageNumber=&pageSize=&sortBy=
GET    /api/v1/viva/questions/{questionId}

GET    /api/v1/me/viva/questions/{questionId}/answer-note
PUT    /api/v1/me/viva/questions/{questionId}/answer-note
POST   /api/v1/me/viva/questions/{questionId}/bookmark
DELETE /api/v1/me/viva/questions/{questionId}/bookmark
GET    /api/v1/me/viva/question-history?pageNumber=&pageSize=
GET    /api/v1/me/viva/sessions?pageNumber=&pageSize=
POST   /api/v1/me/viva/sessions
GET    /api/v1/me/viva/sessions/{sessionId}
PUT    /api/v1/me/viva/sessions/{sessionId}/questions/{sessionQuestionId}
POST   /api/v1/me/viva/sessions/{sessionId}/complete
GET    /api/v1/me/viva/weak-topics
GET    /api/v1/me/viva/profile
PUT    /api/v1/me/viva/profile

GET    /api/v1/admin/viva/categories
POST   /api/v1/admin/viva/categories
PUT    /api/v1/admin/viva/categories/{categoryId}
GET    /api/v1/admin/viva/questions?search=&status=&pageNumber=&pageSize=
GET    /api/v1/admin/viva/questions/{questionId}
POST   /api/v1/admin/viva/questions
PUT    /api/v1/admin/viva/questions/{questionId}
POST   /api/v1/admin/viva/questions/{questionId}/publish
POST   /api/v1/admin/viva/questions/{questionId}/archive
```

Question and category administration requires the `viva.manage` permission. Personal notes, bookmarks, question history, mock sessions, feedback, and profile preparation are scoped to the authenticated user. Mock sessions snapshot the selected question text and category in the requested sequence; completion requires answer notes for each selected question and a self-evaluation rating or feedback. Revision flags power the weak-topic report. The initial categories are editable navigation labels supplied by the project requirements, not an official BPSC topic list. No Viva questions or suggested answers are seeded. Reference verification is editor-entered; voice recording, automated scoring, and AI evaluation are not implemented.

## Library endpoints

Every library endpoint requires authentication. Books are resources with `kind=Book`; there is no separate book endpoint.

Catalog (any signed-in user; returns shared published resources plus the caller's private resources):

```text
GET    /api/v1/library/dashboard
GET    /api/v1/library/resources?search=&kind=&category=&format=&subjectId=&syllabusItemId=&tag=&scope=all|shared|mine&bookmarkedOnly=&sortBy=recent|added|title|published&pageNumber=&pageSize=
GET    /api/v1/library/resources/{resourceId}
GET    /api/v1/library/tags
GET    /api/v1/library/resources/{resourceId}/files/{fileId}/content?download=false
```

Personal resources and study data (owner-scoped):

```text
GET    /api/v1/me/library/storage
GET    /api/v1/me/library/resources/{resourceId}
POST   /api/v1/me/library/resources
PUT    /api/v1/me/library/resources/{resourceId}
DELETE /api/v1/me/library/resources/{resourceId}
POST   /api/v1/me/library/resources/{resourceId}/files?role=Primary|Cover      (multipart/form-data, field "file")
DELETE /api/v1/me/library/resources/{resourceId}/files/{fileId}
POST   /api/v1/me/library/resources/{resourceId}/chapters
PUT    /api/v1/me/library/resources/{resourceId}/chapters/{chapterId}
DELETE /api/v1/me/library/resources/{resourceId}/chapters/{chapterId}
GET    /api/v1/me/library/bookmarks
POST   /api/v1/me/library/bookmarks
DELETE /api/v1/me/library/bookmarks/{bookmarkId}
GET    /api/v1/me/library/progress?status=
PUT    /api/v1/me/library/resources/{resourceId}/progress
DELETE /api/v1/me/library/resources/{resourceId}/progress
GET    /api/v1/me/library/notes?resourceId=
POST   /api/v1/me/library/resources/{resourceId}/notes
PUT    /api/v1/me/library/notes/{noteId}
DELETE /api/v1/me/library/notes/{noteId}
GET    /api/v1/me/library/collections
GET    /api/v1/me/library/collections/{collectionId}
POST   /api/v1/me/library/collections
PUT    /api/v1/me/library/collections/{collectionId}
DELETE /api/v1/me/library/collections/{collectionId}
POST   /api/v1/me/library/collections/{collectionId}/items
DELETE /api/v1/me/library/collections/{collectionId}/items/{resourceId}
```

Shared library administration (`library.manage`):

```text
GET    /api/v1/admin/library/resources?search=&status=&kind=
GET    /api/v1/admin/library/resources/{resourceId}
POST   /api/v1/admin/library/resources
PUT    /api/v1/admin/library/resources/{resourceId}
POST   /api/v1/admin/library/resources/{resourceId}/publish
POST   /api/v1/admin/library/resources/{resourceId}/archive
DELETE /api/v1/admin/library/resources/{resourceId}
POST   /api/v1/admin/library/resources/{resourceId}/files?role=Primary|Cover
DELETE /api/v1/admin/library/resources/{resourceId}/files/{fileId}
GET    /api/v1/admin/library/resources/{resourceId}/files/{fileId}/content
POST   /api/v1/admin/library/resources/{resourceId}/chapters
PUT    /api/v1/admin/library/resources/{resourceId}/chapters/{chapterId}
DELETE /api/v1/admin/library/resources/{resourceId}/chapters/{chapterId}
```

Rules:

- Resources created under `/me` are private and immediately available to their owner. Resources created under `/admin` are shared drafts; learners see them only after publishing. Published shared resources must be archived before deletion.
- Private resources of another user return `404`. Managers cannot read learners' private resources or files.
- The resource `kind` (`Book`, `File`, `ExternalLink`, `Note`) is fixed at creation. External links need an `http`/`https` URL without credentials; notes need content; book details are accepted only for books and ISBNs are checksum-validated.
- Files require a declared `rightsBasis`. `PersonalCopy` is allowed only for private resources; shared `GovernmentPublication`, `OpenLicense` and `PermissionGranted` files need a `rightsNote`.
- Uploads stream through the API (there are no presigned URLs, because the provider is local). Status codes: `413` over the size limit, `415` for an unsupported or mismatched type, `409` when over quota, `429` when rate-limited (30 uploads per 10 minutes per user). A new upload replaces the existing file in the same role.
- File content is served with the server-detected content type, `nosniff`, `no-store`, and a sandboxing CSP. Only PDF, image and text files are inline; Office documents are always attachments. The backend never exposes a storage URL or key.
- Topic mappings must reference published syllabus versions (any stage); subject mappings must reference active subjects. The `syllabusItemId` filter also matches subtopics of that topic and book chapters mapped to it.
- Bookmarks may target a resource, a chapter, or a page, and are deduplicated per location. Progress percentage is explicit or derived from pages, and completion forces 100%.

See [LIBRARY_SECURITY_REVIEW.md](LIBRARY_SECURITY_REVIEW.md) for the complete copyright, access-control and file-security controls.

## Planner and progress endpoints

```text
GET    /api/v1/me/dashboard
GET    /api/v1/me/goals
POST   /api/v1/me/goals
PUT    /api/v1/me/goals/{id}
GET    /api/v1/me/study-sessions
POST   /api/v1/me/study-sessions
PUT    /api/v1/me/study-sessions/{id}/complete
GET    /api/v1/me/revision
POST   /api/v1/me/revision/{id}/review
GET    /api/v1/me/progress
PUT    /api/v1/me/topics/{topicId}/progress
```

Dashboard metrics are calculated or cached from real database records.

## Current-affairs endpoints

```text
GET    /api/v1/current-affairs
GET    /api/v1/current-affairs/{id}
POST   /api/v1/admin/current-affairs
PUT    /api/v1/admin/current-affairs/{id}
POST   /api/v1/admin/current-affairs/{id}/publish
GET    /api/v1/government-resources
```

## Admin and security endpoints

```text
GET    /api/v1/admin/access
GET    /api/v1/admin/users
PUT    /api/v1/admin/users/{id}/status
PUT    /api/v1/admin/users/{id}/roles
GET    /api/v1/admin/roles
PUT    /api/v1/admin/roles/{id}/permissions
GET    /api/v1/admin/audit-logs
```

Only `/api/v1/admin/access` is implemented as a permission-policy smoke endpoint; the remaining endpoints are planned.

## Security conventions

- JWT access token sent using the approved bearer mechanism
- Refresh token stored securely and rotated
- Authorization on every endpoint, even if hidden in the UI
- Resource ownership checks on every `/me` operation
- Rate limits on login, registration, password reset, and upload initiation
- Content-length and upload-size limits
- CSRF protection if refresh/auth cookies are used
- CORS allowlist
- Audit critical mutations
- No sensitive values in URLs or logs

## Validation

All commands require:

- DTO validation
- authorization
- database existence/version checks
- uniqueness checks
- resource-ownership checks
- status-transition checks
- concurrency checks
