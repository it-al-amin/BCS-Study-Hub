# BCS Study Hub – Business Logic (short)

Edit this file freely; it summarises what the system does today.

## Purpose
A study platform for Bangladesh Civil Service (BCS) candidates: structured syllabus, practice (MCQ / Written / Viva), a digital library, current affairs, and personal planning/progress tracking. Content is curated by editors; nothing is scraped or AI-graded.

## Users and roles
| Role | Can do |
|---|---|
| Candidate (default on registration) | Dashboard, read published content, track own progress, use practice, library, planner |
| ContentManager | Candidate + manage syllabus, MCQ, Written, Viva, library, current affairs |
| Administrator | Everything, including users, site settings, audit log |

Access is permission-based (`dashboard.read`, `syllabus.read`, `content.manage`, `syllabus.manage`, `mcq.manage`, `written.manage`, `viva.manage`, `library.manage`, `currentaffairs.manage`, `users.manage`, `settings.manage`, `audit.read`, `admin.access`). Auth is ASP.NET Identity + JWT (30-minute access token); passwords need 12+ chars with upper, lower, digit and symbol; 5 failed logins lock the account for 15 minutes. Admin actions are audit-logged.

## Modules

1. **Syllabus**
   - Exam stages: Preliminary, Written, Viva.
   - Versioned syllabus per stage/track → subjects → papers → topics/subtopics, each with source links and a verification status.
   - Lifecycle: draft → published → archived. Only published syllabus is visible to candidates.
   - Candidates mark per-topic progress and mastery.

2. **MCQ (Preliminary)**
   - Question with options (single or multiple answer), difficulty, explanation, exam year/reference, tags, source references, linked to a syllabus topic.
   - A question can only be published if it has a verified source and a topic.
   - Practice modes: topic, subject, mixed, mock exam; configurable question count, timer and marking (correct / wrong / unanswered).
   - An attempt snapshots the questions; answers are validated and scored on the server; results, review, history, bookmarks, a mistake list and analytics follow.

3. **Written**
   - Question bank mapped to the syllabus with verified sources and model answers.
   - Candidates write answers in a workspace (drafts, revisions, submissions), keep notes and goals, and self-assess against a rubric. No AI marking.

4. **Viva**
   - Editable categories and questions with suggested answers.
   - Candidates bookmark, write answer notes, run ordered mock sessions, self-evaluate, and see a weak-topic report.

5. **Digital library**
   - Books, files, external links and notes with category, rights basis, subjects, tags, chapters/page ranges.
   - Private (owner only) or shared (published by managers; shared files only for redistributable rights bases and with a rights note).
   - Upload rules: extension allowlist, content checks, size limit, per-user quota, rate limits. Files live in local storage; MySQL holds only metadata.
   - Bookmarks, reading progress, notes and collections are per user.

6. **Current affairs**
   - Entries by category with summary, event/source dates, tags, cited source and links to syllabus topics or questions.
   - Daily, weekly, monthly and yearly views; private bookmarks, notes and spaced revision (Again / Hard / Good / Easy).
   - Official resource directory: manually verified source links; changing a URL or a failed check unpublishes the entry; verification expires after 365 days.

7. **Planner and progress**
   - Daily/weekly/monthly goals, prioritised tasks, schedules, session timer and manual time logs, calendar.
   - Progress by topic, subject and stage, study time, streaks, and MCQ/Written/Viva performance.

8. **Admin console**
   - Users and roles, content management, site settings (name, announcement, footer), audit log.

## Key rules
- Content policy: editors write summaries in their own words with a cited source; no copyrighted full-text upload without rights; no scraping of third-party content.
- Learners only see published content; drafts and archived items stay hidden.
- Everything a learner creates (progress, notes, bookmarks, attempts) is private to that user.
- Sensitive operations are permission-checked, validated, rate-limited where relevant, and audited.

## Tech stack
React + TypeScript + Vite + Tailwind (web) · ASP.NET Core 10 Clean Architecture (Api / Application / Domain / Infrastructure) · EF Core with MySQL/MariaDB · JWT auth.
