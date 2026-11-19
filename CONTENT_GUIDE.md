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

## Adding PDFs and documents

Before upload:

- confirm permission and copyright status
- enter title and description
- select resource type and visibility
- add source name and URL
- associate subjects/topics/subtopics
- add tags and collections
- record publication date and publisher

Allowed file types and size limits will be configured by the API. Upload metadata remains in MySQL; file bytes remain in storage.

## Adding images

Record source attribution for every image where required. Add title, description, alt text, tags, and syllabus associations. Do not upload screenshots or scanned content without considering copyright and privacy.

## Adding external resources

Prefer official government and international organization sources:

- Bangladesh Public Service Commission
- ministries and government agencies
- Bangladesh Bank
- Bangladesh Bureau of Statistics
- official gazette and publications
- United Nations agencies and other official international organizations

Record URL, source name, publisher, publication date, access date, and related syllabus topics.

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
- suggested response areas
- Bangladesh/International Affairs or subject associations
- personal/profile dimensions
- cadre or job relevance
- source or topic references
- status

Mock-viva answers should remain private unless explicitly shared.

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
