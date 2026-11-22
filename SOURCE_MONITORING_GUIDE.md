# Source Monitoring Guide

Status: **Phases 1-2 implemented** (source registry, admin UI, safe fetcher, feed/sitemap/HTML adapters, discovered-content storage). The daily 12:00 AM scheduler arrives in Phase 4.

## What exists today
- `ContentSource` (`ContentSources` table): name, Bangla name, type, extraction method, base/feed/sitemap URLs, category, language, priority, reliability, scan interval, retention policy, include/exclude URL patterns, rights note, verification flag, enabled flag, health and last-scan fields.
- `ContentSourceScan` (`ContentSourceScans` table): one row per scan request with trigger (Scheduled / Manual / Retry), status, counts and error.
- Admin API `api/v1/admin/content-sources` (permission `sources.manage`, held by Administrator and ContentManager):
  `GET /`, `GET /summary`, `GET /{id}`, `POST /`, `PUT /{id}`, `POST /{id}/enable|disable|archive|restore`, `DELETE /{id}` (archived only), `POST /{id}/scans`, `GET /scans`, `POST /scans/{scanId}/retry`.
- Admin page: `/admin/content-sources` (add/edit, enable/disable, archive, queue scan, scan history, retry).

## Rules enforced now
- New sources are created **disabled**; enabling is a deliberate step after reading the site's terms.
- Only absolute `https://` URLs on the default port, no credentials, no localhost/private/link-local/metadata addresses or internal suffixes (`SourceUrlRules`). The Phase 2 fetcher must repeat these checks on the resolved IP and on every redirect.
- Feed extraction needs a feed URL; sitemap extraction needs a sitemap URL; `FullTextPermitted` retention needs a rights note.
- Base URL is unique. Archived sources cannot be enabled or scanned; only archived sources can be deleted.
- At most one queued/running scan per source (no duplicate jobs). Only failed/partial/cancelled scans can be retried.
- Every change is written to the audit log (`contentsource.*`).

## Phase 2: how a scan works
1. **Queue.** `POST /{id}/scans` (or Retry) creates a `Queued` scan. `ContentScanWorker` (a hosted service polling every 10 s) claims the oldest queued scan, marks it `Running` and runs `ContentScanProcessor`. On start it marks scans stuck `Running` for over 30 minutes as `Failed` so they can be retried. It assumes **one API instance**.
2. **Fetch.** Everything goes through `SafeHttpFetcher` (see CRAWLING_POLICY.md): HTTPS only, public IPs only (checked on every connection), redirects re-validated and capped at 3, 2 MB / 20 s limits, content-type allowlist, one request per second per host, `robots.txt` honoured (with Crawl-delay).
3. **Adapt.** By extraction method: `Feed` -> RSS 2.0/1.0 and Atom (`FeedParser`); `Sitemap` -> urlset and sitemap index, own host only, max 6 files (`SitemapParser`); `HtmlExtraction` -> fetch the base page, follow up to 20 same-site links that match the include/exclude patterns, extract title, description, canonical URL, language, date and main text (`HtmlPage`). `ManualOnly` sources fail the scan with a clear message. XML is parsed with DTDs disabled.
4. **Record.** Items are keyed by (source, SHA-256 of canonical URL). New items become `PendingReview` and `Private`; unchanged items only refresh `LastSeenAt`; changed content (SHA-256 of title+text) creates version N+1 and keeps the earlier version in `DiscoveredContentVersions`. A reviewed item that changes returns to `Updated`. Identical text found at another URL is flagged `Duplicate`.
5. **Retention.** `LinkAndMetadataOnly` stores title, URL and date only; `ExcerptAndSummary` adds a 500-character excerpt; `FullTextPermitted` also stores text (max 200,000 characters).
6. **Result.** The scan ends `Succeeded`, `PartiallySucceeded` (some pages failed) or `Failed`; the source health becomes Healthy / NeedsAttention / Failing (3 consecutive failures). One bad page or source never stops the others.

Settings live in the `Crawler` configuration section (`UserAgent`, `MinHostIntervalMilliseconds`, `MaxBytes`, `TimeoutSeconds`, `MaxRedirects`, `MaxItemsPerScan`, `MaxPagesFetchedPerScan`, `WorkerEnabled`, `WorkerPollSeconds`).

## Reading what was found
Admin API `GET api/v1/admin/discovered-content` and `/{id}` (permission `sources.manage`); admin page `/admin/discovered-content`.

## Not yet implemented
Daily scheduler, due-date logic from `ScanIntervalMinutes`, ETag/Last-Modified reuse between scans, "removed or unavailable" detection, document/PDF extraction, classification, review actions (verify/publish/reject) and the learner-facing library.
