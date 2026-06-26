---
name: VIPER
description: Internal web application suite for the UC Davis Weill School of Veterinary Medicine.
colors:
  aggie-blue: "#022851"
  blue-hover: "#033266"
  blue-secondary: "#4b6983"
  aggie-gold: "#ffbf00"
  gold-accent: "#ffc519"
  gold-nav: "#ffd24c"
  redwood: "#266041"
  merlot: "#79242f"
  tahoe: "#00b2e3"
  poppy: "#f18a00"
  arboretum: "#00c4b3"
  cabernet: "#481268"
  ink: "#1d1d1d"
  body-grey: "#666666"
  surface: "#ffffff"
  table-header: "#eeeeee"
  blue-10: "#cdd6e0"
typography:
  display:
    fontFamily: "Proxima Nova, -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Arial, sans-serif"
    fontSize: "clamp(2.75rem, 5.5vw, 4.25rem)"
    fontWeight: 800
    lineHeight: 1
    letterSpacing: "-0.02em"
  chrome:
    fontFamily: "Proxima Nova, -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Arial, sans-serif"
    fontWeight: 500
    letterSpacing: "normal"
  heading:
    fontFamily: "Roboto, -apple-system, Helvetica Neue, Helvetica, Arial, sans-serif"
    fontSize: "1.2rem"
    fontWeight: 700
    lineHeight: 1.2
  body:
    fontFamily: "Roboto, -apple-system, Helvetica Neue, Helvetica, Arial, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Roboto, -apple-system, Helvetica Neue, Helvetica, Arial, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 500
    letterSpacing: "0.04em"
rounded:
  none: "0"
  sm: "0.25rem"
  default: "4px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "24px"
  xl: "48px"
components:
  button-primary:
    backgroundColor: "{colors.aggie-blue}"
    textColor: "{colors.surface}"
    rounded: "{rounded.default}"
  button-positive:
    backgroundColor: "{colors.redwood}"
    textColor: "{colors.surface}"
    rounded: "{rounded.default}"
  button-negative:
    backgroundColor: "{colors.merlot}"
    textColor: "{colors.surface}"
    rounded: "{rounded.default}"
  button-info:
    backgroundColor: "{colors.tahoe}"
    textColor: "{colors.ink}"
    rounded: "{rounded.default}"
  button-warning:
    backgroundColor: "{colors.gold-accent}"
    textColor: "{colors.ink}"
    rounded: "{rounded.default}"
  button-secondary:
    backgroundColor: "{colors.blue-secondary}"
    textColor: "{colors.surface}"
    rounded: "{rounded.default}"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.default}"
    padding: "16px"
  welcome-cta:
    backgroundColor: "{colors.aggie-blue}"
    textColor: "{colors.surface}"
    rounded: "{rounded.none}"
    padding: "16px 20px"
  welcome-cta-hover:
    backgroundColor: "{colors.blue-hover}"
    textColor: "{colors.surface}"
---

# Design System: VIPER

## 1. Overview

**Creative North Star: "The Teaching Hospital"**

VIPER wears two faces, and the system is the building that holds both. The public face is the lobby: a full-bleed photographic welcome where the work of the school (an eye exam, a foal, the SVM building under California sky) is the hero, framed in Aggie Blue with a thin gold rule, calm and institutional. The working interior is the clinic floor: dense, fast, legible, every pixel earning its place because the people here are mid-task and the screen is a tool, not a destination. The brand greets you at the door; the product gets out of your way once you are inside.

This is institutional software for a public university veterinary school, not a consumer SaaS product, and it refuses the tells of one. No hero-metric template, no gradient text, no glassmorphism, no identical icon-card grids, no marketing-deck buzzwords. The color discipline is the UC Davis brand discipline: Aggie Blue carries the surface, gold is a rare accent that never dominates, and everything is held to WCAG AA because the audience includes clinicians, staff, faculty, and students using this daily under real time pressure. Built on Quasar, the visual vocabulary is deliberately restrained: flat surfaces, compressed headings, tight rem-based spacing, and components reused rather than reinvented.

The system is bilingual by design. Brand chrome (the blue header bar, the unauthenticated welcome splash) speaks in **Proxima Nova**, the UC Davis campus typeface. The application workspace speaks in **Roboto**, narrower and quieter so dense tables and forms stay readable. The two never mix on the same surface.

**Key Characteristics:**
- Aggie Blue surface, gold as a disciplined accent (never dominant)
- Two typefaces with one job each: Proxima Nova for brand chrome, Roboto for the workspace
- Flat by default; elevation is reserved, not decorative
- WCAG AA is a floor, not a goal: greys, gold, and tab states are all contrast-corrected
- Dense, compressed, functional product UI; warm, photographic brand surfaces
- rem everywhere, no inline styles, no `!important` outside documented contrast overrides

## 2. Colors: The Aggie Palette

A UC Davis institutional palette: Aggie Blue is the foundation, Aggie Gold is the signature accent, and a small secondary set adds status and emphasis. All values are canonical UC Davis brand hex; the blue, gold, and black scales each run a full 10–100 ramp in `VueApp/src/styles/colors.css`.

### Primary

- **Aggie Blue** (#022851): The load-bearing brand color. The top header chrome, primary buttons, the welcome splash background, active nav text, link color, and skip-to-content chip. This is the surface that says "UC Davis." Quasar `primary`. The full blue ramp (`--ucdavis-blue-10` #cdd6e0 → `--ucdavis-blue-100` #022851) provides hover (`blue-90` #033266), active-row tints (`blue-10`), and tree/arrow strokes.
- **Aggie Gold** (#ffbf00, accent shade #ffc519): The signature accent. The 3px rule under the header bar, the gold brand-mark tile behind the rod-of-asclepius, the welcome card's top border, the CTA arrow, and focus rings on dark surfaces. Quasar `accent` and `warning` both map to `gold-90` (#ffc519). The gold section-nav bar uses lighter ramp steps (`gold-70` #ffd24c band, `gold-40` #ffe599 selected).

### Secondary

- **Blue 70** (#4b6983): Quasar `secondary`. Secondary buttons and muted blue chrome. (Note: the CSS ramp token `--ucdavis-blue-70` is `#355b85`; the Quasar semantic `secondary` is `#4b6983`. Treat the Quasar value as canonical for `color="secondary"`.)
- **Tahoe** (#00b2e3): Quasar `info`. Informational/tertiary actions. Light enough that it always pairs with `text-color="dark"`.

### Tertiary

- **Redwood** (#266041): Quasar `positive`. Success and create actions.
- **Merlot** (#79242f): Quasar `negative`. Danger and delete actions.
- **Poppy** (#f18a00): Tips and highlights only. A warm orange used sparingly for callouts.
- **Arboretum** (#00c4b3) / **Cabernet** (#481268): Secondary-palette accents, never dominant. Reserve for charts, tags, and categorical distinction.

### Neutral

- **Ink** (#1d1d1d): Quasar `dark`; default high-contrast text. `dark-page` is #121212.
- **Body Grey** (#666666): `--ucdavis-black-60`, the AA-safe muted text color. Quasar's default `.text-grey`/`.bg-grey` are remapped to this so muted text still clears 4.5:1.
- **Surface** (#ffffff): Card and panel background.
- **Table Header** (#eeeeee): Sticky `q-table` header fill.

### Named Rules
**The Gold-Is-Accent Rule.** Gold is never a dominant surface and never body text. Per the UC Davis secondary-palette guidance, accents are "never dominant." Gold appears as a thin rule, a focus ring, a brand-mark tile, or a single arrow — measured in pixels of width, not percent of screen. Aggie Blue carries weight; gold punctuates it.

**The Bright-Gold-Never-On-White Rule.** `#ffbf00`/`#ffc519` on white fails AA at text sizes. For gold *text* on light backgrounds, use the darkened `--text-warning` (#664d03). Bright gold is a background and accent color only.

**The AA-Floor Rule.** Every foreground/background pair clears WCAG AA (4.5:1 body, 3:1 large/UI). Quasar greys (`grey`, `grey-5`, `grey-6`) are remapped to `grey-7`; inactive tabs are de-faded (`tabs-no-fade`); input clear-button and tree-arrow opacities are bumped. Contrast correction is a system-level commitment, not a per-screen afterthought.

## 3. Typography

**Display / Brand Font:** Proxima Nova (with Roboto, then system-sans fallback). Self-hosted woff2 at weights 400/500/700/800; weight 900 downshifts to 800 (the family caps there).
**Body / Workspace Font:** Roboto (variable woff2, weights 100–900, with system-sans fallback).
**Icon Font:** Material Icons (self-hosted woff2).
**Print-Only:** Ryman Eco is the UC Davis print display face; it is **not** loaded on the web. Arial / Aptos are the brand-approved fallbacks when Proxima is unavailable.

**Character:** Proxima Nova is the warm, confident geometric campus voice; Roboto is the neutral, space-efficient workhorse for data-dense screens. The contrast axis between them is *role*, not style: brand vs. work. Pairing them keeps the institutional identity at the edges while letting the interior breathe.

### Hierarchy

- **Display** (Proxima Nova 800, `clamp(2.75rem, 5.5vw, 4.25rem)`, line-height 1, letter-spacing −0.02em): The welcome-splash headline only. The single place display-scale type appears.
- **Chrome / Brand Title** (Proxima Nova 500, ~1.6em): The blue top header bar and brand lockup text.
- **Heading** (Roboto 700, 1.2rem, line-height 1.2): In-app page and dialog headings (`h1`–`h3` inside `q-page-container`/`q-dialog` are deliberately compressed to 1.2rem). `h4` is 1.1rem, `h5` 1rem bold, `h6` 1rem regular.
- **Body** (Roboto 400, base 14px → 16px ≥768px): All application copy. Root font-size scales up at the 768px breakpoint, so rem-based sizing grows with it. Cap prose at 65–75ch.
- **Label** (Roboto 500, 0.75rem, letter-spacing 0.04em): Left-nav subheaders, footer text, breadcrumbs, skip-to-content. Short labels only.

### Named Rules
**The Two-Font Rule.** Proxima Nova belongs to brand chrome (the blue header bar, the welcome splash). Roboto belongs to the workspace (everything inside `q-page-container` and the gold section nav, which is intentionally Roboto so its many items fit). Never set workspace body copy in Proxima; never set brand chrome in Roboto.

**The Compressed-Heading Rule.** Inside the app, headings are 1–1.2rem and bold, not display-scale. Hierarchy in the workspace comes from weight and spacing, not size. Display-scale type (>2.75rem) is reserved for brand surfaces. An in-app `h1` that looks like a marketing hero is wrong.

## 4. Elevation

The system is flat by default. Surfaces sit on the page with borders and tonal fills rather than drop shadows; depth is the exception, earned by state or by the one dramatic brand moment. Per CLAUDE.md, "cards are the lazy answer" — containers are used only when they are the right affordance, and never nested.

### Shadow Vocabulary

- **Focus ring** (`box-shadow: 0 0 0 0.1rem white, 0 0 0 0.25rem #258cfb`): The keyboard-focus halo on form controls and buttons. On the dark welcome splash this becomes a gold ring (`0 0 0 0.1875rem rgba(255,197,25,0.85)`) since default outlines vanish on the photo.
- **Splash card** (`box-shadow: 0 1.875rem 5rem rgba(0,0,0,0.45)`): The single heavy elevation in the system — the white sign-in card lifting off the full-bleed hero photo. Dramatic on purpose, and used exactly once.
- **Sticky table header** (`background-color: #eee`): Tonal layering, not shadow — the sticky `q-table` header reads as elevated through fill and stickiness alone.

### Named Rules
**The Flat-By-Default Rule.** Workspace surfaces are flat at rest. Shadow appears as a response to focus, or as the single deliberate lift of the welcome card over its hero photo. If a routine panel or card has a drop shadow "for depth," remove it.

## 5. Components

VIPER is built on Quasar; the prescription is **always use Quasar components**, styled with brand tokens, not bespoke markup. The pieces below are the ones that carry the system's character.

### Buttons

- **Shape:** Quasar default radius (~4px); the brand-splash CTA is square (0 radius) with a gold top accent.
- **Color roles (brand-mapped):** Primary action → `primary` (Aggie Blue); Success/Create → `positive` (Redwood); Danger/Delete → `negative` (Merlot); Info/Tertiary → `info text-color="dark"` (Tahoe); Warning/Caution → `warning text-color="dark"` (Gold); Secondary → `secondary` (Blue 70).
- **Loading state:** A `q-btn` with a text label plus `:loading` needs a `#loading` slot (`<q-spinner size="1em" class="q-mr-sm" />` + the label text). Icon-only buttons use the default spinner.
- **Interactive non-buttons:** Anything clickable that isn't a `q-btn` needs `@keyup.enter` + `@keyup.space` + `tabindex="0"` + `role="button"` + `aria-label`.

### Badges

- **Use `StatusBadge`** (wraps `q-badge`, auto-pairs `text-color` with `color` for contrast). Pass the label via the `label` prop or default slot. Avoid raw `q-badge` with manual `getAccessibleTextColor`.

### Banners

- **Use `StatusBanner`** in Vue SPAs (`type="success|error|warning|info"`). Only `type="error"` is assertive (`role="alert"`) by default; everything else is polite (`role="status"`). Override with the `live` prop only when needed: `live="assertive"` for a warning/info banner shown in direct response to a user action (post-submit validation, time-sensitive notice), `live="off"` for a purely decorative banner with no dynamic content (drops the role entirely). Don't reach for `type="warning"` to force an assertive announcement on a persistent state indicator — that's what `live="assertive"` is for, and most page-load warnings should stay polite. Razor pages use `q-banner` with accessible classes (`bg-warning text-dark`, `role="status"`/`"alert"`). Error surfaces outside `StatusBanner` use the `.error-surface` treatment (12% Merlot tint, Merlot left accent, Merlot text).

### Cards / Containers

- **Corner Style:** Quasar default (~4px); brand splash card is square with a 3px gold top border.
- **Background:** White surface on the workspace; the welcome card is white over the Aggie Blue hero.
- **Shadow Strategy:** Flat in the workspace (see Elevation). Never nest cards.
- **Internal Padding:** rem-based Quasar spacing (`md` 16px typical).

### Inputs / Fields

- **Style:** Quasar `q-field` / `q-input` / `q-select`. Selects are **always `dense` + `options-dense`**.
- **Focus:** The white+blue focus ring (see Elevation). Clear-button opacity is forced to 1 for contrast.

### Dialogs

- **Every `q-dialog`** needs (1) an accessible name via `aria-labelledby` → the title `id` (or `aria-label` if there's no visible title), and (2) a visible close affordance. Persistent SPA dialogs use `@click="handleClose"` instead of `v-close-popup`. Error banners go in a separate `q-card-section` below the header. **Footer actions:** confirmation and simple action dialogs use Submit + Delete only, with no Cancel (the X and Escape dismiss). **Data-entry form dialogs may add a footer Cancel** beside the primary action: once a user has been filling in fields, an explicit Cancel reads more clearly than relying on the X to discard. When present, Cancel, the X, and Escape all route through the same `handleClose` (carrying the unsaved-changes guard on persistent dialogs). Canonical header pattern:

  ```html
  <q-dialog aria-labelledby="my-dialog-title" ...>
      <q-card>
          <q-card-section class="row items-center q-pb-none">
              <div id="my-dialog-title" class="text-h6">Dialog Title</div>
              <q-space />
              <q-btn icon="close" flat round dense aria-label="Close dialog" v-close-popup />
          </q-card-section>
  ```

### Navigation

- **Top chrome:** Aggie Blue bar (`#mainLayoutHeader`, min-height 86px ≥768px) in Proxima Nova 500, carrying the `.viper-brand` lockup — a gold tile (`--ucdavis-gold-100`) holding the white rod-of-asclepius mark, beside the school-name lockup image (lockup name hidden below 1024px).
- **Section nav:** A gold band (`gold-70` #ffd24c) in Roboto 400; the selected item gets `gold-40` (#ffe599).
- **Left drawer:** `#leftNavMenu` with bold section headers, 500-weight subheaders, indent tiers; active router links tint to `blue-10` with `primary` text at weight 500. The mobile overlay-drawer glow is suppressed.

### Welcome Splash (signature component)
The unauthenticated landing (`web/Views/Home/Welcome.cshtml`, `Layout = null`): full-bleed hero photo (one of five, randomized per load, served `image-set` AVIF→JPG) under an Aggie-Blue gradient, with an editorial column (gold rule + Proxima 800 headline + tagline) and a white sign-in card carrying a single CAS "Sign in →" CTA. The one place in the system that goes drenched, photographic, and display-scale. A standalone stylesheet (`welcome.css`) redeclares the brand tokens because it cannot load the Vue/Quasar token files.

### Landmarks

- **Exactly one `<main>` per page.** Razor pages get it from `_VIPERLayout.cshtml`; Vue SPAs get it from `ViewerLayout`/`ViperLayout.vue` inside `q-page-container`. Never add `<main>` to an SPA `App.vue` or page component.

## 6. Do's and Don'ts

### Do:

- **Do** carry brand color through Quasar roles: `primary` Aggie Blue, `positive` Redwood, `negative` Merlot, `info`+`text-color="dark"` Tahoe, `warning`+`text-color="dark"` Gold, `secondary` Blue 70.
- **Do** reference tokens, not literals: `var(--q-primary)`, `var(--ucdavis-blue-100)`, `var(--ucdavis-gold-90)`. Route any new color through the canonical token files.
- **Do** use rem for spacing, sizing, and typography — never px.
- **Do** prefer Quasar utility classes over custom CSS, and combine selectors when rules overlap.
- **Do** use Proxima Nova for brand chrome and Roboto for the workspace, and keep them off each other's surfaces.
- **Do** keep gold as a thin accent (rule, ring, mark, arrow). Aggie Blue carries the surface.
- **Do** verify ≥4.5:1 body / ≥3:1 large-and-UI contrast; lean on the remapped greys (`grey-7`) and darkened gold text (#664d03).
- **Do** use `StatusBadge` and `StatusBanner` instead of raw `q-badge`/`q-banner` in SPAs, and set `dense` + `options-dense` on every select.
- **Do** give every dialog an accessible name and a close affordance, with Submit/Delete-only footers.
- **Do** keep exactly one `<main>` landmark per page.
- **Do** use `text-wrap: balance` on display/brand headings.

### Don't:

- **Don't** use inline `style=""`. Extract to a class.
- **Don't** use `!important` except for the documented Quasar contrast overrides (`.text-grey`, `.text-warning`, `tabs-no-fade`).
- **Don't** put bright gold (#ffbf00 / #ffc519) on white as text — it fails AA. Gold is a background and accent only.
- **Don't** let gold become a dominant surface; the secondary palette is "never dominant."
- **Don't** set in-app headings at display scale — workspace `h1`–`h3` are 1.2rem bold. Display type (>2.75rem) is brand-surface only.
- **Don't** add drop shadows to routine cards/panels; the system is flat by default.
- **Don't** nest cards, or reach for a card when a list or plain section is the better affordance.
- **Don't** hardcode hex in component or page CSS — derive from the brand tokens.
- **Don't** render two `<main>` landmarks on one page (one from layout + one from a component).
- **Don't** import consumer-SaaS tells: gradient text, glassmorphism, hero-metric templates, identical icon-card grids, or marketing buzzwords. This is institutional veterinary-school software.
