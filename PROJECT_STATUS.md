# Project Status

## Last updated

2026-11-19

## Current phase

Phase 2 - React frontend foundation

## Completed

- [x] Project vision captured
- [x] Technology stack identified
- [x] Core module scope identified
- [x] Clean Architecture boundary proposed
- [x] Database entity families proposed
- [x] Frontend route/module structure proposed
- [x] Backend module structure proposed
- [x] Authentication and authorization approach proposed
- [x] File-storage abstraction proposed
- [x] Development phases proposed
- [x] Initial architectural risks documented
- [x] README, architecture, development, and content guides created
- [x] Database design documented
- [x] API conventions documented
- [x] Development dependencies and roadmap documented
- [x] React frontend foundation implemented
- [x] React production build verified

## Pending implementation

- [ ] Confirm .NET and package versions
- [ ] Scaffold ASP.NET Core backend
- [ ] Scaffold ASP.NET Core Clean Architecture solution
- [ ] Configure MySQL and EF Core
- [ ] Implement authentication and permissions
- [ ] Implement versioned syllabus hierarchy
- [ ] Implement admin content workflows
- [ ] Implement file storage
- [ ] Implement library and resource metadata
- [ ] Implement MCQ practice and exams
- [ ] Implement written practice
- [ ] Implement viva practice
- [ ] Implement planner and revision
- [ ] Implement current-affairs workflows
- [ ] Implement real-data dashboard
- [ ] Add Docker and CI/CD
- [ ] Add automated tests
- [ ] Add backup and restore procedures

## Planned first vertical slice

The first implementation slice should be small enough to validate architecture end to end:

1. Project scaffolding
2. Authentication baseline
3. Database bootstrap
4. One versioned syllabus hierarchy
5. One admin CRUD workflow
6. React authenticated shell and route guard
7. Build and test

It should not implement all subjects, exam rules, or all content modules in advance.

## Known issues
-
- No ASP.NET Core backend or database migrations exist.
- The frontend intentionally uses isolated temporary presentation data.
- Official BPSC syllabus details have not yet been manually reviewed and versioned.
- The exact .NET and Node package versions are unselected.
- Identity and refresh-token details require implementation decisions.
- Initial storage provider is unselected.
- Search behavior is unselected.
- Initial public registration policy is unselected.

## Testing status

Frontend production build verified with `npm run build` on 2026-11-19.
Backend and database tests are not applicable in this frontend phase.
