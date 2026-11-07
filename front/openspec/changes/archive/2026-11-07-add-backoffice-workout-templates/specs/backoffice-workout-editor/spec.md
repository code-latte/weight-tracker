# Spec Delta

## ADDED Requirements

### Requirement: The properties card offers Guardar como plantilla

The properties card SHALL show an outlined "Guardar como plantilla" button immediately to the left of its "Guardar",
for both admins and trainers. It SHALL be disabled while a field differs from the saved workout, with the hint "Guarda
los cambios antes de crear la plantilla.", and while its own request is in progress.

#### Scenario: Button shown

- **WHEN** a workout is loaded
- **THEN** "Guardar como plantilla" is shown, outlined, right before "Guardar" in the properties card

#### Scenario: Unsaved edits

- **WHEN** the user changes Nombre and has not saved yet
- **THEN** "Guardar como plantilla" is disabled and shows "Guarda los cambios antes de crear la plantilla."

#### Scenario: Saved again

- **WHEN** the user then clicks "Guardar" and the update succeeds
- **THEN** "Guardar como plantilla" is enabled again

### Requirement: Guardar como plantilla creates a template from the saved workout

Activating "Guardar como plantilla" SHALL send exactly one template creation from the workout's id, with no body, and
SHALL NOT send any workout update. On success the screen SHALL show "Plantilla creada." with a "Ver plantilla" link to
`/workout-template?id=<new id>`, and the user SHALL stay on the workout.

#### Scenario: Template created

- **WHEN** the user clicks "Guardar como plantilla" on workout `w-1` and the API answers with template `wt-9`
- **THEN** exactly one template creation from workout `w-1` is sent and no workout update
- **AND** "Plantilla creada." is shown with "Ver plantilla" leading to `/workout-template?id=wt-9`
- **AND** the user is still on `/workout?id=w-1`

#### Scenario: Template creation refused

- **WHEN** the creation fails with `VALIDATION`
- **THEN** the message for `VALIDATION` is shown in the properties card and the workout stays unchanged

### Requirement: Guardar como plantilla is translated

The button, its hint and its outcome SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the button reads "Save as template" and the confirmation "Template created."
