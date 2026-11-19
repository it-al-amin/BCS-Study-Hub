# BCS Study Hub

BCS Study Hub is a long-term, database-driven preparation platform for Bangladesh Civil Service (BCS) candidates. It will organize preliminary, written, viva, current-affairs, resource-library, revision, planning, and performance-tracking workflows across changing syllabus versions.

This repository currently contains the Phase 1 architecture only. It intentionally does not contain application code or database migrations.

## Planned technology

- React, TypeScript, Vite, and Tailwind CSS
- ASP.NET Core Web API
- Clean Architecture
- Entity Framework Core
- MySQL
- JWT authentication and role/permission-based authorization
- Docker for repeatable local and deployment environments
- Local or object storage for uploaded resources

## Documentation

- [Architecture](ARCHITECTURE.md) - system and module design
- [Database Design](DATABASE_DESIGN.md) - entities, relationships, indexes, and migrations
- [API Design](API_DESIGN.md) - REST conventions and module contracts
- [Development Guide](DEVELOPMENT_GUIDE.md) - environment and setup commands
- [Development Roadmap](DEVELOPMENT_ROADMAP.md) - implementation phases and dependencies
- [Content Guide](CONTENT_GUIDE.md) - content and source-management rules
- [Project Status](PROJECT_STATUS.md) - current status and checklist

## Proposed repository structure

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
    BCSStudyHub.Domain.Tests/
    BCSStudyHub.Application.Tests/
    BCSStudyHub.Infrastructure.Tests/
    BCSStudyHub.Api.IntegrationTests/
    web/
  BCSStudyHub.sln
```

This structure is a proposal for empty-workspace setup. Phase 1 has not created it.

## Recommended next phase

Implement the foundation vertical slice: solution and workspace scaffolding, health/version configuration, MySQL/EF Core baseline, authentication, permission baseline, and the smallest versioned-syllabus read/admin flow. Do not add the remaining study modules in that phase.

## Running the frontend

```powershell
cd web
$env:PATH="C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Microsoft\VisualStudio\NodeJs;" + $env:PATH
npm install
npm run dev
npm run build
```

The frontend currently presents temporary UI data only. It does not pretend to persist records.
