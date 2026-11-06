# Design

## Context

See `proposal.md` (Why) and the two delta specs for the required behaviour. Current state that shapes the approach:

- `workouts/components/molecules/workout-context-strip.tsx` renders caption/text `Entry` pairs and a `Link` to the
  mesocycle. `WorkoutView` builds `WorkoutContextMesocycle` (`"loading" | { label, href }`) with `mesocycleEntryOf`.
- `mesocycles/components/organisms/mesocycle-details-card.tsx` takes
  `people: { kind: "pickers" } | { kind: "names" }` and renders names with a local `NameLine`; the duration uses the
  `DurationField` molecule (its only user).
- `mesocycles/components/molecules/mesocycle-week-list-item.tsx` renders a flat `Button size="small"` and a `List` of
  `MesocycleWeekWorkoutItem` links.
- `UserMenu` imports MUI `Avatar` directly and holds a local `initials(name, surname)`. `theme.ts` already styles
  `MuiAvatar` (tinted primary background, bold).
- `validateCreateMesocycleForm` / `validateMesocycleDetailsForm` in `core/src/application/validation/mesocycle-rules.ts`
  already check duration as required, whole number, 1–52 (`range`). Copy `mesocycle.validation.range` exists.
- `WorkoutListItem` (generated) carries `exercises`; the count needs no API change.

## Goals / Non-Goals

**Goals:**

- One `PersonCard` molecule shared by the workout and mesocycle strips, ready to take a picture later without changing
  its callers' layout.
- Keep the mesocycle and workout screens' strips structurally identical (same section, wrap, spacing, card height).
- Stay inside the invariants: atoms wrap shared MUI widgets, imports flow downward, no business rule, no core change.

**Non-Goals:**

- A generic `ContextStrip` in `common` shared by both areas (two thin organisms are clearer while one carries a
  mesocycle card and the other does not).
- Pictures, role colours beyond the theme's default avatar style, interactive cards.
- Moving the mesocycle save/error alerts out of the card.

## Decisions

### Mapping to core and the fronts

- **core:** no change. No domain alias, port, repository, use case or `Dependencies` key. No `container.ts` change in
  either front.
- **app:** no change.
- **backoffice:** presentation only (components, views, translations). No new state hook, provider or route.

### Components, atomic level and folder

| Piece                          | Level    | Folder                                 | Status                                          |
| ------------------------------ | -------- | -------------------------------------- | ----------------------------------------------- |
| `Avatar`                       | atom     | `common/components/atoms/avatar.tsx`   | new, thin wrapper like `chip.tsx`               |
| `initialsOf`                   | helper   | `common/initials.ts`                   | new, moved out of `user-menu.tsx`               |
| `PersonCard`                   | molecule | `common/components/molecules/`         | new                                             |
| `UserMenu`                     | organism | `common/components/organisms/`         | changed: imports `Avatar` atom and `initialsOf` |
| `WorkoutContextStrip`          | organism | `workouts/components/organisms/`       | moved from `molecules/` and changed             |
| `MesocycleContextStrip`        | organism | `mesocycles/components/organisms/`     | new                                             |
| `MesocycleDetailsCard`         | organism | `mesocycles/components/organisms/`     | changed                                         |
| `MesocycleWeeksCard`           | organism | `mesocycles/components/organisms/`     | changed                                         |
| `MesocycleWeekListItem`        | molecule | `mesocycles/components/molecules/`     | changed                                         |
| `MesocycleWeekWorkoutsTable`   | molecule | `mesocycles/components/molecules/`     | new                                             |
| `DurationField`                | molecule | `mesocycles/components/molecules/`     | removed                                         |
| `MesocycleWeekWorkoutItem`     | molecule | `mesocycles/components/molecules/`     | removed                                         |
| `WorkoutView`, `MesocycleView` | view     | `workouts/views/`, `mesocycles/views/` | changed                                         |

Reused unchanged: atoms `Card`, `CardContent`, `Typography`, `Button`, `IconButton`, `TextField`, `Skeleton`, `Table`,
`TableHead`, `TableBody`, `TableRow`, `TableCell`, `TableContainer`, `Collapse`; helper `fullNameOf`.

Imports flow view → organism → molecule → atom. The strips become organisms **because** they compose the
`PersonCard` molecule; a molecule importing a molecule would blur the levels. `MesocycleWeekWorkoutsTable` composes
only atoms, so it is a molecule; how it reaches the week row without a molecule-to-molecule import is the next
decision.

### `MesocycleWeekListItem` and the table

`MesocycleWeekListItem` is a molecule and must not import another molecule. Two options:

- **A (chosen):** `MesocycleWeekListItem` takes the expanded body as a `children` slot (`ReactNode`), and
  `MesocycleWeeksCard` (organism) composes `<MesocycleWeekWorkoutsTable>` into it. The row keeps owning the expand
  state, the count, the create button and the edit action; the organism decides what the table shows.
- B: promote `MesocycleWeekListItem` to organism. Rejected: it is a row of atoms with local UI state, the molecule
  definition fits, and the slot keeps it that way.

`WeekWorkouts` therefore loses `entries` in favour of `hasEntries: boolean` (decides collapsible vs plain row) while
`countLabel`, `showLabel`, `hideLabel`, `createLabel`, `onCreate` stay. The create button becomes
`variant="contained" size="small" startIcon={<AddOutlined />}`, rendered first inside the collapsed body, then
`children`.

`MesocycleWeekWorkoutsTable` props: `rows: { id, name, exerciseCount, href, editLabel }[]` plus the header labels.
Each row: name as `Typography` text, count, and an `IconButton` with `EditOutlined`, rendered as `component={NextLink}`
with `href` and `aria-label={editLabel}` (navigation stays a link for middle-click and the E2E `href` assertion).
`MesocycleWeeksCard` builds the rows from `WorkoutListItem` (`exerciseCount: workout.exercises.length`).

### `PersonCard`

Props: `{ name: string; initials: string; role: string }` — all display-ready strings, so the molecule holds no
domain logic and has no branches. Later, an optional `pictureUrl` goes straight to `Avatar`'s `src`, with initials as
the fallback child.

Layout: `Card variant="outlined"` with `display: "inline-flex"` (content-sized), dense padding (`py: 1, px: 1.5`, no
`CardContent` bottom-padding gap), `Avatar` at 32 px with `fontSize: "0.875rem"`, name `body2` weight 600, role
`caption` `text.secondary` under the name. The role caption is the role indication; no colour cue is added (the
theme's single avatar style is kept). This answers the brief's open "colour per role" option with "no".

### Strips

Both strips render a `section` with an `aria-label` and `display: flex; flexWrap: wrap; gap: 2; mb: 3;
alignItems: stretch`, so all cards share the row's height.

- `WorkoutContextStrip` props: `athlete: PersonCardProps`, `trainer: PersonCardProps | null`,
  `mesocycle: "loading" | { name: string; week: string } | null`. The mesocycle card is a `Card` with the same padding,
  caption "Mesociclo" and MUI `Breadcrumbs` (separator "/", both items `Typography`, the week with
  `aria-current="page"`, `aria-label` from `workout.context.breadcrumb`). `Breadcrumbs` has one user, so it is imported
  directly per the invariants; loading shows `Skeleton width={160}` in the same card. `href` disappears from the type.
- `MesocycleContextStrip` props: `athlete`, `trainer` (same types). Rendered by `LoadedMesocycle` right after
  `MesocycleHeader`; `CreateMesocycle` does not render it.
- Views build the props: `displayNameOf(person)` (already in both views), `initialsOf(person)`, and the role caption
  from `common:person.role.*`. The `ADMIN`-only trainer rule stays in the views, as today.

### `initialsOf`

`initialsOf(person: { name: string; surname: string; email: string }): string` — first letter of name and of surname,
upper-cased, skipping empty parts; when both are empty, the first letter of the email upper-cased. `UserMenu` calls it
with its profile (which has an email), so its output for named users is unchanged ("AG").

### `MesocycleDetailsCard`

- `people` becomes `{ kind: "pickers"; pickers } | { kind: "none" }`; `MesocyclePeopleNames` and `NameLine` go.
  `creating` stays derived from `kind === "pickers"`.
- Layout as `WorkoutDetailsCard`: a grid `gridTemplateColumns: { xs: "1fr", sm: "minmax(0, 1fr) auto auto" }` holding
  Nombre, Fecha de inicio (`type="date"`, label shrunk) and Duración; then Objetivo general `multiline minRows={2}`;
  then the save button bottom right. Pickers (create mode) and alerts stay above the fields, as today.
- Duración: `TextField {...field("durationInWeeks")}` with `type="number"`, `slotProps.htmlInput` `min`/`max`/`step=1`
  from `MESOCYCLE_DURATION_MIN/MAX` and a fixed width (~160 px). `type="number"` chosen over `inputMode="numeric"`:
  native spinners are acceptable and mobile keyboards are not a backoffice concern; the brief's "no +/- icons" refers
  to the custom buttons. The value stays a string in the form values; validation is the existing core rule, whose
  `range` message already renders through `messageOf`. No counter on this field (no max length).

### State

No provider or hook changes. Held state stays: `MesocycleWeekListItem` expand flag (local, reset on unmount); the
views' form values. Nothing new to reset on logout.

### Errors and copy

No new error code; the HTTP error mapper is untouched. Existing `errors:*` messages and `mesocycle.validation.range`
are reused.

| Namespace    | Key                             | es                     | en                 |
| ------------ | ------------------------------- | ---------------------- | ------------------ |
| `common`     | `person.role.athlete`           | Atleta                 | Athlete            |
| `common`     | `person.role.trainer`           | Entrenador             | Trainer            |
| `workouts`   | `workout.context.week`          | Semana {{number}}      | Week {{number}}    |
| `workouts`   | `workout.context.breadcrumb`    | Mesociclo y semana     | Mesocycle and week |
| `mesocycles` | `mesocycle.context.label`       | Personas del mesociclo | Mesocycle people   |
| `mesocycles` | `weeks.workoutsTable.name`      | Nombre                 | Name               |
| `mesocycles` | `weeks.workoutsTable.exercises` | Ejercicios             | Exercises          |
| `mesocycles` | `weeks.editWorkout`             | Editar {{name}}        | Edit {{name}}      |

Removed: `workouts:workout.context.mesocycleWeek`, `workouts:workout.context.athlete`,
`workouts:workout.context.trainer`, `mesocycles:mesocycle.duration.decrease`, `mesocycles:mesocycle.duration.increase`.
Kept: `workouts:workout.context.label`, `workouts:workout.context.mesocycle`, `mesocycles:mesocycle.fields.*`,
`mesocycles:weeks.newWorkout`, `mesocycles:weeks.workoutCount_*`. The edit column's header is left empty, as in
`workouts-table.tsx`; each edit button carries its own accessible name, so no `actions` key is added.

### Tests

Patterns follow the existing tests of each tier; no helper or pattern changes.

- `test/presentation/common/initials.test.ts` (new, mirrors `full-name.test.ts`): name and surname, name only, blank
  name and surname falls back to the email, trimming and upper-casing.
- `test/presentation/common/components/organisms/user-menu.test.tsx`: stays green unchanged.
- `test/presentation/workouts/views/workout-view.test.tsx` (strip block around line 3330): person cards with initials,
  name and "Atleta"; admin trainer card with "Entrenador"; trainer sees no trainer card; breadcrumb parts with no link;
  loading and failed mesocycle cases; English strip.
- `test/presentation/mesocycles/views/mesocycle-view.test.tsx`: replace the stepper tests (around lines 327–334, 553)
  with typing and the `range` / required errors; move the edit-mode names assertions (around 530–543) from the details
  card to the person cards; replace the workout link test (around 876) with the table row, count and "Editar Pierna"
  href; expanded body shows the button before the table; create mode shows no person card; English copy.
- `e2e/mesocycles.spec.ts` (around lines 156–167): the workout context asserts the breadcrumb text and no link; the
  week asserts the "Editar Pierna" `href` instead of the name link. Check `e2e/workout.spec.ts` for any mesocycle-link
  or context-text assertion and update it.
- Translation parity test passes.

## Risks / Trade-offs

- [`type="number"` accepts `e`, `.` and scroll-wheel changes] → the core rule already rejects non-integers with
  `range`; the save is the only gate, as for every other field.
- [Removing the mesocycle link drops a shortcut when "Volver" leads elsewhere] → "Volver" on a placed workout already
  leads to its mesocycle (`backHrefOf`), so nothing is lost.
- [Slot-based `MesocycleWeekListItem` spreads the week body over two components] → the row keeps every behaviour that
  depends on its state; the organism only fills the table, which keeps the molecule free of molecule imports.
- [Initials of a single-word name give one letter] → acceptable and the same as `UserMenu` today.

## Migration Plan

Presentation-only change with no data or API impact; ships with the backoffice build and rolls back by reverting.
