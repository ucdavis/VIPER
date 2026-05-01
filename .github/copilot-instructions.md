# Copilot Instructions

Code-review rules for VIPER (ASP.NET 10 + Vue 3 + Quasar). Flag violations
of the following.

## C\#

- **Exceptions**: prefer specific types (`DbUpdateException`, `SqlException`,
  `InvalidOperationException`). Don't catch `Exception` unless it's a top-level
  boundary (middleware, controller action, background-job entry point) or you
  rethrow/wrap after logging — flag broad catches inside service/business
  logic.
- **DateTime**: default to `DateTime.Now` / `DateTimeKind.Local` (DB stores
  local). `UtcNow` only where required by spec (HTTP date headers,
  Unix-epoch math, protocol timestamps).
- **Paths**: prefer `Path.Join()` over `Path.Combine()` for new code.
- **LINQ**: `.Where()` for filtering (not `if` inside `foreach`), `.Select()`
  for mapping (not `foreach + Add`).
- **Mapping**: prefer Mapperly. Static partial mapper per area with
  `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]`. Avoid
  hand-written property-by-property mapping.
- **DI**: prefer Scrutor convention registration over manual `AddScoped`.
  Follow `IFooService` / `FooService` naming.
- **Comments**: explain *why*, not *what*. Sparingly, complex logic only.

## EF Core / Database

- **SQL Server 2016** target — reject `STRING_AGG`, `TRIM`, `CONCAT_WS`,
  `GREATEST`, `LEAST`.
- **Read-only queries** must use `.AsNoTracking()`.
- `.Include()` before `.Select()` is unnecessary — EF resolves navigations in
  projections.
- Avoid correlated subqueries: `.Any()` inside `.Where()` / `.CountAsync()` on
  large tables. Pre-load ID sets and use `.Contains()`, or replace with
  `.Join()`.
- `.Contains()` with 10+ items: wrap with `EF.Parameter(...)` for `OPENJSON`
  translation.
- Never mix raw SQL with EF entities (causes auth failures). Raw SQL only for
  non-EF tables via `GetConnectionString()`.
- `DbContext` is not thread-safe — flag parallel EF queries.

## API & Auth

- Routes: absolute `/api/{area}/{controller}` with `ApiController` base.
  `[Area("...")]` is allowed alongside the route attribute (existing API
  controllers use it); flag only when an API route is missing the absolute
  `/api/...` prefix or the `ApiController` attribute, not for the presence of
  `[Area]`.
- Authorization: `[Permission(Allow = "SVMSecure.{Area}.{Action}")]`.
  Authenticate before validating params.

## Security

- **Log injection**: sanitize user input before logging via `LogSanitizer`
  (`SanitizeId()`, `SanitizeString()`, `SanitizeYear()`). Skip for hard-coded
  strings, enums, DB-sourced values.

## Vue / Quasar

- Use Quasar components (`q-btn`, `q-dialog`, `q-banner`, etc.). Selects need
  `dense` + `options-dense`.
- **Dialogs (`q-dialog`)**: must have an accessible name (`aria-labelledby`
  pointing to the title `id`, or `aria-label`) **and** an accessible close
  affordance. Persistent dialogs use `@click="handleClose"`, not
  `v-close-popup`.
- **Badges**: use the `StatusBadge` component (not raw `q-badge` with manual
  `getAccessibleTextColor`).
- **Banners (Vue SPAs)**: use `StatusBanner`. `type="error"` auto-maps to
  `role="alert"`; warning/info default to `role="status"`. Use
  `live="assertive"` only for direct user-action responses, not persistent
  state indicators. Razor pages use `q-banner` with accessible classes
  (`bg-warning text-dark`, `role="status"` or `role="alert"`).
- **Button colors**: `primary` (Aggie Blue), `positive` (create), `negative`
  (delete), `info text-color="dark"` (tertiary), `warning text-color="dark"`
  (caution), `secondary`.
- **Pluralization**: `inflect("word", count)` from the `inflection` package.
  Reject hand-rolled ternaries.
- Prefer **VueUse** composables over custom reactive helpers.

## Accessibility

- `<main>` landmark is provided by layouts (`_VIPERLayout.cshtml`,
  `VIPERLayoutSimplified.cshtml`, `ViperLayout.vue`, `ViperLayoutSimple.vue`).
  Do not add `<main>` to `App.vue` or page components.
- Interactive non-button elements need `@keyup.enter` + `@keyup.space` +
  `tabindex="0"` + `role="button"` + `aria-label`. Prefer `q-btn`.

## Frontend API & CSS

- API calls go through the service layer with `useFetch()` (unwraps
  `{ result, success }`). Never raw `fetch()`.
- Use `${import.meta.env.VITE_API_URL}`. Never hardcode `/api/` (TEST uses
  `/2/` prefix).
- No inline styles, no `!important`. Prefer Quasar utility classes. Use `rem`
  (not `px`) for spacing/sizing/typography.
