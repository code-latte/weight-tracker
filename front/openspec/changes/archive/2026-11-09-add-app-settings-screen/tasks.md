# Tasks

Core has no work in this change (design.md, decision 1): the preference use cases exist and are registered in both
containers. Tasks start at the app.

## 1. Invariants and configuration

- [x] 1.1 Amend `.stpr/template/STPR_INVARIANTS.md`: splash bullet (section 4.3, "Root layout and auth gate") adds the
      device preferences as a third wait condition with "a failure only ends the wait"; "Internationalisation" initial
      language starts from the stored choice; theme bullet names `MD3LightTheme` and `MD3DarkTheme` and the stored-else-
      device mode. Verify the three bullets read as in design.md decision 4 and the drift check in `AGENTS.md` prints
      nothing.
- [x] 1.2 Set `userInterfaceStyle` to `"automatic"` in `app/app.json`; verify `pnpm dlx expo-doctor` from `app/`
      reports no new issue.

## 2. Translations

- [x] 2.1 Add `app/src/i18n/{es,en}/settings.json` with the keys of design.md decision 8, register the namespace in
      `app/src/i18n/resources.ts`, and add `back` (`Volver` / `Back`) to both `common.json`; verify the translation
      parity test passes.

## 3. Atoms

- [x] 3.1 Create `SafeArea` atom (`app/src/presentation/common/components/atoms/safe-area.tsx`): `SafeAreaView`,
      theme background, `edges` by props, default without bottom; verify `pnpm typecheck` passes.
- [x] 3.2 Create `SegmentedButtons` atom (`.../atoms/segmented-buttons.tsx`): Paper `SegmentedButtons`, all content by
      props; verify `pnpm typecheck` passes.
- [x] 3.3 Add `SafeAreaProvider` with fixed `initialMetrics` to `app/test/support/render-with-providers.tsx` (the
      documented helper exception, part 1); verify the existing app tests still pass.

## 4. Preferences state hook and theme composition

- [x] 4.1 Create `preferences-context.ts`, `preferences-provider.tsx` (common `context/`) and `use-preferences.ts`
      (common `hooks/`) as in design.md decision 2: parallel reads, known-value filter, stored language applied before
      `ready`, no `changeLanguage` when nothing stored, failed writes ignored.
- [x] 4.2 Add `test/presentation/common/context/preferences-provider.test.tsx`, modelled on the session provider test:
      stored values win; unknown value ignored; read failure -> device values and `ready`; `ready` only after both
      reads; setters call the set use cases; write failure still applies. Verify it passes.
- [x] 4.3 Create `AppThemeProvider` (`app/src/presentation/common/components/app-theme-provider.tsx`): Paper theme by
      mode, navigation theme derived from it (memoised), `StatusBar` style by mode; verify `pnpm typecheck` passes.
- [x] 4.4 Re-compose `AppProviders` as `I18nextProvider` > `ContainerProvider` > `PreferencesProvider` >
      `AppThemeProvider` > `SessionProvider`, removing the module-scope navigation theme; add `PreferencesProvider` to
      the test helper between `ContainerProvider` and `SessionProvider` (helper exception, part 2). Verify all app
      tests pass.
- [x] 4.5 In `app/src/app/_layout.tsx`, add `usePreferences().ready` to the splash condition and remove its
      `StatusBar`; verify `pnpm typecheck` passes and, manually in Expo Go, a stored dark mode opens without a light
      flash.

## 5. Safe area on existing views

- [x] 5.1 Wrap the root of `login-view`, `signup-view`, `home-view`, `workouts-view`, `progress-view`,
      `not-found-view` and `profile-view` in `SafeArea`, keeping each view's own padding; verify their existing view
      tests pass and, in Expo Go on a notched iPhone, no content overlaps the status bar.

## 6. Profile tab stack and section header

- [x] 6.1 Create `SectionHeader` molecule (`app/src/presentation/common/components/molecules/section-header.tsx`):
      `SafeArea` top edge, `IconButton` `chevron-left` with `backLabel`, title `Text` as heading; add
      `test/presentation/common/components/molecules/section-header.test.tsx` (title, caret label, `onBack`). Verify
      it passes.
- [x] 6.2 Replace `app/src/app/(app)/(tabs)/profile.tsx` with `profile/_layout.tsx` (`ProfileLayout`, `Stack`, `index`
      without header, `settings` with `SectionHeader` through `options.header`) and `profile/index.tsx` (two-statement
      route to `ProfileView`); verify `pnpm typecheck` passes with typed routes and the Perfil tab still opens the
      profile screen in Expo Go.

## 7. Settings screen

- [x] 7.1 Create `PreferenceSelector` molecule (`app/src/presentation/profile/components/molecules/preference-selector.tsx`):
      label `Text` + `SegmentedButtons`, props only; verify `pnpm typecheck` passes.
- [x] 7.2 Create `SettingsView` (`app/src/presentation/profile/views/settings-view.tsx`): `SafeArea` without top edge,
      two `PreferenceSelector`s from `usePreferences()` and the `settings` keys; add route
      `app/src/app/(app)/(tabs)/profile/settings.tsx` (two statements). Verify `pnpm typecheck` passes.
- [x] 7.3 Add `test/presentation/profile/views/settings-view.test.tsx`, modelled on `profile-view.test.tsx`: both
      settings with current values selected; nothing stored and device dark -> "Oscuro"; choosing "Oscuro" calls the
      set use case with `"dark"`; choosing "English" calls it with `"en"` and the view re-renders in English (language
      restored to `es` in setup); write failure shows no error. Verify it passes.
- [x] 7.4 Wire the cog in `ProfileView` to `router.push("/profile/settings")`, removing the "no screen yet" comment;
      in `profile-view.test.tsx` replace the inert-cog case with "tapping Ajustes pushes `/profile/settings`". Verify
      it passes.

## 8. Review

- [x] 8.1 Review the app against the mapped references (proposal "Canonical References"): routes against the
      two-statement route and the routing tree, atoms against `icon-button.tsx`, molecules against `trainer-card.tsx`,
      the state hook against `session-provider.tsx`, `SettingsView` against `profile-view.tsx`; confirm only atoms
      import Paper widgets and `SafeAreaView`, presentation imports no `src/infrastructure` outside `app-providers.tsx`,
      and every string is in `es` and `en`. Record any finding and fix it.
- [x] 8.2 Confirm core, backoffice and `container.ts` in both fronts are unchanged: this command prints nothing,
      `git diff --stat -- core backoffice app/src/infrastructure/container.ts`.
- [x] 8.3 Manual check in Expo Go on a notched iPhone: safe area on every screen; Ajustes from the cog, caret and Perfil
      tab back; theme and language change live and survive a restart and a logout; with nothing stored, toggling the
      device appearance changes the app.
- [x] 8.4 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify it passes. No E2E is planned.
