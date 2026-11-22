# Content Ingestion Guide

Planned pipeline (adapt to the real code as phases land): `SourceMonitor` -> source adapter (feed / sitemap / HTML / document) -> normalisation -> fingerprint (SHA-256, canonical URL) -> change detection -> duplicate detection -> classification and syllabus mapping -> versioning -> review queue.

| Phase | Scope | Status |
|---|---|---|
| 1 | Source registry, DB foundation, admin UI, scan-request records, docs | **Done** |
| 2 | RSS/Atom, sitemap, permitted HTML acquisition with SSRF-safe fetcher, scan worker, discovered-content tables | **Done** |
| 3 | Document / PDF discovery and extraction | Next |
| 4 | Daily 12:00 AM Asia/Dhaka scheduler, retries, locking, recovery | Planned |
| 5 | Discovery, change detection, duplicates, versions | Planned |
| 6 | Classification, Bangla/English handling, syllabus mapping, review workflow | Planned |
| 7 | Knowledge centre and search | Planned |
| 8 | Integration with MCQ, Written, Viva, Current Affairs, Library, Planner | Planned |
| 9 | Sharing, access, performance, analytics | Planned |
| 10 | Testing, security audit, deployment, final docs | Planned |
