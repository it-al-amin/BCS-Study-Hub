# Privacy and Access Guide

- The source-management API requires `sources.manage` (Administrator, ContentManager); anonymous requests get 401. Checks run on the backend, not only in the UI.
- Collected content will be **Private by default**; visibility levels (Private, Shared, Published) and review gates arrive with the content entities in Phase 5-6. Nothing collected is published automatically.
- Source records are admin-only and never exposed through public endpoints. Source URLs are validated to prevent SSRF.
- Secrets: keep the DB connection string and JWT signing key in user-secrets or environment variables, never in `appsettings.json`.
