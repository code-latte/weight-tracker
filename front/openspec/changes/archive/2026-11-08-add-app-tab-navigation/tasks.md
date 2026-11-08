# Tasks

Core: no work (no alias, port, repository or use case). Backoffice: no work.

## 1. App setup

- [x] 1.1 Read the Expo Router tabs guide for SDK 58 (`https://docs.expo.dev/versions/v58.0.0/`, via
      `https://docs.expo.dev/llms.txt`) and note any API difference from `design.md`; verify the `Tabs` and
      `tabBarIcon` usage planned there still applies.
- [x] 1.2 From `app/`, run `pnpm exec expo install @expo/vector-icons`; verify it appears in `app/package.json` and
      `node -e "require.resolve('@expo/vector-icons/package.json')"` succeeds from `app/`.
- [x] 1.3 Run `pnpm dlx expo-doctor` from `app/`; verify it reports no new dependency issue (it asked for the
      `expo-font` peer, installed with `pnpm exec expo install expo-font`; the SDK patch-version mismatches it still
      reports predate this change).

## 2. App translations

- [x] 2.1 Add `tabs.home`, `tabs.workouts`, `tabs.progress`, `tabs.profile` to `app/src/i18n/es/common.json`
      ("Inicio", "Entrenamientos", "Progreso", "Perfil") and `app/src/i18n/en/common.json` ("Home", "Workouts",
      "Progress", "Profile"); verify `app/test/i18n/translation-parity.test.ts` passes.

## 3. App views

- [x] 3.1 Create `WorkoutsView` in `app/src/presentation/workouts/views/workouts-view.tsx` (heading through the `Text`
      atom with `t("tabs.workouts")`, modelled on `not-found-view.tsx`) and
      `app/test/presentation/workouts/views/workouts-view.test.tsx` asserting the heading "Entrenamientos"; verify the
      test passes.
- [x] 3.2 Create `ProgressView` in `app/src/presentation/progress/views/progress-view.tsx` and
      `app/test/presentation/progress/views/progress-view.test.tsx` asserting the heading "Progreso"; verify the test
      passes.
- [x] 3.3 Create `ProfileView` in `app/src/presentation/profile/views/profile-view.tsx` and
      `app/test/presentation/profile/views/profile-view.test.tsx` asserting the heading "Perfil"; verify the test
      passes.

## 4. App routes and layouts

- [x] 4.1 Create `app/src/app/(app)/(tabs)/_layout.tsx` exporting `TabsLayout`: `Tabs` with `headerShown: false`,
      active/inactive tint and tab bar background from `useTheme()`, and `Tabs.Screen` for `index`, `workouts`,
      `progress`, `profile` in that order, titles from `t("tabs.*")` and Paper `Icon` `home`, `dumbbell`,
      `chart-bar`, `account`; verify `pnpm --filter app typecheck` passes.
- [x] 4.2 Move `app/src/app/(app)/index.tsx` to `app/src/app/(app)/(tabs)/index.tsx` (no copy left behind) and add
      the two-statement route files `workouts.tsx`, `progress.tsx`, `profile.tsx` in `(tabs)/`; verify
      `(app)/index.tsx` no longer exists and each route file is exactly an import and a default export.
- [x] 4.3 Update `app/src/app/(app)/_layout.tsx` to declare `<Stack.Screen name="(tabs)" />` first, keeping
      `headerShown: false`; verify `pnpm --filter app typecheck` passes and `home-view.test.tsx` still passes
      unchanged.
- [x] 4.4 Manual check in Expo Go (`pnpm dev:app`): the four tabs show in order with their icons and Spanish labels;
      Inicio is selected after login and on relaunch with a session; tapping each tab shows its heading and
      highlights it in the primary colour; logging out from Inicio shows login without the tab bar; opening
      `weighttracker:///workouts` without a session shows login.

## 5. Review

- [x] 5.1 Review the app against its references: route files against `app/src/app/(app)/index.tsx`, `TabsLayout`
      against `app/src/app/(auth)/_layout.tsx` and `STPR_INVARIANTS.md` 4.3 "Routing" / "Nested layouts and route
      files", views against `not-found-view.tsx`, tests against `not-found-view.test.tsx`; verify no Paper widget is
      imported outside atoms and the tabs layout, nothing non-route sits in `src/app`, and the AGENTS.md quality
      checklist holds.
- [x] 5.2 Verify `.stpr/template/` has no drift with the drift loop in `AGENTS.md` (no output).

## 6. Verification

- [x] 6.1 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`; verify all pass.
