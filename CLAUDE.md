# CLAUDE.md

When I ask a question or make an observation, respond with an answer - do NOT jump to making code changes unless I explicitly ask for them.

## Environment & Commands

**Line endings must be CRLF (`\r\n`)** - never use bare LF.

- Run via npm scripts, never direct .NET/dotnet commands (avoids lock-file conflicts): Dev `npm run dev` | Test `npm run test` (`test:backend`, `test:frontend`; single test: `npm run test:backend -- <TestClassName>` / `npm run test:frontend -- <file-pattern>`) | Lint `npm run lint <path>` | Build `npm run verify:build`
- **Stale cache**: Add `-- --clear-cache` to `verify:build` or `lint` if builds fail with cached errors

## Architecture

- **Backend**: ASP.NET 10, `web/Areas/{AreaName}/` | **Frontend**: Vue 3 multi-SPA, Vite → `wwwroot/vue/`
- **DB**: SQL Server 2016 + EF Core (CTS, RAPS, AAUD schemas) | **Auth**: CAS + `[Permission]` (see API & Cross-Environment)
- **Identity:** `AaudUser.AaudUserId` = `Person.PersonId`. If mismatched, TEST DB needs refresh.
- **Design system (UI)**: All UI rules (colors, typography, components, `<main>` landmark, WCAG-AA contrast) live in [DESIGN.md](DESIGN.md) — read it before building or changing UI. Always use Quasar components.
- **VueUse**: Prefer VueUse composables over hand-rolled reactive logic.
- **O(n²) lookups (JS & C#)**: Never nest per-item searches (`.find()`/`.some()`/`.FirstOrDefault()`/`.Any()`) inside a loop over another growable list. Pre-build a `Map`/`Set`/`Dictionary` once, then look up in the loop. Small fixed reference lists (~10 items) are fine. In EF this is N+1 — see Correlated subqueries.
- **Plurals**: Use `inflect("word", count)` from the `inflection` package — never hand-roll ternaries for noun pluralization

## Database & EF Core

- **SQL Server 2016** — no `STRING_AGG`, `TRIM`, `CONCAT_WS`, `GREATEST/LEAST`
- Prefer EF entities over raw SQL. Raw SQL only for non-EF tables via `GetConnectionString()`. Never mix raw SQL + EF entities (causes auth failures).
- **Read-only queries**: Always `.AsNoTracking()` | `.Include()` before `.Select()` is unnecessary — EF resolves navigations in projections
- **Correlated subqueries**: Avoid `.Any()` on large tables inside `.Where()`/`.CountAsync()` — pre-load ID sets then use `.Contains()`, or replace with `.Join()`
- **`.Contains()` with large collections (10+)**: Wrap with `EF.Parameter()` for `OPENJSON` translation: `.Where(x => EF.Parameter(largeList).Contains(x.Id))`. Small collections (<10) are fine without it.
- **Thread safety**: DbContext not thread-safe — no parallel EF queries

## API & Cross-Environment

- **Routes**: Absolute `/api/{area}/{controller}` + `ApiController` base. Never `[Area]` on APIs (causes 403)
- **Frontend API calls**: Service layer + `useFetch()`, never raw `fetch()` (must unwrap `{ result, success }`)
- **API URL**: `${import.meta.env.VITE_API_URL}` — never hardcode `/api/` (TEST uses `/2/` prefix)
- **Subpath PathBase (`/2`)**: TEST/PROD run VIPER 2 under a `/2` PathBase (IIS sub-app), legacy VIPER 1 at `/`; with no base locally, these bugs surface only on TEST/PROD (not in unit tests). Use `~/` for app-root redirects, never bare `/` (escapes to the legacy site). Guards matching root-relative paths (`/api`, `/welcome`) must strip the base off the base-prefixed `ReturnUrl` (`/2/...`) or use `Request.PathBase`. `RedirectToAction`, `@Url.Content("~/")`, and tag-helpers include the base; raw string paths (`Redirect("/x")`, `returnUrl.StartsWith("/api")`) don't.
- **Auth**: `[Permission(Allow = "SVMSecure.{Area}")]`, or finer `"SVMSecure.{Area}.{Permission}"` — authenticate before validating params

## C# Standards

- **Exceptions**: Catch specific types (`DbUpdateException`, `SqlException`, `InvalidOperationException`). Never generic `catch (Exception ex)`.
- **Paths**: `Path.Join()` not `Path.Combine()` | **DateTime**: prefer `DateTimeKind.Local`
- **LINQ**: `.Where()` for filtering (not `if` inside foreach), `.Select()` for mapping (not foreach + Add)
- **Mapperly**: Prefer over manual property mapping. Static partial mapper class per area with `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]`. Use `[MapperIgnoreTarget]` for computed properties, manual wrappers for transforms. Align entity/DTO names — use EF `HasColumnName()` to decouple from DB columns.
- **Scrutor**: Convention-based DI auto-registers `*Service`/`*Validator` from configured namespaces — prefer over manual `AddScoped`. Follow `IFooService`/`FooService` naming. Explicit `AddScoped` before Scrutor takes precedence (`RegistrationStrategy.Skip`).
- **Comments**: Sparingly, why-not-what, complex logic only.
- **Bug fixes**: Verify ALL code paths using affected logic. Check for duplicate/parallel implementations and fix consistently or DRY into shared method.
- **Log injection**: Sanitize user input before logging via `LogSanitizer` (`SanitizeId()`, `SanitizeString()`, `SanitizeYear()`). Skip hard-coded strings, enums, DB values.

## Testing & Git

- **UI**: Test UI changes with Playwright MCP (modals, forms, keyboard nav)
- **API**: Use Playwright MCP to visit endpoints — APIs require browser auth, `curl` fails
- **Git**: NEVER stage files until after code review. Workflow: changes → test → lint → summary → approval → stage
- **Branch & merge flow**: Branch off `main`, named `feature/`|`fix/`|etc. plus the JIRA ticket if applicable (e.g. `feature/VPR-104-clinical-scheduler`). After code review, merge into `Development` and push, which deploys to TEST. After the PR is approved on TEST, merge into `main`. Every change goes through `Development` first.
- **Never branch off `Development`**: it is a merge/deploy target, never a base. A branch being "behind `Development`" is expected and not a concern (you never sync or rebase from it). Its history is messy by design and never rewritten.
- **Squash during review**: If a branch is still unmerged and worked by a single developer, squash code-review fixes into the relevant existing commit for cleaner history rather than stacking "address review" commits.
- **Plan/smoketest notes**: `PLAN-*.md` and `SMOKETEST-*.md` files at the repo root are local working notes — never stage or commit them. They are intentionally left untracked.
- **Commit messages**: Conventional Commits `type(scope): subject` (`feat`|`fix`|`refactor`|`docs`|`test`|`chore`; prefer `feat` for new behavior), ticket ID from branch as prefix (e.g. `VPR-104 fix(a11y): ...`). Subject: imperative, max 72 chars, no trailing period, intent not implementation. Body only when the subject is insufficient: `-` bullets that each earn their place (skip plumbing/helpers/test scaffolding), wrapped at 72.
