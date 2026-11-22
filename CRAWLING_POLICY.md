# Crawling Policy

This policy governs the Source Monitoring module and supersedes the earlier "no scraping" note in OFFICIAL_SOURCES.md **only** for sources an authorised user has configured and enabled. The official-source directory itself is still verified manually.

1. **Only configured sources.** The system fetches nothing that was not added as a `ContentSource` and enabled by a person.
2. **Respect the site.** Obey robots.txt and stated terms; identify the bot with a clear User-Agent; limit concurrency and rate (default one request per second per host); honour ETag/Last-Modified; stop on repeated 429/5xx.
3. **No circumvention.** Never bypass paywalls, logins, CAPTCHAs or anti-bot protection.
4. **Retention limits.** Each source has a retention policy: `LinkAndMetadataOnly`, `ExcerptAndSummary`, or `FullTextPermitted` (requires a written rights note). Private access does not grant a right to copy protected content.
5. **Safe fetching (SSRF).** HTTPS only, default port, public hosts only; resolve DNS and re-check the IP (block loopback, private, link-local, metadata); re-validate each redirect (max 3); size, time and content-type limits.
6. **Provenance.** Every item keeps its source, canonical URL, retrieval time and rights note. Collected content is unverified and private until reviewed.
7. **Enforced in code (Phase 2).** `SafeHttpFetcher` rejects non-HTTPS, credentials, non-default ports and internal host names; resolves DNS itself and refuses to connect unless **every** returned address is public; disables automatic redirects and re-validates each hop (max 3); caps response size (2 MB), time (20 s) and content types; sends a clear User-Agent (`BCSStudyHubBot`); spaces requests to a host by at least 1 s (more if robots.txt gives Crawl-delay). robots.txt is fetched per host and cached for an hour: a missing file allows crawling, an unreadable one (5xx/network) blocks it for ten minutes.
8. **Do not fabricate.** No invented facts, dates, results or questions; AI output must be labelled and never auto-verified.
