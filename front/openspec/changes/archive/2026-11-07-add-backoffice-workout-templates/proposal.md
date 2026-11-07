# Proposal

## Why

The API now offers workout templates (`/workout-templates`, `POST /workouts/{workoutId}/templates`,
`POST /workout-templates/{templateId}/workouts`), but `core/openapi/v1.json` predates them and the "Plantillas de
entrenamiento" drawer entry leads nowhere. Trainers rebuild the same session by hand for every athlete and week; a
template library they can edit like a workout, start workouts from, and fill from existing workouts removes that work.

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-workout-templates.md`.

## What Changes

- **Contract (own commit).** Refresh the snapshot (`openapi:refresh` + `openapi:generate`) so the template endpoints
  and schemas exist in `core/openapi/v1.json` and `core/src/contract/generated`.
- **Core.** Template domain aliases, a `WorkoutTemplateRepositoryInterface` port and its HTTP repository, and one use
  case per operation: list, read, create, update, delete, add / update / remove exercise, reorder exercises, create /
  break superseries, create a workout from a template, create a template from a workout. Registered in **both**
  `container.ts` files (they share one `Dependencies` type); only the backoffice uses them.
- **Drawer.** "Plantillas de entrenamiento" links to `/workout-templates` and is selected there.
- **Templates list** at `/workout-templates`: table of the templates the API returns (a trainer's own; every
  trainer's for an `ADMIN`, with a trainer column), "Nueva plantilla" button, loading / empty / failure states.
- **Create template dialog**: Nombre, Descripción and, for an `ADMIN` only, Entrenador. No date, mesocycle, week or
  athlete. Success opens the template editor.
- **Template editor** at `/workout-template?id=`: the workout screen's properties card (Nombre, Descripción, no week)
  and its exercises card with the full exercise flow (catalog dialog, configuration dialog, remove, reorder,
  superseries, favorites), plus "Eliminar plantilla" with confirmation. No athlete / trainer person cards, no week.
- **Create workout from a template**: the standalone create workout dialog and the mesocycle week dialog get a section
  list on their left, like the exercise catalog dialog: "Desde cero" (today's form, unchanged) and "Desde plantilla"
  (the trainer's templates with name, description and exercise count). Creating from a template opens the new workout.
- **Save a workout as a template**: an outlined "Guardar como plantilla" button just left of the properties card's
  "Guardar" on the workout screen; it creates a template from the workout as saved and confirms it.

No breaking change: today's "Desde cero" forms, the workout screen's behaviour and every other drawer entry stay as they are.

## Decisions taken from the brief's open points

- **Where the template screens live.** The template editor reuses the workout editor's organisms, so the template
  screens, hooks and dialogs live in the existing `presentation/workouts` domain area instead of a new
  `workout-templates` area. This keeps every shared organism where it is, with no cross-area import and no
  copy-pasted editor. Only the pieces the mesocycle dialog also needs (template picker, section list and their hook)
  move to or are created in `presentation/common`. This refines the brief's classification, which left the split to
  this proposal.
- **One editor, two targets.** The exercise-editing part of the workout screen becomes one organism fed by hooks that
  take a target (`workout` or `template`) and resolve the matching use cases. It is not a second copy.
- **Save as template with unsaved edits.** "Guardar como plantilla" is disabled while the properties card has unsaved
  edits, with a hint that the template copies the saved workout.
- **After saving as a template.** A confirmation alert "Plantilla creada." with a "Ver plantilla" link to the new
  template; the user stays on the workout.
- **Delete** lives in the template editor only; the table keeps one edit action per row, like the workouts table.

## Capabilities

### New Capabilities

- `workout-template-management`: `core` operations on workout templates and their exercises and superseries, creating a
  workout from a template and a template from a workout, each returning the API's answer or a `DomainError`.
- `backoffice-workout-templates`: the backoffice templates list at `/workout-templates` and its create template dialog.
- `backoffice-workout-template-editor`: the backoffice template screen at `/workout-template?id=`, editing a
  template like a workout and deleting it.

### Modified Capabilities

- `backoffice-navigation`: "Plantillas de entrenamiento" stops being a placeholder and links to `/workout-templates`.
- `backoffice-workout-creation`: the create workout dialog gains the "Desde cero" / "Desde plantilla" sections and
  the creation of a standalone workout from a template.
- `backoffice-mesocycle-editor`: the week "Nuevo entrenamiento" dialog gains the same sections and creates the week's
  workout from a template.
- `backoffice-workout-editor`: the properties card gains "Guardar como plantilla".

## Impact

- **API contract**: `core/openapi/v1.json` and `core/src/contract/generated` regenerated; no API change (every endpoint
  already exists in `api/`). Needs the API running locally for `openapi:refresh`.
- **Core**: new domain file, port, repository and `use-cases/workout-templates/`; `dependencies.ts`; HTTP error mapper
  unchanged (the API maps the template exceptions to 400 / 403 / 404 / 409, already mapped).
- **Backoffice**: drawer, two routes, two views, new organisms, generalised workout editor organisms and hooks, create
  workout dialog, mesocycle week dialog, workout screen, `workouts` / `common` / `mesocycles` copy in `es` and `en`,
  container, component tests and E2E.
- **App**: `container.ts`, its test-support placeholders and container test only; no screen or copy.
- **Template**: `.stpr/template/` is not changed; atomic UI references stay `pending`.
