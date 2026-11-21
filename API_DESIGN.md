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

## Question endpoints

```text
GET    /api/v1/questions
GET    /api/v1/questions/{id}
POST   /api/v1/admin/questions
PUT    /api/v1/admin/questions/{id}
POST   /api/v1/admin/questions/{id}/publish
POST   /api/v1/admin/questions/{id}/archive
POST   /api/v1/practice/mcq/sessions
POST   /api/v1/practice/mcq/sessions/{id}/answers
POST   /api/v1/practice/mcq/sessions/{id}/submit
GET    /api/v1/practice/mcq/attempts
GET    /api/v1/practice/mcq/attempts/{id}
```

The practice response may include the selected and correct options only after answering/submission according to the selected practice mode. Correct answers are never returned in the question list used to render an exam.

## Exam endpoints

```text
GET    /api/v1/exams
GET    /api/v1/exams/{id}
POST   /api/v1/exams/{id}/attempts
GET    /api/v1/exam-attempts/{id}
PUT    /api/v1/exam-attempts/{id}/answers
POST   /api/v1/exam-attempts/{id}/submit
GET    /api/v1/exam-attempts/{id}/review
```

## Written and viva endpoints

```text
GET    /api/v1/written/questions
POST   /api/v1/written/questions/{id}/answers
GET    /api/v1/me/written/answers
PUT    /api/v1/me/written/answers/{id}
POST   /api/v1/me/written/answers/{id}/submit

GET    /api/v1/viva/questions
POST   /api/v1/viva/sessions
POST   /api/v1/viva/sessions/{id}/answers
POST   /api/v1/viva/sessions/{id}/complete
GET    /api/v1/me/viva/sessions
```

## Library endpoints

```text
POST   /api/v1/library/upload-sessions
PUT    /api/v1/library/upload-sessions/{id}/complete
POST   /api/v1/library/resources
GET    /api/v1/library/resources
GET    /api/v1/library/resources/{id}
GET    /api/v1/library/resources/{id}/download
POST   /api/v1/library/resources/{id}/bookmarks
DELETE /api/v1/library/resources/{id}/bookmarks
PUT    /api/v1/library/resources/{id}/progress
POST   /api/v1/library/collections
POST   /api/v1/library/books
POST   /api/v1/library/books/{id}/chapters
```

Uploads use a provider-neutral presigned upload or streaming API depending on provider. The backend never exposes a permanent storage URL for private resources.

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
