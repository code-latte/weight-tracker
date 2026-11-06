# Spec Delta

## ADDED Requirements

### Requirement: Exercises outside a superseries can be selected

Every saved exercise of the workout that is not in a superseries SHALL show a "Seleccionar para superserie" checkbox in
its card. Exercises inside a group card SHALL show no checkbox. An exercise whose superseries has no other listed
member SHALL be treated as outside a superseries.

#### Scenario: Checkboxes on ungrouped exercises

- **WHEN** the workout lists "Sentadilla", "Press banca" and "Dominadas", and only "Press banca" and "Dominadas" share
  a superseries
- **THEN** "Sentadilla" shows a "Seleccionar para superserie" checkbox
- **AND** "Press banca" and "Dominadas" show none

#### Scenario: Lone member

- **WHEN** "Sentadilla" carries a superseries id that no other listed exercise carries
- **THEN** "Sentadilla" is shown outside any group card, with a checkbox

### Requirement: The group button appears with two or more selected exercises

An "Agrupar en superserie" button with a link icon SHALL be shown above the exercises when two or more exercises are
checked, and SHALL NOT be rendered otherwise.

#### Scenario: One exercise checked

- **WHEN** the user checks "Sentadilla" only
- **THEN** no "Agrupar en superserie" button is shown

#### Scenario: Two exercises checked

- **WHEN** the user checks "Sentadilla" and "Press banca"
- **THEN** the "Agrupar en superserie" button is shown

#### Scenario: Unchecked back to one

- **WHEN** the user checks "Sentadilla" and "Press banca", then unchecks "Press banca"
- **THEN** no "Agrupar en superserie" button is shown

### Requirement: Grouping creates the superseries and moves its members together

"Agrupar en superserie" SHALL send one superseries creation with the entry ids of the checked exercises in list order.
On success it SHALL place the first checked exercise where it was and the others right after it, keep their relative
order, send one reorder with the full new order, and clear every checkbox.

#### Scenario: Members brought together

- **WHEN** the workout lists "Sentadilla", "Press banca", "Remo" and "Dominadas", the user checks "Press banca" and
  "Dominadas" and clicks "Agrupar en superserie", and the API accepts both requests
- **THEN** exactly one superseries creation is sent with the entry ids of "Press banca" and "Dominadas"
- **AND** exactly one reorder is sent with the catalog ids of "Sentadilla", "Press banca", "Dominadas" and "Remo" in
  that order
- **AND** "Press banca" and "Dominadas" are shown inside one group card, listed as 2 and 3, and "Remo" as 4
- **AND** no checkbox is checked and the button is gone

#### Scenario: Members already together

- **WHEN** the user groups "Sentadilla" and "Press banca", listed as 1 and 2, and the API accepts the creation
- **THEN** the order stays the same and no reorder is sent

### Requirement: A refused grouping is shown

When the superseries creation fails, the list and the checked exercises SHALL stay as they were, no reorder SHALL be
sent, and the message for the error code SHALL be shown in the exercises card; a `VALIDATION` failure SHALL explain
that only exercises with the same total number of sets can be grouped. When the creation succeeds but the
reorder fails, the members SHALL be shown as grouped in the order they had before, and the reorder's message SHALL be
shown.

#### Scenario: Different numbers of sets

- **WHEN** the API answers 400 to the creation
- **THEN** the list is unchanged, the checkboxes stay checked and no reorder is sent
- **AND** "Solo se pueden agrupar en superserie ejercicios con el mismo número total de series." is shown

#### Scenario: Reorder refused after the creation

- **WHEN** the user groups "Press banca" and "Dominadas", listed as 2 and 4, the creation is accepted and the API
  answers 409 to the reorder
- **THEN** the exercises keep their previous positions, and "Press banca" and "Dominadas" show no checkbox
- **AND** "La operación entra en conflicto con el estado actual." is shown

### Requirement: Superseries members are shown inside one group card

Consecutive listed exercises that share a superseries SHALL be shown inside one group card, which spans the full width
of the list and holds its members' cards in rows of up to three. The group card SHALL show a "Romper superserie" link
icon button in its top-left corner, followed by its drag handle and the title "Superserie". Numbering SHALL run continuously across group cards.

#### Scenario: Group card shown

- **WHEN** "Sentadilla" is listed first and "Press banca" and "Dominadas", listed as 2 and 3, share a superseries
- **THEN** one group card holds "Press banca" and "Dominadas" and shows "Romper superserie" in its top-left corner
- **AND** the group card shows the title "Superserie"
- **AND** "Sentadilla" is outside that card and numbered 1

#### Scenario: Members apart

- **WHEN** "Press banca" and "Dominadas" share a superseries and "Remo" is listed between them
- **THEN** "Press banca" and "Dominadas" are each shown in their own group card with a "Romper superserie" button

### Requirement: Romper superserie asks for confirmation

"Romper superserie" SHALL open a confirmation dialog. Cancelling SHALL close it without sending a request.

#### Scenario: Dialog opened

- **WHEN** the user clicks "Romper superserie" on the group of "Press banca" and "Dominadas"
- **THEN** a confirmation dialog to break the superseries is shown and no request is sent

#### Scenario: Cancelled

- **WHEN** the user opens the dialog and clicks "Cancelar"
- **THEN** the dialog closes, the group card stays and no request is sent

### Requirement: Confirming breaks the superseries

Confirming SHALL send exactly one break of that superseries. On success the dialog SHALL close, the members SHALL leave
the group card in their current positions and each SHALL show its checkbox again.

#### Scenario: Superseries broken

- **WHEN** the user confirms breaking the superseries of "Press banca" and "Dominadas" and the API accepts it
- **THEN** exactly one break of that superseries is sent
- **AND** "Press banca" and "Dominadas" are shown as separate cards, keeping their numbers, each with a checkbox

### Requirement: A refused break is shown

When the break fails, the group card SHALL stay as it was, the dialog SHALL close, and the message for the error code
SHALL be shown in the exercises card.

#### Scenario: Break rejected

- **WHEN** the API answers 404 to the break
- **THEN** the group card stays and "No hemos encontrado lo que buscas." is shown

### Requirement: Grouping and breaking are disabled while in progress

While a superseries creation, a reorder that follows it, or a break is in progress, "Agrupar en superserie" and the
dialog's confirm button SHALL be disabled, so a second request cannot start before the first is answered.

#### Scenario: Creation in progress

- **WHEN** a superseries creation has been sent and the API has not answered yet
- **THEN** "Agrupar en superserie" is disabled

#### Scenario: Break in progress

- **WHEN** a break has been confirmed and the API has not answered yet
- **THEN** the dialog's confirm button is disabled

## MODIFIED Requirements

### Requirement: Dropping a saved exercise reorders the list and saves the order

Dragging a saved exercise outside a superseries, or a group card, by its handle and dropping it at another position
SHALL reorder the list on screen at once, renumbering it. A group card SHALL move all its members together, in their
order, and no exercise SHALL be dropped between two members of a group. The screen SHALL then send exactly one reorder
of the workout with the catalog ids of every saved exercise in the new order. On success the list SHALL keep the new
order.

#### Scenario: Exercise moved down

- **WHEN** the workout lists "Sentadilla" then "Press banca" and the user drops "Sentadilla" after "Press banca"
- **THEN** "Press banca" is listed as 1 and "Sentadilla" as 2 before the API answers
- **AND** exactly one reorder is sent with the ids of "Press banca" and "Sentadilla" in that order

#### Scenario: Reorder accepted

- **WHEN** the API accepts the reorder
- **THEN** the list keeps the new order and no error is shown

#### Scenario: Group moved

- **WHEN** the workout lists "Sentadilla", then a group card with "Press banca" and "Dominadas", and the user drops the
  group card before "Sentadilla"
- **THEN** "Press banca" is listed as 1, "Dominadas" as 2 and "Sentadilla" as 3 before the API answers
- **AND** exactly one reorder is sent with the ids of "Press banca", "Dominadas" and "Sentadilla" in that order

#### Scenario: Members have no handle

- **WHEN** "Press banca" and "Dominadas" are shown inside a group card
- **THEN** the group card shows one "Reordenar superserie" handle
- **AND** neither "Press banca" nor "Dominadas" shows a "Reordenar ejercicio" handle

### Requirement: Listed exercises are cards in rows of up to three

Each exercise of the workout SHALL be shown in its own bordered card. The cards SHALL flow left to right in rows of at
most three, wrapping onto the next row, with fewer per row on narrower screens. Inside a card, the first row SHALL hold
the drag handle and the selection checkbox when the exercise is outside a superseries, the position, the name and the
action icons; the second the body-part and muscle labels; the third the sets.

#### Scenario: Rows inside a card

- **WHEN** "Sentadilla", listed first, outside a superseries, with the sets 3 × 10, is shown and the catalog has more
  than one legs muscle
- **THEN** its card's first row holds "Reordenar ejercicio", "Seleccionar para superserie", "1", "Sentadilla",
  "Quitar ejercicio" and "Editar ejercicio"
- **AND** the labels "Piernas" and "Cuádriceps" come below that row, and "3 × 10" below the labels

#### Scenario: Exercises in a row

- **WHEN** the workout has two exercises on a wide screen
- **THEN** their cards sit side by side in the same row, the first on the left

#### Scenario: At most three per row

- **WHEN** the workout has four exercises on a wide screen
- **THEN** the first three share a row and the fourth starts the next one

#### Scenario: Member card

- **WHEN** "Press banca", listed as 2, is inside a group card
- **THEN** its card's first row holds "2", "Press banca", "Quitar ejercicio" and "Editar ejercicio", with no handle and
  no checkbox
