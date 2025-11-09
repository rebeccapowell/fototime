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

* Wire the Temporal development container into the AppHost manifest and service graph.
* Register Temporal infrastructure (client, worker, health check) within the Infrastructure project.
* Expose a `/ping` smoke endpoint that exercises a sample Temporal workflow/activity.
* Add automated checks covering the Temporal worker startup and smoke endpoint behavior.
  **Acceptance:**
  - Temporal worker starts cleanly through Aspire with the Temporal container wired in.
  - `/ping` endpoint responds successfully and is validated by an automated test invoking the workflow.
  - `/health` endpoint reports the Temporal health check as healthy, with tests verifying the response shape/status.
  **Depends:** T01

---

### T04 — Auth: OIDC + Cookie + Anti-Forgery

**Status:** Done
**Summary:** Secure site with OIDC (demo provider), cookie auth, authorization policies.
**Details:**

* Add login/logout, account area, anti-forgery for POST/PUT/DELETE.
* Secure headers (CSP, HSTS in prod), SameSite/Lax, HTTPS redirect.
  **Acceptance:** Auth required for main app; unauth redirect to login; headers present.
  **Depends:** T01

---

### T05 — Domain Modeling (Groups, Users, Profiles)

**Status:** Done
**Summary:** Create core entities, value objects, and invariants.
**Details:**

* Entities:
  * `Group` — owns `Membership` records, enforces globally unique `Slug` values, keeps canonical `PhotoLimits`, and is the aggregate root for topics, photos, and votes within the group.
  * `Membership` — ties a user to a group once, carries role/state (Owner, Member, Suspended), and guarantees there is only one active membership per user per group.
  * `Profile` — one per user, requires a valid `DisplayName`, optional avatar & bio constrained by `ContentSafetyTag`, and records opt-in privacy flags.
  * `Invite` — generated by a group owner, carries a unique token, email, inviter, and expiration `Period`; once accepted or expired it cannot be reused.
  * `Challenge` — backlog entry owned by a `Group`, transitions Proposed → Approved → Scheduled/Used → Archived and prevents skipping steps or reusing archived identifiers.
  * `WeeklyTopic` — scheduled instance of a `Challenge` with a `Period`; only one active topic per group, and it must reference an Approved or Scheduled challenge.
  * `Photo` — belongs to a `WeeklyTopic` and a submitting `Membership`, respects `PhotoLimits` (file size/count per member, safe-tag rules), and may only be submitted while the topic is open.
  * `Like` — reaction from a `Membership` to a `Photo`, limited to one like per member per photo and immutable after creation.
  * `Comment` — authored by a `Membership` on a `Photo`, stores sanitized text, enforces non-empty content, and can be soft-deleted while retaining audit metadata.
  * `SideQuest` — optional challenge variant tied to a `WeeklyTopic`, enforces unique slugs within the topic and inherits the topic `Period` constraints.
  * `EventItem` — audit log entry capturing domain events, ensures monotonic timestamps and references the originating aggregate identifier.
  * `Vote` — cast by a `Membership` for a `Photo` candidate within a `WeeklyTopic`, limited to one active vote per member per contest and only allowed during the voting window.
* Value objects:
  * `DisplayName` — trims whitespace, enforces 3–50 visible characters, forbids control characters and disallowed emoji, and normalizes case for comparison.
  * `Slug` — lowercase alphanumeric plus single hyphen separators, 3–40 characters, disallows leading/trailing hyphens, and normalizes Unicode to ASCII.
  * `PhotoLimits` — encapsulates file size (≤10 MB), resolution (≤8K on the long edge), and per-topic submission caps (default 1 photo/member) with guards against zero/negative values.
  * `Period` — requires non-null start/end timestamps, ensures `Start <= End`, forbids overlapping with existing periods when used for the same aggregate, and exposes helpers for containment checks.
  * `ContentSafetyTag` — enumerates `{Safe, Mature, Restricted}`, validates transitions (e.g., cannot downgrade from Restricted without moderator approval), and provides helper methods for UI hints.
* Domain events:
  * `InviteSent` — emitted when an `Invite` is created; payload includes `GroupId`, `InviteId`, inviter `MembershipId`, invitee email, and expiration timestamp.
  * `TopicStarted` — emitted when a `WeeklyTopic` moves into its active window; payload includes `GroupId`, `WeeklyTopicId`, associated `ChallengeId`, and the active `Period`.
  * `PhotosSubmitted` — emitted after a member successfully submits a `Photo`; payload includes `GroupId`, `WeeklyTopicId`, `PhotoId`, submitting `MembershipId`, and applied `ContentSafetyTag`.
  * `VotingClosed` — emitted when the voting window for a `WeeklyTopic` ends; payload includes `GroupId`, `WeeklyTopicId`, closing `Period`, and summarized vote totals per candidate.
* Domain entities and value objects stay persistence-agnostic: no EF Core attributes or types (`[Key]`, `DbSet`, `EntityTypeConfiguration`, etc.); map persistence concerns in `src/Infrastructure`, and keep Domain references limited to BCL or other domain types.
  **Acceptance:**
  - Compiles with the Domain project referencing only BCL or other domain types.
  - Domain project has no package references to EF Core or other infrastructure libraries.
  - Unit tests cover domain behaviors, the invariants above, value object validation rules, and the emission of each listed domain event without touching the database.
  **Depends:** T02

---

### T06 — Invitations: Use Case + Email + Temporal Workflow

**Status:** Todo
**Summary:** Allow group owner (parent) to invite users by email; expire after N days.
**Details:**

* Command: `SendInviteCommand` (Application), Domain create `Invite`.
* Email via dev Mailpit.
  * Use the Aspire MailPit hosting integration documented at [learn.microsoft.com/dotnet/aspire/community-toolkit/hosting-mailpit](https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/hosting-mailpit?tabs=dotnet-cli). Add the NuGet package to the app host (`dotnet add src/AppHost/AppHost.csproj package CommunityToolkit.Aspire.Hosting.MailPit`) so the generated `MailPitResource` is available.
  * Register the container through the extension method in `src/AppHost/Program.cs`:

    ```csharp
    var builder = DistributedApplication.CreateBuilder(args);

    var mailpit = builder.AddMailPit("mailpit");

    builder.AddProject<Projects.Web>("web")
        .WithReference(mailpit);

    builder.Build().Run();
    ```

    The integration pins Mailpit to the `axllent/mailpit:v1.22.3` image and exposes the standard SMTP (1025/tcp) and HTTP (8025/tcp) services. The reference automatically flows a `ConnectionStrings__mailpit` setting into the web project; parse it (format: `endpoint=smtp://host:port`) when configuring the invitation email sender.
  * Integration tests confirm delivery by querying the Mailpit HTTP API (`GET /api/v1/messages`) via the Aspire-provided HTTP endpoint (for example `http://localhost:8025/api/v1/messages`). Use this API to assert that an invite email was enqueued before proceeding with acceptance steps.
  * Local contributors can reproduce the setup by running `dotnet run --project src/AppHost/AppHost.csproj` and, in another terminal, invoking `curl http://localhost:8025/api/v1/messages` to inspect captured messages.
* Temporal workflow: send, reminder, expiry.
  * Implement workflow reminders and expiry handling using Temporal time skipping inside the .NET Testing Suite. Follow the official guidance at [Temporal .NET Testing Suite](https://docs.temporal.io/develop/dotnet/testing-suite) and exercise `TimeSkippingWorkflowEnvironment.RunAsync` (or equivalent) so automated tests advance virtual time to trigger reminder and expiry timers deterministically.
  * Provide contributors with a regression test example similar to:

    ```csharp
    await using var env = await TimeSkippingWorkflowEnvironment.StartAsync();
    var client = env.Client;
    var workflow = await client.StartWorkflowAsync(new InvitationWorkflow(input), options);
    await env.Client.WorkflowService.ForceTimeSkippingAsync();
    await env.SleepAsync(TimeSpan.FromDays(3)); // triggers reminder
    await env.SleepAsync(TimeSpan.FromDays(4)); // triggers expiry
    ```
* Accept invite: creates user membership, prompts profile creation + password (if local) or OIDC link.
  **Acceptance:**
  - End-to-end flow works via UI + HTMX; audit trail saved.
  - Integration tests verify invite email delivery by asserting the Mailpit API returns the expected message payload.
  - Temporal workflow tests pass using time skipping to cover reminder and expiry paths.
  **Depends:** T03, T04, T05

---

### T07 — Profiles: Create/Edit + Safety Controls

**Status:** Todo
**Summary:** Full profile editor covering identity fields, safety preferences, and granular privacy toggles.
**Details:**

* Profile fields and visibility defaults:
  * **Display name** — required, always public (cannot be hidden).
  * **Avatar image** — optional upload, defaults to public but can be marked private to show a placeholder to non-owners.
  * **Bio** — optional text, defaults to public but can be hidden from other members.
  * **Preferred content safety tag** — defaults to `Safe`, always public so other members and moderators understand the member's safety expectations.
  * **Email notifications opt-in** — defaults to enabled, always private to the member and stored as a preference only.
  * **Hide from search** — defaults to false; when enabled the profile is excluded from directory/search results even if other fields are public.
* Persist privacy selections on the `Profile` aggregate (e.g., `IsAvatarPrivate`, `IsBioPrivate`) and mirror them in projections/view models so query handlers can filter hidden data; reuse `HideProfileFromSearch` for directory privacy.
* Store avatars on disk in development with hashed filenames; record moderation hints/content safety metadata alongside privacy flags so downstream pipelines respect both.
* When serving profiles to other users, omit private field values and replace them with neutral placeholders or badges indicating the member chose to hide the information; only the owner sees full data in edit and view screens.
* Server-side validation still enforces file size/type checks, bio length, and safety tag transition rules even when fields are private.
  **Acceptance:**
  * Server rejects invalid uploads or safety tag transitions and persists privacy flags atomically with profile updates.
  * Queries/view models never expose private avatar/bio values to non-owners and hide-from-search members stay invisible in listing endpoints.
  * HTMX forms show inline validation errors, dynamically reveal/hide preview snippets when privacy toggles change, and re-render the public profile fragment so the user can confirm how others will see it.
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

* Scheduling configuration model stored per group:
  * Persist a canonical IANA timezone identifier (e.g., `Europe/Berlin`).
  * Persist the cron expression governing when voting opens (e.g., "Sun 18:00"), along with offsets for topic start/end windows (e.g., voting duration, topic open/close offsets in minutes).
  * Represent the data via a strongly typed value object (`GroupScheduleConfig`) exposed from the domain. Persistence options include new nullable columns on `Group` (`ScheduleTimezone`, `VotingCron`, `VotingOffsetMinutes`, `TopicStartOffsetMinutes`, `TopicDurationMinutes`) or a dedicated configuration table keyed by `GroupId`; whichever approach we choose must allow Temporal workers to query a single row per group.
* Store the configuration in the relational database via EF Core; infrastructure repositories map the value object to the chosen columns/table. Temporal worker startup loads all active group schedule configs into memory and watches for changes via periodic refresh (e.g., every few minutes) so per-group schedule updates flow through without restarts.
* Temporal cron: evaluate each group’s config so voting opens per the persisted cron + offsets, defaulting to Sunday 18:00 group-local time when unset.
* Temporal calculates topic periods using the group timezone, applying offsets for topic start (e.g., Monday 00:01 local) and duration. Persist the resulting `Period` on `WeeklyTopic` along with audit metadata tying back to the config snapshot used when scheduling.
  **Acceptance:**
  * Dry-run Temporal shows scheduled events; DB state updates.
  * Daylight Saving Time transitions respect the configured timezone: jobs scheduled across DST boundaries fire at the correct local times without duplicates or gaps.
  * Automated verification covers multiple groups with distinct schedules to prove that cron expressions, offsets, and timezone conversions are respected independently.
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
* Enforcement pipeline:
  * Domain guard on the `WeeklyTopic` aggregate rejects submissions that would exceed the caller’s per-topic allotment or arrive outside the active `Period`.
  * Web middleware on the upload endpoint performs a fast-fail check against a cached `RemainingPhotoSlots` projection so users receive immediate feedback before the request body streams.
  * Background reconciliation job audits daily uploads to ensure storage invariants stay in sync with domain counts; mismatches trigger alerts.
* Concurrency: application service wraps uploads in a transaction that locks the member/topic row, re-reads the latest submission count, and retries once on concurrency violations so parallel uploads can settle without exceeding limits.
* Countdown renders via a shared HTMX partial (`src/Web/Shared/_TopicCountdown.cshtml`) backed by a lightweight view model (`TopicCountdownViewModel`) exposing server time, topic end timestamp, and remaining slot data from the same projection powering the middleware.
  **Acceptance:** Automated tests cover domain guard + middleware enforcement, and UX review verifies the countdown component behavior and data binding.
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
* `/health` (JSON response with dependency health, including Postgres and Temporal checks) and `/alive` (liveness ping).
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
