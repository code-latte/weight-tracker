# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-app-settings-screen.md`.

## Why

Every app screen starts right under the iPhone status bar and notch: views pad 24 points but ignore the safe area.
The profile's section screens (My trainer, My data, My goals, Limitations, Notifications, ...) are about to arrive
and need one place to live and one header style. The athlete also cannot choose the theme or the language: the app is
always light and follows the device language only, although the brand theme already has a dark variant and core
already has the preference use cases.

## What Changes

- Every screen keeps its content below the status bar and notch (top safe-area inset), plus its current padding.
- The Perfil tab becomes a stack: the profile screen first, then section screens pushed over it, with the tab bar
  still visible. Section screens share one header: a back caret that returns to the profile screen, then the title.
- New **Ajustes** screen, opened from the cog on the profile screen (the cog was inert), with two settings:
  Tema (Claro / Oscuro) and Idioma (Español / English). A change applies at once to the whole app.
- While nothing is stored, the theme follows the device appearance (live) and the language follows the device language
  (as today). Once chosen, each value is stored on the device, through the same SQLite key-value store as the session
  tokens, and wins on every launch. Preferences survive logout.
- **BREAKING (spec):** the app no longer always renders in light mode; it renders dark when chosen, or when the device
  is dark and nothing is stored.
- The splash screen also waits until the stored preferences are read, so the first screen already shows the stored
  theme and language. A failed read only ends the wait.
- `STPR_INVARIANTS.md` is amended (not deviated from locally): the splash rule gains the preferences as a third wait
  condition; the initial-language rule starts from the stored choice; the theme rule names both modes.

Fronts and roles: app only, role `USER`, no role difference. The preferences also apply on the signed-out screens
(login, signup) because they belong to the device. Ajustes is reachable only while signed in. The backoffice does not
change: it already has its own theme mode and language preferences.

API: none. Preferences live on the device. No endpoint is used and no snapshot refresh is needed.

## Capabilities

### New Capabilities

- `app-settings`: the Ajustes screen in the app: which settings it offers, their defaults from the system, how a
  choice applies and is remembered on the device, and what happens when storage fails.

### Modified Capabilities

- `app-appearance`: light-only mode is replaced by the chosen or system colour mode; the brand-colour and surface
  requirements are scoped to light mode with dark counterparts; every screen respects the safe area; the launch waits
  for the stored preferences.
- `app-navigation`: the Perfil tab pushes section screens that keep the tab bar, under a shared section header.
- `app-profile`: the cog on the title row opens Ajustes instead of doing nothing.

No core capability changes: `language-preference` and the theme-mode use cases already behave as needed.

## Canonical References

- Profile tab stack and header through navigator options -> routing tree and "Headers ... passed through options" in
  `STPR_INVARIANTS.md` section 4.3 -> no tab stack exists yet: **this change establishes it**.
- Route files -> `app/src/app/(app)/(tabs)/profile.tsx` (two-statement route).
- `SafeArea` atom, `SegmentedButtons` atom -> `app/src/presentation/common/components/atoms/icon-button.tsx` (thin
  library wrapper, props only). Both new atoms.
- `SectionHeader` molecule (common), `PreferenceSelector` molecule (profile) ->
  `app/src/presentation/profile/components/molecules/trainer-card.tsx`. Both new molecules; no organism.
- Reused atoms: `IconButton`, `Text`. Reused molecules: none.
- Preferences state hook -> `app/src/presentation/auth/context/session-provider.tsx` (app provider shape); behaviour
  mirrors `backoffice/src/presentation/common/components/theme-mode-provider.tsx` as a value reference only.
- `SettingsView` -> `app/src/presentation/profile/views/profile-view.tsx`.
- View tests -> `app/test/presentation/profile/views/profile-view.test.tsx`; provider test ->
  `app/test/presentation/auth/context/`.

The backoffice is not a UI reference.

## Conflicts With The Template

- The splash rule, the initial-language rule and the theme rule of `STPR_INVARIANTS.md` do not cover stored
  preferences or a dark mode. Resolved by amending those rules in this change, as the user decided.
- Tokens and preferences are stored unencrypted in SQLite, an accepted decision of the invariants (Session storage).
  No change.
- `app/test/support/render-with-providers.tsx` must mount the preferences provider and the safe-area context; the
  exception is documented in design.md.

No business rule enters the front, and there are no forms.

## Impact

- App routes: `(tabs)/profile.tsx` becomes `(tabs)/profile/_layout.tsx`, `index.tsx` and `settings.tsx`; root
  `_layout.tsx` waits for the preferences.
- App presentation: new atoms, molecules, preferences state hook, theme provider composition and `SettingsView`;
  every view adopts the safe area; `ProfileView` wires the cog; `AppProviders` re-composed.
- App config and translations: `app.json` `userInterfaceStyle` -> `"automatic"`; new `settings` namespace in `es`
  and `en`.
- Test helper and tests in `app/test`.
- `.stpr/template/STPR_INVARIANTS.md`: three amended bullets.
- Core, backoffice and the API: no change. No new dependency (`react-native-safe-area-context` and
  `expo-status-bar` are already in `app`); no development build needed.
