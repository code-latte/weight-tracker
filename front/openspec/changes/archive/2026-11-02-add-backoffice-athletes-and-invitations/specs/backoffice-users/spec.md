# Spec Delta

## MODIFIED Requirements

### Requirement: The screen shows loading, empty, no-match and failure states

While the users load, the screen SHALL show a loading placeholder instead of the table. An empty list SHALL show
"Todavía no hay usuarios.". Filters that match nothing, including the hidden inactive users, SHALL show "Ningún usuario
coincide con la búsqueda.". A failed load SHALL show the error for its code with a retry action.

#### Scenario: Empty list

- **WHEN** the API returns no users
- **THEN** "Todavía no hay usuarios." is shown instead of rows

#### Scenario: No match

- **WHEN** users are loaded and the search matches none
- **THEN** "Ningún usuario coincide con la búsqueda." is shown instead of rows

#### Scenario: Only inactive users

- **WHEN** the API returns only inactive users and "Mostrar inactivos" is off
- **THEN** "Ningún usuario coincide con la búsqueda." is shown instead of rows

#### Scenario: Load fails and is retried

- **WHEN** the listing fails
- **THEN** the error for its code is shown with a retry action
- **AND** activating retry requests the users again

#### Scenario: Trainer opens the URL directly

- **WHEN** a trainer opens `/users` and the listing fails with `FORBIDDEN`
- **THEN** the error for `FORBIDDEN` is shown with a retry action
