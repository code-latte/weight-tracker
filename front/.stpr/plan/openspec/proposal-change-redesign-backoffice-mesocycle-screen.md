# Change Proposal: Redesign The Backoffice Mesocycle Screen And Context Cards

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/redesign-backoffice-mesocycle-screen/`.

## Change

Align the backoffice mesocycle screen (`/mesocycle?id=…`) with the "Editar entrenamiento" screen, and restyle the
context both screens show between "Volver" and the cards:

- the athlete and trainer leave the "Información general" card and show, on both screens, as small person cards with
  an initials avatar, the name and the person's role;
- on the workout screen, the mesocycle and week show in a small card as a breadcrumb ("Hipertrofia / Semana 2"),
  with no link;
- the mesocycle card takes the workout card's two-row layout with a plain duration field;
- each week's workouts show as a small table with a regular "Nuevo entrenamiento" button above it.

## Objective

The mesocycle and workout screens are opened one from the other but look different: the mesocycle card mixes who the
plan is for with its editable data, the duration uses a +/- stepper no other screen uses, and the week's workouts are
bare links with no detail and a flat create button that is easy to miss. The workout screen's context is plain
caption-and-text pairs, where it is hard to tell at a glance who trains and who is trained, and its mesocycle link
repeats what "Volver" already does. One compact, shared way of showing people and placement makes both screens read
as one editor, and leaves room for profile pictures later.

## Capabilities

### Person cards (mesocycle and workout screens)

- A **person card** is a small outlined card, sized to its content (it never stretches to the row's width), holding:
  - an avatar with the person's initials (first letter of name and surname; first letter of the email when they have
    no name). The avatar is built so a picture can replace the initials later without changing the card;
  - the person's name (full name, or email fallback via the existing `displayNameOf`);
  - the role, as a caption under the name: "Atleta" or "Entrenador". This is the indication of who is who; it is
    text, so it reads the same for screen readers.
- The cards sit in one row between the header and the screen's cards, wrapping on narrow widths, athlete first.
- Who is shown is unchanged: the athlete always; the trainer only for `ADMIN` (and only when the workout has one).

### Workout screen (`/workout?id=…`)

- The context strip shows the person cards and, for a workout placed in a mesocycle, a **mesocycle card** of the same
  size and style:
  - caption "Mesociclo";
  - a breadcrumb of the mesocycle's name and the week: "Hipertrofia / Semana 2";
  - **no link**: "Volver" already leads to the mesocycle.
  - While the mesocycle loads, a skeleton in the card (unchanged); when it cannot be read, or for a standalone
    workout, the card is left out (unchanged).

### Mesocycle screen, edit mode (`/mesocycle?id=…`)

- **Context strip.** Between the header ("Volver", title, subtitle) and the cards, the person cards for the athlete
  and, for `ADMIN`, the trainer. The names leave the "Información general" card.
- **"Información general" card layout**, as `WorkoutDetailsCard`:
  - first row: "Nombre" (grows), "Fecha de inicio", "Duración (semanas)";
  - second row: "Objetivo general", full width, multiline (as "Descripción" in the workout card);
  - the save action stays at the bottom right, unchanged.
- **Duration field.** "Duración (semanas)" becomes a regular `TextField` (numeric input), with no increase/decrease
  buttons. Its structural validation (required, range `MESOCYCLE_DURATION_MIN`–`MESOCYCLE_DURATION_MAX`) and error
  copy are unchanged.
- **Week rows.**
  - A week without workouts: its "Nuevo entrenamiento" button in the row becomes a regular (contained) button instead
    of the flat text one.
  - A week with workouts: the row is unchanged (chip, title, objective, count, expand, edit). The collapsible body shows,
    in this order:
    1. the "Nuevo entrenamiento" button, regular (contained), not flat;
    2. a table of the week's workouts with columns "Nombre", "Ejercicios" (the number of exercises of that workout) and
       an edit icon button per row that opens `/workout?id=…` (accessible name "Editar {{name}}").

### Mesocycle screen, create mode (`/mesocycle`)

Keeps the athlete and trainer **pickers inside the card**: they are form fields of the creation, there is nothing to
show as a person card yet. It does take the new card layout and the plain duration field.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER`
  - `ADMIN`: on both screens, sees the athlete and the trainer person cards.
  - `TRAINER`: on both screens, sees the athlete person card; must **not** see a trainer card (unchanged rule, new
    widget).
  - The mesocycle card, the week rows and the workouts table are the same for both roles.
- app -> lands here: no -> no change.

## Classification

Backoffice, `common`:

- `Avatar` -> atom (now used by two files: the user menu and the person card) ->
  `backoffice/src/presentation/common/components/atoms/avatar.tsx`.
- `initialsOf` (initials from name and surname, email fallback) -> presentation helper, moved out of `user-menu.tsx`
  -> `backoffice/src/presentation/common/initials.ts`, beside `full-name.ts`.
- `PersonCard` (avatar, name, role caption) -> molecule in `common`, since two areas use it ->
  `backoffice/src/presentation/common/components/molecules/person-card.tsx`.
- `UserMenu` -> organism, changed only to import the `Avatar` atom and `initialsOf` ->
  `backoffice/src/presentation/common/components/organisms/user-menu.tsx`.

Backoffice, `workouts` domain area:

- `WorkoutContextStrip` -> **moves from molecule to organism** (it now composes the `PersonCard` molecule), rendering
  the person cards and the mesocycle card with a breadcrumb and no link; `WorkoutContextMesocycle` loses `href` ->
  `backoffice/src/presentation/workouts/components/organisms/workout-context-strip.tsx`.
- `WorkoutView` -> view, changed: `mesocycleEntryOf` builds the breadcrumb parts (mesocycle name, week label) and no
  href -> `backoffice/src/presentation/workouts/views/workout-view.tsx`.

Backoffice, `mesocycles` domain area:

- `MesocycleView` -> view, changed to render the strip after `MesocycleHeader` in edit mode and stop passing names to
  the card -> `backoffice/src/presentation/mesocycles/views/mesocycle-view.tsx`.
- `MesocycleContextStrip` (athlete, optional trainer, as person cards) -> organism ->
  `backoffice/src/presentation/mesocycles/components/organisms/mesocycle-context-strip.tsx`.
- `MesocycleDetailsCard` -> organism, changed: `people` narrows to the pickers (create mode) or nothing (edit mode),
  `NameLine` goes, new two-row layout, plain duration `TextField` ->
  `backoffice/src/presentation/mesocycles/components/organisms/mesocycle-details-card.tsx`.
- `DurationField` -> molecule, **removed** (its only user is the card) ->
  `backoffice/src/presentation/mesocycles/components/molecules/duration-field.tsx`.
- `MesocycleWeekListItem` -> molecule, changed: contained create button, button before the table in the body ->
  `backoffice/src/presentation/mesocycles/components/molecules/mesocycle-week-list-item.tsx`.
- `MesocycleWeekWorkoutsTable` (rows of name, exercise count, edit icon link) -> molecule ->
  `backoffice/src/presentation/mesocycles/components/molecules/mesocycle-week-workouts-table.tsx`.
- `MesocycleWeekWorkoutItem` -> molecule, **removed**, replaced by the table ->
  `backoffice/src/presentation/mesocycles/components/molecules/mesocycle-week-workout-item.tsx`.
- `MesocycleWeeksCard` -> organism, changed: each `WeekWorkoutEntry` gains `exerciseCount` (from
  `workout.exercises.length`) and an edit label ->
  `backoffice/src/presentation/mesocycles/components/organisms/mesocycle-weeks-card.tsx`.

No core change: no new alias, repository method, use case or `Dependencies` key.

## Canonical Template Mapping

- `Avatar` atom -> the existing atoms in `backoffice/src/presentation/common/components/atoms/` (e.g. `chip.tsx`) ->
  thin re-export of the MUI widget, themed by the existing `MuiAvatar` override in `theme.ts`.
- `initialsOf` -> `backoffice/src/presentation/common/full-name.ts` -> a pure helper over a person, beside it.
- `PersonCard` -> `backoffice/src/presentation/common/components/molecules/load-error.tsx` and the `Entry` of
  today's `workout-context-strip.tsx` -> a few atoms with a caption, no state.
- `WorkoutContextStrip` / `MesocycleContextStrip` -> today's
  `backoffice/src/presentation/workouts/components/molecules/workout-context-strip.tsx` -> same labelled `section`,
  same wrapping row, same `mb: 3` placement after the header.
- Strip placement in the views -> `backoffice/src/presentation/workouts/views/workout-view.tsx` (`LoadedWorkout`) ->
  header, then strip, then the cards grid; trainer only for `ADMIN`.
- `MesocycleDetailsCard` layout -> `backoffice/src/presentation/workouts/components/organisms/workout-details-card.tsx`
  -> first row a grid with the name growing and fixed-width fields after it, second row the multiline text, save
  bottom right.
- `MesocycleWeekWorkoutsTable` -> `backoffice/src/presentation/workouts/components/organisms/workouts-table.tsx` ->
  the `Table*` atoms with an edit `IconButton` per row; kept a molecule because it only composes atoms and is used
  inside a molecule.
- Contained create button -> `backoffice/src/presentation/workouts/components/organisms/workout-exercises-card.tsx`
  ("Añadir ejercicio") and "Añadir semana" in `mesocycle-weeks-card.tsx` -> `variant="contained"` with `AddOutlined`.
- Atom, molecule and organism references are `pending` in `.stpr/template/README.md`; follow the backoffice shapes
  above. `.stpr/template/` is not updated here.
- Tests -> `backoffice/test/presentation/workouts/views/workout-view.test.tsx`, the mesocycle view test in
  `backoffice/test/presentation/mesocycles/views/`, `backoffice/e2e/mesocycles.spec.ts`, `backoffice/e2e/workout.spec.ts`.

UI references come from the backoffice only.

## Screens And Navigation

- backoffice -> `/mesocycle?id=…` -> `MesocycleView` -> from "Planes asignados" or "Volver" on a placed workout ->
  - edit icon on a workout row -> `/workout?id=…` (replaces the name link);
  - "Nuevo entrenamiento" -> `CreateWeekWorkoutDialog`, unchanged; on success opens `/workout?id=…`, unchanged.
- backoffice -> `/mesocycle` -> `MesocycleView` (create mode) -> unchanged navigation; only the card layout changes.
- backoffice -> `/workout?id=…` -> `WorkoutView` -> the mesocycle card no longer navigates; "Volver" is the only way
  back to the mesocycle (unchanged).

## API Contract

- `GET /mesocycles/{id}` -> in `core/openapi/v1.json`: yes -> `Mesocycle` with `athlete` and `trainer`, unchanged.
- `GET /workouts/{id}` -> yes -> `WorkoutById` with `athlete` and `trainer`, unchanged.
- The mesocycle's workouts read by `useMesocycleWorkouts` -> yes -> `WorkoutListItem[]`, which already carries
  `exercises`; the count is `exercises.length`. No new field needed.
- `PUT /mesocycles/{id}`, `POST /mesocycles`, `POST /workouts` -> yes -> unchanged.
- No picture field exists on the person summaries (`UserProfileResponse`); pictures are a later API change.

No snapshot refresh. No hand-written type.

## Held State

- `MesocycleWeekListItem` -> expanded or not -> the expand button -> resets on screen exit (unchanged).
- Nothing else is added.

## Forms And Structural Validation

- Mesocycle details -> duration -> required, integer in `MESOCYCLE_DURATION_MIN`–`MESOCYCLE_DURATION_MAX`
  (unchanged rules from `validateCreateMesocycleForm` / `validateMesocycleDetailsForm`; only the input widget changes).
- Name, start date, objective, pickers -> unchanged.

## Errors And Copy

- No new failure and no new error code. The HTTP error mapper is not touched. Save errors and "Mesociclo guardado."
  stay where they are today.

Copy changes, `common` namespace (or wherever `PersonCard` reads its role labels; decide at proposal time):

- `person.role.athlete` -> "Atleta" -> "Athlete"
- `person.role.trainer` -> "Entrenador" -> "Trainer"

Copy changes, `workouts` namespace:

- `workout.context.week` -> "Semana {{number}}" -> "Week {{number}}"
- `workout.context.breadcrumb` -> "Mesociclo y semana" -> "Mesocycle and week" (accessible name of the breadcrumb)
- Removed: `workout.context.mesocycleWeek`; `workout.context.athlete` / `trainer` if the person card's role caption
  replaces them.

Copy changes, `mesocycles` namespace:

- `mesocycle.context.label` -> "Personas del mesociclo" -> "Mesocycle people" (accessible name of the strip)
- `weeks.workoutsTable.name` -> "Nombre" -> "Name"
- `weeks.workoutsTable.exercises` -> "Ejercicios" -> "Exercises"
- `weeks.workoutsTable.actions` -> "Acciones" -> "Actions" (only if the table header names the edit column)
- `weeks.editWorkout` -> "Editar {{name}}" -> "Edit {{name}}"
- Removed: `mesocycle.duration.decrease`, `mesocycle.duration.increase`.
- Reused: `mesocycle.fields.athlete`, `mesocycle.fields.trainer` (create pickers), `weeks.newWorkout`.

## Platform Capabilities And Dependencies

None. `Avatar` and `Breadcrumbs` come from the existing `@mui/material`.

## Out Of Scope

- Profile pictures: no upload, no picture field, no API change. The avatar shows initials only.
- Making the person cards interactive (links to the athlete, menus).
- Moving the save/error alerts out of the mesocycle card as the workout screen does.
- Changing the headers (title, subtitle, "Volver") or the "Planificación semanal" card header.
- Showing the mesocycle's date range, the week dates or a workout's date in the table.
- Removing or reordering workouts from the week table.
- Editing who the mesocycle or workout is for.
- Must not change: create/update requests, form validation rules, the week dialog, `CreateWeekWorkoutDialog`, core,
  the app.

## Notes

- **Role indication.** Recommended: the role caption under the name ("Atleta" / "Entrenador") as the indication,
  since it is explicit and accessible. Optional, to confirm at proposal time: a different avatar colour per role from
  the theme palette, as a second cue that never stands alone.
- **Person card size.** As small as reads well: dense padding, a ~32 px avatar, name in `body2` semibold, role in
  `caption`; content-sized, not full width. All cards in the strip (people and mesocycle) share the same height.
- **Breadcrumb.** MUI `Breadcrumbs` with "/" as the separator, both items as plain text, the week as the current
  item. Used by one file only, so imported directly, per the invariants; it becomes an atom if a second file needs it.
- **Why the strips become organisms.** They now compose the `PersonCard` molecule, and imports flow downward only.
- **`UserMenu` touch.** Required, not cosmetic: once a second file uses MUI `Avatar`, the invariants make it an atom
  that every file imports, and the initials helper is shared rather than duplicated.
- **"Regular button".** Read as `variant="contained"` (size small inside the row), the same as "Añadir semana" and
  "Añadir ejercicio". Confirm at proposal time if `outlined` was meant instead.
- **Workout name in the table.** Plain text; the edit icon is the only navigation, so the row has one action.
- **Duration input.** `TextField` with `type="number"` or `inputMode="numeric"`; decide at proposal time. The value stays
  a string in the form values, as today.
- **Verification tiers:**
  - core unit: none.
  - backoffice unit: `initialsOf` (name and surname, name only, no name falls back to the email).
  - backoffice component tests:
    - `person-card` (if it branches): initials, name, role caption.
    - workout view: athlete card with initials and "Atleta"; as `ADMIN` a trainer card with "Entrenador"; as
      `TRAINER` no trainer card; a placed workout shows "Hipertrofia / Semana 2" and **no link** to the mesocycle;
      a standalone workout shows no mesocycle card; the loading skeleton while the mesocycle reads.
    - mesocycle view:
      - edit mode: athlete card; as `ADMIN` the trainer card too; as `TRAINER` no trainer card; the details card no
        longer contains the names;
      - the duration is a textbox and there are no "Reducir duración" / "Aumentar duración" buttons; a value out of
        range shows the existing range error;
      - a week without workouts shows a "Nuevo entrenamiento" button; a week with workouts, once expanded, shows the
        button before a table with each workout's name and exercise count, and "Editar {{name}}" leads to
        `/workout?id=…`;
      - create mode: pickers still inside the card, no person cards.
  - backoffice E2E: `e2e/mesocycles.spec.ts` and `e2e/workout.spec.ts` updated for any step that used the duration
    stepper, a workout name link or the mesocycle link on the workout screen; the create-week-workout flow keeps
    working with the new button.
  - translation parity passes.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed beyond the ones stated in these notes.
