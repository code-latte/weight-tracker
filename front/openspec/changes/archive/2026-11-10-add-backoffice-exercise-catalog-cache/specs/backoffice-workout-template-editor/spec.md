# Spec Delta

## MODIFIED Requirements

### Requirement: The template screen is reached by the template id

The backoffice SHALL offer a signed-in screen at `/workout-template?id=<template id>` that reads that template and the
user's favorites once on opening and uses the session's exercise catalog. A visitor without a session SHALL end on `/login`.

#### Scenario: Trainer opens a template

- **WHEN** a trainer with a stored session opens `/workout-template?id=wt-1`
- **THEN** exactly one read of template `wt-1` and one of the favorites are sent, and no read of the exercise
  catalog, the catalog having been read after sign-in
- **AND** the screen is shown beside the navigation drawer

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/workout-template?id=wt-1`
- **THEN** the visitor ends on `/login` and no template is read
