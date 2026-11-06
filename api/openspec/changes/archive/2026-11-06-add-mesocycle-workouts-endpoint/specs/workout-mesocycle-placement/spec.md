# Spec Delta

## MODIFIED Requirements

### Requirement: Workout responses expose the placement
`POST /workouts`, `GET /workouts/{id}` and `PUT /workouts/{id}` SHALL return `mesocycleId` and `mesocycleWeekNumber`, both `null` for a standalone workout. The workout list items of `GET /workouts` and `GET /mesocycles/{id}/workouts` SHALL carry the same two properties, as `workout-retrieval` and `mesocycle-retrieval` specify.

#### Scenario: Reading a linked workout
- **WHEN** an allowed caller reads the seeded workout placed in week 1 of the seeded mesocycle
- **THEN** the response has the mesocycle's id and week number 1

#### Scenario: Reading a standalone workout
- **WHEN** an allowed caller reads a workout with no placement
- **THEN** `mesocycleId` and `mesocycleWeekNumber` are both `null`

#### Scenario: Listing a linked workout
- **WHEN** an allowed caller lists workouts and the seeded workout placed in week 1 of the seeded mesocycle is listed
- **THEN** its item has the mesocycle's id and week number 1
