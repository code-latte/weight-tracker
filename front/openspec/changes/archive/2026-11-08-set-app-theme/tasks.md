# Tasks

Core: no work (no alias, port, repository or use case). Backoffice: no work.

## 1. App setup

- [x] 1.1 Read the SDK 58 fonts guide (`https://docs.expo.dev/versions/v58.0.0/sdk/font/` and the fonts page linked
      from `https://docs.expo.dev/llms.txt`); verify `useFonts` from `expo-font` still accepts a `{ name: asset }` map
      and returns `[loaded, error]` as `design.md` assumes, and note any difference.
- [x] 1.2 From `app/`, run `pnpm exec expo install @expo-google-fonts/inter @expo-google-fonts/plus-jakarta-sans`;
      verify both appear in `app/package.json` and `pnpm dlx expo-doctor` reports no new issue (the existing SDK
      patch-version mismatches predate this change).

## 2. App theme

- [x] 2.1 Replace `app/src/presentation/common/theme.ts` with the theme from the brief's appendix, keeping every
      value and export; verify `pnpm --filter app typecheck` passes, and that any edit beyond formatting is type-only
      and noted here (type-only: `withFont` takes `` `${MD3TypescaleKey}` `` keys, since Paper types the typescale
      keys as an enum and rejected the plain names; no value changed).
- [x] 2.2 Build the navigation theme from the Paper theme's colours (`primary`, `background`, `card`, `text`,
      `border`, `notification`) over `expo-router`'s `DefaultTheme`; verify `pnpm --filter app typecheck` passes and
      its `background` is `#F8FAFC` and `primary` `#0F766E` (hand-built: `adaptNavigationTheme` rejects
      `expo-router`'s vendored `DefaultTheme`; placed in `AppProviders`, not `theme.ts`, see design).
- [x] 2.3 Wrap the children of `AppProviders` in `expo-router`'s `ThemeProvider` with `navigationTheme`, keeping
      `PaperProvider theme={theme}`; verify all app view tests still pass unchanged (`pnpm --filter app test`).

## 3. App fonts

- [x] 3.1 Create `app/src/presentation/common/fonts.ts` default-exporting the eight-entry map from `design.md`; verify
      every key matches a family name in `theme.ts` (`body.*` and `heading.*`) and `pnpm --filter app typecheck`
      passes.
- [x] 3.2 In `app/src/app/_layout.tsx`, call `useFonts(fonts)` in `RootNavigator`, hide the splash and render the
      `Stack` only once the session is not `restoring` and the fonts are loaded or failed; verify typecheck passes, the
      gate is unchanged, and no other file under `src/app` changed.

## 4. Invariants

- [x] 4.1 Update the splash bullet of `.stpr/template/STPR_INVARIANTS.md` 4.3 to the wording in `design.md`
      (session known and fonts loaded or failed); verify the theme bullet is untouched and Prettier passes on the file.

## 5. Review

- [x] 5.1 Manual check in Expo Go (`pnpm dev:app`): login has a `#F8FAFC` background and a teal filled button with
      an Inter Bold label; home shows "Trainera" in Plus Jakarta Sans and body text in Inter; the selected tab is teal
      on a white bar; no purple anywhere; no font jump after the splash; with the device in dark mode the app stays
      light.
- [x] 5.2 Review against the references: `theme.ts` against the brief's appendix (values unchanged) and
      `STPR_INVARIANTS.md` 4.3 theme bullet; root layout against the updated splash bullet; `AppProviders` against the
      providers rule; verify no colour literal outside `theme.ts`, no Paper widget imported outside atoms and the tabs
      layout, and the AGENTS.md quality checklist holds.
- [x] 5.3 Verify `.stpr/template/` has no drift with the drift loop in `AGENTS.md` (no output).

## 6. Verification

- [x] 6.1 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`; verify all pass.
