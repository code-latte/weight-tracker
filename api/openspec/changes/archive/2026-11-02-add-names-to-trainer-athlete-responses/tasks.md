# Tasks

## 1. Application views and loaders

- [x] 1.1 Extend `TrainerAthleteView` with `Trainer`; add `InvitationView`; change `CreatedInvitation` to carry an `InvitationView`; verify the build succeeds
- [x] 1.2 Add `TrainerAthleteViewLoader` and `InvitationViewLoader` in `Application/Services` (one `GetAllByIds` per call, `UserNotFoundException` for a missing user) and register them in `Program.cs`; add unit tests in `test/WeightTracker.Unit/Application/Services` (builds views, single lookup, null invitee, missing user); verify the tests pass

## 2. Use cases return views

- [x] 2.1 `InviteAthleteUseCase` and `LinkAthleteToTrainerUseCase` build the view from the users they already loaded; update their unit tests to assert the view's users; verify the tests pass
- [x] 2.2 `RevokeInvitationUseCase`, `AcceptInvitationUseCase`, `RejectInvitationUseCase`, `GetTrainerInvitationsUseCase`, `EndTrainerAthleteRelationshipUseCase` and `GetTrainerAthletesUseCase` return views through the loaders; update their unit tests; verify the tests pass

## 3. API responses

- [x] 3.1 Add `trainerName`, `trainerSurname`, `userName`, `userSurname` to `InvitationResponse` and `CreatedInvitationResponse`; `trainerName`, `trainerSurname`, `athleteName`, `athleteSurname` to `TrainerAthleteResponse`; `trainerName`, `trainerSurname` to `TrainerAthleteListItemResponse`; update `AthleteInvitationsController` and `TrainerAthletesController`; verify the build succeeds
- [x] 3.2 Update the endpoint tests in `test/WeightTracker.Integration/AthleteInvitations` and `TrainerAthletes`: exact field sets, names of both sides on invite (registered and unregistered), revoke, accept, reject, list, link, end and both athlete lists; verify the tests pass
- [x] 3.3 Review against `TrainerAthleteView`/`GetTrainerAthletesUseCase` and the DTO rules in `STPR_INVARIANTS.md`; confirm no deviation

## 4. Verification

- [x] 4.1 Run `dotnet build WeightTracker.slnx` (no warnings), the full `dotnet test WeightTracker.slnx` with Docker, the parity check and `openspec validate add-names-to-trainer-athlete-responses --strict`; verify all pass

## Workflow follow-up

- Refresh the front OpenAPI snapshot in a separate front plan.
- Archive the change after review.
