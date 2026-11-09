# Change Proposal: Add App Settings Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-app-settings-screen/`.

## Change

Keep every app screen clear of the iPhone status bar and notch, turn the Perfil tab into a stack that holds the
profile's section screens under one shared header, and add the first of them: an Ajustes (Settings) screen, opened from the cog,
where the athlete chooses the theme mode (light / dark) and the language (Spanish / English).

## Objective

The top of every view sits right under the status bar (time, signal, battery, notch): views pad 24 points but ignore
the safe area. The profile's section screens (My trainer, My data, My goals, Limitations, Notifications, ...) are about
to arrive, and they need one place to live and one header style, so this change sets both up before the first
section exists. The app also follows the device's language only and is always light, though the brand theme already
has a dark variant and core already has the preference use cases, so the athlete cannot choose either.

## Capabilities

1. **Safe area on every screen.** Every view keeps its content below the status bar and notch (top safe-area inset),
   plus the existing 24-point padding. This covers login, signup, the four tabs and not-found. Section screens get it
   from their header (below).
2. **Profile stack.** The Perfil tab becomes a stack: the profile screen first, then section screens pushed over it.
   The tab bar stays visible on section screens.
3. **Section header.** Every section screen shares one header: a back caret on the left that returns to the profile
   screen, then the section title. The profile screen keeps its own title row and has no such header.
4. **Ajustes screen**, opened by tapping the cog on the profile screen, with two settings:
   - **Tema:** Claro / Oscuro.
   - **Idioma:** Español / English.
     The current value is shown selected. A change applies immediately to the whole app (theme, navigator colours,
     status bar and every string) and is remembered on the device.
5. **Defaults from the system.** While nothing is stored, the theme follows the device appearance (and changes with
   it) and the language follows the device language (first supported one, else `es`, as today). The screen then shows
   the value the system resolves to. Once the athlete picks a value, the stored one wins on every launch.
6. **Stored on the device** through the same store as the session tokens (`KeyValueStoreInterface`, SQLite in the
   app), under the keys the core preference use cases already own. Preferences survive logout.

## Fronts And Roles

- backoffice -> lands here: no -> it already has its theme mode and language preferences; nothing changes.
- app -> lands here: yes -> roles: `USER` -> the athlete sees screens clear of the notch, opens Ajustes from the cog
  and changes theme and language.

No role difference inside the app. The preferences also apply on the signed-out screens (login, signup), since they
belong to the device, not to the session. Ajustes itself is reachable only while signed in, through the Perfil tab.

## Classification

Core: none. `Get/SetPreferredThemeModeUseCase` and `Get/SetPreferredLanguageUseCase`
(`core/src/application/use-cases/preferences/`) exist and are already registered in `app/src/infrastructure/container.ts`.

App:

- Profile tab stack (`Stack`; profile screen without header; section screens with the section header through
  `options.header`, title from `useTranslation`) -> layout -> `app/src/app/(app)/(tabs)/profile/_layout.tsx`
- Profile route (moves from `(tabs)/profile.tsx`) -> route -> `app/src/app/(app)/(tabs)/profile/index.tsx`
- Settings route -> route -> `app/src/app/(app)/(tabs)/profile/settings.tsx`
- `Screen` (safe-area view with the theme background and the standard padding; `edges` by props so section screens
  skip the top edge) -> atom -> `app/src/presentation/common/components/atoms/screen.tsx`
- `SegmentedButtons` (Paper `SegmentedButtons`, options and value by props) -> atom ->
  `app/src/presentation/common/components/atoms/segmented-buttons.tsx`
- `SectionHeader` (top safe-area inset, back caret `IconButton` and title `Text`; `onBack` and title by props) ->
  molecule -> `app/src/presentation/common/components/molecules/section-header.tsx`
- `PreferenceSelector` (label + `SegmentedButtons`) -> molecule ->
  `app/src/presentation/profile/components/molecules/preference-selector.tsx`
- `PreferencesProvider` / `usePreferences` (reads both stored preferences at launch; exposes the effective theme mode
  and language, `setThemeMode`, `setLanguage` and `ready`; applies the language with `i18n.changeLanguage`) ->
  state hook -> `app/src/presentation/common/context/preferences-provider.tsx`, its context beside it and the hook
  in `app/src/presentation/common/hooks/use-preferences.ts`
- `AppProviders` (mounts `PreferencesProvider`; passes `lightTheme` or `darkTheme` to `PaperProvider` and builds the
  navigation theme from the active one; status bar style follows the mode) -> provider composition ->
  `app/src/presentation/common/components/app-providers.tsx`
- `SettingsView` (two `PreferenceSelector`s) -> view -> `app/src/presentation/profile/views/settings-view.tsx`
- `ProfileView` (the cog navigates to `/profile/settings`; content wrapped in `Screen`) -> view ->
  `app/src/presentation/profile/views/profile-view.tsx`
- Login, signup, home, workouts, progress and not-found views (content wrapped in `Screen`) -> views -> their
  current files
- Root layout (holds the splash until the preferences are read too) -> layout -> `app/src/app/_layout.tsx`
- Splash rule (adds the device preferences as a third wait condition) -> invariant amendment ->
  `.stpr/template/STPR_INVARIANTS.md`, section 4, "Root layout and auth gate"
- `detectLanguage` stays the system default in `app/src/infrastructure/i18n.ts`
- `app/app.json` -> config -> `userInterfaceStyle` from `"light"` to `"automatic"`, so the device appearance is
  reported
- `settings.*` keys -> i18n -> new `settings` namespace in `app/src/i18n/{es,en}/settings.json`, registered in
  `app/src/i18n/resources.ts`

## Canonical Template Mapping

- Profile tab stack -> `STPR_INVARIANTS.md` routing tree (`<tab>/` with `_layout.tsx` Stack and `index.tsx`) and
  "Headers ... are components in `presentation`, passed through options" -> no tab stack exists yet: **this change
  establishes it**, plus the first header passed through options.
- Route files -> `app/src/app/(app)/(tabs)/profile.tsx` -> two-statement route.
- Atoms -> `app/src/presentation/common/components/atoms/icon-button.tsx` and `card.tsx` -> thin Paper / library
  wrappers, props only.
- Molecules -> `app/src/presentation/profile/components/molecules/trainer-card.tsx` -> a few atoms, no use cases.
- `PreferencesProvider` -> `app/src/presentation/auth/context/session-provider.tsx` for the app's provider shape
  (context, hook, use cases through the container); behaviour mirrors
  `backoffice/src/presentation/common/components/theme-mode-provider.tsx` as a value reference only (stored mode
  validated against the known list, failed write ignored), not a UI reference.
- `SettingsView` -> `app/src/presentation/profile/views/profile-view.tsx` -> closest view in the same area.
- Tests -> `app/test/presentation/profile/views/profile-view.test.tsx` for the view tier; the session provider test
  for the provider.

The backoffice is not a UI reference. `.stpr/template/` and its README are not updated.

## Screens And Navigation

- app -> `/profile` -> `ProfileView` -> tapping "Perfil" -> unchanged, except the cog now pushes `/profile/settings`.
- app -> `/profile/settings` -> `SettingsView` under the section header "Ajustes", tab bar visible -> the cog on the
  profile screen -> the caret goes back to `/profile`; a change applies in place, no navigation.

Navigation uses typed absolute hrefs (`router.push("/profile/settings")`, `router.back()` for the caret). Re-tapping
the Perfil tab while on Ajustes returns to the profile screen (the tab's default stack behaviour).

## API Contract

None. Preferences live on the device only. No snapshot refresh.

## Held State

- `PreferencesProvider` -> stored theme mode (`"light"` / `"dark"` / none), stored language (`"es"` / `"en"` / none),
  `ready` -> changed by `SettingsView` -> read once at launch, kept for the app's lifetime; logout does not reset it
  (it sits above the session). The effective theme is the stored one, else the device appearance (`useColorScheme`);
  the effective language is the stored one, else `detectLanguage()`.
- Stored values -> written by `SetPreferredThemeModeUseCase` / `SetPreferredLanguageUseCase` on each change -> never
  removed by this change (logout leaves them).

## Forms And Structural Validation

None. A stored value outside the known lists is ignored and the system default applies.

## Errors And Copy

- Reading a preference fails -> `UNEXPECTED` -> nothing shown; the system default applies.
- Storing a preference fails -> `UNEXPECTED` -> nothing shown; the choice applies for this run and the next launch
  starts from what was stored before (same as the backoffice).

No new error code.

New `settings` namespace (Spanish proposed from the request; English proposed):

- `title` -> "Ajustes" -> "Settings" (section header)
- `theme.label` -> "Tema" -> "Theme"
- `theme.light` -> "Claro" -> "Light"
- `theme.dark` -> "Oscuro" -> "Dark"
- `language.label` -> "Idioma" -> "Language"
- `language.es` -> "Español" -> "Español"
- `language.en` -> "English" -> "English"
- `back` -> "Volver" -> "Back" (accessible label of the caret)

Language names are shown in their own language in both locales.

## Platform Capabilities And Dependencies

None. `react-native-safe-area-context` is already a dependency of `app`, and the store is the existing
`SqliteKeyValueStore`. No development build is needed: `userInterfaceStyle: "automatic"` works in Expo Go.

## Out Of Scope

- A "Sistema" option to go back to following the device once a value is chosen (not requested; the two options per
  setting are exactly the ones asked for).
- Moving the preferences or the tokens to encrypted storage (`expo-secure-store`). Tokens are in SQLite, unencrypted,
  an accepted decision of `STPR_INVARIANTS.md` (Session storage); preferences follow the same store. Changing that is
  its own change.
- The other section screens (My trainer, My data, My goals, Limitations, Notifications, About) and wiring their menu
  items. This change only prepares the stack and header they will use.
- Syncing preferences with the API or across devices.
- Must not change: core, the API, the backoffice, the access gate, the tab bar, the profile screen's content and
  order, the brand theme values.

## Notes

- **Storage (decided).** Preferences are stored in SQLite through `KeyValueStoreInterface`, exactly like the session
  tokens. No new adapter, no encryption.
- **First paint (decided).** The root layout (`app/src/app/_layout.tsx`) holds the splash until the session state is
  known, the fonts are loaded or have failed, **and** `usePreferences().ready` is true, so the first screen already
  renders in the stored theme and language. A failed preference read only ends the wait, like a font failure; the
  system defaults apply. This extends the root-layout rule of `STPR_INVARIANTS.md`, which this change amends: the
  splash bullet in section 4 (Routing, "Root layout and auth gate") gains the device preferences as a third condition,
  with the same "a failure only ends the wait" clause. That amendment is a task of this change, and
  `.stpr/template/` must stay byte-identical to its counterparts afterwards.
- **Safe area.** `Screen` is the only place that applies the top inset to views; views do not read insets directly.
  The section header applies the top inset itself, so section screens use `Screen` without the top edge. The bottom
  edge stays with the tab bar on tab screens.
- **Status bar.** Dark mode switches the status bar text to light, through `expo-status-bar` (already bundled with
  Expo), set in `AppProviders` from the effective mode.
- **Specs.** New `app-settings` capability (screen, defaults, persistence). `app-profile` changes: the cog opens
  Ajustes and the Perfil tab is a stack. A new `app-screen-layout` requirement (safe area on every screen) may sit in
  the navigation spec instead, whichever already describes the screens.
- **Test helpers.** `app/test/support/render-with-providers.tsx` must mount `PreferencesProvider` (or a fake value) so
  existing view tests keep rendering; the preference use cases are already stubbed there if every key is listed.
- **Verification tiers:**
  - core unit: none (no core change).
  - app component: `settings-view.test.tsx`: shows both settings with the current value selected; with nothing stored
    shows the system values (device dark -> "Oscuro" selected; device language `en` -> English); choosing "Oscuro"
    calls `setPreferredThemeModeUseCase` with `"dark"`; choosing "English" calls `setPreferredLanguageUseCase` with
    `"en"` and the screen re-renders in English; a failed write still applies the choice and shows no error.
  - app component: `preferences-provider.test.tsx`: stored values win over the system; an unknown stored value is
    ignored; a read failure falls back to the system and still turns `ready` true; `ready` turns true after both
    reads.
  - app component: `profile-view.test.tsx` gains: tapping the cog navigates to `/profile/settings`.
  - app component: `section-header.test.tsx` (or covered in the settings view test): the title shows and the caret
    calls `onBack`.
  - Translation parity covers the `settings` namespace.
  - No backoffice tests, no E2E (app E2E is a known gap).
  - Manual check in Expo Go on an iPhone with a notch: every screen clear of the status bar; Ajustes from the cog and
    back; theme and language change live and survive a restart; with nothing stored, toggling the device appearance
    changes the app.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.
- No other deviation is allowed. Where a reference is missing (tab stack, header through options), this change
  establishes it as described above. The splash rule is amended in the invariants, not deviated from locally.
