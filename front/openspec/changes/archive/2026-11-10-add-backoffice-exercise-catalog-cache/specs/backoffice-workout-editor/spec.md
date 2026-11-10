# Spec Delta

## MODIFIED Requirements

### Requirement: The workout screen is reached by the workout id

The backoffice SHALL offer a signed-in screen at `/workout?id=<workout id>` that reads that workout once on opening and
uses the session's exercise catalog. A visitor without a session SHALL end on `/login`.

#### Scenario: Trainer opens a workout

- **WHEN** a trainer with a stored session opens `/workout?id=w-1`
- **THEN** exactly one read of workout `w-1` is sent and no read of the exercise catalog, the catalog having been
  read after sign-in
- **AND** the screen is shown beside the navigation drawer

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/workout?id=w-1`
- **THEN** the visitor ends on `/login` and no workout is read
