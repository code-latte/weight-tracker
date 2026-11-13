# Spec Delta

## ADDED Requirements

### Requirement: Events are dispatched after the change that raised them is saved
Events raised by an aggregate SHALL be dispatched only after the change that raised them is saved. A change that fails to save SHALL dispatch none of its events. Once dispatched, the events SHALL be cleared from the aggregate so they are never delivered twice. A handler failure SHALL NOT undo the saved change.

#### Scenario: Completed workout
- **WHEN** a prescribed workout is completed and the completion is saved
- **THEN** its completion event is dispatched once

#### Scenario: Save fails
- **WHEN** completing a workout fails while saving
- **THEN** its completion event is not dispatched

#### Scenario: Handler fails after save
- **WHEN** a handler of the completion event fails
- **THEN** the workout stays `COMPLETED`
