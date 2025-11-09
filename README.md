Here’s a compact, Codex-friendly **Markdown ticket plan** you can paste into a repo (e.g., `/tickets/plan.md`).
It’s structured for small, testable steps, with strict coding standards and clear dependencies.
**Status values:** `Todo | InProgress | Blocked | Review | Done`.

---

# TeamTunes (Working Title) — Delivery Plan

## Development Standards

⚠️ **IMPORTANT: Zero-Warning Policy** ⚠️

This project maintains strict quality standards:
1. **Build Quality:**
   - All builds MUST have zero warnings
   - Latest .NET analyzers must be enabled
   - No suppressed or disabled warnings allowed
   - All new code must compile without warnings

2. **Testing Standards:**
   - All tests MUST pass with zero warnings
   - New features require test coverage
   - Test warnings are treated as failures
   - Integration tests must be included for DB/API changes

3. **Code Analysis:**
   - Latest .NET analyzers enabled
   - All analyzer rules enforced
   - IDE0011 (braces) and similar style rules enforced
   - No pragma warning disables without team review

4. **Review Process:**
   - PR builds must be warning-free
   - Test coverage must be maintained
   - Style/analyzer compliance required
   - No merging with pending warnings

## Guiding Principles

* **Security first:** OAuth2/OIDC auth, least privilege, output encoding, input validation, secure headers, CSRF/anti-forgery for unsafe verbs.
* **Privacy by default:** Minimum profile fields; parental-consent friendly UX; delete/export my data endpoints.
* **Architecture:** Clean Architecture (Domain, Application, Infrastructure, Web). No logic in controllers/razor pages; domain services pure; repositories via interfaces.
* **Observability:** OpenTelemetry (traces, logs, metrics); health checks; structured logging (JSON).
* **UX:** Server-rendered Razor + **HTMX** for interactivity; **shadcn/ui** for styling; responsive, accessible; infinite scroll UX for photos.
* **Workflows/Scheduling:** **Temporal** for invites, weekly topic/voting windows, reminders, results computation.
* **DB:** PostgreSQL via EF Core (code-first, migrations). Row-level scoping by GroupId.
* **Code Quality:** .editorconfig, latest analyzers enabled, nullable required, async all I/O, guard clauses, comprehensive tests, SOLID principles.

## Installing the .NET 9 SDK

Install the .NET 9 SDK by running the official install script for your platform:

* **macOS/Linux**

  ```bash
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0
  ```

  The script installs into `~/.dotnet` by default. Add `~/.dotnet` and `~/.dotnet/tools` to your `PATH` (for example by updating `~/.bashrc` or `~/.zshrc`) so that `dotnet --info` resolves the new SDK.

* **Windows (PowerShell)**

  ```powershell
  irm https://dot.net/v1/dotnet-install.ps1 | iex
  dotnet-install --channel 9.0
  ```

  The script installs into `%USERPROFILE%\.dotnet` unless you pass a custom `-InstallDir`. Ensure `%USERPROFILE%\.dotnet` and `%USERPROFILE%\.dotnet\tools` are on your `PATH` before launching a new shell.

After installation, verify the SDK is available with:

```bash
dotnet --version
```

You should see a `9.0.x` version number in the output.

## Repository Layout (target)

```
/src
  /AppHost               # Aspire AppHost
  /Web                   # ASP.NET (Razor + HTMX), shadcn, auth
  /Application           # Use-cases, DTOs, validators
  /Domain                # Entities, value objects, events, services
  /Infrastructure        # EF Core, Postgres, Temporal, email
/tests
  /Unit
  /Integration
/prd
  SECURITY.md
  ARCHITECTURE.md
/tickets/plan.md
```

---

## Tickets

### T01 — Bootstrap Aspire Solution (.NET 9)

**Status:** Done
**Summary:** Create Aspire solution with projects per repo layout and wiring.
**Details:**

* New solution with projects and references matching structure above.
* Add Aspire manifest to run Web, Postgres, Temporal (dev server), Mailpit.
* Add .editorconfig, Directory.Build.props (nullable, strict analyzers), clear warnings policy.
  **Acceptance:**
  - `dotnet build` succeeds with zero warnings
  - All tests pass with zero warnings
  - Code analysis is clean (no disabled rules)
  - `AppHost` runs all services
  - README contains clear dev-run instructions
  **Quality Gates:**
  - ✅ No warnings policy: All builds must have zero warnings
  - ✅ Test coverage: All new code must have tests
  - ✅ Code analysis: Latest .NET analyzers enabled and warnings treated as errors
  **Depends:** —

---

### T02 — Infrastructure: Postgres + EF Core + Migrations

**Status:** Done
**Summary:** Configure EF Core, DbContext, migrations, Postgres connection from Aspire.
**Details:**

* Add `AppDbContext`, design-time factory, migration bundle.
* Configure Npgsql mappings (DateTime UTC), snake_case.
* Health check for DB.
* Initial migration bundle created; migrator consumes `ConnectionStrings__fototime` from Aspire.
  **Acceptance:** Migration applies on startup; healthcheck green.
  **Depends:** T01

---

### T03 — Temporal Integration (dev)

**Status:** Todo
**Summary:** Add Temporal client/worker hosting; health check.
**Details:**

* Add `ITemporalClient` and Worker in Infrastructure; register with Aspire.
* Add sample “ping” workflow/activity for smoke test.
  **Acceptance:** Temporal Worker starts; sample workflow runs via test endpoint.
  **Depends:** T01

---

### T04 — Auth: OIDC + Cookie + Anti-Forgery

**Status:** Todo
**Summary:** Secure site with OIDC (demo provider), cookie auth, authorization policies.
**Details:**

* Add login/logout, account area, anti-forgery for POST/PUT/DELETE.
* Secure headers (CSP, HSTS in prod), SameSite/Lax, HTTPS redirect.
  **Acceptance:** Auth required for main app; unauth redirect to login; headers present.
  **Depends:** T01

---

### T05 — Domain Modeling (Groups, Users, Profiles)

**Status:** Todo
**Summary:** Create core entities, value objects, and invariants.
**Details:**

* Entities: `Group`, `Membership`, `Profile`, `Invite`, `Challenge`, `WeeklyTopic`, `Photo`, `Like`, `Comment`, `SideQuest`, `EventItem`, `Vote`.
* Value objects: `DisplayName`, `Slug`, `PhotoLimits`, `Period`, `ContentSafetyTag`.
* Domain events: `InviteSent`, `TopicStarted`, `PhotosSubmitted`, `VotingClosed`.
  **Acceptance:** Compiles; unit tests for invariants.
  **Depends:** T02

---

### T06 — Invitations: Use Case + Email + Temporal Workflow

**Status:** Todo
**Summary:** Allow group owner (parent) to invite users by email; expire after N days.
**Details:**

* Command: `SendInviteCommand` (Application), Domain create `Invite`.
* Email via dev Mailpit.
* Temporal workflow: send, reminder, expiry.
* Accept invite: creates user membership, prompts profile creation + password (if local) or OIDC link.
  **Acceptance:** End-to-end flow works via UI + HTMX; audit trail saved.
  **Depends:** T03, T04, T05

---

### T07 — Profiles: Create/Edit + Safety Controls

**Status:** Todo
**Summary:** Simple profile (display name, avatar with moderation hint, bio limited).
**Details:**

* Server-side validation; file size/type checks; store avatars in disk (dev) with hash filename.
* Option for private profile fields (not shown to others).
  **Acceptance:** Create/edit profile works; validation and error UX via HTMX.
  **Depends:** T04, T05

---

### T08 — Challenges Backlog (CRUD + Proposals)

**Status:** Todo
**Summary:** Any member can propose challenge ideas; group owner can approve/publish.
**Details:**

* Backlog list, create proposal form (HTMX), approve/retire actions.
* State machine: Proposed → Approved → Scheduled/Used → Archived.
  **Acceptance:** Backlog screens; unit tests on transitions.
  **Depends:** T05, T07

---

### T09 — Weekly Topic Cycle (Scheduling with Temporal)

**Status:** Todo
**Summary:** Every Sunday evening start the vote; Monday start Topic of the Week.
**Details:**

* Temporal cron: Sun 18:00 Europe/Berlin → open voting for `WeeklyTopic` (from approved backlog).
* Temporal starts topic at Mon 00:01; stores `Period` (start/end).
* Configurable per group.
  **Acceptance:** Dry-run Temporal shows scheduled events; DB state updates.
  **Depends:** T03, T05, T08

---

### T10 — Voting: Topic of the Week (HTMX UI + Anti-Double Vote)

**Status:** Todo
**Summary:** Users vote among candidate challenges; one vote/user; visible result after close.
**Details:**

* `Vote` entity keyed by (UserId, TopicId).
* HTMX partials for live counts (polling or SSE later).
* Close voting on schedule; persist winner → `WeeklyTopic.Active`.
  **Acceptance:** Vote once enforced; winner selected; audit trail.
  **Depends:** T09

---

### T11 — Topic Execution Window + Photo Limits

**Status:** Todo
**Summary:** Enforce “up to N photos per user” during active topic week.
**Details:**

* Config `PhotoLimits.MaxPerTopic` default 5.
* Prevent uploads after end; show countdown.
  **Acceptance:** Limits enforced; UX shows remaining slots.
  **Depends:** T05, T09

---

### T12 — Photo Upload + Storage + Thumbnail Pipeline

**Status:** Todo
**Summary:** Upload photos with basic moderation flags; generate thumbnails.
**Details:**

* Stream uploads; store originals + sizes; EXIF strip; compute hash for dedupe.
* Background resize (sync in dev).
* Content safety flag (manual toggle now).
  **Acceptance:** Upload works; thumbnails render; large images performant.
  **Depends:** T11

---

### T13 — Pinterest-Style Infinite Scroll (HTMX)

**Status:** Todo
**Summary:** Paginated waterfall grid with infinite scrolling for photos.
**Details:**

* HTMX `hx-get` next page, append grid items; maintain scroll position; server paging.
* Include likes/comment counts on cards.
  **Acceptance:** Smooth infinite scrolling; no full page reloads; lighthouse passes.
  **Depends:** T12

---

### T14 — Likes & Comments (Optimistic HTMX)

**Status:** Todo
**Summary:** Users can like and comment on any photo in the group.
**Details:**

* POST endpoints; anti-forgery; idempotent like (toggle).
* HTMX swaps to update counts and comment list.
  **Acceptance:** Like toggle/auth enforced; comments render; spam limits (min delay).
  **Depends:** T12, T04

---

### T15 — Side Quests (Weekly Mini-Challenges)

**Status:** Todo
**Summary:** Weekly side quests list; optional participation; separate voting.
**Details:**

* CRUD for side quests; scheduling (Temporal) aligned with topic week.
* Submission rules: 1 photo per side quest (config).
  **Acceptance:** Side quest appears in diary; submissions and voting work.
  **Depends:** T03, T05, T12

---

### T16 — Voting: Challenge Winners (Topic + Side Quest)

**Status:** Todo
**Summary:** End-of-week voting for best submissions; per-user single vote per contest.
**Details:**

* Voting window closes Sunday 17:59; compute winners; tiebreaker random among ties.
* Persist results; announce via notification email (dev).
  **Acceptance:** Results computed by Temporal; winners visible; email delivered to Mailpit.
  **Depends:** T03, T10, T11, T15

---

### T17 — Diary: Upcoming Events & Side Quests

**Status:** Todo
**Summary:** Calendar/diary list showing topic/voting windows and side quests.
**Details:**

* Read from `WeeklyTopic`, `SideQuest`, `EventItem`.
* Friendly “this week” and next 4 weeks.
  **Acceptance:** Diary page responds <200ms p95; accessible.
  **Depends:** T09, T15

---

### T18 — Group Management (Create/Join/Leave)

**Status:** Todo
**Summary:** Owners create groups; members join via accepted invite; leave group.
**Details:**

* Slug for shareable URL; owner transfer; delete (soft) with safeguards.
  **Acceptance:** CRUD works; RBAC checks; tests for permissions.
  **Depends:** T06, T05

---

### T19 — Security Hardening Pass

**Status:** Todo
**Summary:** CSP, anti-clickjacking, rate-limiting, validation, audit fields.
**Details:**

* CSP nonces for inline HTMX swaps; output encoding helpers.
* Minimal PII; audit columns (created/updated/by).
  **Acceptance:** Security headers verified; OWASP ASVS L1 checks pass.
  **Depends:** T04, T12, T14

---

### T20 — Observability & Health

**Status:** Todo
**Summary:** OpenTelemetry wired for Web, EF Core, Temporal; health endpoints.
**Details:**

* Add traces for command handlers; app metrics (requests/sec, queue length).
* `/healthz`, `/readyz`, `/livez`.
  **Acceptance:** Traces visible; Postgres + Temporal dependency checks green.
  **Depends:** T01–T03

---

### T21 — CI (GitHub Actions) + Checks

**Status:** Todo
**Summary:** Build, test, lint, format, security scan on PR.
**Details:**

* `dotnet format --verify-no-changes`; `dotnet test --collect:"XPlat Code Coverage"`.
* FOSSA/OWASP dep scan (or dotnet list package outdated + audit).
  **Acceptance:** Passing badge; required checks enforced.
  **Depends:** T01

---

### T22 — Seeding & Demo Data

**Status:** Todo
**Summary:** Seed script to create demo group, invites, backlog, photos.
**Details:**

* Separate seed profile; idempotent.
  **Acceptance:** `--seed` flag populates dev DB; demo logins documented.
  **Depends:** T02, T05–T08, T12

---

### T23 — Documentation (ARCHITECTURE.md, SECURITY.md)

**Status:** Todo
**Summary:** Explain flows, boundaries, data retention, parental controls.
**Details:**

* Sequence diagrams for invite, weekly topic, voting, side quest.
* Data deletion/export process.
  **Acceptance:** Docs reviewed; links from README.
  **Depends:** T06, T09, T16

---

### T24 — Accessibility & UX Polish

**Status:** Todo
**Summary:** Keyboard nav, focus states, aria-labels, color contrast.
**Details:**

* Add shadcn components with accessible patterns.
* Screen-reader audit on photo cards, forms, infinite scroll.
  **Acceptance:** Axe devtools passes (no critical issues); keyboard-only usable.
  **Depends:** T13–T17

---

## Coding & Review Checklist (apply to every PR)

* ✅ Unit tests for domain/application; integration tests for repositories/endpoints.
* ✅ No business logic in controllers/razor pages; application services orchestrate use cases.
* ✅ All I/O async; cancellation tokens honored.
* ✅ DTOs validated (FluentValidation); server-side validations mirrored in UI error states.
* ✅ Logging is structured and contextual; no PII in logs.
* ✅ EF queries reviewed for N+1; projections used; indexes created where needed.
* ✅ HTMX endpoints return partials only; full pages for direct navigation.
* ✅ Anti-forgery tokens for unsafe requests; idempotency where applicable.
* ✅ Temporal workflows deterministic; activities isolated; retry policies set.
* ✅ Secrets via Aspire dev secrets; never committed.

---

### Notes for Codex

* Update each ticket’s **Status** inline (search by `Txx —` id).
* Keep diffs small; prefer one ticket per PR when possible.
* When splitting, suffix with `.1`, `.2` (e.g., `T12.1 Thumbnails`).

---

If you want, I can generate starter files (solution, projects, AppHost manifest, and initial Temporal worker + HTMX scaffolding) next.
