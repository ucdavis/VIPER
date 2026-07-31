---
name: VIPER
description: Internal web application suite for the UC Davis Weill School of Veterinary Medicine.
colors:
  aggie-blue: "#022851"
  blue-90: "#033266"
  blue-80: "#1d4776"
  blue-70: "#355b85"
  blue-10: "#cdd6e0"
  aggie-gold: "#ffbf00"
  gold-90: "#ffc519"
  gold-70: "#ffd24c"
  gold-40: "#ffe599"
  gold-text: "#664d03"
  redwood: "#266041"
  merlot: "#79242f"
  tahoe: "#00b2e3"
  poppy: "#f18a00"
  arboretum: "#00c4b3"
  cabernet: "#481268"
  ink: "#1d1d1d"
  dark-page: "#121212"
  body-grey: "#666666"
  surface: "#ffffff"
  table-header: "#eeeeee"
  focus-blue: "#258cfb"
  warning-banner-text: "#5d4600"
  splash-card-ink: "#13253f"
  splash-card-muted: "#64748b"
typography:
  display:
    fontFamily: "Proxima Nova, -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Arial, sans-serif"
    fontSize: "clamp(2.75rem, 5.5vw, 4.25rem)"
    fontWeight: 800
    lineHeight: "1"
    letterSpacing: "-0.02em"
  chrome:
    fontFamily: "Proxima Nova, -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Arial, sans-serif"
    fontWeight: 500
  heading:
    fontFamily: "Roboto, -apple-system, Helvetica Neue, Helvetica, Arial, sans-serif"
    fontSize: "1.2rem"
    fontWeight: 700
    lineHeight: "1.2rem"
  body:
    fontFamily: "Roboto, -apple-system, Helvetica Neue, Helvetica, Arial, Apple Color Emoji, Segoe UI Emoji, Noto Color Emoji, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
  label:
    fontFamily: "Roboto, -apple-system, Helvetica Neue, Helvetica, Arial, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 400
rounded:
  none: "0"
  sm: "0.25rem"
  default: "4px"
  pill: "1rem"
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
  button-secondary:
    backgroundColor: "{colors.blue-70}"
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
    backgroundColor: "{colors.gold-90}"
    textColor: "{colors.ink}"
    rounded: "{rounded.default}"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.default}"
    padding: "16px"
  header-bar:
    backgroundColor: "{colors.aggie-blue}"
    textColor: "{colors.surface}"
    typography: "{typography.chrome}"
    height: "86px"
  brand-mark:
    backgroundColor: "{colors.aggie-gold}"
    height: "2.75rem"
    width: "2.75rem"
    rounded: "{rounded.none}"
  section-nav:
    backgroundColor: "{colors.gold-70}"
    textColor: "{colors.aggie-blue}"
    height: "36px"
  section-nav-selected:
    backgroundColor: "{colors.gold-40}"
    textColor: "{colors.aggie-blue}"
  nav-item-active:
    backgroundColor: "{colors.blue-10}"
    textColor: "{colors.aggie-blue}"
  table-header:
    backgroundColor: "{colors.table-header}"
    textColor: "{colors.ink}"
  input-outlined:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.default}"
  banner-success:
    backgroundColor: "color-mix(in srgb, #266041 12%, white)"
    textColor: "{colors.redwood}"
    rounded: "{rounded.default}"
  banner-error:
    backgroundColor: "color-mix(in srgb, #79242f 12%, white)"
    textColor: "{colors.merlot}"
    rounded: "{rounded.default}"
  banner-warning:
    backgroundColor: "color-mix(in srgb, #ffc519 15%, white)"
    textColor: "{colors.warning-banner-text}"
    rounded: "{rounded.default}"
  banner-info:
    backgroundColor: "color-mix(in srgb, #00b2e3 12%, white)"
    textColor: "{colors.blue-80}"
    rounded: "{rounded.default}"
  field-error-chip:
    backgroundColor: "{colors.merlot}"
    textColor: "{colors.surface}"
    typography: "{typography.label}"
    rounded: "{rounded.pill}"
    padding: "0.125rem 0.5rem 0.125rem 0.25rem"
  env-badge:
    backgroundColor: "{colors.merlot}"
    textColor: "{colors.surface}"
    rounded: "{rounded.default}"
  skip-link:
    backgroundColor: "{colors.aggie-blue}"
    textColor: "{colors.surface}"
    rounded: "{rounded.sm}"
    padding: "0.5rem 1rem"
  status-toast:
    backgroundColor: "{colors.redwood}"
    textColor: "{colors.surface}"
    rounded: "{rounded.sm}"
    padding: "0.75rem 1.5rem"
  splash-card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.splash-card-ink}"
    rounded: "{rounded.none}"
    padding: "2.5rem"
---

# Design System: VIPER

## Overview

**Creative North Star: "The Teaching Hospital"**

VIPER wears two faces, and the system is the building that holds both. The public face is the lobby: a full-bleed photographic welcome where the work of the school (an eye exam, a foal, the SVM building under California sky) is the hero, framed in Aggie Blue with a thin gold rule, calm and institutional. The working interior is the clinic floor: dense, fast, legible, every pixel earning its place because the people here are mid-task and the screen is a tool rather than a destination. The brand greets you at the door; the product gets out of your way once you are inside.

That split is the whole system in one sentence, and it governs every choice below. The lobby is the one place VIPER goes drenched, photographic, and display-scale. The moment a user signs in, the vocabulary changes: white surfaces, compressed headings, flat panels, and no decoration that a working screen has to pay for. Two typefaces carry the divide, and they never mix on the same surface.

This is institutional software for a public university veterinary school and it refuses the tells of a consumer SaaS product: no hero-metric template, no gradient text, no glassmorphism, no identical icon-card grids. The color discipline is UC Davis brand discipline, with the full official 10 to 100 ramps for blue, gold, and black living in `VueApp/src/styles/colors.css`. Every foreground and background pair is held to WCAG AA because the audience is clinicians, staff, faculty, and students using this daily under real time pressure. The system also runs twice over, once in Razor (`web/wwwroot/css/site.css`) and once in the Vue SPAs (`VueApp/src/styles/`), and the two are kept deliberately in sync.

**Key Characteristics:**
- Aggie Blue chrome, a thin gold band, white workspace, no third surface
- Two typefaces with one job each: Proxima Nova for brand chrome, Roboto for the workspace
- Display type exists in exactly one place, the welcome splash; in-app headings top out at 1.2rem bold
- Flat by default; shadow is spent on fixed chrome, keyboard focus, transient toasts, and the one card that lifts off a photograph
- WCAG AA is a floor: greys, gold text, tab fade, clear-button opacity, and tree arrows are all contrast-corrected
- Root font-size steps 14px to 16px at 768px, so rem-based sizing carries the density change
- Every override exists twice, once for Razor and once for the SPA, on purpose

## Colors

A UC Davis institutional palette: Aggie Blue is the foundation, Aggie Gold is the signature accent, and a small secondary set carries status. All values are canonical UC Davis brand hex, with full 10 to 100 ramps for blue, gold, and black.

### Primary

- **Aggie Blue** (`aggie-blue`): The load-bearing brand color and Quasar `primary`. The top header bar, primary buttons, link color, active nav text, section-nav item text on the gold band, the skip-to-content chip, and the welcome splash background. This is the surface that says UC Davis.
- **Blue 90** (`blue-90`): Hover and pressed states for blue chrome, including the splash sign-in call to action.
- **Blue 80** (`blue-80`): The main-nav loading placeholder fill, and the text color of informational status banners.
- **Blue 70** (`blue-70`): Tree expand and collapse arrows, sized up to 1.4rem so they clear AA as UI. Also the intended fill for `color="secondary"`. See the Blue 70 Parity Rule below.
- **Blue 10** (`blue-10`): The active left-nav row tint under `primary` text at weight 500.
- **Aggie Gold** (`aggie-gold`): The signature accent, gold ramp step 100. The 2.75rem brand-mark tile behind the rod of asclepius, and the welcome card's top border. Quasar `accent` and `warning` both map to Gold 90 (`gold-90`), which is the shade that pairs with dark text.
- **Gold 70** (`gold-70`) and **Gold 40** (`gold-40`): The section-nav band and its selected item.

### Secondary

- **Tahoe** (`tahoe`): Quasar `info`. Informational and tertiary actions. Light enough that it always pairs with `text-color="dark"`.

### Tertiary

- **Redwood** (`redwood`): Quasar `positive`. Success and create actions, and the status-toast fill.
- **Merlot** (`merlot`): Quasar `negative`. Danger and delete actions, validation error chips, and the environment badge.
- **Poppy** (`poppy`): Tips and highlights only. A warm orange used sparingly for callouts.
- **Arboretum** (`arboretum`) and **Cabernet** (`cabernet`): Secondary-palette accents, never dominant. Reserve for charts, tags, and categorical distinction.

### Neutral

- **Ink** (`ink`): Quasar `dark`; default high-contrast text. `dark-page` is the dark page background.
- **Body Grey** (`body-grey`): The AA-safe muted text color, `--ucdavis-black-60`. Quasar's default `.text-grey` and `.bg-grey` are remapped to this so muted text still clears 4.5:1.
- **Surface** (`surface`): Card, panel, and workspace background, and the welcome card over the hero photo.
- **Table Header** (`table-header`): Sticky `q-table` header fill, and the fill on `q-table__top` and `q-table__bottom`.
- **Gold Text** (`gold-text`): The darkened gold used for gold-colored *text* on light backgrounds, since bright gold fails AA at text sizes.
- **Focus Blue** (`focus-blue`): The outer ring of the keyboard focus halo in the app. The only non-brand hue in the system.
- **Splash Card Ink** (`splash-card-ink`) and **Splash Card Muted** (`splash-card-muted`): Body and secondary text inside the white sign-in card. Slightly warmer and softer than the workspace pairing, because the card sits on a photograph rather than on a page.

### On-Dark Text Alphas

The splash sets white text over a darkened photograph, where a flat hex cannot adapt. Four alpha steps carry the hierarchy: full white for the headline, 0.92 for body copy (raised from the design mock's 0.85 to clear AA at body sizes), 0.7 for footer text, and 0.22 for dividers. These `rgba()` literals are the one sanctioned exception to the no-hardcoded-color rule, because CSS has no syntax for alpha-modifying a custom property in a static sheet.

### Named Rules

**The Gold-Is-Accent Rule.** Gold is never a page surface and never body text. It appears as the 36px section-nav band, the 2.75rem brand tile, a 3px rule on the welcome card, and the splash call-to-action arrow. Measured in pixels of width, not percent of screen. Aggie Blue carries weight; gold punctuates it. Per UC Davis secondary-palette guidance, accents are never dominant.

**The Bright-Gold-Never-On-White Rule.** `aggie-gold` and `gold-90` on white fail AA at text sizes. For gold text on light backgrounds use `gold-text` (#664d03), applied through the `.text-warning` override. Bright gold is a background and accent color only.

**The AA-Floor Rule.** Every foreground and background pair clears WCAG AA, 4.5:1 for body and 3:1 for large text and UI. The enforcement is concrete and lives in code: Quasar `grey`, `grey-5`, and `grey-6` are remapped to `grey-7` by `toContrastSafeColor`; `warning`, `info`, and `accent` backgrounds are auto-paired with dark text by `getAccessibleTextColor`; inactive tabs are de-faded with `tabs-no-fade`; input clear-button opacity is forced to 1; tree arrows are recolored and enlarged; on-dark body text was raised to 0.92 alpha. Contrast correction is a system-level commitment, not a per-screen afterthought.

**The Blue 70 Parity Rule.** Two values ship under this name: the ramp step `--ucdavis-blue-70` (#355b85, used by tree arrows) and the Quasar `secondary` brand value in `VueApp/src/config/colors.ts` (#4b6983). The ramp is canonical. The config value is a small inconsistency to reconcile toward #355b85, not a second legitimate token. Do not introduce a third.

**The Two-Sided Parity Rule.** Razor and the SPAs are separate stylesheet worlds that must render identically. `.text-grey`, `.bg-grey`, `.error-surface`, the heading compression, the skip link, the focus ring, the clear-button opacity fix, and the Proxima Nova `@font-face` block are each defined more than once, across `web/wwwroot/css/site.css`, `web/wwwroot/css/welcome.css`, and `VueApp/src/styles/`. Change one, change the others in the same commit. The splash redeclares the brand tokens outright, since it loads with `Layout = null` and cannot see the Vue token files.

## Typography

**Display and Brand Font:** Proxima Nova, the UC Davis campus typeface, self-hosted as woff2 at 400, 500, 700, and 800, with `-apple-system`, BlinkMacSystemFont, Segoe UI, Roboto, then Arial as fallbacks. Weight 900 downshifts to 800, since the family caps there. Font files carry a `?v=1` cache-buster because they are served with long-lived immutable headers; bump it in every stylesheet when a file is replaced.
**Body and Workspace Font:** Roboto, self-hosted as a variable woff2 covering weights 100 to 900, with the system sans stack behind it, extended with Apple Color Emoji, Segoe UI Emoji, and Noto Color Emoji so emoji render in the user's native set.
**Icon Font:** Material Icons, self-hosted woff2, with a remote `fonts.gstatic.com` fallback declared in `site.css`.
**Print-Only:** Ryman Eco is the UC Davis print display face and is deliberately not loaded on the web. Arial and Aptos are the brand-approved fallbacks when Proxima is unavailable.

**Character:** Proxima Nova is the warm, confident geometric campus voice; Roboto is the neutral, space-efficient workhorse for data-dense screens. The contrast axis between them is *role*, not style: brand versus work. Pairing them keeps the institutional identity at the edges while letting the interior breathe.

### Hierarchy

- **Display** (Proxima Nova 800, `clamp(2.75rem, 5.5vw, 4.25rem)`, line-height 1, letter-spacing -0.02em, `text-wrap: balance`): The welcome-splash headline, and the single place display-scale type appears. Below 1024px it retunes to `clamp(2.5rem, 11vw, 3.25rem)` at line-height 0.98 so it still fits a phone.
- **Chrome** (Proxima Nova 500): The blue top header bar and the footer. Applied to `#mainLayoutHeader` and `.q-footer` as a whole, so everything inside inherits it except the gold section nav, which overrides back to Roboto.
- **Heading** (Roboto 700, 1.2rem, line-height 1.2rem): In-app page and dialog headings. `h1`, `h2`, and `h3` inside `q-page-container` and `q-dialog` are all deliberately compressed to the same 1.2rem bold. `h4` is 1.1rem, `h5` is 1rem bold, `h6` is 1rem regular. The splash card's own heading is the exception at 1.5rem bold with -0.01em tracking, since it is brand surface.
- **Body** (Roboto 400, 1rem): All application copy. Root font-size is 14px, stepping to 16px at 768px, so 1rem means 14px on phones and 16px on desktop. The splash tagline sits at 1.125rem. Cap prose at 65 to 75ch.
- **Label** (Roboto 400, 0.75rem): Breadcrumbs, validation error chips, and small metadata. Short strings only.

### Named Rules

**The Two-Font Rule.** Proxima Nova belongs to brand chrome: the blue header bar, the footer, and the welcome splash. Roboto belongs to the workspace, meaning everything inside `q-page-container` plus the gold section nav, which is intentionally Roboto because it is narrower and the nav carries many items. Never set workspace body copy in Proxima; never set brand chrome in Roboto. The one deliberate crossing is that section nav, and it is documented in the code that does it.

**The Compressed-Heading Rule.** Inside the app, headings are 1rem to 1.2rem and bold, never display-scale. Hierarchy in the workspace comes from weight and spacing, not size. Display type above 2.75rem is brand-surface only. An in-app `h1` that looks like a marketing hero is wrong, and the global heading overrides will fight it.

**The Root-Scale Rule.** Hand-authored type and spacing go in rem, because the root font-size is the responsive mechanism: a px value there silently opts out of the 14px to 16px step at 768px and breaks density parity between phone and desktop. Two things are deliberately exempt. Quasar's own spacing scale is px, and using it is fine. Fixed chrome dimensions are px so the shell holds its size when the type inside it grows: the 86px header, the 36px section-nav band, the 600px table viewport, and the avatar sizes.

## Layout

The authenticated shell is a Quasar layout in `hHh lpR fFf` configuration: a header spanning full width and staying put, a left drawer below it, and a footer. The header carries `height-hint="98"` so the layout reserves space before hydration, backed by `#headerPlaceholder` and `#mainNavPlaceholder` blocks that paint blue at the right heights while the page loads.

**Vertical structure**, top to bottom: the Aggie Blue toolbar (min-height 86px at 768px and up) holding the brand lockup, the wordmark, mobile menu buttons, and the profile picture; then the 36px gold section-nav band; then the left drawer beside the page container; then the page body at `q-pa-md` (16px); with `body` reserving a 60px bottom margin for the footer.

**Density and breakpoints.** Root font-size is the primary responsive lever: 14px below 768px, 16px at 768px and above, which also raises the header to 86px and the left drawer to a 300px minimum. Quasar's own breakpoints govern visibility: `gt-sm` (1024px and up) shows the desktop wordmark button and the section-nav band, while `lt-md` (below 1024px) swaps in the hamburger MiniNav and a compact button, and drops the school-name half of the brand lockup so the toolbar still fits. The left drawer becomes an overlay at mobile widths.

**Tables** are the dominant working layout. `.sticky-header-table` fixes height at 600px so the header can stick, with a 48px offset accounting for one header row (`top: 48px` when the loading row appears, and a matching `scroll-margin-top` so focus does not scroll rows under the sticky header). Table containers carry 1.5rem of bottom margin to separate stacked tables.

**Forms** use the shared `.compact-form` treatment, which removes Quasar's reserved hint and error space so fields sit tight, then expands only when a validation error appears.

**The welcome splash** is the exception to all of this: a standalone page with `Layout = null`, no Quasar shell, and its own full-viewport composition of hero photograph, editorial column, and sign-in card.

### Named Rules

**The Reserved-Space Rule.** Chrome that appears after hydration gets a placeholder at its exact final height (50px placeholders, `height-hint="98"`, 86px header). The page must not reflow when the app boots.

## Elevation & Depth

The system is flat by default. Workspace surfaces sit on the page with borders, tonal fills, and left accent rules rather than drop shadows. Depth is spent deliberately, and each place it appears is either fixed chrome, a response to focus, something transient, or the single brand moment.

### Shadow Vocabulary

- **Header lift** (`box-shadow: 0 0 10px 2px rgba(0, 0, 0, 0.2), 0 0px 10px rgba(0, 0, 0, 0.24)`): Quasar's `elevated` prop on `q-header`, rendered onto a `.q-layout__shadow` overlay. Separates the fixed blue chrome from scrolling content.
- **Focus ring** (`box-shadow: 0 0 0 0.1rem white, 0 0 0 0.25rem #258cfb`): The keyboard-focus halo on form controls and buttons. A white inner ring separates the blue outer ring from the control's own edge, so it reads on both light and colored backgrounds.
- **Splash focus ring** (`box-shadow: 0 0 0 0.1875rem rgba(255, 197, 25, 0.85)`): The gold focus halo on the splash, because default outlines and the blue app ring both disappear against a gradient-darkened photograph.
- **Splash card lift** (`box-shadow: 0 1.875rem 5rem rgba(2, 40, 81, 0.5)`): The heaviest elevation in the system, and the white sign-in card is the only thing that earns it. Dramatic on purpose, and used exactly once.
- **Splash text halo** (`box-shadow: 0 0.125rem 0.5rem rgba(2, 40, 81, 0.5)`) and **footer halo** (`0 0.0625rem 0.1875rem rgba(2, 40, 81, 0.6)`): Legibility backing for white text sitting directly on photography.
- **Status toast** (`box-shadow: 0 0.125rem 0.5rem rgba(0, 0, 0, 0.3)`): The fixed bottom-center `.viper-status-notification`. Transient, so it may float.

### Tonal Layering

Everything else conveys depth without shadow. Sticky table headers read as elevated through the `table-header` fill plus stickiness alone. Status surfaces use a 12% tint of their semantic color mixed into white (15% for warning) with a 0.25rem left border in the full-strength color. Active nav rows use the `blue-10` tint. No routine card, panel, or dialog in the workspace carries a drop shadow.

### Motion

Motion is nearly absent by design. The status toast is the only custom transition in the authenticated app: opacity and transform over 0.3s, sliding up 1rem as it fades in. Quasar is configured with `animations: "all"`, so its component transitions are available, and the loading overlay uses `QSpinnerOval` after a 100ms delay so brief waits never flash a spinner.

### Named Rules

**The Flat-By-Default Rule.** Workspace surfaces are flat at rest. Shadow appears only as fixed chrome, as a response to keyboard focus, on something transient that floats above the page, or as the single deliberate lift of the welcome card over its hero photo. If a routine panel or card has a drop shadow for depth, remove it.

**The Blue-Shadow Rule.** On the splash, every darkening layer derives from Aggie Blue rather than black: the hero gradient, the legibility bands, the text halos, and the card lift all tint `rgba(2, 40, 81, ...)`. Black shadows over brand photography read as dirt; blue ones read as atmosphere. Alphas sit slightly above their black equivalents to preserve perceived depth.

**The Delayed-Spinner Rule.** Loading indicators wait 100ms before appearing. Work that finishes faster than that shows nothing at all.

## Shapes

The form language is rectangular and quiet, at Quasar's default scale. Corners are softened just enough to read as interface rather than as document, and the brand surfaces are squarer still.

- **4px** is the system default, applied by Quasar to buttons, cards, inputs, badges, and dialogs. Radius is not used expressively; nothing is more rounded to draw attention.
- **Square (0 radius)** on the brand surfaces: the 2.75rem gold brand-mark tile, and the welcome sign-in card with its 0.1875rem (3px) gold top border. Squareness is what makes them read as institutional rather than as app furniture.
- **0.25rem** on chrome affordances authored by hand: the skip-to-content chip (bottom corners only, so it reads as pulling down from the top edge) and the status toast.
- **1rem pill** appears exactly once, on validation error chips under form fields, where the pill shape plus a Merlot fill plus an inline Material Icons `error` glyph make an error read as a label rather than as body text.
- **50% circles** come from Quasar for round icon buttons.
- **Portrait rectangles** for people. Avatars are not circles: 40 by 31px in the header and in small photo contexts, 111 by 87px in directory results. The roughly 0.78 ratio is an ID-photo shape, and it is a deliberate institutional signal rather than a social-app one.
- **Left accent rules** at 0.25rem carry status on banners and on `.error-surface`, reinforcing the border-and-tint approach to depth.
- **Hairline borders** separate chrome: 1px under the gold section-nav band, and vertical `q-separator` rules between its items.

### Named Rules

**The One-Pill Rule.** The 1rem pill radius belongs to validation error chips and nothing else. Any other pill-shaped element is either a Quasar badge at 4px or a mistake.

**The Square-Brand Rule.** Brand elements are square; product elements are 4px. If a brand mark or a splash surface picks up a radius, it has started to look like an app component, which is the opposite of the intent.

## Components

VIPER is built on Quasar and the prescription is to always use Quasar components, styled with brand tokens, rather than bespoke markup. The splash is the sanctioned exception, since it renders without the Quasar shell.

### Buttons

- **Shape:** Quasar default (4px); the brand-splash call to action is square with a gold accent.
- **Color roles:** Primary action `primary` (Aggie Blue); Success or create `positive` (Redwood); Danger or delete `negative` (Merlot); Info or tertiary `info` with `text-color="dark"` (Tahoe); Warning or caution `warning` with `text-color="dark"` (Gold 90); Secondary `secondary` (Blue 70).
- **Loading state:** A `q-btn` with a text label plus `:loading` needs a `#loading` slot (`<q-spinner size="1em" class="q-mr-sm" />` plus the label text), otherwise the label vanishes. Icon-only buttons use the default spinner.
- **Interactive non-buttons:** Anything clickable that is not a `q-btn` needs `@keyup.enter`, `@keyup.space`, `tabindex="0"`, `role="button"`, and `aria-label`.

### Badges

Use **`StatusBadge`**, which wraps `q-badge` and does two things automatically: `toContrastSafeColor` remaps Quasar's contrast-dead-zone greys, and `getAccessibleTextColor` pairs light backgrounds (`warning`, `info`, `accent`) with dark text. Pass the label via the `label` prop or the default slot. Avoid raw `q-badge` with hand-written text colors in SPAs.

### Banners

Use **`StatusBanner`** in Vue SPAs with `type="success|error|warning|info"`. Each type carries its own Material icon (`check_circle`, `error`, `warning`, `info`), a 12% tint background (15% for warning), a 0.25rem left border, and matching text color. Only `type="error"` is assertive (`role="alert"`) by default; everything else is polite (`role="status"`). Override with `live`: `live="assertive"` for a warning or info banner shown in direct response to a user action, `live="off"` for a decorative banner with no dynamic content. Do not reach for `type="warning"` to force an assertive announcement on a persistent state indicator. Banners are `rounded` with `inline-actions`, sit on `q-mb-md`, and accept an optional dismiss button. Razor pages use `q-banner` with accessible classes. Error surfaces outside `StatusBanner` use the shared `.error-surface` treatment so `GenericError` and expired-session dialogs match.

### Cards and Containers

- **Corner style:** Quasar default (4px); the splash card is square with a 3px gold top border.
- **Background:** `surface` white on the workspace, and white over the Aggie Blue hero on the splash.
- **Shadow strategy:** Flat in the workspace. See Elevation & Depth.
- **Internal padding:** Quasar `md` (16px) is typical; the splash card takes 2.5rem.
- Never nest cards, and do not reach for a card when a list or plain section is the better affordance.

### Inputs and Fields

- **Style:** Quasar `q-field`, `q-input`, `q-select`, always `outlined`. This is settled convention rather than preference: 180 of the 181 field-style props across the SPAs are `outlined`, with a single stray `filled`. A bordered box reads more clearly than an underline in dense forms and against table backgrounds. Selects are always `dense` plus `options-dense`.
- **Focus:** The white-then-blue focus ring. Clear-button opacity is forced to 1 for contrast.
- **Compact form:** Wrap a form in `.compact-form` to drop Quasar's reserved bottom space. Validation errors then render as Merlot pill chips at 0.75rem with a leading `error` glyph.

### Rich Text Editor

- Use `RichTextEditor` (`@/components/RichTextEditor.vue`), never a raw `<q-editor>`, for any HTML content editing. It wraps Quasar's QEditor and centralizes what QEditor omits: an accessible name on every icon toolbar button, a "view source" button whose tooltip/name describes the action rather than the current mode, and an accessible name on the contenteditable region.
- Pass the area's own `toolbar` (button sets are intentionally area-specific: CMS content blocks carry headings/link/hr, CTS descriptions carry alignment) and an accessible name via `aria-label`, or `label-id` when a visible label already exists. Other QEditor props (`min-height`, `dense`, `outlined`, `class`) fall through via `$attrs`.

### Dialogs

Every `q-dialog` needs an accessible name via `aria-labelledby` pointing at the title's `id` (or `aria-label` when there is no visible title), and a visible close affordance. Persistent SPA dialogs use `@click="handleClose"` rather than `v-close-popup`. Error banners go in a separate `q-card-section` below the header. Confirmation and simple action dialogs use Submit plus Delete only, with no Cancel, since the X and Escape both dismiss. Data-entry form dialogs may add a footer Cancel beside the primary action, because once a user has been filling in fields an explicit Cancel reads more clearly. When present, Cancel, the X, and Escape all route through the same `handleClose`, carrying the unsaved-changes guard.

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

- **Top chrome:** The Aggie Blue bar (`#mainLayoutHeader`, min-height 86px at 768px and up) in Proxima Nova 500, carrying the `.viper-brand` lockup, the `VIPER 2.0` wordmark button, an environment badge when applicable, and the profile picture. Below 1024px the wordmark button is replaced by a hamburger MiniNav, a drawer-toggle button, and a compact label button.
- **Brand lockup:** `.viper-brand` is a flex row with 0.75rem gap: a 2.75rem square gold tile (`aggie-gold`) holding the white rod-of-asclepius mark at 2.5rem, leaving a thin inset, beside the school-name lockup image at 2.75rem tall. The name half hides below 1024px, where the toolbar cannot fit it alongside the nav buttons and profile; the splash header has room and keeps both. Factored into `_ViperBrand.cshtml` so Razor and the SPAs share one lockup.
- **Section nav:** The gold band (`#mainLayoutHeaderSections`, `gold-70`, min-height 36px) with a 1px bottom border, items in Aggie Blue at regular weight, vertical separators between them, and a trailing help button with a tooltip. The selected item takes `gold-40`. Set in Roboto rather than the header's Proxima, deliberately, because it is narrower and the band carries many items. Desktop-only (`gt-sm`), populated from `layout/topnav`.
- **Left drawer:** `#leftNavMenu` with bold `h2` section titles at 1.2rem, bold 1rem headers, 500-weight subheaders, and two indent tiers at 1.5rem and 2.5rem. Active router links tint to `blue-10` with `primary` text at weight 500. The Razor side uses `.leftNavActive` for the same purpose.

### Tables

Tables are the primary working surface. `q-table` headers, tops, and bottoms take the `table-header` fill with `white-space: nowrap`. Add `.sticky-header-table` for a fixed 600px viewport with a sticky header. Containers carry 1.5rem bottom margin.

### Welcome Splash (signature component)

The unauthenticated landing (`web/Views/Home/Welcome.cshtml`, `Layout = null`, backed by `WelcomePageHelper.cs`): a full-bleed hero photograph, one of five randomized per load and served `image-set` AVIF then JPG, under an Aggie Blue gradient, with an editorial column (gold rule, display headline, tagline) and a white sign-in card carrying a single CAS sign-in call to action. The five photographs live at `web/wwwroot/images/login/`: guinea pig, horse and foal, ophthalmology, the SVM building, and vetmed admin. The card is square with a 3px gold top border, 2.5rem padding, and the system's one heavy elevation. Focus rings go gold here. A standalone `welcome.css` redeclares the brand tokens because the page cannot load the Vue and Quasar token files, and its `:root` block is a mirror of `colors.css` that must be updated alongside it.

This is the one place in the system that goes drenched, photographic, and display-scale. Nothing else may borrow its vocabulary.

### Other Signature Components

- **The environment badge.** A Merlot `q-badge` reading `Development` or `Test`, sitting inline against the wordmark, marked `role="presentation"` since it is decorative to screen readers. It is the one place the workspace deliberately shouts, and it exists so nobody edits production data thinking they are on TEST. In production it renders nothing at all.
- **The status toast.** `.viper-status-notification`, fixed bottom-center, Redwood, 0.25rem radius, `role="status"`, fading and sliding up 1rem over 0.3s. The app's only custom motion.
- **The skip-to-content chip.** Parked at `top: -2.5rem` and dropping to `top: 0` on focus, in Aggie Blue with white text and rounded bottom corners. Present on every page, visible only to keyboard users.

### Landmarks

Exactly one `<main>` per page. Razor pages get it from `_VIPERLayout.cshtml` (`<main id="main-content" tabindex="-1">`); Vue SPAs get it from `ViperLayout.vue` inside `q-page-container`. Never add `<main>` to an SPA `App.vue` or page component. The `tabindex="-1"` exists so route changes can move focus for screen-reader announcements, which is why `#main-content:focus` suppresses its outline.

## Do's and Don'ts

### Do:

- **Do** carry brand color through Quasar roles: `primary` Aggie Blue, `positive` Redwood, `negative` Merlot, `info` Tahoe, `warning` Gold 90, `secondary` Blue 70. Pair `info` and `warning` with `text-color="dark"`.
- **Do** reference tokens, not literals, routing new colors through `colors.css` and mirroring them into `welcome.css` when the splash needs them.
- **Do** size hand-authored type and spacing in rem; fixed chrome dimensions stay px.
- **Do** use Proxima Nova for brand chrome and Roboto for the workspace.
- **Do** mirror every override across `site.css`, `welcome.css`, and `VueApp/src/styles/` in one commit, bumping the font `?v=` when a woff2 changes.
- **Do** use `StatusBadge` and `StatusBanner` over raw `q-badge` and `q-banner`, and set `dense` plus `options-dense` on selects.
- **Do** set `outlined` on every field.
- **Do** give every dialog an accessible name and a close affordance.
- **Do** keep exactly one `<main>` landmark per page.
- **Do** give post-hydration chrome a placeholder at its exact final height.
- **Do** keep gold to a band, a tile, a rule, or an arrow.
- **Do** use `text-wrap: balance` on display and brand headings.

### Don't:

- **Don't** use inline `style=""`. Two ship today and should go when those files are next touched: `_VIPERLayout.cshtml` and `MainNav.vue`.
- **Don't** use `!important` outside the documented contrast overrides (`.text-grey`, `.bg-grey`, `.text-warning`, `tabs-no-fade`, avatar sizes).
- **Don't** put bright gold on white as text. Use `gold-text` (#664d03).
- **Don't** add a third darkened gold. `#664d03` and `#5d4600` both ship and should converge.
- **Don't** hardcode an untokenized color. Three ship and should stop spreading: `silver` on the section-nav border, `#e6ecf2` on the Razor active row, and a stale `#335379` where `--ucdavis-blue-80` is `#1d4776`. The splash on-dark alphas are the one exception.
- **Don't** set in-app headings at display scale; `h1` through `h3` are 1.2rem bold.
- **Don't** add drop shadows to routine cards or panels.
- **Don't** use black shadows over brand photography.
- **Don't** nest cards, or use a card where a list or plain section fits.
- **Don't** render two `<main>` landmarks on one page.
- **Don't** import consumer-SaaS tells: gradient text, glassmorphism, hero-metric templates, identical icon-card grids, neon-on-dark accents, marketing buzzwords, or an undifferentiated Material or Bootstrap admin look.
