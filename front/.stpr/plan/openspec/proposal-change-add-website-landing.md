# Change Proposal: Add Website Landing

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-website-landing/`.

Content appendix: `.stpr/plan/openspec/proposal-change-add-website-landing.landing-spec.md` holds the product owner's
landing specification verbatim (Spanish). It is the source of every copy text, section order and visual rule. Where
this brief decides differently, this brief wins.

## Change

Add `website/`, a fourth workspace of the `front/` monorepo: a Next.js static site that is Trainera's public website.
In this version it is a one-page Spanish landing for trainers (header, hero, problem, features, how it works, athlete
experience, AI, Founding Trainers with an early-access form, FAQ, final CTA, footer) plus the legal pages, that
registers leads through `POST /customer-leads`. It does not depend on `core`, `backoffice` or `app`.

## Objective

Trainera has no public presence. The product is being built with a first set of trainers in mind, and the team needs
a page that explains what Trainera is to a trainer who does not know it, and collects the emails of the ones who want
to take part, starting now and without an account.

## Capabilities

1. **One-page landing** in the section order of the appendix (§2), with anchor navigation, a sticky header that compacts
   on scroll, a mobile hamburger menu, smooth scrolling (none under `prefers-reduced-motion`) and every CTA leading to
   the early-access form (`#acceso-anticipado`).
2. **Copy** exactly as the appendix gives it, in Spanish. The badge reads "PROGRAMA FOUNDING TRAINERS" (the appendix's
   "FUNDING" is a typo).
3. **Honest feature status.** Each feature, AI capability and "how it works" step is shown as available or with a
   "Próximamente" / "En desarrollo" label, from one content file, never hard-coded in components. Today:
   - available: athlete management (athletes, invitations, profile), workout planning (workout builder, exercise
     catalog, sets / reps / rest / effort, weeks and mesocycles, templates), the athlete app's workout list and workout
     screen, set records;
   - "Próximamente": every AI capability (none exists), training feedback, progress charts and statistics, the rest
     timer, the calendar.
     Confirmed by the product owner on 2026-11-11.
4. **Product images** for the hero, the features and the steps: real screenshots of the backoffice and the athlete
   app, captured with fictitious demo data, exported as WebP/AVIF and labelled "Imagen demostrativa" in their alt text
   and caption. No stock photos of people lifting weights.
5. **Early-access form (email only).** Fields: email (required) and the privacy-policy checkbox (required, not sent).
   It validates the email format in the browser, shows a loading state, blocks double submission, and sends
   `{ email }`. On `204` it replaces the form with the appendix's confirmation message; the same message shows for an
   email already registered, since the API answers it the same way. A hidden honeypot field silently drops bot
   submissions. The richer form of the appendix (§9) is deferred until the API accepts those fields.
6. **Launch state switch.** A single setting (`NEXT_PUBLIC_LAUNCH_STATE`, `prelaunch` by default) selects the CTA
   labels and which blocks show, from the content file. Only the pre-launch state (appendix §15, state A) is built;
   the switch exists so state B is added later without touching the components.
7. **Analytics event layer, no provider.** CTA clicks, form start, form submit (success / error) and section views
   are emitted as named events through one `track(event, data)` function that does nothing in this version. UTM
   parameters and the referrer are read on landing so the events can carry them once a provider is chosen. No
   cookies are set, so no cookie banner is needed yet.
8. **SEO:** the appendix's title and meta description, Open Graph and Twitter card with an image built from the brand
   logo, favicons from `brand/`, a single `h1` and ordered headings, `sitemap.xml` and `robots.txt`, canonical URLs
   from `NEXT_PUBLIC_SITE_URL`.
9. **Legal pages** `/aviso-legal`, `/privacidad`, `/cookies`, `/condiciones`, linked from the footer and the form's
   checkbox. Their text is supplied by the product owner (see Notes); none is invented.
10. **Responsive, accessible and fast:** mobile-first with the appendix's breakpoints, no horizontal scroll, 44 px touch
    targets, visible focus, keyboard navigation (menu, FAQ accordion, form), WCAG AA contrast, lazy-loaded images
    below the fold.

## Fronts And Roles

- website -> lands here: yes -> roles: none (public, anonymous) -> a trainer visiting from social media or a link
  reads the landing and leaves their email.
- backoffice -> lands here: no -> unchanged.
- app -> lands here: no -> unchanged.

No sign-in, no session, no role. The website never shows or asks for anything about users of the product.

## Classification

The website is not built with the Clean Architecture layers of `core` and the fronts: it holds no domain, no use
cases and no container. It is a static content site with one API call.

- Workspace -> new pnpm workspace -> `front/website` (package `website`), added to `pnpm-workspace.yaml` and to the
  Turborepo pipeline (`typecheck`, `lint`, `test`, `test:e2e`, `build`)
- Routes -> route -> `website/src/app/page.tsx` (landing), `website/src/app/{aviso-legal,privacidad,cookies,condiciones}/page.tsx`,
  `website/src/app/sitemap.ts`, `website/src/app/robots.ts`, `website/src/app/layout.tsx` (fonts, metadata)
- Sections -> section components -> `website/src/sections/` (one per landing section: `header`, `hero`, `problem`,
  `features`, `how-it-works`, `athlete-experience`, `ai`, `founding-trainers`, `faq`, `final-cta`, `footer`)
- UI pieces -> components -> `website/src/components/` (button, section, card, badge, status label, accordion,
  mobile menu, product image frame)
- Content and launch state -> content module -> `website/src/content/landing.ts` (every copy text, feature statuses,
  FAQ, nav items, per-state CTAs)
- Lead submission -> client module -> `website/src/leads/register-lead.ts` (`fetch` to `POST /customer-leads`, result as
  `{ ok: true } | { ok: false; reason: "invalid" | "tooMany" | "network" }`) and the form component in
  `founding-trainers`
- Analytics -> client module -> `website/src/analytics/track.ts` (no-op `track`, UTM / referrer capture)
- Styles and tokens -> CSS -> `website/src/styles/tokens.css` (the appendix's colour, radius, type and spacing tokens
  as CSS custom properties) and CSS Modules per component
- Brand and product images -> static assets -> `website/public/brand/` (copied from `brand/trainera-logo/`) and
  `website/public/screens/`

## Canonical Template Mapping

- Workspace, Next config and static export -> `backoffice/package.json`, `backoffice/next.config.ts` -> the monorepo's
  only Next.js app: same Next version, `output: "export"`, same lint / format / typecheck setup. It is a reference for
  tooling only; no backoffice code, style or component is reused or imported.
- Fonts and colour values -> `backoffice/src/app/layout.tsx` (`next/font/google`, Plus Jakarta Sans + Inter) and the
  palette in `backoffice/src/presentation/common/theme.ts` -> same fonts and the same hex values, re-declared in the
  website's own `tokens.css` (the appendix §13 table matches the theme exactly).
- Component tests -> `backoffice/test/` (Vitest + React Testing Library + jsdom) -> same tier and Arrange / Act /
  Assert convention.
- E2E -> `backoffice/e2e/*.spec.ts` with `page.route` interception -> same tier for the form flow.
- Sections, content module, lead client, tracking -> `pending`: no reference exists in the repo; this change
  establishes them for the website only.

`.stpr/template/` is not updated with website files.

## Screens And Navigation

- website -> `/` -> landing -> from any link to the site -> anchors `#funcionalidades`, `#como-funciona`,
  `#para-entrenadores`, `#faq`, `#acceso-anticipado`; after a successful submission the visitor stays on the page with
  the confirmation in place of the form.
- website -> `/aviso-legal`, `/privacidad`, `/cookies`, `/condiciones` -> legal pages -> from the footer and the form's
  checkbox label -> back to `/` through the header logo.
- No login, signup or product links in the pre-launch state.

## API Contract

- `POST /customer-leads` -> **not in `core/openapi/v1.json`, and must not be**: the website calls it directly with
  `fetch`, without `core` or generated types. Request `{ "email": string }`; `204 No Content` for a new or an already
  known email; `400` (validation problem) for a missing or invalid email; `429` when the client sends more than the
  rate limit (5 per minute by default).
- Provided by the API change `add-customer-leads` (in `api/`, in progress). The website change must not be applied
  before that endpoint is merged.
- Base URL from `NEXT_PUBLIC_API_URL`, falling back to `http://localhost:5199`, as in the other fronts.

## Held State

- Early-access form -> email, consent, honeypot, status (`idle`, `submitting`, `sent`, `failed` with a reason) -> changed
  by typing and submitting -> resets on page reload; `sent` stays until reload so the form cannot be sent twice.
- Header -> compact flag and mobile menu open flag -> changed by scrolling and the hamburger button -> resets on reload;
  the menu closes when a link in it is followed.
- Landing attribution -> UTM parameters and referrer read on load -> kept in memory for the events -> resets on reload.

No browser storage, no cookies.

## Forms And Structural Validation

- Early-access form -> email -> required; email format; at most 254 characters (the API's `Email` limit).
- Early-access form -> privacy consent -> required (checked); not sent to the API.
- Early-access form -> honeypot -> must stay empty; a filled honeypot shows the confirmation without sending anything.

The email's normalisation and uniqueness are the API's business; the website adds no rule beyond structure.

## Errors And Copy

- Invalid email (client) -> no request -> "Introduce un email válido."
- Consent not checked -> no request -> "Debes aceptar la política de privacidad."
- `400` from the API -> `invalid` -> "Introduce un email válido."
- `429` from the API -> `tooMany` -> "Has enviado demasiadas solicitudes. Inténtalo de nuevo en unos minutos."
- No response or `5xx` -> `network` -> "No hemos podido enviar tu solicitud. Inténtalo de nuevo."
- Success (or a known email) -> confirmation text of the appendix §9.

All copy is Spanish and lives in `website/src/content/landing.ts`. The website is Spanish only in this version; the
`es` / `en` rule of the fronts does not apply to it. No error code is added to `core`'s HTTP error mapper (the website
does not use it).

## Platform Capabilities And Dependencies

- `next`, `react`, `react-dom` -> `website` -> same versions as the backoffice, kept in step.
- `lucide-react` -> `website` -> icons for the problem cards, feature blocks, steps and UI (menu, accordion, check). The
  brand folder only holds the logo, symbol and favicons, which the website uses for its own identity; no existing
  dependency of the website covers general icons, and the backoffice's MUI icons are not shared.
- Test tooling (`vitest`, `@testing-library/react`, `jsdom`, `@playwright/test`) -> `website` -> as in the backoffice.

No UI component library: plain components with CSS Modules and the token custom properties keep the page light.

## Out Of Scope

- The full Founding Trainers form (name, business, athletes, specialty, tools, message), lead source, UTM storage and
  the consent timestamp on the server: each needs the API to accept and store them first. That API change comes
  first; the website form grows after it.
- An analytics provider, cookie banner and consent management. Only the no-op event layer ships.
- The product-available state (state B): pricing, plans, testimonials, signup / login links.
- Social profiles and a contact email: the footer shows neither until they exist. "Contacto" in the footer links to the
  early-access form.
- Deployment, hosting, domain and the API's production CORS policy (today CORS is enabled in Development only). They
  are infra and API changes and block going live, not this change.
- Video, product animations, blog, a separate Founding Trainers page, calculators (appendix §16 "Mejoras posteriores").
- An English version.
- Must not change: `core`, `backoffice`, `app`, the API, `brand/`.

## Notes

- **Founding Trainers limit (decided).** The programme takes ten trainers; the section states it as a condition of
  the programme ("Plazas limitadas a 10 entrenadores"), never as a countdown or a "plazas restantes" counter.
- **Invariants and AGENTS.md (approved by the product owner on 2026-11-11).** `STPR_INVARIANTS.md` states it covers the
  three workspaces `core`, `backoffice` and `app`, and that the fronts share only `core`. The website is a fourth
  workspace that by request must not use `core`. This change therefore adds, with the product owner's approval:
  - in `AGENTS.md`: the `website` row in the workspace table and its commands;
  - in `STPR_INVARIANTS.md`: a short section stating that `website` is a static public site outside the layered
    architecture, never imports `core`, `backoffice` or `app` (and none of them imports it), calls only public API
    endpoints with `fetch`, ships no server runtime (`output: "export"`), and holds no user data beyond what a form
    sends;
  - an ESLint `no-restricted-imports` rule in `website` that forbids `@weight-tracker/core` and the other workspaces.
- **Email-only form (decided).** The appendix's §9 form fields and §14 lead source / UTM capture are deferred, because
  `POST /customer-leads` takes only `email`. The consent checkbox is still required on the page, and the privacy page
  must describe what is stored (the email and its registration time).
- **Analytics (decided).** Deferred to a later change; this one ships the named events behind a no-op `track`, so
  adding a provider later touches one module.
- **Feature status (confirmed).** The available / "Próximamente" table in Capabilities §3 reflects the repository on
  2026-11-11: no AI code exists, the backoffice's "Feedback de entrenamientos" and "Calendario" menu entries have no
  screen, the app's progress tab is empty, and there is no rest timer.
- **Legal texts (blocking for going live).** The four legal pages need text from the product owner (or their legal
  adviser). Until it is supplied, the pages render a clearly marked "Contenido pendiente" placeholder, and the site
  must not be published.
- **Screenshots.** Captured once from the running backoffice and app against fictitious data (for example the
  backoffice with its API mocked as in the E2E tests), then committed as optimised images. The capture method is not
  part of the shipped site. No real user data ever appears.
- **Brand assets.** `brand/trainera-logo/svg/trainera-logo-light.svg` (header, light backgrounds),
  `trainera-logo-mono-white.svg` (AI section, final CTA, footer on dark), `png/favicon-32.png`, `favicon-192.png` and
  `app-icon-ios-1024.png` (apple-touch icon), and `trainera-logo-light-1200.png` as the base of the Open Graph image.
  They are copied into `website/public/brand/`, not linked from outside the workspace.
- **Dev port.** `next dev` runs on port 3001 so it does not clash with the backoffice on 3000.
- **Specs.** New capabilities `website-landing` (sections, navigation, copy, feature status, launch state, SEO,
  responsive and accessibility rules) and `website-lead-capture` (the form, its validation, errors, honeypot,
  confirmation).
- **Verification tiers:**
  - core unit: none.
  - website component (Vitest + RTL): the form (invalid email and missing consent send nothing; loading state
    disables the button; `204` shows the confirmation; `400`, `429` and network errors show their messages; a filled
    honeypot sends nothing; one request for a double click); `register-lead` (each status mapped); the launch-state
    switch (CTA labels from the setting); feature status labels from the content; the header menu (opens, closes on a
    link, keyboard); the FAQ accordion (keyboard, `aria-expanded`); `track` is called with the named events.
  - website E2E (Playwright, `page.route` on `POST /customer-leads`): a visitor goes from the hero CTA to the form,
    sends their email and sees the confirmation; a `429` shows its message; the mobile menu reaches the FAQ; the page
    has no horizontal scroll at 360 px.
  - No backoffice or app tests change.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`, with `website` in each.
- No deviations from the mapped references are allowed beyond the documented invariant amendment.
