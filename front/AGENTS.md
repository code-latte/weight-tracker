# Development Guidelines

This file is the operational guide for working in `front/`. Keep it focused on precedence, workflow, verification,
and maintenance. The front is maintained with the STPR workflow. Architectural rules live in
`.stpr/template/STPR_INVARIANTS.md`.

## Project Overview

`front/` is a pnpm + Turborepo monorepo with four workspaces:

| Workspace     | Package                | Stack                                     | Users                 |
| ------------- | ---------------------- | ----------------------------------------- | --------------------- |
| `core/`       | `@weight-tracker/core` | Plain TypeScript                          | shared by both fronts |
| `backoffice/` | `backoffice`           | Next.js static SPA + MUI                  | `ADMIN`, `TRAINER`    |
| `app/`        | `app`                  | Expo (managed / CNG) + React Native Paper | athletes (`USER`)     |
| `website/`    | `website`              | Next.js static site, no UI library        | public visitors       |

`core`, `backoffice` and `app` follow a layered Clean Architecture with an anemic domain generated from the API contract, and use **STPR**
as their development workflow. `website` stands outside those layers and depends on no other workspace (see
`.stpr/template/STPR_INVARIANTS.md`, section 4.4). The front is independent from the API's STPR in `api/.stpr/`. All modified code must align with:

1. `.stpr/template/STPR_INVARIANTS.md`
2. `AGENTS.md`

## Rule Precedence

Use these sources with this precedence when there is overlap or tension:

1. **`.stpr/template/STPR_INVARIANTS.md`**: non-negotiable technical contract
2. **`front/AGENTS.md`**: operational guidance for maintaining and extending the front
3. **`backoffice/AGENTS.md`, `app/AGENTS.md`**: pointers back here. The backoffice file also holds a block that
   `next dev` writes and re-adds between `<!-- BEGIN:nextjs-agent-rules -->` markers; leave it there and commit it
   with your work. Its content is already reflected in the invariants (section 4.2).

## Scope Of This Guide

This file is intentionally concise. It captures project-level rules, workflow, testing, and maintenance guidance.
Detailed architectural rules, classification and reference examples belong in `.stpr/template/`.

## Development Guidelines

### Adding a New API Operation

1. Check that the endpoint exists in `core/openapi/v1.json`. If it does not, it is an API change first, planned
   under `api/.stpr/`, then a snapshot refresh here.
2. Add or reuse the domain alias, the repository contract method and its implementation, and the use case in `core`,
   following the reference selected through `.stpr/template/README.md`.
3. Register them in the `container.ts` of every front that uses them.

### Adding a New Screen

1. Add the route under `src/app` (routing only), the view under `src/presentation/<area>/views`, and its components
   and state hooks beside it.
2. Model them on the closest screen **in the same front**. The other front is never a UI reference.
3. Classify each component as atom, molecule or organism and place it in `components/<level>`; reuse existing atoms
   and molecules before creating new ones.
4. Add every string to `src/i18n/es` and `src/i18n/en`.

### Changing The API Contract

- Run `pnpm --filter @weight-tracker/core openapi:refresh` with the API running, then `openapi:generate`; review the diff of `core/openapi/v1.json` and the regenerated
  types, and commit both as a step of their own.
- Never edit `core/src/contract/generated` by hand.

### Fixing Bugs

- Identify the correct place: route, view, component or state hook (presentation), use case (application),
  repository or error mapper (core infrastructure), or adapter and wiring (front infrastructure).
- If the bug is a wrong business outcome, it belongs to the API, not here.
- Fix the issue where it belongs and run the affected tests and, when possible, the full suite.

### Environment and Configuration

- The API base URL comes from `NEXT_PUBLIC_API_URL` (backoffice) and `EXPO_PUBLIC_API_URL` (app). The fallback is
  `http://localhost:5199`, the port of the API's VS Code **Debug API** configuration.
- Never hardcode URLs or keys in code outside those fallbacks.
- Secrets must not be committed. Anything prefixed `NEXT_PUBLIC_` or `EXPO_PUBLIC_` ships to the client and is
  never secret.

## Testing Guidelines

- **No real API.** Every tier mocks it at the boundary that fits:
  - core unit tests fake the ports (`HttpClientInterface`, repository contracts, `KeyValueStoreInterface`);
  - component tests replace use cases through the container with fakes returning chosen `Result` values;
  - backoffice E2E intercepts API calls with Playwright's `page.route`.
- **Core** (`core/test`, Vitest): every use case, every repository implementation (success and every error code)
  and every branch of the HTTP error mapper.
- **Backoffice** (`backoffice/test`, Vitest + React Testing Library + jsdom; `backoffice/e2e`, Playwright): a
  component test per view, and an E2E per user flow the plan names.
- **Website** (`website/test`, Vitest + React Testing Library + jsdom; `website/e2e`, Playwright): a component test
  per section with behaviour and for the lead form, and an E2E per user flow the plan names.
- **App** (`app/test`, Jest + `jest-expo` + React Native Testing Library): a component test per view. App E2E is
  a known gap, accepted for now.
- **Roles.** When a view differs per role, cover every role branch and assert what is hidden, not only what is shown.
- **Translations.** Each front has a parity test that fails when a key exists in only one language. Component tests
  render in `es` unless the test is about language.
- **Location.** Tests live outside `src`, mirroring its structure. In the app this is required: Expo Router treats
  every file under `src/app` as a route.
- **Template parity.** Before adding a test, open the closest existing test of that tier and replicate its structure.
- **Arrange / Act / Assert.** Every test body is split into labelled phases. See
  `.stpr/template/STPR_INVARIANTS.md` for the exact convention.
- **Determinism.** Tests never depend on another test's state or order.

## Running And Verifying Locally

Requires Node `^22.13.0 || >=24` (jsdom's floor) and pnpm (version pinned in `package.json`'s `packageManager`).

```bash
pnpm install
pnpm dev:backoffice   # next dev
pnpm dev:app          # expo start
pnpm dev:website      # next dev on port 3001
pnpm typecheck
pnpm lint             # ESLint + Prettier check
pnpm format           # Prettier write
pnpm test             # unit + component tiers
pnpm test:e2e         # backoffice Playwright, builds first; required when a plan names an E2E flow
pnpm build
```

**Done means** `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`, plus `pnpm test:e2e` when the plan
names an E2E flow. The first E2E run on a machine needs `pnpm --filter backoffice exec playwright install chromium`.

### Backoffice (Next.js)

- **This is not the Next.js you know.** Next 16 has breaking changes in APIs, conventions and file structure.
  Before writing Next code, read the relevant guide in `backoffice/node_modules/next/dist/docs/` (in this monorepo
  the `next` package may not resolve from `front/`; read it from `backoffice/`). Heed deprecation notices.

### App (Expo)

- **Expo has changed. Do not trust your training data.** Before writing code that touches an Expo, EAS or React
  Native API:
  1. Read the major version of `expo` in `app/package.json`.
  2. Fetch the matching versioned docs: `https://docs.expo.dev/versions/v<major>.0.0/`.
  3. For anything else, fetch `https://docs.expo.dev/llms.txt`, an index of all Expo docs with corrections to
     common LLM misconceptions, and follow its links to the page you need.
- Commands, run from `app/`. This is a pnpm workspace, so use `pnpm exec` where the Expo docs say `npx`:

  ```bash
  pnpm exec expo install <package>   # ALWAYS instead of pnpm add: resolves SDK-compatible versions
  pnpm exec expo start               # dev server
  pnpm dlx expo-doctor               # diagnose dependency and config issues
  pnpm exec expo install --fix       # fix incompatible package versions
  ```

- Expo Go only includes its bundled native modules. After adding a library with native code, the app needs a
  development build: `pnpm exec expo run:ios|android` locally, or `eas build --profile development`.
- **EAS** builds, signs and submits in the cloud (`eas build`, `eas submit`) and ships over-the-air updates
  (`eas update`), with no local Xcode or Android Studio. Run it as `pnpm dlx eas-cli@latest <command>` wherever the
  docs say `eas`. Docs: `https://docs.expo.dev/eas/index.md`.

`.stpr/template/` holds curated copies of canonical files under their `front/`-relative paths. Check they have not
drifted, from `front/`:

```bash
for f in $(cd .stpr/template && find core backoffice app -type f 2>/dev/null); do
  cmp -s ".stpr/template/$f" "$f" || echo "DRIFT: $f"
done
```

## Best Practices & Maintenance Guidelines

### Working process (for non-trivial changes)

1. **Understand the request** and which fronts and roles it touches (see `.stpr/template/` for reference).
2. **Plan** with OpenSpec (see `.stpr/README.md`): fill `.stpr/plan/openspec/proposal-change-template.md` as
   `.stpr/plan/openspec/proposal-change-<change-slug>.md`, then `/opsx:propose`; artifacts go to
   `openspec/changes/<change-slug>/`.

   If the API must change too, that is a separate plan in `api/`.

3. **Implement** by following the STPR references and invariants first; use this document only for project-level guidance.
4. **Review before considering done**: use the checklist below; run `.stpr/review/CHECKLIST_REVISION.md` if the
   change is large.

### Quality checklist (before considering a task complete)

- [ ] The change follows `.stpr/template/STPR_INVARIANTS.md` without local exceptions.
- [ ] Structure, naming and wiring match `.stpr/template/`.
- [ ] No business rule was added to the front; form validation is structural only.
- [ ] No domain type was hand-written, and `core/src/contract/generated` was not edited by hand.
- [ ] Every API interaction goes through a use case returning `Result`; nothing throws except `FetchHttpClient`
      and the wiring guard in `useContainer`.
- [ ] Every new error code is mapped in the HTTP error mapper and translated in `es` and `en`.
- [ ] Components sit in their atomic level folder, imports flow downward, and only atoms import the UI library's widgets.
- [ ] `src/app` holds routes only, and presentation never imports `@weight-tracker/core/infrastructure`.
- [ ] New repositories, adapters and use cases are registered in each affected front's `container.ts`.
- [ ] `backoffice`, `app` and `website` share no UI and do not import each other; `website` imports no workspace.
- [ ] The backoffice is still a static export with no server-side code.
- [ ] No file was hand-edited under `app/ios` or `app/android`.
- [ ] Every user-visible string is translated in `es` and `en`.
- [ ] New behaviour is covered by the tiers the plan names, with the API mocked.
- [ ] Every new test is split into labelled `// Arrange` / `// Act` / `// Assert` phases, with exactly one Act.
- [ ] `.stpr/template/` is still byte-identical to its counterparts.
- [ ] `pnpm typecheck && pnpm lint && pnpm test` passes.

### When this guide applies

For **trivial changes** (typos, single-line fixes, config tweaks), you can edit directly without following a full
change workflow. Use this guide for anything that touches architecture, a new screen, a new API operation, or test strategy.

## Resources

| What you need                            | Where to look                                           |
| ---------------------------------------- | ------------------------------------------------------- |
| STPR method entry point                  | `.stpr/README.md`                                       |
| Code structure and patterns              | `.stpr/template/`                                       |
| STPR invariants and non-negotiable rules | `.stpr/template/STPR_INVARIANTS.md`                     |
| Classification and reference selection   | `.stpr/template/README.md`                              |
| OpenSpec change brief template           | `.stpr/plan/openspec/proposal-change-template.md`       |
| OpenSpec configuration                   | `openspec/config.yaml`                                  |
| API contract                             | `core/openapi/v1.json`                                  |
| Error codes                              | the HTTP error mapper in `core/src/infrastructure/http` |
| Manual review checklist                  | `.stpr/review/CHECKLIST_REVISION.md`                    |

Following this guide keeps the front consistent with the template and its invariants.
