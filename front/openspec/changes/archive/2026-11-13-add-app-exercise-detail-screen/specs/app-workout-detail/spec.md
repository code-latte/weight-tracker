# Spec Delta

## MODIFIED Requirements

### Requirement: Each row shows a details caret

On its right, each row SHALL show a right caret labelled "Ver detalle". Tapping the row or the caret SHALL open the
exercise screen at `/workouts/<workout id>/exercises/<workout exercise id>`.

#### Scenario: Caret

- **WHEN** the screen lists an exercise
- **THEN** its row shows a caret labelled "Ver detalle"

#### Scenario: Row opens the exercise

- **WHEN** the athlete taps the row of workout exercise `we1` of workout `wk1`
- **THEN** the app navigates to `/workouts/wk1/exercises/we1`

## REMOVED Requirements

### Requirement: Each row links to the exercise's video

**Reason**: the video link lives in the exercise screen's header, one tap away, so the rows no longer repeat it.
**Migration**: open the exercise from its row and use "Ver vídeo" in the exercise screen's header.

## ADDED Requirements

### Requirement: Each row shows the exercise's body part and muscle

Under the exercise name, a row SHALL show a small filled orange chip with the exercise's body part and, beside it, a
small outlined orange chip with its muscle, both named in the active language. When the exercise catalog has a single muscle
for that body part, the row SHALL show the body part chip alone. While the catalog is not read, or when it cannot be,
the row SHALL show both chips.

#### Scenario: Body part with several muscles

- **WHEN** an exercise works the quadriceps (legs) and the catalog has more than one muscle for legs
- **THEN** its row shows "Piernas" filled and "Cuádriceps" outlined

#### Scenario: Body part with one muscle

- **WHEN** an exercise's body part has a single muscle in the catalog
- **THEN** its row shows the body part chip only

#### Scenario: Catalog unavailable

- **WHEN** the catalog cannot be read
- **THEN** every row shows both chips

### Requirement: The panel offers to start a published workout

Next to the progress bar, the panel SHALL show a "Iniciar entrenamiento" button when the workout's status is `PUBLISHED`. Tapping
it SHALL change the workout's status to `STARTED` and re-read the workout. The button SHALL be disabled while the
change is in flight.

#### Scenario: Published workout started

- **WHEN** the workout is `PUBLISHED` and the athlete taps "Iniciar entrenamiento"
- **THEN** exactly one status change to `STARTED` is sent for that workout
- **AND** once it succeeds the workout is read again and the panel shows a "Finalizar entrenamiento" button

#### Scenario: Start fails

- **WHEN** the status change fails with `CONFLICT`
- **THEN** the error for `CONFLICT` is shown below the panel
- **AND** "Iniciar entrenamiento" stays enabled and the workout is not read again

### Requirement: The panel offers to finish a started workout

When the workout's status is `STARTED`, the panel SHALL show a "Finalizar entrenamiento" button with a stop icon in the same place.
Tapping it SHALL open a confirmation dialog and send nothing yet.

#### Scenario: Question before finishing

- **WHEN** the workout is `STARTED` and the athlete taps "Finalizar entrenamiento"
- **THEN** a confirmation dialog is shown
- **AND** no status change is sent

### Requirement: The confirmation says what finishing does

The dialog SHALL be titled "¿Finalizar el entrenamiento?" and say the workout goes to the athlete's trainer for review
and that the records can be changed until they review it. For a workout without a trainer it SHALL say it will be
marked as completed and the records can still be changed.

#### Scenario: With a trainer

- **WHEN** the dialog opens for a workout with a trainer
- **THEN** it reads "Se enviará a tu entrenador para su revisión. Podrás cambiar los registros hasta que lo revise."

#### Scenario: No trainer

- **WHEN** the dialog opens for a workout without a trainer
- **THEN** it reads "Quedará completado. Podrás seguir cambiando los registros."

### Requirement: Confirming finishes the workout and cancelling does not

Confirming SHALL change the workout's status to `COMPLETED` and read the workout again. Cancelling SHALL close the
dialog and send nothing.

#### Scenario: Finish confirmed

- **WHEN** the athlete confirms the dialog
- **THEN** exactly one status change to `COMPLETED` is sent for that workout
- **AND** once it succeeds the workout is read again and the panel shows a disabled "Finalizado" button

#### Scenario: Finish cancelled

- **WHEN** the athlete taps "Cancelar" in the dialog
- **THEN** the dialog closes and no status change is sent

#### Scenario: Finish fails

- **WHEN** the status change fails with `CONFLICT`
- **THEN** the dialog closes and the error for `CONFLICT` is shown below the panel
- **AND** the workout is not read again

### Requirement: The panel shows a finished workout as finished

When the workout's status is `COMPLETED` or `REVIEWED`, the panel SHALL show a disabled "Finalizado" button in the same
place. When it is `DRAFT`, the panel SHALL show no button. In English the buttons SHALL read "Start workout", "Finish workout" and
"Finished".

#### Scenario: Completed workout

- **WHEN** the workout is `COMPLETED`
- **THEN** the panel shows a disabled "Finalizado" button and no "Iniciar entrenamiento" or "Finalizar entrenamiento" button

#### Scenario: Reviewed workout

- **WHEN** the workout is `REVIEWED`
- **THEN** the panel shows a disabled "Finalizado" button

#### Scenario: Draft workout

- **WHEN** the workout is `DRAFT`
- **THEN** the panel shows neither "Iniciar entrenamiento", "Finalizar entrenamiento" nor "Finalizado"

#### Scenario: English

- **WHEN** the active language is English and the workout is `PUBLISHED`
- **THEN** the button reads "Start workout"
