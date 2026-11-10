# Spec Delta

## MODIFIED Requirements

### Requirement: Workout listings embed translations and muscle
`GET /workouts` SHALL return, for every workout entry, the referenced exercise's `translations`, `muscle`, `bodyPart` and `type` in place of the former `name`. Entries SHALL NOT contain `name`. `GET /workouts/{id}` SHALL carry the same exercise fields per entry, as `workout-retrieval` specifies.

#### Scenario: Listing a workout with exercises
- **WHEN** the owner sends `GET /workouts` and has a workout with the seeded Press Banca
- **THEN** that entry carries the translations of Press Banca, `muscle` `CHEST`, `bodyPart` `CHEST` and `type` `STRENGTH`
- **AND** the entry has no `name` field

#### Scenario: Reading a workout with exercises by id
- **WHEN** the owner sends `GET /workouts/{id}` for a workout with the seeded Press Banca
- **THEN** that entry carries the same `translations`, `muscle`, `bodyPart` and `type` as in `GET /workouts`
- **AND** the entry has no `name` field
