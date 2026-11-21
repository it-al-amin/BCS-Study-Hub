# Content Guide

## Content model

The platform separates the syllabus structure from content collections:

```text
Exam stage
  -> Syllabus version
    -> Subject
      -> Paper
        -> Topic
          -> Subtopic
```

Syllabus versions prevent old questions and resources from being silently associated with new syllabus rules.

## Adding syllabus content

Use official BPSC publications as source records. Before entering marks, papers, subjects, or syllabus wording:

1. Record the official source title and URL.
2. Record publication/effective dates.
3. Create a separate syllabus version.
4. Enter subjects, papers, topics, and subtopics.
5. Link the version to sources.
6. Review hierarchy changes before publishing.

Do not copy unverified syllabus details into hardcoded code. The database and admin interface must remain the editable source of truth.

## Adding study notes

Each note should include:

- title
- concise summary
- full content
- one or more syllabus associations
- tags
- source references where factual
- draft/published/archived status

Personal notes are separate from published study notes.

## Adding books

Record:

- title
- author/editor
- publisher
- edition
- publication year
- ISBN if available
- description
- cover resource
- source attribution
- chapters
- topic associations

Do not upload a copyrighted full book unless authorized to store or distribute it. Prefer summaries, chapter links, and official purchase/source links.

In the app, choose **Add resource → Book** (private, under `/library/new`) or **New shared resource → Book** (administrators, under `/admin/library/new`). Save the metadata first; then upload an optional digital copy and cover, and add chapters with page ranges, chapter topics, and chapter-wise resources. The ISBN is optional and checksum-validated.

### Private versus shared

| | Private library | Shared library |
| --- | --- | --- |
| Who can see it | Only you | Every signed-in learner, once published |
| Who manages it | You | `library.manage` holders (Administrator, ContentManager) |
| Workflow | Available immediately | Draft → Publish → Archive (archive before deleting) |
| Personal copy of a commercial book | Allowed | **Not allowed**: catalogue it without a file or link to the publisher |

### Rights basis

Every resource with a file declares a rights basis:

- **Personal copy**: a copy you own for private study. Private library only.
- **My own work**: your notes, summaries or diagrams.
- **Public domain**: copyright has expired or never applied.
- **Government publication**: released for public distribution. In the shared library, record where it was published and on what terms.
- **Open licence**: for example CC BY. In the shared library, record the licence.
- **Permission granted**: the rights holder allowed distribution. In the shared library, record the permission.
- **No file / metadata only**: catalogue entries and links.

The full rationale and enforcement points are in [LIBRARY_SECURITY_REVIEW.md](LIBRARY_SECURITY_REVIEW.md).

## Adding PDFs and documents

Before upload:

- confirm permission and copyright status
- enter title and description
- select resource type and visibility
- add source name and URL
- associate subjects/topics/subtopics
- add tags and collections
- record publication date and publisher

Accepted files: PDF, PNG, JPEG, WEBP, GIF, DOCX, XLSX, PPTX, TXT, MD and CSV. The default limit is 50 MB per file and 2 GB of private storage per user (`LibraryStorage` configuration). Files are checked by content, not just extension. SVG, HTML, legacy `.doc/.xls/.ppt`, macro-enabled documents, and executables are rejected. Text files must be UTF-8. Uploading again replaces the existing file. Metadata remains in MySQL; file bytes remain in storage. PDFs, images and text preview in the browser; Office documents download.

## Adding images

Record source attribution for every image where required. Add title, description, alt text, tags, and syllabus associations. Do not upload screenshots or scanned content without considering copyright and privacy.

Use the **File** kind with a suitable category (for example *Newspaper article* for a clipping, *Reference* for a map), or upload a **Cover** on a book. Covers must be images.

## Adding external resources

Prefer official government and international organization sources:

- Bangladesh Public Service Commission
- ministries and government agencies
- Bangladesh Bank
- Bangladesh Bureau of Statistics
- official gazette and publications
- United Nations agencies and other official international organizations

Record URL, source name, publisher, publication date, access date, and related syllabus topics.

Use the **External link** kind. The URL must be `http` or `https` without embedded credentials. Set **Source type** (*Official / government*, *Academic*, *News media*, *Reference work*, *Commercial publisher*, *Personal*, *Other*). Source type describes provenance; it is not a reliability score, but it lets learners prioritise official material. Use the *Newspaper article*, *Report* and *Government publication* categories so these items can be filtered. Linking is the preferred way to reference government publications and news articles when redistribution rights are unclear.

## Adding MCQ questions

Record:

- question prompt
- question type
- options
- exactly one correct option or unambiguous rule
- answer explanation
- syllabus associations
- difficulty
- source attribution
- tags
- status

Never expose correct-answer metadata to practice clients before submission.

## Adding written questions

Record:

- prompt
- stage/paper/topic associations
- marks or word limits
- expected structure
- source
- model answer and references
- difficulty
- status

Personal submitted answers and evaluations remain private to the owner.

## Adding viva questions

Record:

- question
- editable category and optional editor-defined topic
- BCS exam year/number or exam reference when known
- question-source and suggested-answer references with publisher and HTTPS URL
- source verification status entered by the editor
- status

Do not represent application categories or editor-defined topics as an official BPSC Viva syllabus. No Viva questions or suggested answers are seeded. Personal answer notes, mock-session answers, self-evaluations, profile preparation, and improvement areas remain private to the user. No voice recording or automated/AI evaluation is available.

## Adding current affairs

For every item:

- use a factual, concise title
- include a summary and fuller content
- enter event and publication dates
- select category
- add tags
- record source name and URL
- link official reference files
- associate syllabus topics and related questions
- choose draft/published/archived status

Curate official sources manually. Do not blindly scrape or store third-party material without authorization.

## Publication workflow

Draft content is not visible in normal study flows. Published content is visible and can be linked to. Archived content remains available for audit and historical views but is excluded from default searches.

## Administrative workflow

Every editable domain should support:

- search
- filtering
- pagination
- draft/published/archived status
- preview
- validation
- audit record
- source attribution where applicable

Bulk imports should be rejected, accepted for review, or queued only after validation and authorization. No module should silently mark unverified content as official.
