# Deployment, Backup, and Restore

> **Status:** these files and steps were written and reviewed, but **not executed**: no Docker or MySQL was available where they were prepared. Validate everything on a staging host first. See [AUDIT_REPORT.md](AUDIT_REPORT.md).

## Components

| Component | Runs as | Persistent data |
|---|---|---|
| MySQL 8.4 | `mysql` service | `mysql-data` volume: all records, including file **metadata** |
| API (.NET 10) | `api` service, port 8080, non-root | `library-files` volume: uploaded file **bytes** |
| Web (React build + nginx) | `web` service, port 80 | none |

Both volumes must be backed up together. Neither is useful without the other.

## Configuration

All configuration comes from environment variables (Compose reads `.env`). Never put secrets in `appsettings*.json`.

| Variable (API) | Purpose |
|---|---|
| `ConnectionStrings__MySql` | MySQL connection string |
| `Jwt__SigningKey` | Random value of at least 32 bytes (`openssl rand -base64 48`); validated at startup |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenMinutes` | Token settings (lifetime 5–60 min, default 30) |
| `Cors__AllowedOrigins__0` | Public site origin, if the frontend is served from a different origin |
| `AllowedHosts` | Host names the API answers to (`;`-separated); do not leave `*` in production |
| `ReverseProxy__Enabled`, `ReverseProxy__KnownProxies__0` | Trust `X-Forwarded-*` from the proxy (required behind nginx or a load balancer) |
| `Database__ApplyMigrationsOnStartup` | `true` applies pending migrations on start (see below) |
| `LibraryStorage__RootPath`, `__MaxFileSizeBytes`, `__UserQuotaBytes` | Upload storage location and limits |

Frontend build argument: `VITE_API_BASE_URL` (default `/api/v1`, same origin through nginx).

## First deployment (on staging)

1. Copy `deploy/.env.example` to `.env` at the repository root and replace every value.
2. Set `APPLY_MIGRATIONS_ON_STARTUP=true` for this first start only.
3. `docker compose up -d --build`
4. Check health: `curl http://localhost:8080/health/live` and `/health/ready` (the second verifies the database).
5. Set `APPLY_MIGRATIONS_ON_STARTUP=false` and run `docker compose up -d` again.
6. Register your account in the web app, then **grant the Administrator role** (no administrator is created automatically):

   ```sql
   INSERT INTO AspNetUserRoles (UserId, RoleId, AssignedAt)
   SELECT u.Id, r.Id, UTC_TIMESTAMP(6)
   FROM AspNetUsers u JOIN AspNetRoles r ON r.Name = 'Administrator'
   WHERE u.NormalizedEmail = UPPER('you@example.org');
   ```

   Run it with `docker compose exec mysql mysql -u root -p <database>`. Sign out and in again; permissions are read into the token at sign-in. Use `ContentManager` for editors.
7. Smoke test: sign in; open the admin dashboard; create, publish, and view one item in each module; upload and preview a PDF in the library; start and stop a study timer; review a revision item; check that the official directory lists the seeded verified sources.

### Applying migrations without startup migration (recommended for production)

Generate the reviewed, idempotent script and apply it in a maintenance window, **after taking a backup**:

```powershell
dotnet ef migrations script --idempotent --project src\BCSStudyHub.Infrastructure --startup-project src\BCSStudyHub.Api -o migrate.sql
```

All migrations so far are additive; none drops data. Phase 10 adds three columns to existing tables (`TopicProgress.Mastery`, `TopicProgress.MasteryUpdatedAt`, `CurrentAffairsRevisions.Step`), with defaults.

## HTTPS

Terminate TLS in front of the `web` service, for example with a host nginx or Caddy holding certificates, or a cloud load balancer, and forward to `WEB_PORT`. Keep `ReverseProxy__Enabled=true` so the API sees the real client IP for rate limiting. Outside Development the API sends HSTS.

## Backups

```sh
sh deploy/scripts/backup.sh ./backups
```

This writes `db-<timestamp>.sql.gz` (a consistent `mysqldump --single-transaction`) and `library-<timestamp>.tar.gz`. Schedule it daily, for example with cron, copy both files off the host, and keep several generations.

## Restore

```sh
sh deploy/scripts/restore.sh backups/db-<stamp>.sql.gz backups/library-<stamp>.tar.gz
```

The script asks you to type `RESTORE`, stops the API, replaces the database and library files, and restarts the API. Afterwards, check `/health/ready` and sign in. **Practice a restore on staging regularly**; an untested backup is not a backup.

## Upgrading

1. Back up.
2. `git pull`, then review new migrations (`dotnet ef migrations script <last-applied> --idempotent`).
3. Apply the script, or start once with `APPLY_MIGRATIONS_ON_STARTUP=true`.
4. `docker compose up -d --build`, then run the smoke test.

## CI

`.github/workflows/ci.yml` runs on pushes to `main` and on pull requests:
- **Backend:** Release build with warnings as errors, all tests, a migration/model consistency check, and a NuGet vulnerability scan.
- **Frontend:** `npm ci`, lint, production build, and `npm audit`.

It does not deploy. The same commands passed locally on 2026-11-21, except the `npm ci` step, which was not run locally.
