# Proposal

## Why

Invitation and trainer–athlete responses carry only ids (`trainerId`, `userId`, `athleteId`). To show who is who, the
frontend has to fetch each user separately. Returning the names of both sides in the same response removes those calls.

## What Changes

- Every invitation response (`POST`, `GET`, `revoke`, `accept`, `reject` on `/athlete-invitations`) adds
  `trainerName`, `trainerSurname`, `userName` and `userSurname`. `userName` and `userSurname` are `null` when the
  invitation has no `userId` (unregistered email).
- Every trainer–athlete relationship response (`POST /trainer-athletes/{trainerId}`,
  `PUT /trainer-athletes/{trainerId}/end`) adds `trainerName`, `trainerSurname`, `athleteName` and `athleteSurname`.
  The relationship side is called `athlete`, matching its existing `athleteId`.
- List items of `GET /trainer-athletes` and `GET /trainer-athletes/{trainerId}` add `trainerName` and
  `trainerSurname`. They already carry the athlete's `name` and `surname`, which keep their names (not breaking).
- Additive only: no field is removed or renamed.

## Capabilities

### New Capabilities

### Modified Capabilities
- `trainer-invitations`: invitation responses carry both sides' names; the exact list-item field set grows.
- `trainer-athletes`: relationship responses carry both sides' names; the exact list-item field set grows.

## Canonical Template References

| Piece | Reference | Why |
| --- | --- | --- |
| Use case returning aggregate plus users | `src/WeightTracker.Application/UseCases/TrainerAthletes/TrainerAthleteView.cs` + `GetTrainerAthletesUseCase` | Existing view record joining a relationship with its athlete |
| Batch user lookup | `IUserRepository.GetAllByIds` | Already used for the same join |
| Response DTOs | `src/WeightTracker.Api/Endpoints/*/Dtos` with static `From(...)` | DTO rule in `STPR_INVARIANTS.md` §4 |
| Endpoint tests | existing `AthleteInvitations` and `TrainerAthletes` endpoint tests | Field-shape assertions already there |

## Business Identifiers And Mutability

No identifier changes. Names are read from `User` at response time, not stored on the invitation or relationship, so
they always show the current name.

## Out Of Scope

- Renaming the existing athlete `name`/`surname` fields of list items.
- Emails of the trainer or athlete (only the invited email is already present on invitations).
- Any schema change.

## Impact

- **Application**: use cases return views with the users loaded (`InvitationView`, extended `TrainerAthleteView`,
  `CreatedInvitation`); revoke, accept, reject and end use cases gain `IUserRepository`.
- **Api**: `InvitationResponse`, `CreatedInvitationResponse`, `TrainerAthleteResponse`,
  `TrainerAthleteListItemResponse` add the name fields.
- **Tests**: unit tests for the changed use cases; endpoint tests for each response shape.
- **Clients**: additive fields only.
