# Invariants

This file is the front's technical contract under the STPR method.

Use this file only for non-negotiable rules. It covers the three layered workspaces of `front/` (`core`,
`backoffice`, `app`) and, through section 4.4 only, the `website` workspace, which stands outside the layered
architecture. The API has its own, independent STPR under `api/.stpr/`; nothing here applies to it and nothing there
applies here.

## 1. STPR Method Invariants

These rules describe the portable part of STPR. They should remain stable even if the stack, tool, input channel,
or concrete UI library changes.

### Skeleton

- The project must have a stable technical base before requesting non-trivial generation.
- The technical base must define a minimum structure, local environment, and verification strategy so that each
  iteration does not start from scratch.
- If the project already exists, the project itself acts as the operating skeleton.

### Template

- The template must contain real, functional, and sufficiently representative references to guide faithful generation.
- Generation must not invent structure, naming, or patterns when an applicable canonical reference already exists.
- If no canonical reference exists yet for a capability, the plan must say so explicitly and establish the pattern,
  rather than inventing one silently.

### Plan

- Work must start from an explicit plan captured in files in this project, not only in chat context.
- The plan must be sufficient to execute the work without hidden assumptions that materially affect scope, the
  affected front(s), role visibility, held state, the API contract, or test expectations.
- A change that spans the API and the front is two plans: one under `api/.stpr/work/` and one under
  `front/openspec/changes/`. The front plan names the API contract it depends on and never plans API code.
- If the plan artifacts conflict with the selected reference, these invariants, or the classification rules,
  planning must stop and request clarification before code generation.

#### Modeling

- Before generating code, every planned piece must be classified explicitly with the taxonomy in section 2
  ("Building blocks").
- The classification must be sufficient to determine the workspace, the folder, the dependencies and the reference
  that applies.
- Every planned feature must state, per front, whether it lands there and how its behaviour differs. The same
  feature often lands in both fronts with different behaviour (the backoffice defines, the app answers).
- Business rules belong to the API. The plan must not move a business rule into the front, even as a duplicate.
- Any state a piece holds in a context must be stated in the plan: what it holds, who changes it, and when it resets.

### Execution

- Execution must implement what was approved in the plan without expanding scope by its own criteria.
- Execution must respect the selected reference and the active invariants.
- Execution is considered complete when it implements what was approved in the plan, respects the selected
  reference, complies with these invariants, and passes the verification required by the change.

#### Execution Validation

- Tests validate behavior; AI does not replace final validation.
- The required verification must be executed at the correct tier for the type of change.
- If verification fails, the work is not complete.

### Review

- Final review must check architecture, layer placement, relevant coverage, and alignment with the plan.
- Review does not validate only functionality; it also validates sustainability and fidelity to the correct pattern.
- If recurrent deviations appear, improving the skeleton, template, invariants, or plan artifacts must be evaluated.

### Mandatory Stop Cases

- Implementation must stop and the plan must be clarified if it requires a business rule in the front (anything
  beyond the structural validation allowed in section 2).
- Implementation must stop and the plan must be clarified if it requires a domain type that the OpenAPI snapshot
  does not contain. The fix is an API change and a snapshot refresh, never a hand-written type.
- Implementation must stop and the plan must be clarified if it requires `backoffice` and `app` to share UI,
  styles, translations or presentation code.
- Implementation must stop and the plan must be clarified if it requires server-side rendering, server components
  with data access, server actions or route handlers in the backoffice.
- Implementation must stop and the plan must be clarified if a dependency would point outward (see the dependency
  rule below).

## 2. Architecture Invariants

The reference architecture is a layered Clean Architecture with an **anemic domain**. There is no DDD on the front:
the API owns every business rule, and the front's domain is named from the API contract.

### The Dependency Rule

| Code                        | May import                                                       |
| --------------------------- | ---------------------------------------------------------------- |
| `core/src/contract`         | nothing                                                          |
| `core/src/domain`           | `components` from `core/src/contract`, nothing else              |
| `core/src/application`      | `core/src/domain`                                                |
| `core/src/infrastructure`   | `core/src/contract`, `core/src/domain`, `core/src/application`   |
| `backoffice/src`, `app/src` | `@weight-tracker/core/domain`, `/application`, `/infrastructure` |

- `core` never imports a front, React, Next, Expo, React Native, MUI, React Native Paper, or any UI library.
- `core/src/contract` is the generated API contract. It sits beside the layers, not inside one: the domain reads only
  its schemas (`components`), and its transport shapes (`paths`, `operations`) are for infrastructure alone. It is
  not exported from `core/package.json`; fronts use the domain aliases.
- `core/src/contract`, `core/src/domain` and `core/src/application` use no platform globals (no DOM, no Node, no React Native). Their
  `tsconfig` (`lib: ["ES2022"]`, `types: []`) enforces this; do not widen it.
- `core/src/infrastructure` may use web-standard APIs present on both platforms (`fetch`, `URL`), nothing else.
- `backoffice` and `app` never import each other. They are independent applications that share only `core`.
- `website` never imports `core`, `backoffice` or `app`, and none of them imports it. It shares nothing with them.
- Inside a front, `src/presentation` imports `core/domain` and `core/application` freely, but never
  `core/infrastructure`; only `src/infrastructure` (the composition root) touches it.
- The one exception is the root providers component (`src/presentation/common/components/app-providers.tsx`),
  which imports the composition root: the built container from `src/infrastructure/container.ts` and the i18n
  instance from `src/infrastructure/i18n.ts`. No other presentation file imports from `src/infrastructure`.
- These import rules are enforced by ESLint `no-restricted-imports`, not only by review.

### Workspace layout

Layer first, then domain area. A domain area (`auth`, `workouts`, `exercises`, ...) is a folder name repeated in each
layer, not a module file.

```
core/openapi/v1.json                                 committed snapshot of the API contract
core/src/contract/generated/                         types generated from the snapshot (never edited by hand)
core/src/domain/<area>/                              named aliases of the generated types
core/src/domain/common/                              DomainError, Result
core/src/application/use-cases/<area>/               use cases
core/src/application/ports/                          repository, platform and container contracts
core/src/application/dependencies.ts                 key-to-type map of every registrable port and use case
core/src/infrastructure/repositories/                repository implementations over HttpClientInterface
core/src/infrastructure/http/                        FetchHttpClient, HTTP error mapper
core/src/infrastructure/di/                          the DI container

<front>/src/app/                                     routing only
<front>/src/presentation/<area>/{views,components,hooks,context}/
<front>/src/presentation/<area>/components/{molecules,organisms}/
<front>/src/presentation/common/{views,components,hooks,context}/
<front>/src/presentation/common/components/{atoms,molecules,organisms}/
<front>/src/infrastructure/                          platform adapters, i18n setup, container.ts
<front>/src/i18n/{es,en}/<area>.json                 translations
```

### Building blocks

Classify every planned piece as exactly one of these before writing code.

- **Entity type**: an anemic data shape. It is an alias of a generated OpenAPI type and has no behaviour.
  Lives in `core/src/domain/<area>`.
- **Repository contract**: one per domain area, the port through which use cases reach the API.
  Lives in `core/src/application/ports`.
- **Platform port**: a contract for something each platform implements differently (`KeyValueStoreInterface`,
  `NotificationServiceInterface`). Lives in `core/src/application/ports`.
- **Use case**: one operation the user can trigger. Lives in `core/src/application/use-cases/<area>`.
- **Repository implementation**: implements a repository contract over `HttpClientInterface`.
  Lives in `core/src/infrastructure/repositories`.
- **Platform adapter**: implements a platform port for one front. Lives in `<front>/src/infrastructure`.
- **Route**: a file under `<front>/src/app`. It only default-exports a view.
- **Layout**: a layout file under `<front>/src/app` (`layout.tsx` in the backoffice, `_layout.tsx` in the app). It
  renders the outlet or navigator for the routes beside it (see "Presentation" and section 4.3).
- **View**: a screen, the page of atomic design. Composes organisms, molecules and hooks, and holds the page layout
  (the template level), so no separate template folder exists. Lives in `<front>/src/presentation/<area>/views`.
- **Atom**: one themed primitive of the UI library (button, text field, text, icon, chip). It has no hooks, no use
  cases and no i18n calls: text arrives by props. Lives in `<front>/src/presentation/common/components/atoms`.
- **Molecule**: a few atoms forming one small unit (a field with its helper text, a menu row). It may hold local UI
  state, and never calls a use case or reads the container. Lives in `components/molecules`.
- **Organism**: a distinct section with behaviour (a form, a header, a drawer, a dialog, a menu). It may use state
  hooks and translate. Lives in `components/organisms`.
- **State hook**: a custom hook that owns state through the Context API (`<Name>Provider` plus `use<Name>`) and calls
  use cases. Lives in `<front>/src/presentation/<area>/{context,hooks}`. A hook that only holds one view's local
  `useState` is part of that view, not a state hook.

### Domain: anemic, generated

- The domain is named from the API contract. Entity types are generated from `core/openapi/v1.json` with
  `openapi-typescript` into `core/src/contract/generated` and never written by hand.
- Each domain area has a hand-written file that aliases the generated names into domain names, for example
  `export type Workout = components["schemas"]["WorkoutResponse"]`. Request bodies are aliased the same way.
- Domain aliases import only `components` from the contract. Routes, methods and parameters (`paths`) are transport
  details and never enter the domain.
- The domain holds no rules, no classes with behaviour and no validation.
- The snapshot is refreshed by script from the running API and committed, so contract changes appear in diffs.
  A refresh is its own reviewed step, never a side effect of another task.

### Application

- Every interaction with the API goes through a use case, including simple reads.
- A use case is a class implementing `UseCaseInterface`, with one public method `execute`.
- `execute` returns `Promise<Result<T, DomainError>>`. A use case never throws.
- Use cases receive their dependencies through the constructor and know nothing about the DI container.
- Session handling (login, refresh, logout) is use cases. Rotating the refresh token is the `RefreshSession` use case;
  no HTTP interceptor refreshes tokens.

### Infrastructure

- Repository implementations call the API through `HttpClientInterface` and return the generated domain types as
  they are. There are no DTO-to-domain mappers: the API response **is** the domain type.
- Repositories return `Promise<Result<T, DomainError>>`. They catch `HttpError` and network failures and convert
  them through the single HTTP error mapper in `core/src/infrastructure/http`, which turns a status code or
  `ProblemDetails` into a `DomainError` code. That mapper is the only place that decides error codes.
- The only `throw` in the code base is `FetchHttpClient` raising `HttpError`, always caught by the calling repository.
- Exception, for wiring mistakes only: a context hook used outside its provider (`useContainer` without
  `ContainerProvider`) throws. It is a programming error that no runtime input can trigger, it is never caught, and
  it is the only other `throw` allowed.
- Wiring happens in each front's `src/infrastructure/container.ts`, using the container from
  `core/src/infrastructure/di`. It registers core repositories, the front's platform adapters and the use cases
  under the keys of `Dependencies` (`core/src/application/dependencies.ts`). A missing or mistyped registration
  fails to compile.
- Presentation sees the container only through `ContainerInterface` (`core/src/application/ports`) and the
  `useContainer` hook.

### Presentation

- `src/app` holds routing only. A route file imports one view and default-exports it; it holds no logic, no state
  and no UI of its own.
- A backoffice layout file renders only the root providers component around the outlet: the document shell plus
  `children`. App layouts, which also hold the auth gate and the navigators, follow section 4.3 ("Routing").
- Presentation reaches use cases through the container, never repositories, the HTTP client or platform adapters.
- State is held in Context API providers exposed through custom hooks. No other state library is used.
- Components follow atomic design (see "Building blocks"). Imports flow downward only: view, organism, molecule,
  atom. A lower level never imports a higher one.
- The UI library is raw material. A widget used by two or more component or view files is wrapped by an atom (MUI
  `Button`, `TextField`; Paper `Button`, `TextInput`), and molecules, organisms and views import the atom. A
  structural widget or icon used by a single component (`Drawer`, `AppBar`, `Dialog`, `InputAdornment`, an icon)
  may be imported there directly; it becomes an atom when a second file uses it. Layout primitives (`Box`, `Stack`, `Grid`, `Container`, `View`,
  `Portal`, `KeyboardAvoidingView`) may be imported at any level.
- Atoms live only in `common/components/atoms`. A molecule starts in its area and moves to `common` when a second
  area needs it. An organism stays in its area unless two areas share it.
- Providers and gates (`<Name>Provider`, `RequireSession`, `AppProviders`) are composition, not atomic components.
  They stay at the root of `components/`, outside the three level folders, and may import the library's theme and
  cache providers (`ThemeProvider`, `CssBaseline`, `AppRouterCacheProvider`, `PaperProvider`).
- A component file belongs to exactly one level; a file that mixes levels is split.
- Presentation turns a failed `Result` into UI. It never uses `try/catch` for flow control.
- Session refresh is reactive: when a use case fails with the unauthorized code, the session hook calls
  `RefreshSession` once and retries the original call. If the refresh fails, the session ends.

### Validation

- The API owns business validation. The front repeats none of it.
- Forms validate structure only (required, format, maximum length) to give fast feedback. Limits mirror the API's
  declared limits. This validation lives in presentation.

### Session storage

- Tokens are stored through `KeyValueStoreInterface`.
- The backoffice adapter uses `localStorage`; the app adapter uses `expo-sqlite`. Both are accepted decisions:
  `localStorage` is readable by any script injected through XSS, and SQLite data is not encrypted on the device.

### Product split

- `backoffice` serves `ADMIN` and `TRAINER`. `app` serves athletes (`USER`).
- A feature usually lands in both fronts with different behaviour. Each front implements its own presentation for
  it; use cases and repositories in `core` are shared when the operation is the same.

### Internationalisation

- No user-visible string is hard-coded. Every string comes from a translation key.
- Spanish (`es`) is the default language and English (`en`) the fallback. Both are required from the first key.
- The initial language is the one the user chose on this device, when one is stored; otherwise it is detected from
  the device or browser, falling back to `es`.
- Keys are namespaced by domain area; shared strings live in `common`. Each front owns its own translations.
- `DomainError` codes are translated in presentation through `errors.<code>`.

### Naming and exports

- File names are kebab-case everywhere, including components (`login-form.tsx`) and hooks (`use-session.ts`).
- Each file default-exports its main symbol. Barrel files re-export by name:
  `export { default as LoginUseCase } from "./login-use-case";`
- Allowed exceptions, which use named exports:
  - small helper files that are a family of related exports (`result.ts` with `Result`, `ok`, `fail`);
  - files whose exports are type aliases (`domain-error-code.ts`, the domain alias files). TypeScript rejects
    `export default` of a type alias under `verbatimModuleSyntax` (TS1284). Interfaces are not affected and are
    default-exported.
- Contracts carry the `Interface` suffix: `UseCaseInterface`, `HttpClientInterface`, `AuthRepositoryInterface`.
  Implementations take the plain name (`AuthRepository implements AuthRepositoryInterface`). Data shapes take no
  suffix (`Workout`).
- Use case files end in `-use-case.ts`, repository files in `-repository.ts`, contracts in `-interface.ts`.
- React components and their types are PascalCase; hooks are `use<Name>`.

## 3. Testing Invariants

- The plan must specify the verification tiers required for the change.
- No test calls the real API. The API is replaced by fakes at the boundary that fits the tier.
- Every use case has a unit test, with its repository contract or platform port faked.
- Every repository implementation has a unit test with a faked `HttpClientInterface`, covering success and every
  error code it can produce.
- The HTTP error mapper is unit tested for every status it maps.
- Every view has a component test. Use cases are replaced through the container with fakes returning chosen
  `Result` values; the test asserts what the user sees for success and for each failure.
- When a view behaves differently per role, every role branch is covered, asserting what is hidden as well as what
  is shown.
- Every translation key exists in both `es` and `en`. A parity test per front enforces it.
- Component tests render in `es` unless the test is about language.
- Atoms need no test of their own unless they add behaviour. A molecule or organism with branches or behaviour has a
  component test beside the closest existing one; otherwise the view test covers it.
- Tests must not depend on the order or the residue of other tests.
- Missing tests required by the change invalidate completion of execution.

### Arrange / Act / Assert

Every test body is split into labelled phases, separated by a blank line:

```ts
it("returns the unauthorized code when the credentials are rejected", async () => {
  // Arrange
  const repository = new FakeAuthRepository({ login: fail(new DomainError("UNAUTHORIZED", "")) });
  const useCase = new LoginUseCase(repository, new FakeKeyValueStore());

  // Act
  const result = await useCase.execute({ email: "athlete@example.com", password: "wrong" });

  // Assert
  expect(result).toEqual(fail(expect.objectContaining({ code: "UNAUTHORIZED" })));
});
```

- `// Act` marks exactly one statement: the behaviour under test. If the test needs two acts, it is testing two
  things and should be split.
- When constructing the subject _is_ the behaviour under test, use a single `// Arrange & Act` label.
- The behaviour named in the test name belongs in the Act phase, not buried inside an assertion.
- In component tests, rendering is Arrange and the user interaction is the Act. A test that only checks the
  initial render uses `// Arrange & Act`.
- Fake setup is Arrange; assertions on calls a fake received are Assert.

### Tiers

| Workspace    | Tier      | Tooling                                             | Location                                      |
| ------------ | --------- | --------------------------------------------------- | --------------------------------------------- |
| `core`       | unit      | Vitest                                              | `core/test`, mirroring `core/src`             |
| `backoffice` | component | Vitest, React Testing Library, jsdom                | `backoffice/test`, mirroring `backoffice/src` |
| `backoffice` | E2E       | Playwright, API calls intercepted with `page.route` | `backoffice/e2e`                              |
| `app`        | component | Jest, `jest-expo`, React Native Testing Library     | `app/test`, mirroring `app/src`               |
| `app`        | E2E       | none yet: a known, accepted gap                     | —                                             |

Tests live outside `src`. In the app this is also required, because Expo Router treats every file under
`src/app` as a route.

## 4. Stack Invariants

### 4.1 Core: TypeScript

- Plain TypeScript, consumed as source through the subpath exports in `core/package.json`; there is no build step.
- No runtime dependencies beyond what the DI container and generated types need. A new runtime dependency in
  `core` must run unchanged on Hermes and in the browser, and the plan must name it.
- The DI container uses no decorators and no `reflect-metadata`. Registrations are typed against `Dependencies`, so a
  missing or mistyped registration fails to compile and resolving can never fail at runtime.
- `core/src/contract/generated` is produced by `openapi-typescript` from `core/openapi/v1.json` and never edited by
  hand.

### 4.2 Backoffice: Next.js + MUI

- **This is not the Next.js you know.** Next 16 has breaking changes in APIs, conventions and file structure. Read
  the relevant guide in `backoffice/node_modules/next/dist/docs/` before writing Next code, and heed deprecation notices.
- The backoffice is a static SPA: `output: "export"` in `next.config.ts`. There is no server runtime, so no server
  actions, route handlers, middleware, or server-side data access.
- The root layout is the only server component. It renders the document shell and the client providers, nothing else.
  Every view is a client component (`"use client"`).
- Routes use the App Router under `src/app`, holding routing only.
- Cache Components, partial prefetching and any other feature that needs a server runtime stay disabled.
- UI is built with MUI (community edition, `@mui/material`). No other component or styling library, and no
  CSS Modules or global CSS beyond the MUI baseline and theme.
- Atoms wrap MUI widgets and style them only through the theme and `sx` theme tokens, never with colour or spacing
  literals.
- Browser-only APIs (`localStorage`, `window`) are reached only from `src/infrastructure` adapters.

### 4.3 App: Expo + React Native Paper

- **Expo has changed. Do not trust your training data.** Expo ships breaking changes every SDK release. Before
  writing code that touches an Expo, EAS or React Native API, read the `expo` major version in `app/package.json`
  and use the matching versioned docs (`https://docs.expo.dev/versions/v<major>.0.0/`), plus
  `https://docs.expo.dev/llms.txt` for everything else.
- Navigation uses Expo Router. Routes live in `src/app`; every file there is a screen and `_layout.tsx` files define
  navigators. Nothing that is not a route lives in `src/app`. Its organisation is fixed by "Routing" below.
- UI is built with React Native Paper. No other component or styling library; styles beyond Paper's theme use
  `StyleSheet`.
- Atoms wrap Paper widgets and read colours with `useTheme()`; they are the only files that import Paper's widgets
  directly.
- The project uses Continuous Native Generation. `ios/` and `android/` are generated and never created or edited by
  hand; native behaviour is configured in `app.json` and config plugins.
- Dependencies are added with `expo install`, never with a bare package-manager add, so they resolve to SDK-compatible
  versions.
- Prefer Expo modules over third-party libraries. A library with native code requires a development build
  (Expo Go only bundles its own native modules); the plan must say so when it adds one.

#### Routing

`src/app` is split into route groups by access state. Groups do not change URLs, and every URL resolves to exactly
one route file.

```
app/src/app/
  _layout.tsx           root: AppProviders, splash hold, Stack with one Stack.Protected per access state
  +not-found.tsx        re-exports a view from presentation/common/views
  (auth)/
    _layout.tsx         Stack of the signed-out screens
    login.tsx
    signup.tsx
  (app)/
    _layout.tsx         Stack: the home (or (tabs)) first, then the screens pushed over it
    index.tsx           signed-in home, while there is one top-level destination
    (tabs)/             from the second top-level destination on
      _layout.tsx       Tabs, one Tabs.Screen per tab
      index.tsx         first tab (the home moves here)
      <tab>/            a tab that pushes screens which keep the tab bar
        _layout.tsx     Stack
        index.tsx
    <screen>.tsx        full-screen screen pushed over the home or the tabs (forms, details)
    (<flow>)/           a multi-screen flow whose screens share state
      _layout.tsx       mounts the flow's provider around a Stack
      <step>.tsx
```

- `(auth)` holds the screens of a signed-out user; `(app)` holds every screen that needs a session. A state a
  signed-in user must leave before reaching `(app)` (completing a profile, for example) is its own sibling group with
  its own guard.
- Screens that must hide the tab bar sit in `(app)` beside `(tabs)` and are pushed on the `(app)` stack. The tab bar is
  never hidden by inspecting the pathname.
- Every group folder and every tab folder has a `_layout.tsx`.
- Segments and group names are kebab-case (`edit-workout.tsx`, `(auth)`). Dynamic segments (`[id].tsx`) are not
  used: a screen's inputs travel as search params.

Root layout and auth gate:

- `src/app/_layout.tsx` is the only place that decides access. It default-exports `RootLayout`, which renders
  `AppProviders` around one local, unexported `RootNavigator`. `RootNavigator` reads the session from the session
  state hook and renders a `Stack` with `<Stack.Protected guard={signedIn}>` around `(app)` and
  `<Stack.Protected guard={!signedIn}>` around `(auth)`.
- No code navigates on sign-in, sign-out or failed refresh. The use case result changes the session state, the guard
  flips, and Expo Router moves the user to the first available screen. `Redirect` and `router.replace` are never used
  for access control.
- While the session is being restored, the fonts are loading or the device preferences are being read, the root
  layout keeps the native splash up: `SplashScreen.preventAutoHideAsync()` at module scope and `SplashScreen.hide()`,
  both from `expo-router`, once the session state is known, the fonts (`useFonts` from `expo-font`, with the map in
  `presentation/common/fonts.ts`) are loaded or have failed, and the device preferences (colour mode and language,
  `ready` from the preferences state hook) are read or have failed, and, with a stored session, the current user's
  profile read has ended (`settled` from the current-user state hook). A font failure only ends the wait; the app falls
  back to the system font. A preferences failure only ends the wait; the app follows the device. A profile failure only
  ends the wait; the gate then shows the onboarding. There is no loading route.
- Until the session state hook exists (the login slice), the root `Stack` is unguarded, but routes already sit in the
  groups above.
- `+not-found.tsx` sits at the root. No other `+` file is used (`+html`, `+middleware` and `+api` need web or a
  server).

Nested layouts and route files:

- A nested `_layout.tsx` renders one navigator (`Stack` or `Tabs`) with its `Screen` declarations and options,
  wrapped in at most one flow provider. Its only hooks are `useTranslation` for titles and labels and `useTheme` for
  navigator colours; tab icons use Paper's `Icon`. Headers, drawers and menus are components in `presentation`,
  passed through options, never defined in the layout file.
- A layout default-exports a function named `<Name>Layout` (`AuthLayout`, `TabsLayout`).
- A route file is exactly two statements: `import <Name>View from "@/presentation/<area>/views/<name>-view";` and
  `export default <Name>View;`. It reads no params, sets no options and does not wrap the view.

Navigation:

- `experiments.typedRoutes` stays enabled in `app.json`. Hrefs are typed, absolute literals, written as
  `{ pathname, params }` when they carry params, and never built by string concatenation.
- Params carry ids and primitives only, never a serialised entity: the target view loads what it needs through a
  use case. The view reads them with `useLocalSearchParams<{ ... }>()`, typed at the call.
- Navigation calls (`router`, `Link`) live in views and components, after the `Result` they depend on. State hooks
  and providers never navigate.

#### Presentation

- An area folder holds only `views`, `components`, `hooks` and `context`. `views`, `hooks` and `context` are flat.
  `components` holds only the `atoms`, `molecules` and `organisms` folders (atoms in `common` only), each flat, plus
  provider and gate files at its root. There are no sub-areas and no other subfolders.
- A view file is `<name>-view.tsx` and default-exports `<Name>View`.
- A state hook is three files: `context/<name>-context.ts` (the `createContext` call), `context/<name>-provider.tsx`
  (`<Name>Provider`) and `hooks/use-<name>.ts` (`use<Name>`), as `ContainerContext` does.
- App-wide providers (container, i18n, theme, session) are composed in `app-providers.tsx`. A provider whose state
  belongs to one flow is mounted in that flow's group layout, so its state resets when the user leaves the flow.
  There is no third place.
- State that one screen alone uses (field values, a submitting flag, the last error code) is `useState` in the view
  or in a `hooks/use-<action>.ts` beside it. It becomes a state hook only when a second screen reads it. Hooks hold
  `DomainError` codes, never translated text; the view translates them.
- Loading, empty and error states and confirmation dialogs are components in `presentation/common/components`. The
  first view that needs one creates it there; later views reuse it instead of building their own.
- Dialogs, menus and snackbars render inside Paper's `Portal`.
- Forms use Paper's `TextInput` with `error` set and a `HelperText type="error"` under the field, holding a
  translated key. Values are trimmed before structural validation; any other normalisation is the API's. The
  submit `Button` shows `loading` and is disabled while the request is pending. A form view is wrapped in
  `KeyboardAvoidingView`.
- The theme is the single `presentation/common/theme.ts`, built with `buildTheme` from Paper's `MD3LightTheme` and
  `MD3DarkTheme`. The active mode is the one the user chose on this device, otherwise the device's colour scheme.
  Views and components read colours with `useTheme()`; no colour literal appears outside `theme.ts`.
- Styles are a `StyleSheet.create` at the bottom of the file. Inline style objects are allowed only for values read
  from the theme.

### 4.4 Website: Next.js static site

- `website` is Trainera's public website. It is not built with the layers of section 2: it holds no domain, no use
  cases, no container and no atomic-design folders, and the rules of sections 2 and 4.2 do not apply to it except
  where this section repeats them.
- It depends on no other workspace and does not use the generated contract. It calls only public, anonymous API
  endpoints, with `fetch`, through one module per endpoint; those endpoints are not added to `core`. ESLint
  `no-restricted-imports` forbids `@weight-tracker/*` and relative imports into the other workspaces.
- Next.js 16 with `output: "export"`: no server runtime, so no server actions, route handlers, middleware or
  server-side data access. Read the relevant guide in `node_modules/next/dist/docs/` before writing Next code.
- Copy is Spanish only and lives in `src/content`; components receive their texts from it and hold none. What the site
  says about the product shows only what exists; anything else is labelled "Próximamente".
- Its colours, type and spacing are its own CSS custom properties in `src/styles/tokens.css`; it never imports styles,
  components or translations from the other workspaces.
- It stores nothing in the browser and sets no cookie until a change adds analytics with consent. Forms send only
  what the endpoint needs.
- Tests live outside `src`: component and unit tests in `website/test` (Vitest + React Testing Library + jsdom), E2E
  in `website/e2e` (Playwright, API intercepted with `page.route`), with the Arrange / Act / Assert convention of
  section 3.
