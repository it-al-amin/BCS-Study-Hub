# BCS Study Hub

BCS Study Hub is a database-driven preparation platform for Bangladesh Civil Service (BCS) candidates. The React frontend and ASP.NET Core / MySQL foundation are implemented, including the versioned source-linked BPSC syllabus outline, Preliminary MCQ practice engine, Written preparation workspace, and Viva preparation system.

## Technology

- React, TypeScript, Vite, and Tailwind CSS
- ASP.NET Core 10 Web API with Clean Architecture
- Entity Framework Core 10 and MySQL
- ASP.NET Core Identity, short-lived JWT authentication, and permission-based authorization
- Planned: Docker for repeatable local and deployment environments
- Planned: local or object storage for uploaded resources

## Documentation

- [Architecture](ARCHITECTURE.md) - system and module design
- [Database Design](DATABASE_DESIGN.md) - entities, relationships, indexes, and migrations
- [API Design](API_DESIGN.md) - REST conventions and module contracts
- [Development Guide](DEVELOPMENT_GUIDE.md) - environment and setup commands
- [Development Roadmap](DEVELOPMENT_ROADMAP.md) - implementation phases and dependencies
- [BPSC Syllabus Sources](SYLLABUS_SOURCES.md) - official references, verified outline, and coverage limits
- [Content Guide](CONTENT_GUIDE.md) - content and source-management rules
- [Library Security Review](LIBRARY_SECURITY_REVIEW.md) - copyright, access-control, and file-security controls for the digital library
- [Project Status](PROJECT_STATUS.md) - current status and checklist

## Repository structure

```text
BCS-Study-Hub/
  src/
    BCSStudyHub.Domain/
    BCSStudyHub.Application/
    BCSStudyHub.Infrastructure/
    BCSStudyHub.Api/
  web/
  docker/
  docs/
  tests/
    BCSStudyHub.Api.Tests/
  BCSStudyHub.sln
```

The API includes Identity, audit and permission tables, versioned syllabus tables, Preliminary MCQ/practice tables, Written preparation tables, and Viva question/session tables.

## Syllabus content coverage

The seeded Preliminary and Written versions contain the BPSC-published subject groups and marks. Detailed PDF topics are not yet transcribed or verified. The Viva version reports only the official oral-exam marks/pass mark; it does not invent topic guidance. See the [source register](SYLLABUS_SOURCES.md).

## Preliminary MCQ practice

The MCQ question bank, configurable topic/subject/mixed/mock-exam sets, timed attempts, answer review, history, bookmarks, mistake tracking, and performance analytics are implemented. The API validates submitted selections and calculates scores from attempt snapshots; clients cannot provide correctness or score values. No exam rule or negative marking is hardcoded, and multiple-answer items require an exact option-set match. No sample questions or detailed topics are seeded; verified Preliminary syllabus topics and attributed question sources are prerequisites for published practice content.

## Written preparation

The Written module provides a source-aware syllabus explorer and descriptive/analytical/essay/composition/translation question bank, model answers with verified references, topic-linked resources and private study notes, answer drafts/submissions/revisions, bookmarks, configurable writing goals, and personal progress reports. Word count is server-calculated and time is self-reported. Self-assessment is personal only; no automatic AI evaluation is claimed. No Written questions, model answers, or detailed topics are seeded.

## Viva preparation

The Viva module provides editable categories, a searchable and topic-filtered question bank, editor-entered source/suggested-answer references, bookmarks, private answer notes with revisions, question history, profile preparation, and ordered mock sessions with answer notes, self-evaluation, feedback, and weak-topic flags. Initial categories are user-requested navigation labels, not an official BPSC topic list. No Viva questions are seeded; reference verification is editor-entered. Voice and AI evaluation are not implemented.

## Digital library

The library manages books (publisher, edition, year, ISBN, cover, chapters with page ranges), PDFs, images, Office documents, text notes, external links, government publications, reports, and newspaper references. Each item can be mapped to syllabus subjects, topics, and chapters and tagged. Learners keep a private library and can use bookmarks (resource, chapter, or page), reading progress, personal notes, and collections. Content managers curate a shared library with a draft/publish/archive workflow. Uploads are verified by content signature, size- and quota-limited, rate-limited, hashed, and stored outside MySQL behind a storage abstraction. Files are served only to authorised users. Shared files must have a redistributable rights basis; personal copies of commercial books stay private. No library content is seeded, and malware scanning is not yet integrated. See [LIBRARY_SECURITY_REVIEW.md](LIBRARY_SECURITY_REVIEW.md).

## Running the frontend

```powershell
cd web
$env:PATH="C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs;" + $env:PATH
npm install
npm run dev
npm run build
npm run lint
```

The syllabus explorer, authentication screens, syllabus/MCQ/Written/Viva administration, and learner workflows use typed API services. Other study modules may still contain clearly labelled temporary UI data. Backend setup, database, migration, and API commands are in the [Development Guide](DEVELOPMENT_GUIDE.md).
