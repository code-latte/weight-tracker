# Spec Delta

## REMOVED Requirements

### Requirement: A started workout's details cannot be edited

**Reason**: a `STARTED`, `COMPLETED` or `REVIEWED` workout no longer opens the editor. It opens the read-only workout
review, so the locked properties card and its banner are no longer reachable for those statuses.

**Migration**: the review covers these workouts. It offers no editing action (`backoffice-workout-review`, "The review
is read-only") and keeps "Guardar como plantilla" (`backoffice-workout-review`, "The review can save the workout as a
template").
