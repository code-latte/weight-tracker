# Change Proposal: Add App Tab Navigation

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-app-tab-navigation/`.

## Change

Add a bottom tab bar to every signed-in screen of the Expo app, with four tabs: Inicio, Entrenamientos, Progreso and
Perfil. Inicio is the default tab. The app has tabs only, no drawer.

## Objective

The app has one signed-in screen (home). The athlete needs the top-level structure of the app now, so later changes
only have to fill each tab with its screen instead of reshaping navigation each time. The layout follows the
`(app)/(tabs)/...` structure already fixed by the routing section of `STPR_INVARIANTS.md` (4.3), using
`../../planea/app` as a visual guide for the tab bar.

## Capabilities

- A bottom tab bar on every signed-in screen, with these tabs, in this order:
  - **Inicio** -> Paper icon `home` -> the current home screen, unchanged.
  - **Entrenamientos** -> Paper icon `dumbbell` -> placeholder screen.
  - **Progreso** -> Paper icon `chart-bar` -> placeholder screen.
  - **Perfil** -> Paper icon `account` -> placeholder screen.
- Inicio is selected when the athlete signs in or opens the app with a stored session.
- The selected tab uses the theme's primary colour; the others use the default inactive colour.
- Each placeholder screen shows only a heading with its tab name.
- Tab labels are translated in `es` and `en`.

## Fronts And Roles

- backoffice -> lands here: no -> the backoffice keeps its drawer and does not change.
- app -> lands here: yes -> roles: `USER` -> the athlete moves between the four tabs.

No role difference. An athlete without a session must NOT see the tab bar: `(tabs)` sits inside `(app)`, which the
root `Stack.Protected` guard already hides when there is no session.

## Classification

App:

- `(app)/_layout.tsx` (Stack: `(tabs)` first; stays the place for screens pushed over the tabs) -> layout -> `app/src/app/(app)/_layout.tsx`
- `(tabs)/_layout.tsx` (`TabsLayout`: `Tabs` with one `Tabs.Screen` per tab) -> layout -> `app/src/app/(app)/(tabs)/_layout.tsx`
- Inicio route (moves from `(app)/index.tsx`) -> route -> `app/src/app/(app)/(tabs)/index.tsx`
- Entrenamientos route -> route -> `app/src/app/(app)/(tabs)/workouts.tsx`
- Progreso route -> route -> `app/src/app/(app)/(tabs)/progress.tsx`
- Perfil route -> route -> `app/src/app/(app)/(tabs)/profile.tsx`
- `WorkoutsView` -> view -> `app/src/presentation/workouts/views/workouts-view.tsx`
- `ProgressView` -> view -> `app/src/presentation/progress/views/progress-view.tsx`
- `ProfileView` -> view -> `app/src/presentation/profile/views/profile-view.tsx`
- `HomeView` -> view -> unchanged, `app/src/presentation/common/views/home-view.tsx`
- `tabs.*` keys -> i18n -> `app/src/i18n/{es,en}/common.json`

Core: none.

## Canonical Template Mapping

- `(app)/(tabs)/` structure -> `STPR_INVARIANTS.md` section 4.3 "Routing" -> it fixes `(tabs)/_layout.tsx`,
  `(tabs)/index.tsx` as the first tab, and single-file tabs while a tab pushes no screens. No tabs layout exists in
  the app yet: **this change establishes it**.
- `TabsLayout` -> `app/src/app/(auth)/_layout.tsx` for the shape of a nested layout (one navigator, `<Name>Layout`
  default export); STPR_INVARIANTS "Nested layouts and route files" for what it may use (`useTranslation` for labels,
  `useTheme` for colours, Paper `Icon` for tab icons).
- Route files -> `app/src/app/(app)/index.tsx` -> the existing two-statement route.
- Placeholder views -> `app/src/presentation/common/views/not-found-view.tsx` -> the simplest app view: a heading
  through the `Text` atom and translated copy.
- Tests -> `app/test/presentation/common/views/not-found-view.test.tsx` and `home-view.test.tsx` for structure.
- Visual guide only: `../../planea/app/src/app/(app)/(tabs)/_layout.tsx` (Paper `Icon` in `tabBarIcon`,
  `tabBarActiveTintColor` from `theme.colors.primary`, `headerShown: false`).

The backoffice is not a UI reference. `.stpr/template/` and its README are not updated.

## Screens And Navigation

- app -> `/` -> `HomeView` in the Inicio tab -> default after sign-in or app start with a session -> unchanged.
- app -> `/workouts` -> `WorkoutsView` -> Entrenamientos tab -> none.
- app -> `/progress` -> `ProgressView` -> Progreso tab -> none.
- app -> `/profile` -> `ProfileView` -> Perfil tab -> none.

URLs: `/` stays home, so the access gate, the not-found "Ir al inicio" link and logout keep working without changes.
`(app)/index.tsx` is removed; `(tabs)/index.tsx` takes its place.

## API Contract

None. No snapshot refresh.

## Held State

None. The selected tab is navigator state owned by Expo Router.

## Forms And Structural Validation

None.

## Errors And Copy

No failure the user can hit. No new error code.

New keys in `common.json` (Spanish from the request; English proposed):

- `tabs.home` -> "Inicio" -> "Home"
- `tabs.workouts` -> "Entrenamientos" -> "Workouts"
- `tabs.progress` -> "Progreso" -> "Progress"
- `tabs.profile` -> "Perfil" -> "Profile"

The placeholder views use the same keys for their heading.

## Platform Capabilities And Dependencies

- `@expo/vector-icons` -> `app` -> Paper's `Icon` renders Material Community Icons through it, and it does not
  resolve from `app/` today. Add it with `pnpm exec expo install @expo/vector-icons`. JS and fonts only, included in
  Expo Go: no development build needed.

No new port or adapter.

## Out Of Scope

- Content for Entrenamientos, Progreso and Perfil beyond the heading.
- Screens pushed inside a tab (tab folders with their own `Stack`) and screens pushed over the tabs.
- A drawer, a header or app bar, badges on tabs.
- Moving logout out of home (to Perfil, for example). It stays where `add-logout` put it.
- Must not change: core, the API, the snapshot, the backoffice, the root layout and its access gate, `(auth)`,
  `HomeView` and its test.

## Notes

- **Differences from planea on purpose.** Planea uses group folders per tab (`(home)/`, `(progress)/`), a drawer
  around the tabs, and hides the tab bar by inspecting the pathname. None of this is copied: the invariants use
  `index.tsx` plus single-file tabs, forbid hiding the tab bar by pathname, and this app has no drawer. Planea's iOS
  `position: "absolute"` tab bar is not copied either.
- **Expo SDK 58.** Read the Expo Router tabs guide for the `expo` major in `app/package.json`
  (`https://docs.expo.dev/versions/v58.0.0/`) before writing the layout. Check that typed routes regenerate for the
  new hrefs.
- **Specs.** The `app-access-gate` and `app-logout` specs speak of "home" at `/`; they still hold. A new
  `app-navigation` capability describes the tab bar.
- **Verification tiers:**
  - app component: one test per new view (`workouts-view.test.tsx`, `progress-view.test.tsx`,
    `profile-view.test.tsx`), each asserting its heading in `es`.
  - app component: `home-view.test.tsx` unchanged and still passing.
  - Translation parity test covers the new keys.
  - No layout test, as no app layout has one today. No E2E (app E2E is a known gap).
  - Manual check in Expo Go: four tabs with their icons, Inicio selected after login, logout from Inicio returns to
    login without the tab bar.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.
- No conflict with a stop case. Where the reference is missing, this change establishes it as described above; no
  other deviation is allowed.
