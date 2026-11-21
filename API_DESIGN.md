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

All endpoints are `[Authorize]` and owner-scoped; another user's IDs return `404`.

```text
GET    /api/v1/me/planner/settings
PUT    /api/v1/me/planner/settings                       revision ladder, outcome step changes, retire step, daily limit, streak minimum, weak-topic thresholds
GET    /api/v1/me/planner/goals?period=&date=
POST   /api/v1/me/planner/goals
PUT    /api/v1/me/planner/goals/{goalId}
DELETE /api/v1/me/planner/goals/{goalId}
GET    /api/v1/me/planner/tasks?from=&to=&status=&subjectId=&stage=
POST   /api/v1/me/planner/tasks
PUT    /api/v1/me/planner/tasks/{taskId}
POST   /api/v1/me/planner/tasks/{taskId}/status          { status, actualMinutes? }
DELETE /api/v1/me/planner/tasks/{taskId}
GET    /api/v1/me/planner/schedules
POST   /api/v1/me/planner/schedules
PUT    /api/v1/me/planner/schedules/{scheduleId}
DELETE /api/v1/me/planner/schedules/{scheduleId}
POST   /api/v1/me/planner/schedules/{scheduleId}/generate  { from, to } (≤ 92 days; skips existing dates)
GET    /api/v1/me/planner/sessions?from=&to=
GET    /api/v1/me/planner/sessions/active               204 when no timer is running
POST   /api/v1/me/planner/sessions/start
POST   /api/v1/me/planner/sessions/{sessionId}/stop
POST   /api/v1/me/planner/sessions                      log a past session
PUT    /api/v1/me/planner/sessions/{sessionId}
DELETE /api/v1/me/planner/sessions/{sessionId}
GET    /api/v1/me/planner/calendar?from=&to=            ≤ 62 days: tasks, study minutes, revision due, streak days

GET    /api/v1/me/revision/queue?dueOnly=&sourceType=&status=
POST   /api/v1/me/revision/items                        { sourceType, sourceId?, title? (Custom), dueOn? }
POST   /api/v1/me/revision/items/{itemId}/review        { outcome: Again | Hard | Good | Easy }
POST   /api/v1/me/revision/items/{itemId}/suspend
POST   /api/v1/me/revision/items/{itemId}/resume
DELETE /api/v1/me/revision/items/{itemId}
POST   /api/v1/me/revision/import                       { source: McqBookmarks | WrittenBookmarks | VivaBookmarks | McqMistakes | MistakeNotebook | WeakTopics | WrittenNotes | LibraryNotes | VivaNotes }
GET    /api/v1/me/revision/history?from=&to=
GET    /api/v1/me/revision/weak-topics

GET    /api/v1/me/mistakes?search=&status=&category=&sourceType=&syllabusItemId=
GET    /api/v1/me/mistakes/{mistakeId}                   includes revision history
POST   /api/v1/me/mistakes
PUT    /api/v1/me/mistakes/{mistakeId}
POST   /api/v1/me/mistakes/{mistakeId}/resolve
POST   /api/v1/me/mistakes/{mistakeId}/reopen
DELETE /api/v1/me/mistakes/{mistakeId}
POST   /api/v1/me/mistakes/import-mcq

GET    /api/v1/me/dashboard
GET    /api/v1/me/progress
GET    /api/v1/me/progress/topics?versionId=&subjectId=&status=all|completed|pending
PUT    /api/v1/me/progress/topics/{syllabusItemId}/mastery   { mastery: 1–5 | null }
GET    /api/v1/me/reports?period=daily|weekly|monthly&date=
```

Rules:

- Every metric (goal progress, study time, streak, accuracy, weak topics, revision statistics, dashboard) is computed from stored activity at request time; none is hardcoded or cached.
- Revision intervals and outcome effects come only from the user's settings, validated as: 1–20 strictly increasing intervals of 1–3650 days, Hard ≤ Good ≤ Easy step changes, and Easy ≥ +1. Current-affairs revision uses the same settings and history.
- A topic on a task, schedule, session or mistake must belong to a published syllabus version. It fills in its subject and stage, and must agree with any subject or stage that was also given.
- Only one session timer can run at a time. Stopped sessions are capped at 1,440 minutes, and logged sessions cannot end in the future. A task's actual time is the sum of its linked sessions; a manual value is accepted only when no sessions are linked.
- Weak topics: MCQ accuracy below the user's threshold with at least the user's minimum answers, or self-rated mastery of 1–2. Weak subjects apply the same thresholds per subject.
- A streak day needs at least the user's minimum session minutes, or any submitted MCQ attempt, submitted Written answer, completed Viva session, or revision review. Days follow Bangladesh Standard Time.

## Current-affairs endpoints

Public (anonymous; signed-in callers also receive their bookmark/note/revision flags):

```text
GET    /api/v1/current-affairs/entries?search=&category=&from=&to=&tag=&subjectId=&syllabusItemId=&bookmarkedOnly=&sortBy=recent|oldest|updated&pageNumber=&pageSize=
GET    /api/v1/current-affairs/entries/{entryId}
GET    /api/v1/current-affairs/digest?period=Day|Week|Month|Year&date=yyyy-MM-dd&category=
GET    /api/v1/current-affairs/archive
GET    /api/v1/current-affairs/categories
GET    /api/v1/current-affairs/tags
GET    /api/v1/official-sources?search=&category=
GET    /api/v1/official-sources/{sourceId}
```

Personal (`[Authorize]`, owner-scoped):

```text
GET    /api/v1/me/current-affairs/bookmarks
POST   /api/v1/me/current-affairs/entries/{entryId}/bookmark
DELETE /api/v1/me/current-affairs/entries/{entryId}/bookmark
GET    /api/v1/me/current-affairs/notes
PUT    /api/v1/me/current-affairs/entries/{entryId}/note
DELETE /api/v1/me/current-affairs/entries/{entryId}/note
GET    /api/v1/me/current-affairs/revision?dueOnly=
POST   /api/v1/me/current-affairs/entries/{entryId}/revision
POST   /api/v1/me/current-affairs/entries/{entryId}/revision/review      { "outcome": "Again|Hard|Good|Easy" }
DELETE /api/v1/me/current-affairs/entries/{entryId}/revision
```

Administration (`currentaffairs.manage`):

```text
GET    /api/v1/admin/current-affairs/entries?search=&status=&category=
GET    /api/v1/admin/current-affairs/entries/{entryId}
POST   /api/v1/admin/current-affairs/entries
PUT    /api/v1/admin/current-affairs/entries/{entryId}
POST   /api/v1/admin/current-affairs/entries/{entryId}/publish
POST   /api/v1/admin/current-affairs/entries/{entryId}/archive
DELETE /api/v1/admin/current-affairs/entries/{entryId}
GET    /api/v1/admin/current-affairs/question-search?type=&search=
GET    /api/v1/admin/current-affairs/resource-search?search=
GET    /api/v1/admin/official-sources?search=&status=&verificationStatus=
GET    /api/v1/admin/official-sources/{sourceId}
POST   /api/v1/admin/official-sources
PUT    /api/v1/admin/official-sources/{sourceId}
POST   /api/v1/admin/official-sources/{sourceId}/verification             { "status": "Verified|NeedsReview|Unreachable", "note": "..." }
POST   /api/v1/admin/official-sources/{sourceId}/publish
POST   /api/v1/admin/official-sources/{sourceId}/archive
DELETE /api/v1/admin/official-sources/{sourceId}
```

Rules:

- Entries start as drafts. Publishing requires a named source with an http/https URL and `sourceStatus = Verified` (the editor confirmed the source supports the entry); editing a published entry keeps that requirement. Only drafts and archived entries can be deleted.
- Digest weeks are ISO weeks (Monday–Sunday). Without `date`, the period containing today in Bangladesh Standard Time is returned, along with `previousAnchor` and `nextAnchor` for navigation. A digest returns at most 1,000 entries; `total` is the full count.
- Related questions must exist in the bank of the stated type; learners see only published ones. Attached library resources must be shared and published; private uploads can never be attached. Topics must belong to published syllabus versions.
- Directory entries publish only with a `Verified` record no older than 365 days. Changing the URL resets the record to `NeedsReview` and unpublishes the entry; recording `NeedsReview` or `Unreachable` also unpublishes it. Verification is a manual editor action; the API never fetches the URL.
- The revision schedule: `Again` gives 1 day; `Hard`, `Good` and `Easy` start at 2, 3 and 7 days and then multiply the previous interval by 1.2, 2.5 and 3.5, capped at 180 days.

## Admin and security endpoints

Each module's content administration lives in its own section above (syllabus, MCQ, Written, Viva, library, current affairs, official sources). The admin console (Phase 11) adds cross-cutting endpoints and does not duplicate them:

```text
GET    /api/v1/site-settings                          public: site name, active announcement, registration open, support email, footer
GET    /api/v1/admin/settings                         settings.manage
PUT    /api/v1/admin/settings                         settings.manage (audited)
GET    /api/v1/admin/audit?entityType=&entityId=&actorId=&action=&from=&to=   audit.read
GET    /api/v1/admin/audit/entity-types               audit.read
GET    /api/v1/admin/overview                         content.manage: counts per module the caller manages; recent activity only with audit.read
GET    /api/v1/admin/tags?module=&search=             content.manage, limited to the caller's modules
POST   /api/v1/admin/tags/rename                      { module?, from, to }: renaming to an existing tag merges
POST   /api/v1/admin/tags/delete                      { module?, tag }
POST   /api/v1/admin/bulk/status                      { module, action: Publish|Archive, ids (1–100) }
GET    /api/v1/admin/access                           admin.access smoke endpoint
```

Rules:

- **Authorization is server-side.** One policy per permission code is generated from `PermissionCodes`, each requiring an authenticated user with that `permission` claim. A test asserts that every `api/v1/admin/*` controller declares a registered policy and every `api/v1/me/*` controller requires authentication.
- Cross-module endpoints admit `content.manage` holders, then narrow to the modules the caller's own claims cover. Asking for another module returns 403.
- Bulk publish and archive call each module's own service per item, so the same validation applies (verified sources, rights basis, published topics). Items are independent, and the response lists each failure. The change tracker is cleared between items so a failed item cannot leak into the next save.
- `RegistrationOpen = false` makes `POST /auth/register` return 403 at the API, not only in the UI.
- User and role administration endpoints are not implemented; roles are assigned by an operator (see DEPLOYMENT.md).

## Cross-cutting HTTP behaviour (Phase 12)

- Errors are RFC 7807 Problem Details with `Content-Type: application/problem+json`; 500 responses contain only a generic message and the correlation ID.
- `POST /auth/login` and `/auth/register` are limited to 10 requests per minute per client IP (429), on top of Identity's per-account lockout. Uploads are limited to 30 per 10 minutes per user.
- Every response carries `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, a Permissions-Policy, and a restrictive CSP; HSTS is sent outside Development.
- Behind a proxy, set `ReverseProxy__Enabled` so client IPs and the scheme come from `X-Forwarded-*`.

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
