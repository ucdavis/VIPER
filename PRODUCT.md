# Product

## Register

product

## Users

The UC Davis Weill School of Veterinary Medicine community: faculty, clinicians, staff, and students. They authenticate through campus CAS and use VIPER as the internal hub for the school's operational systems — role and permission administration (RAPS), effort reporting, clinical scheduling, competency tracking (CTS), the directory, and CMS-managed content.

These are mandatory, repeat users, not visitors. They arrive mid-workflow, often under time pressure, to get a specific administrative, academic, or clinical task done — the app is a tool, not a destination. Technical comfort ranges widely, from daily power users to occasional ones, and the same screens are used across desktop and mobile.

## Product Purpose

VIPER (2.0) is the School of Veterinary Medicine's internal web application suite: a single authenticated home, behind the UC Davis brand, for the operational systems the school runs on. It consolidates permission management, effort reporting, clinical scheduling, competency tracking, directory, and content into one consistent shell so staff and faculty don't juggle disconnected tools.

Success looks like: tasks completed quickly and correctly with minimal training, consistent behavior across every area, and full accessibility for every user — on any device. The product earns trust by being reliable and clear, not by being novel.

## Brand Personality

Institutional, trustworthy, and efficient — the voice of a public university veterinary school. Three words: **authoritative, clear, accessible.**

It should feel unmistakably like UC Davis: Aggie Blue and gold, calm and credible, brand-disciplined. Warmth comes from the school's mission — animal and public health, teaching, the people and patients — surfaced through brand imagery on welcome surfaces, not from decorative UI inside the workspace. The copy voice is plain, direct, and respectful of the user's time: say what a control does and what will happen.

## Anti-references

VIPER should **not** look like a consumer SaaS marketing site, a trendy startup dashboard, or a generic off-the-shelf admin template. Specifically avoid:

- **Consumer-SaaS tells**: gradient text, glassmorphism, the hero-metric template (big number + small label + gradient accent), and identical icon-card grids.
- **Marketing buzzwords**: streamline, empower, supercharge, leverage, seamless, world-class, enterprise-grade, next-generation, game-changer. Use specific nouns and verbs for what the product literally does.
- **Off-brand color**: bright gold as a dominant surface or as text on white; dark-mode-with-neon-accents; anything that fights the UC Davis institutional palette.
- **Marketing-scale UI inside the app**: display-scale hero headings on working screens (those belong only to brand surfaces like the login splash).
- **Generic Material/Bootstrap admin look**: undifferentiated framework defaults with no brand discipline.

## Design Principles

1. **The tool gets out of the way.** Inside the app, density, speed, and legibility beat decoration; every element earns its place. The brand greets users at the door (the login splash); the workspace stays quiet.
2. **Accessibility is a floor, not a feature.** WCAG AA on every surface — contrast, landmarks, keyboard operation, screen-reader semantics — because the audience is mandatory daily users across a full range of needs.
3. **Brand discipline over novelty.** Carry the UC Davis identity faithfully: Aggie Blue carries the weight, gold accents but never dominates, consistency wins. The school's credibility is the design's job.
4. **Reuse over reinvention.** One Quasar component vocabulary, one token source, shared patterns (StatusBadge, StatusBanner, the dialog pattern). New screens compose existing parts rather than introducing one-offs.
5. **Correctness earns trust.** This is institutional software handling permissions, effort, and schedules. Clarity and predictability outrank cleverness; never surprise the user with what an action does.

## Accessibility & Inclusion

WCAG 2.1 AA is the system-wide standard, enforced rather than aspired to:

- **Contrast**: ≥4.5:1 for body text, ≥3:1 for large text and UI. Quasar's mid-greys are remapped to AA-safe shades, bright gold is corrected before use as text, and inactive tab text is kept above the fade threshold.
- **Structure**: exactly one `<main>` landmark per page; a visible skip-to-content link.
- **Components**: every dialog has an accessible name and a close affordance; interactive non-buttons get full keyboard semantics (`enter`/`space`, `tabindex`, `role`, `aria-label`).
- **Announcements**: screen-reader live regions are polite by default and assertive only in direct response to a user action.
- **Motion & responsiveness**: reduced-motion preferences respected; layouts work from mobile (390px) up. Fonts are self-hosted for reliable rendering.

Visual and component-level specifics live in [DESIGN.md](DESIGN.md).
