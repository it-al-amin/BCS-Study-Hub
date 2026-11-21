# Development Guide

## Current stack

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 and Oracle `MySql.EntityFrameworkCore` 10 provider
- ASP.NET Core Identity with GUID user/role keys
- React, TypeScript, Vite, Tailwind CSS, and React Router
- MySQL 8.0+

## Prerequisites

- Git
- Node.js LTS and npm
- .NET 10 SDK
- MySQL 8.0+ (or a compatible local MySQL server)
- Visual Studio or VS Code

## Repository layout

```text
BCSStudyHub.sln
src/
  BCSStudyHub.Domain/
  BCSStudyHub.Application/
  BCSStudyHub.Infrastructure/
  BCSStudyHub.Api/
tests/
  BCSStudyHub.Api.Tests/
web/
```

## MySQL database setup

Run the following as a MySQL administrator, replacing the example password with a strong local-only value:

```sql
CREATE DATABASE bcs_study_hub CHARACTER SET utf8mb4;
CREATE USER 'bcs_app'@'localhost' IDENTIFIED BY 'REPLACE_WITH_STRONG_PASSWORD';
GRANT ALL PRIVILEGES ON bcs_study_hub.* TO 'bcs_app'@'localhost';
```

Store the connection string and a randomly generated JWT signing key in .NET user-secrets (not in `appsettings.json` or source control):

```powershell
dotnet user-secrets set "ConnectionStrings:MySql" "Server=localhost;Port=3306;Database=bcs_study_hub;User ID=bcs_app;Password=REPLACE_WITH_STRONG_PASSWORD;" --project src\BCSStudyHub.Api

$jwtBytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($jwtBytes)
$jwtKey = [Convert]::ToBase64String($jwtBytes)
dotnet user-secrets set "Jwt:SigningKey" $jwtKey --project src\BCSStudyHub.Api
```

`Jwt:Issuer`, `Jwt:Audience`, and `Jwt:AccessTokenMinutes` have non-secret development defaults in `appsettings.json`. Production must supply an explicit CORS origin allowlist through configuration; the localhost Vite origin is configured only in `appsettings.Development.json`.

## Migrations

Update the EF CLI tool if an older version is installed (or use the install command if it is not installed):

```powershell
dotnet tool update --global dotnet-ef --version 10.0.12
# If dotnet-ef is not installed:
dotnet tool install --global dotnet-ef --version 10.0.12
```

The `InitialIdentityAndAudit`, `AddVersionedSyllabus`, `AddMcqPracticeEngine`, `AddWrittenPreparation`, `AddVivaPreparation`, and `AddDigitalLibrary` migrations are included. For a first install, apply all included migrations with the `database update` command below. Do not regenerate existing migrations. To inspect the migration list:

```powershell
dotnet ef migrations list --project src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj --startup-project src\BCSStudyHub.Api\BCSStudyHub.Api.csproj
```

The Phase 4 syllabus migration was generated with:

```powershell
dotnet ef migrations add AddVersionedSyllabus --project src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj --startup-project src\BCSStudyHub.Api\BCSStudyHub.Api.csproj --output-dir Persistence\Migrations
```

The Phase 5 MCQ migration was generated with:

```powershell
dotnet ef migrations add AddMcqPracticeEngine --project src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj --startup-project src\BCSStudyHub.Api\BCSStudyHub.Api.csproj --output-dir Persistence\Migrations
```

The Phase 6 Written migration was generated with:

```powershell
dotnet ef migrations add AddWrittenPreparation --project src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj --startup-project src\BCSStudyHub.Api\BCSStudyHub.Api.csproj --output-dir Persistence\Migrations
```

The Phase 7 Viva migration was generated with:

```powershell
dotnet ef migrations add AddVivaPreparation --project src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj --startup-project src\BCSStudyHub.Api\BCSStudyHub.Api.csproj --output-dir Persistence\Migrations
```

The Phase 8 digital library migration was generated with:

```powershell
dotnet ef migrations add AddDigitalLibrary --project src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj --startup-project src\BCSStudyHub.Api\BCSStudyHub.Api.csproj --output-dir Persistence\Migrations
```

For later model changes, add a new migration with a unique name and review its generated schema before applying it:

```powershell
dotnet ef migrations add <MigrationName> --project src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj --startup-project src\BCSStudyHub.Api\BCSStudyHub.Api.csproj --output-dir Persistence\Migrations
```

Apply the committed initial migration to the configured MySQL database:

```powershell
dotnet ef database update --project src\BCSStudyHub.Infrastructure\BCSStudyHub.Infrastructure.csproj --startup-project src\BCSStudyHub.Api\BCSStudyHub.Api.csproj
```

The first migration creates Identity, role-permission, and audit tables. The Phase 4 migration adds stages, syllabus versions, subjects, papers, the topic/subtopic item hierarchy, source references, and user-owned topic progress. The Phase 5 migration adds sourced Preliminary MCQs, dynamic practice-set rules, attempts, answer snapshots, bookmarks, and mistakes. The Phase 6 migration adds sourced Written questions/model answers, user-owned answer revisions, bookmarks, notes, goals, and reports. The Phase 7 migration adds editable Viva categories/questions/references, private notes/history/bookmarks/profile preparation, and ordered mock sessions. No questions, answers, or unverified syllabus topics are seeded. The startup seeder adds only BPSC-published syllabus subject/marks outlines and editable Viva category labels; it does not seed official Viva questions or topics.

## Run and verify

From the repository root:

```powershell
dotnet restore BCSStudyHub.sln
dotnet build BCSStudyHub.sln
dotnet test BCSStudyHub.sln
dotnet run --project src\BCSStudyHub.Api\BCSStudyHub.Api.csproj
```

The API seeds the baseline Candidate, ContentManager, and Administrator roles and permission codes, exam stages, verified official outline versions, and source references after migrations have run. It intentionally does not create an administrator user or assign the Administrator role automatically. In Development, Swagger is available at `/swagger`; liveness and database readiness checks are `/health/live` and `/health/ready`.

The frontend commands are:

```powershell
Set-Location web
npm install
npm run dev
npm run build
npm run lint
```

Vite proxies `/api` to the backend's Development HTTP URL (`http://localhost:5217`). The frontend stores access JWTs only in memory; sign-in is required again after a reload or token expiry.

## Configuration and security

- Use .NET user-secrets locally and a managed secret store/environment variables in deployment.
- Never commit database credentials or JWT signing keys.
- Set `Cors:AllowedOrigins` explicitly for each deployed frontend.
- Access JWTs expire within 5–60 minutes; account lockout is enabled after repeated failures.
- Role permissions are included in issued JWT claims; permission changes take effect after the access token expires.
- Public registration assigns only the Candidate role.
- The database must be migrated before the API starts; startup then seeds baseline roles/permissions and the explicitly source-verified syllabus outline.
- Only accounts with the `syllabus.manage` permission can access the syllabus admin API. Published versions are immutable; use a new draft for revisions.
- Library files are stored by the local provider under `LibraryStorage:RootPath` (default `App_Data/library-files` relative to the API content root; git-ignored). In deployment, point it at a persistent volume outside the web root and include it in backups, because MySQL holds only file metadata. `LibraryStorage:MaxFileSizeBytes` (default 50 MB, at most 100 MB) and `LibraryStorage:UserQuotaBytes` (default 2 GB) are validated at startup.
- Only accounts with `library.manage` can manage the shared library. Learners' private library resources are not readable by administrators.

## Validation policy

Every implementation phase should run focused tests, the frontend build/lint where applicable, the backend build/tests, and review generated migrations before applying them to a non-disposable database.
