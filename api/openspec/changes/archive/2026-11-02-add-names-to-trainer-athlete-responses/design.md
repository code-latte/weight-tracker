# Design

## Context

See `proposal.md`. Controllers may only call use cases (`STPR_INVARIANTS.md` §4), so the users must be loaded in
Application. `GetTrainerAthletesUseCase` already returns `TrainerAthleteView(Relationship, Athlete)` built with
`IUserRepository.GetAllByIds`.

## Goals / Non-Goals

**Goals:** one batch user lookup per request; names always current; additive response change.

**Non-Goals:** storing names on the aggregates; changing the aggregates or the schema.

## Decisions

- **Views in Application.** `TrainerAthleteView` becomes `(TrainerAthlete Relationship, User Trainer, User Athlete)`.
  New `InvitationView(TrainerInvitation Invitation, User Trainer, User? Invitee)` in `UseCases/AthleteInvitations`.
  `CreatedInvitation` becomes `(InvitationView View, string Token)`.
- **One shared loader per area.** Application services `InvitationViewLoader` and `TrainerAthleteViewLoader`
  (`Application/Services`) take aggregates, collect every referenced user id, call `GetAllByIds` once and build the
  views. Used by revoke, accept, reject, list invitations, end relationship and list athletes. Orchestration only, no
  business rules. Alternative considered: loading users inline in each use case — rejected, it would repeat the join
  six times.
- **Use cases that already hold the users build the view directly**: `InviteAthleteUseCase` (trainer and invitee
  already loaded) and `LinkAthleteToTrainerUseCase` (trainer and athlete already loaded).
- **Missing user.** Foreign keys guarantee every referenced user exists. A loader that cannot find one throws
  `UserNotFoundException` rather than returning a partial view.
- **Field names.** Invitations use `user*` (matching `userId`); relationships use `athlete*` (matching `athleteId`).
  List items keep their existing athlete `name`/`surname` and add `trainerName`/`trainerSurname`.

## Risks / Trade-offs

- [Revoke, accept, reject and end now do one extra query] → One batched `GetAllByIds` per request; acceptable.
- [Names change if a user edits their profile] → Intended: responses show current names.
