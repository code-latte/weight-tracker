# backoffice-exercise-catalog-cache Specification

## Purpose

Keeps one exercise catalog per signed-in backoffice session: read in the background right after sign-in, shared by
every screen that needs it, refreshed after the user changes an exercise of their own, and dropped when the session
ends.

## Requirements

### Requirement: The catalog is read in the background after sign-in

Once a signed-in `ADMIN` or `TRAINER` session is confirmed, the backoffice SHALL send one read of the exercise catalog
without waiting for it: the shell, the drawer, the header and the current screen SHALL render as they do without it.

#### Scenario: Read after sign-in

- **WHEN** a trainer signs in and lands on the home screen
- **THEN** exactly one read of the exercise catalog is sent
- **AND** the home screen is shown before that read answers

#### Scenario: Read after a page load with a session

- **WHEN** an admin with a stored session loads any signed-in page
- **THEN** exactly one read of the exercise catalog is sent

#### Scenario: No read without a session

- **WHEN** a visitor without a session opens `/login`
- **THEN** no read of the exercise catalog is sent

### Requirement: Screens share the session's catalog

The exercise catalog page, the workout editor and the workout template editor SHALL use the catalog read for the
session and SHALL NOT read it again when they open. A screen opened while that read is still running SHALL show its
loading state until the same read answers, without sending another.

#### Scenario: One read across screens

- **GIVEN** the catalog was read after sign-in
- **WHEN** the trainer opens `/exercises`, then a workout, then a workout template
- **THEN** no further read of the exercise catalog is sent

#### Scenario: Opened during the first read

- **WHEN** the trainer opens a workout before the catalog read after sign-in has answered
- **THEN** the screen shows its loading state, no second catalog read is sent, and the editor is shown when that read
  answers

### Requirement: A failed catalog read is retried from the screen

If the catalog read fails, nothing SHALL be shown at that moment. A screen that needs the catalog and has none SHALL
show its existing error state; its retry SHALL read the catalog again, and every screen SHALL use the result.

#### Scenario: Failure after sign-in

- **GIVEN** the catalog read after sign-in failed
- **WHEN** the trainer opens `/exercises`
- **THEN** the screen shows the message for the error code with a way to retry

#### Scenario: Retry

- **GIVEN** the catalog read failed and `/exercises` shows its error
- **WHEN** the trainer retries and the read succeeds
- **THEN** the catalog is listed, and opening a workout afterwards sends no catalog read

### Requirement: The catalog is refreshed after an own exercise is created

When the signed-in user creates an exercise, the backoffice SHALL keep it listed at once and SHALL then read the
catalog again in the background. Meanwhile every screen SHALL keep showing the current catalog, without a loading
state. If that read fails, the current catalog SHALL be kept.

#### Scenario: Refresh after creation

- **WHEN** a trainer creates an exercise from `/exercises`
- **THEN** it is listed at once and exactly one more read of the catalog is sent
- **AND** the list stays visible while that read runs

#### Scenario: Failed refresh

- **WHEN** the read after a creation fails
- **THEN** the catalog, including the created exercise, stays as it was and no error is shown

### Requirement: The catalog offers a refresh for own exercise updates

The session's catalog SHALL offer the same background refresh for an update of one of the user's own exercises, so a
flow that updates an exercise reads the catalog again after the API accepts the change. The backoffice SHALL NOT read
the catalog again for any other reason than sign-in, a retry, a creation or such an update.

#### Scenario: Navigation alone does not refresh

- **GIVEN** the catalog was read after sign-in
- **WHEN** the trainer moves between screens for the rest of the session without creating or updating an exercise
- **THEN** no further read of the exercise catalog is sent

### Requirement: The catalog is dropped when the session ends

Signing out or the session ending SHALL drop the kept catalog. An answer that arrives after that SHALL be ignored. The
next sign-in SHALL read the catalog again.

#### Scenario: Sign out and back in

- **GIVEN** a trainer's catalog was read
- **WHEN** the trainer signs out and an admin signs in on the same tab
- **THEN** a new read of the catalog is sent and the admin's screens use only its answer
