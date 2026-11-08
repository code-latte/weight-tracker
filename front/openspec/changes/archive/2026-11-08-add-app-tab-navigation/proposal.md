# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-app-tab-navigation.md`.

## Why

The app has one signed-in screen (home). The athlete needs the top-level structure of the app now, so later changes
only fill each tab with its screen instead of reshaping navigation each time. The app uses tabs only, never a drawer.

## What Changes

- A bottom tab bar on every signed-in screen of the Expo app, with four tabs in this order:
  - **Inicio** (`home` icon) -> the current home screen, unchanged, at `/`.
  - **Entrenamientos** (`dumbbell` icon) -> placeholder screen at `/workouts`.
  - **Progreso** (`chart-bar` icon) -> placeholder screen at `/progress`.
  - **Perfil** (`account` icon) -> placeholder screen at `/profile`.
- Inicio is the default tab after sign-in and on launch with a stored session.
- The selected tab uses the theme's primary colour. Tabs show no header, as home does today.
- Each placeholder screen shows only a heading with its tab name.
- Tab labels and headings are translated in `es` and `en`.
- Routes move into `(app)/(tabs)/`. `(app)/index.tsx` becomes `(app)/(tabs)/index.tsx`, so home stays at `/`.
- `@expo/vector-icons` and its required peer `expo-font` are added to `app` (Paper icons need them; they do not
  resolve today). Expo Go bundles both; an existing development build must be rebuilt.

Fronts and roles:

- **app** -> `USER` -> the athlete moves between the four tabs. No role difference. An athlete without a session must
  NOT see the tab bar: the tabs sit inside `(app)`, which the existing root guard hides without a session.
- **backoffice** -> not affected. It keeps its drawer.

Components (atomic design): no new atom, molecule or organism. The three placeholder views reuse the existing `Text`
atom (`app/src/presentation/common/components/atoms/text.tsx`). Tab icons use Paper's `Icon` inside the tabs layout,
which `STPR_INVARIANTS.md` (4.3, "Nested layouts and route files") allows explicitly.

Canonical references:

- Tabs layout and `(app)/(tabs)/` structure -> `STPR_INVARIANTS.md` 4.3 "Routing" and "Nested layouts and route
  files"; shape of a nested layout from `app/src/app/(auth)/_layout.tsx`. No tabs layout exists in the app:
  **this change establishes it**.
- Route files -> `app/src/app/(app)/index.tsx`.
- Placeholder views -> `app/src/presentation/common/views/not-found-view.tsx`.
- View tests -> `app/test/presentation/common/views/not-found-view.test.tsx`.
- `../../planea/app/src/app/(app)/(tabs)/_layout.tsx` is a visual guide only. Its group folder per tab, its drawer and
  its pathname-based tab bar hiding are not copied: the invariants forbid hiding the tab bar by pathname and use
  `index.tsx` plus single-file tabs.

API: none. No endpoint, no snapshot refresh. No business rule, no form, no new error code.

Conflicts with the template, the invariants or a stop case: none.

## Capabilities

### New Capabilities

- `app-navigation`: the app's top-level tab bar: which tabs exist, their order, labels and icons, the default tab, and
  that it shows only to a signed-in athlete.

### Modified Capabilities

None. `app-access-gate` and `app-logout` refer to home at `/`, which still holds.

## Impact

- `app/src/app/(app)/`: `_layout.tsx` updated, `index.tsx` moved, new `(tabs)/` with `_layout.tsx`, `index.tsx`,
  `workouts.tsx`, `progress.tsx`, `profile.tsx`.
- `app/src/presentation/{workouts,progress,profile}/views/`: three new placeholder views.
- `app/src/i18n/{es,en}/common.json`: new `tabs.*` keys.
- `app/package.json`: `@expo/vector-icons`, `expo-font`; `app/app.json`: the `expo-font` config plugin.
- `app/test/presentation/{workouts,progress,profile}/views/`: three new view tests.
- Not affected: core, the API, the snapshot, the backoffice, the root layout and its access gate, `(auth)`, `HomeView`
  and its test.

Acceptance: four tabs with their icons and translated labels; Inicio selected after login; no tab bar without a
session; `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.
