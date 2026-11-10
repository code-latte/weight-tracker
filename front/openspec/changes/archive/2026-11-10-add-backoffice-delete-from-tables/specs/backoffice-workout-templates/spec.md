# Spec Delta

## ADDED Requirements

### Requirement: Each template row offers a delete action

Each row SHALL show, after the edit action, a trash icon button labelled "Eliminar <template name>". Admins and
trainers SHALL see it on every row. Pressing it SHALL open the confirmation "¿Eliminar esta plantilla?" with the text
"Los entrenamientos creados desde ella no cambian." and the buttons "Cancelar" and "Eliminar". Pressing it SHALL NOT
send any request.

#### Scenario: Trainer opens the confirmation

- **WHEN** a trainer presses "Eliminar Fuerza A" on the templates screen
- **THEN** the dialog "¿Eliminar esta plantilla?" opens with "Cancelar" and "Eliminar"
- **AND** no deletion is sent

#### Scenario: Admin sees the delete action

- **WHEN** an admin opens the templates screen with one template "Fuerza A"
- **THEN** its row shows both "Editar Fuerza A" and "Eliminar Fuerza A"

#### Scenario: Cancel sends nothing

- **WHEN** the user presses "Cancelar", presses Escape or clicks outside the dialog
- **THEN** the dialog closes and no deletion is sent

### Requirement: Confirming deletes the template and refreshes the list

"Eliminar" in the dialog SHALL send exactly one deletion of that row's template. While it is in progress, both
buttons SHALL be disabled and the dialog SHALL NOT close. On success the dialog SHALL close and the screen SHALL read
the templates again. On failure the dialog SHALL close, the screen SHALL show the message for the error code above
the table, and the list SHALL stay as it was.

#### Scenario: Template deleted

- **WHEN** the user confirms the deletion of template `t-1` and it succeeds
- **THEN** exactly one deletion of `t-1` is sent
- **AND** the dialog closes and the templates are read again

#### Scenario: Deletion fails

- **WHEN** the user confirms a deletion and it fails with `FORBIDDEN`
- **THEN** the dialog closes and the message for `FORBIDDEN` is shown above the table
- **AND** the rows are unchanged

### Requirement: The template delete action is translated

The delete action's label and its dialog SHALL be available in Spanish and English and SHALL follow the active
language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the row action is labelled "Delete <template name>" and the dialog is titled "Delete this template?"
