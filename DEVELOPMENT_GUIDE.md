# Development Guide

## Status

This guide describes the intended setup. Application projects do not exist yet.

## Prerequisites

- Git
- Node.js LTS
- npm or pnpm
- .NET SDK, selected during implementation
- MySQL 8 or Docker-compatible MySQL
- Docker Desktop if containerized development is approved
- Visual Studio or VS Code

## Planned repository layout

```text
BCS Study Hub/
  src/
    BCSStudyHub.Domain/
    BCSStudyHub.Application/
    BCSStudyHub.Infrastructure/
    BCSStudyHub.Api/
  web/
  docker/
  docs/
```

The exact layout is finalized when solution scaffolding begins.

## Proposed initial setup commands

The maintainer should first install the approved SDKs and confirm versions. Example commands are for Phase 2, not Phase 1:

```powershell
mkdir src, web, tests, docker

dotnet new sln -n BCSStudyHub
dotnet new classlib -n BCSStudyHub.Domain -o src\BCSStudyHub.Domain
dotnet new classlib -n BCSStudyHub.Application -o src\BCSStudyHub.Application
dotnet new classlib -n BCSStudyHub.Infrastructure -o src\BCSStudyHub.Infrastructure
dotnet new webapi -n BCSStudyHub.Api -o src\BCSStudyHub.Api

dotnet sln BCSStudyHub.sln add `
  src\BCSStudyHub.Domain\BCSStudyHub.Domain.csproj `
  src\BCSStudyHub.Application\BCSStudyHub.Application.csproj `
  src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj `
  src\BCSStudyHub.Api\BCSStudyHub.Api.csproj

npm create vite@latest web -- --template react-ts
cd web
npm install
npm install tailwindcss @tailwindcss/vite
```

Project references are added only after the base projects exist:

```powershell
dotnet add src\BCSStudyHub.Application\BCSStudyHub.Application.csproj reference src\BCSStudyHub.Domain\BCSStudyHub.Domain.csproj
dotnet add src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj reference src\BCSStudyHub.Application\BCSStudyHub.Application.csproj
dotnet add src\BCSStudyHub.Api\BCSStudyHub.Api.csproj reference src\BCSStudyHub.Application\BCSStudyHub.Application.csproj
dotnet add src\BCSStudyHub.Api\BCSStudyHub.Api.csproj reference src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj
```

## Environment conventions

- Keep user secrets outside source control.
- Configure database, JWT, storage, CORS, and logging with environment variables or a secret store.
- Use separate development, test, and production settings.
- Never commit passwords or signing keys.
- Seed only neutral bootstrap data needed for the first administrator and permission baseline.

## Planned frontend commands

The frontend is scaffolded under `web/`. Run it with:

```powershell
cd web
npm install
npm run dev
npm run build
npm run lint
```

Use package scripts for repeatability instead of memorizing tool commands.

## Planned backend commands

After solution scaffolding:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src\BCSStudyHub.Api
```

## Database migrations

No migrations exist yet. Before any migration is added, implementation documentation must explain:

- entities changed
- relationship effects
- compatibility and rollback concerns
- exact migration command

Planned commands will use the Infrastructure or startup project conventions established during scaffolding, for example:

```powershell
dotnet ef migrations add <MigrationName> --project src\BCSStudyHub.Infrastructure --startup-project src\BCSStudyHub.Api
dotnet ef database update --project src\BCSStudyHub.Infrastructure --startup-project src\BCSStudyHub.Api
```

The maintainer may review generated migrations and execute them explicitly.

## Validation policy

Every implementation phase must include:

- focused automated tests for changed behavior
- frontend build and applicable lint/test commands
- backend build and applicable test commands
- API smoke testing against Swagger or HTTP files
- manual verification for the primary responsive UI flow
- migration review before database changes

Do not claim a check passed until it is actually run.

## Git practice

- Keep each implementation phase on focused commits or branches.
- Do not commit secrets or generated local data.
- Add generated build output and uploaded files to `.gitignore`.
- Document architecture decisions that affect future modules.
