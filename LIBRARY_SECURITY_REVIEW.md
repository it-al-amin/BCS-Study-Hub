# Digital Library: Copyright, Access-Control and File-Security Review

Reviewed before implementing Phase 8 (2026-11-21). This document records the decisions and where each one is enforced, so that later changes can be checked against them.

## 1. Copyright

### Risk

Most BCS preparation books (guide books, commercial textbooks, question banks) are copyrighted. Bangladeshi copyright law also covers works published by the government, so publication on a government website does not by itself grant permission to redistribute. Uploading a scanned book once and making it visible to every learner would be redistribution. This review is an engineering control, not legal advice; confirm the rights position for any specific shared file.

### Decisions

| Rule | Enforcement |
| --- | --- |
| Every resource records a **rights basis** (`NotApplicable`, `PersonalCopy`, `OwnWork`, `PublicDomain`, `GovernmentPublication`, `OpenLicense`, `PermissionGranted`) and an optional rights note. | `LibraryResource.RightsBasis` / `RightsNote` |
| A file cannot be attached while the rights basis is `NotApplicable`. | `LibraryRules.ValidateFileRights` |
| `PersonalCopy` files may exist **only in private resources**. A shared resource cannot be created or updated with `PersonalCopy`. | `LibraryRules.AllowsSharedDistribution`, `LibraryService.ApplyResourceInputAsync` |
| Shared files under `GovernmentPublication`, `OpenLicense` or `PermissionGranted` require a rights note naming the licence, permission or publication terms. | `LibraryRules.RequiresRightsNote` |
| Rights are re-checked when a shared resource is updated or published, so a file cannot be "laundered" by changing the rights basis afterwards. | `UpdateResourceAsync`, `SetStatusAsync` |
| Book metadata (title, author, ISBN, publisher, chapters) and external links are factual references and may always be shared. Commercial books should be catalogued without a file, or linked to the publisher. | Admin UI guidance; `NotApplicable` rights basis |
| No books, files or resources are seeded. | `DatabaseSeeder` adds only the `library.manage` permission |

Rights declarations are made by the uploader; the system cannot verify them. Shared resources start as drafts and need a deliberate publish action by a `library.manage` holder, which is audited.

## 2. Access control

| Rule | Enforcement |
| --- | --- |
| Every library endpoint requires authentication (unlike the anonymous Written/Viva browse endpoints) because files are access-controlled. | `[Authorize]` on all three controllers |
| **Private** resources are visible to and manageable by their owner only. Other users, administrators included, get `404 Not Found` rather than `403`, so resource IDs cannot be probed. | `LibraryService.VisibleTo`, `ManagedBy` |
| **Shared** resources are created, edited, published, archived and deleted only by holders of the `library.manage` permission (seeded to Administrator and ContentManager). | `AdminLibraryController` policy, `LibraryManagementScope.Shared` |
| Learners see shared resources only once they are `Published`. Archiving hides them again, including from collections, bookmarks and reading lists. | `VisibleTo`, list queries filter on visibility |
| Managers **cannot read learners' private resources or files**; the admin file endpoint only widens access to shared drafts. | `OpenFileAsync(asManager: true)` |
| Bookmarks, reading progress, personal notes and collections are owner-scoped and can reference only resources the user can currently see. | `LibraryService.Personal.cs` |
| Chapters of shared books may link only to shared resources, so a private resource can never leak into the shared catalogue. | `ApplyChapterInputAsync` |
| Published shared resources must be archived before deletion; deletion cascades learner bookmarks, notes and progress for that resource. | `DeleteResourceAsync` |
| Topic mappings must reference published syllabus versions; subject mappings must reference active subjects. | `ValidateTopicsAsync`, `ValidateSubjectsAsync` |
| Every shared-library change, and every file upload and delete in either scope, writes an audit log entry with a correlation ID. | `AddAudit` |

## 3. File security

| Control | Detail |
| --- | --- |
| Extension allowlist | `.pdf .png .jpg .jpeg .webp .gif .docx .xlsx .pptx .txt .md .csv` |
| Content verification | Magic-byte signatures for PDF and images. Office files must be ZIP packages with `[Content_Types].xml` and their main part (`word/document.xml`, `xl/workbook.xml`, `ppt/presentation.xml`). Text must be valid UTF-8 with no NUL bytes and at most 5 MB. The **server-detected** content type is stored; the client's declared MIME type is ignored. |
| Rejected outright | SVG and HTML (script execution), legacy `.doc/.xls/.ppt` and macro-enabled formats, Office files that contain `vbaProject.bin` or ActiveX parts, executables, files with no extension |
| Zip-bomb guard | At most 10,000 entries and 512 MB of declared uncompressed size per Office file |
| Size limits | Per-file limit (default 50 MB, `LibraryStorage:MaxFileSizeBytes`) under a hard 100 MB request ceiling. Per-user private quota (default 2 GB, `LibraryStorage:UserQuotaBytes`). |
| Rate limiting | 30 uploads per user per 10 minutes (`library-upload` fixed-window policy, HTTP 429) |
| Storage keys | Random `yyyy/MM/{guid}` keys without extensions, validated against a strict pattern, and resolved paths confined to the storage root. User file names are sanitised and kept as metadata only, never used as paths. |
| Integrity | A SHA-256 hash is stored for every file and shown in the UI |
| Storage location | `LibraryStorage:RootPath` (default `App_Data/library-files` under the API content root). The API serves no static files, and the directory is git-ignored. |
| No blobs in MySQL | MySQL holds metadata only: name, type, size, hash, provider, key, uploader, time |
| Download path | Files are streamed only through authorised endpoints with `X-Content-Type-Options: nosniff`, `Cache-Control: private, no-store`, a sandboxing `Content-Security-Policy`, and `Cross-Origin-Resource-Policy: same-origin`. Office documents are always served as attachments; only PDF, images and text may be shown inline. |
| Frontend viewer | Files are fetched with the bearer token into short-lived object URLs (tokens never appear in URLs). External links open with `rel="noopener noreferrer nofollow"`; URLs must be `http`/`https` with no embedded credentials. |
| Failure handling | A blob written before a failed database save is deleted. Old blobs are deleted after a successful replacement or delete, and orphaned blobs are logged. |

## 4. Known gaps (accepted for this phase)

- **No malware scanning.** No scanning infrastructure is available. Signature checks block disguised executables and active content, but not, for example, a malicious PDF that exploits a viewer bug. Add an `IFileScanner` step before `storage.SaveAsync` once a scanner such as ClamAV is approved.
- **Local filesystem provider only.** `IFileStorage` isolates the provider; an S3/Azure Blob adapter is future work, along with backup of the storage root (it is not covered by MySQL backups).
- **Rights declarations are self-reported.** The workflow records and audits them but cannot verify them.
- **The PDF viewer iframe is not sandboxed**, because browsers refuse to run their built-in PDF viewer in sandboxed frames. Mitigation: only files with a verified PDF signature are previewed, from a same-origin blob URL.
- **Not yet run against MySQL.** The migration is generated and reviewed but has not been applied, and HTTP-level integration tests await a test database.
