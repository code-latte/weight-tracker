# Spec Delta

## ADDED Requirements

### Requirement: A placed workout's date is the date of its week
When `POST /workouts` or `PUT /workouts/{id}` sends a mesocycle placement, the stored `workoutDate` SHALL be the date of that mesocycle week, and any `workoutDate` that is sent SHALL be ignored without error. On `PUT`, the date SHALL be recalculated whenever a placement is sent, even one equal to the current placement. Taking a workout out of its mesocycle SHALL store the sent `workoutDate`.

#### Scenario: Create with a conflicting date
- **WHEN** the seeded trainer creates a workout for the seeded athlete in week 3 of the seeded mesocycle, which starts on Thursday `2026-10-01`, sending `workoutDate` `2026-12-25`
- **THEN** the response is `201` with `workoutDate` `2026-10-14`

#### Scenario: Create without a date
- **WHEN** the seeded trainer creates a workout for the seeded athlete in week 1 of the seeded mesocycle without `workoutDate`
- **THEN** the response is `201` with `workoutDate` `2026-09-30`

#### Scenario: Move to another week
- **WHEN** the trainer moves the published workout from week 1 to week 3 of the seeded mesocycle
- **THEN** its `workoutDate` is `2026-10-14`

#### Scenario: Unchanged placement after a reschedule
- **WHEN** the seeded mesocycle is rescheduled to `2026-10-08` and the trainer then updates the published workout in week 1, keeping its placement
- **THEN** its `workoutDate` is `2026-10-07`

#### Scenario: Unlink with a date
- **WHEN** the trainer takes the published workout out of the seeded mesocycle sending `workoutDate` `2026-11-04`
- **THEN** the workout is standalone with `workoutDate` `2026-11-04`

#### Scenario: Unlink without a date
- **WHEN** the trainer takes the published workout out of the seeded mesocycle without `workoutDate`
- **THEN** the response is `400` and the workout keeps its placement and date

#### Scenario: Placement refused
- **WHEN** a placement is refused for any placement rule
- **THEN** the workout's `workoutDate` is unchanged

### Requirement: Rescheduling a mesocycle does not move its workouts
Changing a mesocycle's start date SHALL NOT change the `workoutDate` of the workouts placed in it. Their date SHALL be recalculated only on their next `PUT /workouts/{id}` with a placement.

#### Scenario: Reschedule leaves workout dates
- **WHEN** the seeded mesocycle is rescheduled to `2026-10-08`
- **THEN** the published workout in week 1 still has `workoutDate` `2026-09-30`
