# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour; every copy text comes from the content appendix
`.stpr/plan/openspec/proposal-change-add-website-landing.landing-spec.md`. Current state that shapes the approach:

- `front/` is a pnpm + Turborepo monorepo with `core`, `backoffice` and `app`. Turborepo's tasks (`build`, `lint`,
  `typecheck`, `test`, `test:e2e`) are generic, so a new workspace with those scripts joins them without touching
  `turbo.json`. Root `prettier --check .` already covers any new folder.
- The backoffice is the only Next.js app: Next 16.4.2, React 19.3.0, `output: "export"`, `next typegen && tsc` for
  typecheck, ESLint flat config with `eslint-config-next`, Vitest + RTL + jsdom, Playwright served from `out/` with
  `serve`. Its theme palette and fonts (Plus Jakarta Sans, Inter through `next/font/google`) match the appendix's
  §13 token table exactly.
- `POST /customer-leads` (API change `add-customer-leads`, not merged yet) takes `{ email }` and answers `204` for a
  new or known email, `400` for an invalid one and `429` past 5 requests per minute per client. The API enables CORS in
  Development only, for any origin.
- `brand/trainera-logo/` holds the logo, symbol and favicon files in SVG and PNG; it has no general-purpose icons.
- `STPR_INVARIANTS.md` says it covers the three workspaces (line 5) and that `backoffice` and `app` share only `core`
  (§2, line 103); §4 has one stack section per workspace.

## Goals / Non-Goals

**Goals:**

- A fast, static, accessible landing that is easy to change: copy, statuses and launch state live in data, not in
  components.
- Complete independence from the other workspaces, enforced by lint and stated in the invariants.

**Non-Goals:**

- Reusing the fronts' Clean Architecture layers, container, atomic folders or i18n setup.
- A server runtime, API routes or server actions.
- An analytics provider, cookies or consent management.

## Decisions

### Contract amendment (approved)

- `STPR_INVARIANTS.md`: line 5 lists `website` as a fourth workspace that only §4.4 governs; §2's dependency rule adds
  that `website` never imports `core`, `backoffice` or `app` and none of them imports it; a new **§4.4 Website:
  Next.js static site** states: static export, no server runtime; no `core` and no generated contract (public
  endpoints are called with `fetch`); Spanish copy in one content module; own tokens and components, never the
  fronts'; tests in `website/test` (Vitest) and `website/e2e` (Playwright, API intercepted); the layered-architecture,
  atomic-design and `es`/`en` rules of §2 and §4.2 do not apply to it.
- `AGENTS.md`: the workspace table gains `website/` (`website`, Next.js static site, public visitors); "Running And
  Verifying Locally" gains `pnpm dev:website`; "Testing Guidelines" gains the website tiers; the quality checklist line
  "`backoffice` and `app` share no UI" also names `website`.

### Workspace and tooling

- `front/website/package.json` (`name: "website"`, private), modelled on the backoffice's: scripts `dev` (`next dev
--port 3001`), `build`, `lint` (`eslint`), `typecheck` (`next typegen && tsc --noEmit`), `test` (`vitest run`),
  `test:e2e` (`playwright test`); dependencies `next`, `react`, `react-dom` at the backoffice's exact versions and
  `lucide-react`; dev dependencies the backoffice's ESLint, TypeScript, Vitest, RTL, jsdom, Playwright and `serve`
  versions. No `@weight-tracker/*` dependency.
- `pnpm-workspace.yaml` adds `website`; root `package.json` adds `dev:website` (`turbo run dev --filter=website`).
- `next.config.ts`: `output: "export"`, `images: { unoptimized: true }` (static export has no image optimiser; images
  ship pre-optimised), `trailingSlash: false`. Next 16 APIs (metadata, `sitemap.ts`, `robots.ts` with
  `dynamic = "force-static"`, `next/font`) are checked against `website/node_modules/next/dist/docs/` before writing,
  as `AGENTS.md` requires.
- `eslint.config.mjs` as the backoffice's, plus `no-restricted-imports` for `@weight-tracker/*` and any relative path
  into `../core`, `../backoffice` or `../app`.
- `vitest.config.mts` and `playwright.config.ts` as the backoffice's; Playwright serves `out/` on port 4174 so it can
  run beside the backoffice's 4173.

Alternative considered: a standalone folder outside `front/`. Rejected by the product owner: one install, one
pipeline and one `pnpm test` keep it verified with the rest.

### Next 16 notes (task 2.1)

Read in `node_modules/next/dist/docs/` (static exports, metadata and OG images, sitemap, robots, app icons, fonts,
image). Deviations from the first draft of this design:

- Favicons, the apple-touch icon and the Open Graph image use the metadata file conventions instead of `metadata.icons`
  and `openGraph.images`: `src/app/icon.png` (from `favicon-192.png`), `src/app/apple-icon.png` (from
  `app-icon-ios-1024.png`) and `src/app/opengraph-image.png` with its `opengraph-image.alt.txt`. Next emits the tags
  and the static files itself.
- `sitemap.ts` and `robots.ts` are special route handlers, cached by default; they still declare
  `export const dynamic = "force-static"`, as the static-export guide requires for prerendered handlers.
- `next/image` with the default loader is not supported in a static export; images are `next/image` with
  `unoptimized` (set globally through `images.unoptimized`) and ship pre-optimised.
- Fonts: `next/font/google` as in the backoffice, no change.

### Structure

```
website/
  public/brand/        logo-light.svg, logo-mono-white.svg, symbol-light.svg, favicon-32.png, favicon-192.png,
                       apple-touch-icon.png (from app-icon-ios-1024.png), og-image.png (1200×630, from
                       trainera-logo-light-1200.png on the paper background)
  public/screens/      product screenshots (WebP + AVIF), fictitious data only
  src/app/             layout.tsx (fonts, metadata, tokens), page.tsx, aviso-legal/, privacidad/, cookies/,
                       condiciones/ (page.tsx each), sitemap.ts, robots.ts
  src/sections/        header, hero, problem, features, how-it-works, athlete-experience, ai, founding-trainers
                       (with the form), faq, final-cta, footer
  src/components/      button, section, card, badge, status-label, accordion, mobile-menu, product-image,
                       legal-page
  src/content/         landing.ts (all copy, nav, feature statuses, FAQ, per-state CTAs), legal.ts
  src/config.ts        API base URL, site URL, launch state (validated, with defaults)
  src/leads/           register-lead.ts, lead-form-state.ts
  src/analytics/       track.ts, attribution.ts
  src/styles/          tokens.css, globals.css
  test/                component and unit tests
  e2e/                 Playwright specs
```

Components are plain React with CSS Modules; only the ones that need state (header, mobile menu, accordion, form,
section-view observer) are client components (`"use client"`), the rest render at build time.

### Styling and tokens

`tokens.css` declares the appendix's §13 colours (`--color-primary: #0F766E` … `--color-border: #E2E8F0`), radii
(`--radius-card: 16px`, `--radius-button: 12px`), the type scale (h1 52–64 px, h2 36–44 px, h3 24–28 px, body 16–18
px, small 14 px, with `clamp()` for mobile), spacing, shadows and transitions (150–300 ms). Breakpoints are 600, 900
and 1200 px, mobile-first. Fonts load through `next/font/google` as CSS variables, like the backoffice. The strength,
cardio and stretching colours are not declared. Section backgrounds alternate paper, fog and ink; the teal gradient
(`#0F766E` → `#115E59`) is used only by the final CTA.

### Navigation behaviour

- Anchors: `#funcionalidades`, `#como-funciona`, `#para-entrenadores` (athlete experience), `#ia`,
  `#acceso-anticipado`, `#faq`; each section has `scroll-margin-top` equal to the header height.
- Smooth scroll is CSS `scroll-behavior: smooth` inside `@media (prefers-reduced-motion: no-preference)`; transitions
  are disabled under `reduce`.
- The header compacts when a sentinel at the top of the page leaves the viewport (an `IntersectionObserver`, no scroll
  listener).
- The mobile menu is a disclosure: a button with `aria-expanded` / `aria-controls`, a panel with the links, closes on
  a link click and on Escape, returning focus to the button.
- The FAQ accordion: each question is a `<button aria-expanded>` controlling its answer region; several can be open.

### Content and launch state

`content/landing.ts` exports typed data: nav items, every section's texts, feature blocks with
`status: "available" | "soon"` and their image, AI capabilities (all `soon`), steps, benefits, the FAQ, footer links,
and `ctas: Record<LaunchState, …>` with only `prelaunch` filled. `config.ts` reads `NEXT_PUBLIC_LAUNCH_STATE` and
falls back to `prelaunch` for any unknown value. Components receive their texts and statuses from this module; none
holds copy. `content/legal.ts` holds the four pages' titles and `body: null` until the texts are supplied, which the
legal page renders as the "Contenido pendiente" notice.

### Lead capture

- `register-lead.ts`: `registerLead(email): Promise<{ ok: true } | { ok: false; reason: "invalid" | "tooMany" | "network" }>`,
  a `fetch` `POST ${apiUrl}/customer-leads` with `Content-Type: application/json` and `{ email }`. `204` -> ok; `400`
  -> invalid; `429` -> tooMany; anything else or a thrown error -> network. It never throws.
- `lead-form-state.ts`: a `useReducer` with `idle` / `submitting` / `sent` / `failed(reason)`; submitting ignores
  further submits, so a double click sends once.
- Validation is structural: trimmed email, `type="email"` plus a simple `local@domain.tld` check and at most 254
  characters; consent checked. Messages are rendered next to their field and linked with `aria-describedby`;
  `noValidate` on the form so the messages are the website's, in Spanish.
- Honeypot: a text input named `company` inside a visually hidden, `aria-hidden` wrapper with `tabIndex={-1}` and
  `autoComplete="off"`; when it has a value the form goes straight to `sent` without calling `registerLead`.
- The API base URL is `NEXT_PUBLIC_API_URL`, falling back to `http://localhost:5199`.

### Analytics

`track(name, data?)` is the single entry point; in this version it does nothing (a provider is a later change).
`attribution.ts` reads `utm_source`, `utm_medium`, `utm_campaign`, `utm_term`, `utm_content` and `document.referrer`
once on load and keeps them in memory; `track` merges them into every event. Events: `cta_click` (`location`:
`header`, `hero`, `founding`, `final`, `footer`), `form_start` (first focus of the email field), `form_submit`
(`outcome`: `sent`, `invalid`, `tooMany`, `network`), `section_view` (`section` id, once per section, through an
`IntersectionObserver`). No cookie, no storage.

### SEO

`layout.tsx` exports `metadata` with the title, description, `metadataBase` from `NEXT_PUBLIC_SITE_URL` (fallback
`http://localhost:3001`), canonical `/`, Open Graph (`og-image.png`, `es_ES`) and Twitter `summary_large_image`, and
the icons. `sitemap.ts` lists `/` and the four legal pages; `robots.ts` allows all and points to the sitemap. `<html
lang="es">`.

### Product images

Screenshots of the backoffice (athletes list, workout editor with its catalog, mesocycle weeks) and of the app
(workouts list, workout screen) with fictitious data, captured once and committed as WebP and AVIF in two widths. The
capture is a manual step (or a local script) and is not part of the build. Every image is shown through
`product-image` (a `<picture>` with `loading="lazy"` except the hero, an alt text that starts with "Imagen
demostrativa:", and a "Imagen demostrativa" caption). The hero composes a desktop screenshot with a phone screenshot
overlaid.

### Icons

`lucide-react` for problem cards (`FileStack`, `MessageSquareText`, `TrendingUp`, `Clock`), features, steps (`ClipboardList`,
`Dumbbell`, `LineChart`), benefits (`Check`) and UI (`Menu`, `X`, `ChevronDown`). Imported per icon so only those ship.
The brand files are used for the logo, favicons and Open Graph image.

### Tests

- Component and unit (`website/test`, Vitest + RTL, Arrange / Act / Assert with one Act), modelled on the
  backoffice's: `register-lead.test.ts` (each status and a thrown error, `fetch` faked); `early-access-form.test.tsx`
  (fields, invalid email, missing consent, one request on a double click, loading, `204` confirmation, `400`, `429`,
  network, honeypot, `form_start` / `form_submit` events); `header.test.tsx` (links, menu open / close on link and
  Escape with focus back); `faq.test.tsx` (`aria-expanded`, keyboard); `features.test.tsx` and `ai.test.tsx`
  ("Próximamente" from the content); `founding-trainers.test.tsx` (badge, limit, no counter); `footer.test.tsx` (legal
  links, no social links); `launch-state.test.ts` (fallback to `prelaunch`); `attribution.test.ts` (UTM parsing);
  `legal-page.test.tsx` (pending notice).
- E2E (`website/e2e`, Playwright against `out/`, `page.route` on `**/customer-leads`): hero CTA -> form -> email ->
  confirmation, with exactly one request carrying `{ email }`; `429` shows its message; at 375 px the menu reaches the
  FAQ; at 360 px `document.documentElement.scrollWidth` equals the viewport width; `/privacidad` shows the pending
  notice; `/sitemap.xml` and `/robots.txt` are served.

## Risks / Trade-offs

- [The API endpoint is not merged yet] → The website change is applied after `add-customer-leads`; until then its
  tests intercept the call, so the website can be built and verified independently.
- [No production CORS] → Calls from the deployed site fail until the API allows the website's origin; recorded as a
  go-live blocker, outside this change.
- [Screenshots go stale as the product changes] → They are plain files under `public/screens/`, replaced without code
  changes; their alt text marks them as demonstrative.
- [An email-only form gives little context about each lead] → Accepted by the product owner; the richer form follows an
  API change.
- [Legal pages without text] → The pages render a pending notice and the site must not be published until the texts
  are supplied.

## Open Questions

- The final domain for `NEXT_PUBLIC_SITE_URL` and the hosting are decided with the deployment, outside this change.
