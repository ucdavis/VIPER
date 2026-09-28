# CLAUDE.md

When I ask a question ("why does X...", "is Y right?") or make an observation ("X looks off"), answer it and stop. Edit code only when I ask for a change ("fix", "change", "add", "do it").

## Environment & Commands

- Run via npm scripts, never direct `dotnet` commands (avoids lock-file conflicts): Dev `npm run dev` | Test `npm run test` (`test:backend -- <TestClassName>`, `test:frontend -- <file-pattern>`) | Lint `npm run lint <path>` | Build `npm run verify:build`
- **Stale cache**: Add `-- --clear-cache` to `verify:build` or `lint` if builds fail with cached errors

## Architecture

- **DB**: SQL Server 2016 + EF Core (CTS, RAPS, AAUD schemas) | **Auth**: CAS + `[Permission]`
- **Identity:** `AaudUser.AaudUserId` = `Person.PersonId`. If mismatched, TEST DB needs refresh.
- **UI**: Read [DESIGN.md](DESIGN.md) before building or changing UI. Always use Quasar components; prefer VueUse composables over hand-rolled reactive logic.
- **O(n²) lookups**: Pre-build a `Map`/`Set`/`Dictionary` instead of nesting `.find()`/`.FirstOrDefault()` in a loop over a growable list.
- **Plurals**: `inflect("word", count)` from `inflection`, never hand-rolled ternaries

## Database & EF Core

- **SQL Server 2016**: no `STRING_AGG`, `TRIM`, `CONCAT_WS`, `GREATEST/LEAST`
- Prefer EF entities over raw SQL. Raw SQL only for non-EF tables via `GetConnectionString()`. Never mix raw SQL + EF entities (causes auth failures).
- **Read-only queries**: Always `.AsNoTracking()` | No `.Include()` before `.Select()`, projections resolve navigations
- **Correlated subqueries**: Avoid `.Any()` on large tables inside `.Where()`/`.CountAsync()`: pre-load ID sets then `.Contains()`, or `.Join()`
- **`.Contains()` with 10+ items**: `.Where(x => EF.Parameter(list).Contains(x.Id))` for `OPENJSON` translation
- **Thread safety**: DbContext not thread-safe, no parallel EF queries

## API & Cross-Environment

- **Routes**: Absolute `/api/{area}/{controller}` + `ApiController` base
- **Frontend API calls**: Service layer + `useFetch()`, never raw `fetch()` (must unwrap `{ result, success }`)
- **API URL**: `${import.meta.env.VITE_API_URL}`, never hardcode `/api/`
- **Subpath PathBase (`/2`)**: TEST/PROD run VIPER 2 under a `/2` PathBase, legacy VIPER 1 at `/`; locally there is no base, so these bugs only surface on TEST/PROD. Use `~/` for app-root redirects, never bare `/`. Guards matching root-relative paths (`/api`, `/welcome`) must strip the base off `ReturnUrl` (`/2/...`) or use `Request.PathBase`. `RedirectToAction`, `@Url.Content("~/")`, and tag-helpers include the base; raw strings (`Redirect("/x")`, `returnUrl.StartsWith("/api")`) don't.
- **Auth**: `[Permission(Allow = "SVMSecure.{Area}")]` or `"SVMSecure.{Area}.{Permission}"`. Authenticate before validating params

## C# Standards

- **Exceptions**: Catch specific types (`DbUpdateException`, `SqlException`, `InvalidOperationException`), never `catch (Exception ex)`
- **Paths**: `Path.Join()` not `Path.Combine()` (`Combine` silently discards everything before a rooted segment) | **DateTime**: prefer `DateTimeKind.Local`
- **Mapperly** over manual property mapping, one static partial mapper per area (follow existing ones). Align entity/DTO names via EF `HasColumnName()`.
- **DI**: Scrutor auto-registers `*Service`/`*Validator` (`IFooService`/`FooService`); add explicit `AddScoped` only to override.
- **`required` on bound models**: never on a server-generated primary key (a create body has no id, so System.Text.Json 400s it). Use `int?`.
- **Bug fixes**: Check for duplicate/parallel implementations of the affected logic and fix consistently, or DRY into a shared method.
- **Log injection**: Sanitize user input via `LogSanitizer` before logging. Skip hard-coded strings, enums, DB values.

## Testing & Git

- **No VMACS production calls**: Unit tests, local environments, and test environments must NEVER send requests to the VMACS production server. All external HTTP requests (e.g., VMACS, Instinct, IAM API) must be mocked using in-memory databases and mock HTTP clients/factories to prevent production spam.
- **Playwright MCP**: Test UI changes (modals, forms, keyboard nav) and hit API endpoints with it; APIs need browser auth, `curl` fails
- **Frontend mocks**: Vitest `clearMocks` is on, so `vi.clearAllMocks()` in `beforeEach` is redundant
- **Frontend test realm**: `vmThreads` pool means values built outside the test realm (`FormData.getAll()`, component props) have a foreign `Array` prototype and `toStrictEqual` fails with "no visual difference". Spread first: `expect([...fd.getAll("x")]).toStrictEqual([...])`.
- **Branch & merge flow**: Branch off `main` as `feature/`|`fix/`|etc. plus JIRA ticket (e.g. `feature/VPR-104-clinical-scheduler`). After code review, merge into `Development` and push (deploys to TEST); after approval on TEST, merge the PR into `main`. `Development` is a messy merge target only: never branch off, rebase from, or rewrite it, and being behind it is fine.
- **Squash during review**: On an unmerged single-developer branch, squash review fixes into the relevant commit instead of stacking "address review" commits.
- **Plan/smoketest notes**: `PLAN-*.md` and `SMOKETEST-*.md` at the repo root are gitignored local notes.
- **Commit messages**: Conventional Commits `type(scope): subject` (`feat`|`fix`|`refactor`|`docs`|`test`|`chore`; prefer `feat` for new behavior), branch ticket ID as prefix (e.g. `VPR-104 fix(a11y): ...`). Subject: imperative, max 72 chars, no trailing period, intent not implementation. Body only when needed: `-` bullets that each earn their place (skip plumbing/helpers/test scaffolding), wrapped at 72.
- **Line Endings (CRLF)**: Windows checkout and Husky validation strictly require CRLF (`\r\n`) line endings. Always ensure newly created or modified files are saved with CRLF line endings. Running tools/scripts that default to LF (`\n`) will cause pre-commit linter failures.
- **ReSharper Inspection & EF Core Scaffoldings**:
  - Keep scaffolded `partial void OnModelCreatingPartial(...)` method signatures in DB context files. Deleting them violates compiler rule `CS0759` (no defining declaration).
  - To silence ReSharper warnings on single-part partial types/methods, add global rules `/Default/CodeInspection/Highlighting/InspectionSeverities/=<RuleId>/@EntryIndexedValue=DO_NOT_SHOW` inside `Viper.sln.DotSettings` instead of deleting scaffolded code.
  - Avoid redundant null-coalescing operations (`?? ""`) on non-nullable scaffolded entity properties.
- **Staged Conflict Markers (Error CS8300)**: During branch merges, do not stage conflict markers in the git index. Husky builds staged files on commit, and any residual `<<<<<<<` markers will trigger compilation error `CS8300`. Clean up all conflict zones before running `git add`.
