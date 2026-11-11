# Tasks

Core, backoffice, app: no change. Depends on the API change `add-customer-leads` being merged.

## 1. Contract amendment

- [x] 1.1 Amend `.stpr/template/STPR_INVARIANTS.md`: list `website` in the intro (line 5), add to §2's dependency rule that `website` never imports nor is imported by `core`, `backoffice` or `app`, and add §4.4 "Website: Next.js static site" as `design.md` describes; verify the wording matches the approved scope and `pnpm lint` (Prettier) passes
- [x] 1.2 Update `AGENTS.md`: `website` row in the workspace table, `pnpm dev:website` in the commands, website tiers in "Testing Guidelines", `website` in the "share no UI" checklist line; verify Prettier passes

## 2. Workspace scaffolding

- [x] 2.1 Read the Next 16 guides for static export, metadata, `sitemap.ts`/`robots.ts` and `next/font` in `backoffice/node_modules/next/dist/docs/`, and note any deviation from `design.md` in the design before writing code; verify the notes are added (or that none were needed)
- [x] 2.2 Create `front/website` with `package.json` (scripts and versions as `design.md`), `next.config.ts`, `tsconfig.json`, `eslint.config.mjs` (with the `no-restricted-imports` rule), `vitest.config.mts`, `test/setup.ts`, `playwright.config.ts` (port 4174), `.gitignore`, `AGENTS.md` pointer; add `website` to `pnpm-workspace.yaml` and `dev:website` to the root `package.json`; run `pnpm install`; verify `pnpm --filter website typecheck`, `lint` and `build` pass on an empty page
- [x] 2.3 Add a lint fixture check: an import of `@weight-tracker/core` in a website file fails `pnpm --filter website lint` (then remove it); verify the rule fires

## 3. Foundations

- [x] 3.1 Add `src/styles/tokens.css` and `globals.css` (tokens, type scale, breakpoints, reduced motion, focus styles) and `src/app/layout.tsx` with the fonts through `next/font/google`, `lang="es"` and the metadata (title, description, `metadataBase`, canonical, Open Graph, Twitter, icons); verify `pnpm --filter website build` emits them in `out/index.html`
- [x] 3.2 Copy the brand files into `public/brand/` (logo light and mono white, symbol, favicons, apple-touch icon) and produce `og-image.png` 1200×630 from `trainera-logo-light-1200.png`; verify they are served from `out/`
- [x] 3.3 Add `src/config.ts` (API URL, site URL, launch state with `prelaunch` fallback) with `test/config.test.ts`; verify the test passes
- [x] 3.4 Add `src/content/landing.ts` and `src/content/legal.ts` with every text of the appendix, the confirmed feature statuses (AI, feedback, progress charts, rest timer and calendar "soon"), the "PROGRAMA FOUNDING TRAINERS" badge, the ten-trainer limit, the FAQ and the per-state CTAs; verify typecheck passes
- [x] 3.5 Add `src/analytics/attribution.ts` and `track.ts` (no-op, merges attribution) with `test/attribution.test.ts`; verify the test passes

## 4. Components and sections

- [x] 4.1 Add the shared components (`button`, `section`, `card`, `badge`, `status-label`, `accordion`, `mobile-menu`, `product-image`, `legal-page`) with CSS Modules; add `test/accordion.test.tsx` (`aria-expanded`, keyboard) and `test/legal-page.test.tsx` (pending notice); verify they pass
- [x] 4.2 Add the `header` section (sticky, compact on scroll through a sentinel, links, orange CTA, mobile menu) with `test/header.test.tsx` (links and anchors, menu closes on a link and on Escape with focus back, `cta_click` with `header`); verify it passes
- [x] 4.3 Add the `hero`, `problem`, `how-it-works` and `athlete-experience` sections from the content, with the hero image composition and lazy images below the fold; add a test that the hero is the only `h1` and its CTAs point to `#acceso-anticipado` and `#funcionalidades`; verify it passes
- [x] 4.4 Add the `features` and `ai` sections with their status labels; add `test/features.test.tsx` and `test/ai.test.tsx` (available features without label, "Próximamente" on every AI capability, feedback, charts, timer; "La IA propone. El entrenador decide."); verify they pass
- [x] 4.5 Add the `faq`, `final-cta` and `footer` sections; add `test/faq.test.tsx` (questions, pricing answer without amount) and `test/footer.test.tsx` (legal links, "Contacto" to the form, no social links or email); verify they pass
- [x] 4.6 Add the section-view observer emitting `section_view` once per section; cover it in a test with a faked `IntersectionObserver`; verify it passes

## 5. Lead capture

- [x] 5.1 Add `src/leads/register-lead.ts` with `test/register-lead.test.ts` (`204`, `400`, `429`, `500`, thrown error, request URL and body); verify it passes
- [x] 5.2 Add the `founding-trainers` section with the badge, limit, benefits and the early-access form (`lead-form-state.ts`, validation, honeypot, loading, confirmation, errors, `form_start` / `form_submit` events); add `test/founding-trainers.test.tsx` and `test/early-access-form.test.tsx` covering every `website-lead-capture` scenario; verify they pass

## 6. Pages, SEO and images

- [x] 6.1 Compose `src/app/page.tsx` from the sections in order, add the four legal routes, `sitemap.ts` and `robots.ts`; verify `pnpm --filter website build` emits `/`, the four pages, `sitemap.xml` and `robots.txt` in `out/`
- [x] 6.2 Capture the backoffice and app screenshots with fictitious data, export WebP and AVIF in two widths to `public/screens/`, and wire them through `product-image` with "Imagen demostrativa" alt texts and captions; verify no real data appears and the hero image renders on desktop and mobile

## 7. E2E

- [x] 7.1 Add `website/e2e/landing.spec.ts` modelled on `backoffice/e2e/*.spec.ts`: hero CTA to form to confirmation with exactly one `POST /customer-leads` carrying `{ email }`; `429` message; mobile menu to FAQ at 375 px; no horizontal scroll at 360 px; `/privacidad` pending notice; `sitemap.xml` and `robots.txt` served; verify `pnpm --filter website test:e2e` passes

## 8. Review and verification

- [x] 8.1 Review the website against its references (backoffice tooling configs, fonts and palette) and the amended invariants: no import of other workspaces, no copy in components, no invented links or prices, every unbuilt capability labelled "Próximamente"; verify with `grep -r "@weight-tracker" website/src` returning nothing and `git diff --stat` showing no change in `core`, `backoffice` or `app`
- [x] 8.2 Check `.stpr/template/` has not drifted with the loop in `AGENTS.md`; verify it prints no `DRIFT` line
- [x] 8.3 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`; verify it passes with `website` in every task
- [x] 8.4 Manual check in a browser at 360, 768, 1280 and 1440 px: sticky and compact header, smooth scroll (and none with reduced motion), keyboard navigation and focus, contrast of the orange CTA and the ink sections, Lighthouse accessibility and SEO scores ≥ 95; verify and note the results

## Workflow follow-up

- Before going live: legal texts supplied, hosting and domain set (`NEXT_PUBLIC_SITE_URL`), and the API's production CORS policy allowing the website's origin.
