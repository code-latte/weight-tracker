# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-redesign-backoffice-mesocycle-screen.md`.

## Why

The backoffice mesocycle and workout screens open each other but do not look alike. The mesocycle card mixes who the
plan is for with its editable data, its duration uses a +/- stepper no other screen uses, and a week's workouts are bare
links with a flat create button that is easy to miss. On the workout screen, people are plain caption/text pairs where
it is hard to tell who trains and who is trained, and the mesocycle link repeats "Volver". Now that both screens exist,
one compact, shared way of showing people and placement makes them read as one editor and leaves room for profile
pictures later.

## What Changes

Backoffice only, roles `ADMIN` and `TRAINER`. The app is not touched.

- **Person cards (both screens).** Between the page header and the cards, the athlete and (for `ADMIN` only) the
  trainer show as small, content-sized outlined cards with an initials avatar, the name (email fallback) and a role
  caption ("Atleta" / "Entrenador"). A `TRAINER` never sees a trainer card.
- **Workout screen mesocycle card.** A placed workout shows a card of the same size with the caption "Mesociclo" and a
  breadcrumb "<mesocycle> / Semana <n>". **It is no longer a link**; "Volver" is the way back. Loading placeholder and
  "left out on failure or standalone" are unchanged.
- **Mesocycle screen, edit mode.** The athlete and trainer leave the "Información general" card and show as person cards
  above it.
- **"Información general" layout**, matching the workout properties card: first row Nombre (grows), Fecha de inicio,
  Duración (semanas); second row Objetivo general, full width, multiline. Applies in both modes.
- **Duration.** **BREAKING (UI)**: the bounded +/- stepper is removed; Duración (semanas) is a plain numeric text
  field. Its structural rule (required, 1–52) is unchanged and now shown as a field error on save.
- **Week rows.** "Nuevo entrenamiento" becomes a contained button, both in an empty week's row and inside an expanded
  week, where it sits **before** a table of the week's workouts with columns Nombre, Ejercicios (exercise count) and an
  edit icon button "Editar <name>" that opens `/workout?id=<id>`. The workout name is no longer a link.
- **Create mode** keeps the Atleta / Entrenador pickers inside the card; there are no person cards yet.
- Shared presentation pieces: an `Avatar` atom (the user menu switches to it), an initials helper moved out of the
  user menu, and a `PersonCard` molecule in `common`. Old `DurationField` and `MesocycleWeekWorkoutItem` molecules go.

Constraints: no business rule in the front, no API change, no core change, no new dependency. Components follow
atomic design with downward imports; only atoms import MUI widgets used by more than one file. No conflict with the
template, the invariants or a stop case was found.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `backoffice-workout-context`: the athlete and trainer show as person cards with avatar and role; the mesocycle entry
  becomes a card with a "<mesocycle> / Semana <n>" breadcrumb and no link.
- `backoffice-mesocycle-editor`: names move out of the general information card into person cards; the card's field
  layout changes; the duration stepper is replaced by a text field; week workouts show as a table with exercise count
  and edit action, with a contained "Nuevo entrenamiento" before it.

## Impact

- **Fronts and roles.** backoffice: `ADMIN` sees athlete and trainer cards on both screens; `TRAINER` sees only the
  athlete card and must not see any trainer card or name. Week rows, table and mesocycle card are the same for both.
  app: no change.
- **API.** None. Endpoints used, all in `core/openapi/v1.json`: `GET /mesocycles/{id}` (`athlete`, `trainer`),
  `GET /workouts/{id}` (`athlete`, `trainer`, `mesocycleId`, `mesocycleWeekNumber`), the mesocycle's workouts read by
  `useMesocycleWorkouts` (`WorkoutListItem.exercises` gives the count), `PUT /mesocycles/{id}`, `POST /mesocycles`,
  `POST /workouts`, all unchanged. No snapshot refresh.
- **Code (backoffice).**
  - `common`: new `Avatar` atom, new `initials.ts`, new `PersonCard` molecule; `UserMenu` organism uses them.
  - `workouts`: `WorkoutContextStrip` moves from molecule to organism; `WorkoutView` changes.
  - `mesocycles`: new `MesocycleContextStrip` organism and `MesocycleWeekWorkoutsTable` molecule; `MesocycleView`,
    `MesocycleDetailsCard`, `MesocycleWeeksCard`, `MesocycleWeekListItem` change; `DurationField` and
    `MesocycleWeekWorkoutItem` are removed.
  - Copy in `common`, `workouts`, `mesocycles` (`es`, `en`). No new error code.
- **Canonical references** (atom/molecule/organism references are `pending` in `.stpr/template/README.md`, so the
  backoffice shapes below are followed and `.stpr/template/` is not updated):
  - person card and strips -> `workouts/components/molecules/workout-context-strip.tsx` and
    `common/components/molecules/load-error.tsx`;
  - details layout -> `workouts/components/organisms/workout-details-card.tsx`;
  - week workouts table -> `workouts/components/organisms/workouts-table.tsx`;
  - contained add button -> `workouts/components/organisms/workout-exercises-card.tsx`;
  - `Avatar` atom -> `common/components/atoms/chip.tsx`; initials helper -> `common/full-name.ts`.
- **Tests.** backoffice unit (initials), component tests of the workout and mesocycle views, user menu test kept green,
  E2E `e2e/mesocycles.spec.ts` (and `e2e/workout.spec.ts` if affected) updated. API mocked throughout.
