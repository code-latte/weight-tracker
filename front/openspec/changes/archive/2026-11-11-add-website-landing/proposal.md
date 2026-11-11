# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-website-landing.md`. Content appendix (every copy text and
visual rule, Spanish): `.stpr/plan/openspec/proposal-change-add-website-landing.landing-spec.md`.

## Why

Trainera has no public presence. The team needs a page that explains to a trainer who does not know Trainera what it
is, what problem it solves and what it offers, and that collects the emails of trainers who want to join the first
ten Founding Trainers, now and without an account.

## What Changes

- New `front/website` workspace (package `website`): a Next.js static site (`output: "export"`), Spanish only, that
  depends on no other workspace. It joins the pnpm workspace and every Turborepo task.
- `/`: a one-page landing in the appendix's section order (header, hero, problem, features, how it works, athlete
  experience, AI, Founding Trainers, FAQ, final CTA, footer), with anchor navigation, a sticky header that compacts on
  scroll and a mobile menu.
- Every feature, AI capability and step shows "Disponible" content or a "Próximamente" label from one content file.
- An early-access form that sends only the email to `POST /customer-leads`, with a required privacy checkbox (not
  sent), a honeypot, loading and error states, and the appendix's confirmation. The Founding Trainers section states
  the ten-trainer limit as a condition of the programme.
- A launch-state setting (`NEXT_PUBLIC_LAUNCH_STATE`, `prelaunch`) that drives CTA labels and blocks from the content
  file; only the pre-launch state is built.
- A no-op analytics event layer (`track`) with UTM and referrer capture; no provider, no cookies.
- SEO metadata, Open Graph, favicons from `brand/`, `sitemap.xml`, `robots.txt`; four legal pages with placeholder
  text until the product owner supplies it.
- **Contract amendment:** `STPR_INVARIANTS.md` and `AGENTS.md` gain a `website` section: a fourth workspace outside
  the layered architecture that never imports, nor is imported by, `core`, `backoffice` or `app`. Approved by the
  product owner.
- No change to `core`, `backoffice`, `app`, the API or `brand/`.

## Capabilities

### New Capabilities

- `website-landing`: the public landing page and its legal pages: sections, navigation, copy, feature status, launch
  state, product images, SEO, responsive and accessibility rules, analytics events.
- `website-lead-capture`: the early-access form: what it collects, its validation, honeypot, submission to
  `POST /customer-leads`, error and confirmation states.

### Modified Capabilities

None. No existing spec covers a public website.

## Fronts, Roles And References

- **Fronts:** `website` only. `backoffice` and `app` are unchanged.
- **Roles:** none. The site is public and anonymous; it shows no user data and asks for nothing but an email.
- **Canonical references:**
  - workspace, Next config, lint, typecheck, Vitest and Playwright setup -> `backoffice/package.json`,
    `backoffice/next.config.ts`, `backoffice/eslint.config.mjs`, `backoffice/vitest.config.mts`,
    `backoffice/playwright.config.ts` (tooling only; no backoffice code is reused);
  - fonts and colour values -> `backoffice/src/app/layout.tsx` and `backoffice/src/presentation/common/theme.ts`
    (re-declared in the website's own tokens);
  - component and E2E tests -> `backoffice/test/` and `backoffice/e2e/` (same tiers, same Arrange / Act / Assert);
  - sections, content module, lead client, analytics -> `pending`: no reference exists; this change establishes them
    for the website only.
- **Components:** the website has its own components (button, section, card, badge, status label, accordion, mobile
  menu, product image frame) and one component per section. It does not use the atomic levels of the fronts nor any
  of their components.
- **API endpoint:** `POST /customer-leads` (`{ email }` -> `204`, `400`, `429`), provided by the API change
  `add-customer-leads`, which must be merged first. It is called with `fetch` and stays out of
  `core/openapi/v1.json`.
- **Business rules:** none. Email normalisation and uniqueness are the API's; the form validates structure only.
- **Conflicts with the template or invariants:** the invariants cover three workspaces and say the fronts share only
  `core`. This change amends them for `website` (approved). Nothing else conflicts.

## Impact

- New: `front/website/**`; `pnpm-workspace.yaml` (adds `website`); root `package.json` (`dev:website`).
- Updated: `.stpr/template/STPR_INVARIANTS.md` (website section), `AGENTS.md` (workspace table, commands, testing).
- Dependencies (website only): `next`, `react`, `react-dom` at the backoffice's versions; `lucide-react`; the
  backoffice's lint, test and E2E dev tooling.
- Not affected: `core`, `backoffice`, `app`, the API, `brand/`.
- Going live is blocked by items outside this change: the legal texts, hosting and domain, and a production CORS policy
  in the API (CORS is Development-only today).
