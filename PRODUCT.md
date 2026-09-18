# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

The UC Davis Weill School of Veterinary Medicine community: faculty, clinicians, staff, and students. They authenticate through campus single sign-on (CAS, with Entra ID replacing it) and use VIPER as the internal hub for the school's operational systems: role and permission administration (RAPS), effort reporting, clinical scheduling, competency tracking (CTS), the directory, and CMS-managed content.

These are mandatory, repeat users, not visitors. They arrive mid-workflow, often under time pressure, to get a specific administrative, academic, or clinical task done. The app is a tool, not a destination. Technical comfort ranges widely, from daily power users to occasional ones, and the same screens are used across desktop and mobile.

## Product Purpose

VIPER (2.0) is the School of Veterinary Medicine's internal web application suite: a single authenticated home, behind the UC Davis brand, for the operational systems the school runs on. It consolidates permission management, effort reporting, clinical scheduling, competency tracking, directory, and content into one consistent shell so staff and faculty don't juggle disconnected tools.

Success looks like: tasks completed quickly and correctly with minimal training, consistent behavior across every area, and full accessibility for every user, on any device. The product earns trust by being reliable and clear, not by being novel.

## Positioning

VIPER 2 is the incremental replacement for the legacy VIPER 1 site. The two run in parallel indefinitely: VIPER 2 under a `/2` PathBase, VIPER 1 at the root, with each area moving over when it is ready. That migration posture is the durable fact, not a phase to be finished and forgotten.

What no neighboring product could truthfully copy is the school's own operational data and identity graph. VIPER sits directly on the SVM and campus systems of record (RAPS roles, AAUD identity, effort, rotations, competencies) rather than modeling a generic institution. A campus-wide system does not know a fourth-year rotation or a clinician's effort split; an off-the-shelf admin tool does not know UC Davis CAS or RAPS. VIPER's position is the shell that speaks both, so a user crosses between permission administration, effort, and scheduling without changing tools or mental models.

Because migration is incremental, every new surface has to be legible next to a VIPER 1 page a user may hit in the same session, and a user's sense of "the school's system" spans both. Continuity of behavior across the boundary is part of the product, not a transitional courtesy.

## Operating Context

- **Access is never anonymous.** Every working surface is behind campus single sign-on, gated by RAPS roles through `[Permission(Allow = "SVMSecure.{Area}")]`. The only unauthenticated surface in the product is the login and welcome page.
- **Two sign-on providers, mid-migration.** Campus is retiring CAS in favor of Microsoft Entra ID. Both are supported and both sign in to the same cookie, so a session is identical downstream whichever the user picked. `Authentication:EnabledProviders` (`Cas`, `EntraId`, or `Both`) decides what the welcome page offers; TEST runs both, PROD stays CAS until Entra is proven, and the cutover is that one config value.
- **Two sites, one perceived system.** TEST and PROD run VIPER 2 as an IIS sub-application under `/2`, beside legacy VIPER 1 at `/`. Local development has no base path, so subpath bugs surface only on TEST and PROD.
- **Areas in the suite today.** Backend areas: CMS, CTS, ClinicalScheduler, Computing, Curriculum, Directory, Effort, RAPS, Scheduler, Students. Vue SPAs: CAHFS, CMS, CTS, ClinicalScheduler, Computing, Effort, Students.
- **Release path.** Feature branch off `main`, merged to `Development` to deploy to TEST, then to `main` after approval on TEST. Jenkins runs the deploys.
- **Usage scene.** Mid-task, time-pressured, often with a specific record in hand (a person, a rotation, a reporting period). Screens are used on desktop and on mobile from 390px up, including in clinical settings.

## Capabilities and Constraints

- **Stack.** ASP.NET 10 backend organized per area under `web/Areas/{AreaName}/`, with a Vue 3 multi-SPA frontend built by Vite into `wwwroot/vue/`. Quasar is the component vocabulary; new screens compose it rather than introducing bespoke markup.
- **Data.** SQL Server 2016 with EF Core, spanning the CTS, RAPS, and AAUD schemas plus `effort` and `users`. The 2016 target rules out `STRING_AGG`, `TRIM`, `CONCAT_WS`, and `GREATEST`/`LEAST`.
- **Identity.** `AaudUser.AaudUserId` equals `Person.PersonId`. Records are de-duplicated by MothraId.
- **Terminology future work must preserve.** RAPS (roles and permissions), CTS (competency tracking), AAUD (campus identity), effort (reporting), rotation (clinical scheduling), MothraId, area.
- **Open and undecided.** Which remaining VIPER 1 areas migrate next, and in what order, is not recorded here.

## Brand Personality

Institutional, trustworthy, and efficient: the voice of a public university veterinary school. Three words: **authoritative, clear, accessible.** It should feel unmistakably like UC Davis, calm and credible, with warmth coming from the school's mission rather than from decorative UI.

The copy voice is plain, direct, and respectful of the user's time: say what a control does and what will happen. Never reach for marketing buzzwords (streamline, empower, supercharge, leverage, seamless, world-class, enterprise-grade, next-generation, game-changer); use specific nouns and verbs for what the product literally does.

The visual expression of all this, including what VIPER must not look like, is specified in [DESIGN.md](DESIGN.md).

## Evidence on Hand

Real assets that design work may rely on:

- **Proxima Nova**, the UC Davis campus typeface, at regular, medium, bold, and extrabold, loaded from the campus font server at `campusfont.ucdavis.edu`. It may not be self-hosted, so no copies live in this repo; the `@font-face` blocks in `web/wwwroot/css/site.css`, `welcome.css`, and `VueApp/src/styles/base.css` are the only references.
- **Self-hosted Roboto and Material Icons**: `VueApp/src/assets/fonts/roboto-v51-latin.woff2`, `roboto-v51-latin-ext.woff2`, and `material-icons.woff2`, built to `web/wwwroot/vue/assets/`.
- **Five login hero photographs** in AVIF and JPG at `web/wwwroot/images/login/`: guinea pig, horse and foal, ophthalmology, the SVM building, and vetmed admin.
- **Brand marks**: the rod of asclepius (`rod-of-asclepius-white.avif` and `.png`), the `_ViperBrand.cshtml` lockup partial, `web/wwwroot/images/UCDSVMLogo.png`, and `nopic.jpg` as the person-photo placeholder.
- **The welcome splash**: `web/Views/Home/Welcome.cshtml`, `web/wwwroot/css/welcome.css`, and `WelcomePageHelper.cs` with tests.

Absences that future work must not paper over:

- **No marketing evidence exists.** There are no testimonials, customer logos, adoption metrics, benchmarks, or press. VIPER is mandatory internal software; never fabricate social proof for it. Real user, permission, and effort data lives only in the TEST and PROD databases.

## Product Principles

1. **The tool gets out of the way.** Density, speed, and legibility beat decoration; every element earns its place. The brand greets users at the door; the workspace stays quiet.
2. **Accessibility is a floor, not a feature.** WCAG AA on every surface, for an audience that has no choice but to use this software.
3. **Brand discipline over novelty.** Carry the UC Davis identity faithfully and consistently. The school's credibility is the design's job.
4. **Reuse over reinvention.** One component vocabulary, one token source, shared patterns. New screens compose existing parts rather than introducing one-offs.
5. **Correctness earns trust.** This is institutional software handling permissions, effort, and schedules. Clarity and predictability outrank cleverness; never surprise the user with what an action does.

## Accessibility & Inclusion

WCAG 2.1 AA is the system-wide standard, enforced rather than aspired to, because the audience is mandatory daily users across a full range of needs and abilities. Layouts work from mobile (390px) up, reduced-motion preferences are respected, and fonts are self-hosted for reliable rendering.

The component-level contract that delivers this (contrast ratios, landmarks, keyboard semantics, dialog naming, live-region politeness) is specified in [DESIGN.md](DESIGN.md).
