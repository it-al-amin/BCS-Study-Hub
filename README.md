# BCS Study Hub

BCS Study Hub is a database-driven preparation platform for Bangladesh Civil Service (BCS) candidates. The React frontend and ASP.NET Core / MySQL foundation are implemented, including a versioned, source-linked BPSC syllabus outline and authenticated topic-progress APIs.

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

The API includes Identity, audit and permission tables plus versioned syllabus tables. Question-bank, library, planner, and other study-module tables have not been added.

## Syllabus content coverage

The seeded Preliminary and Written versions contain the BPSC-published subject groups and marks. Detailed PDF topics are not yet transcribed or verified. The Viva version reports only the official oral-exam marks/pass mark; it does not invent topic guidance. See the [source register](SYLLABUS_SOURCES.md).

## Running the frontend

```powershell
cd web
$env:PATH="C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs;" + $env:PATH
npm install
npm run dev
npm run build
npm run lint
```

The syllabus explorer, authentication screens, and syllabus administration use typed API services. Other study modules may still contain clearly labelled temporary UI data. Backend setup, database, migration, and API commands are in the [Development Guide](DEVELOPMENT_GUIDE.md).
