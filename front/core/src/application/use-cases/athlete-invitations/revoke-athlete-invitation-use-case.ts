import type { AthleteInvitation, DomainError, Result } from "../../../domain";
import type { AthleteInvitationRepositoryInterface, UseCaseInterface } from "../../ports";

/** Revokes a pending invitation. Who may revoke it, and in which state, belong to the API. */
export default class RevokeAthleteInvitationUseCase implements UseCaseInterface<
  string,
  AthleteInvitation
> {
  constructor(private readonly athleteInvitationRepository: AthleteInvitationRepositoryInterface) {}

  execute(invitationId: string): Promise<Result<AthleteInvitation, DomainError>> {
    return this.athleteInvitationRepository.revoke(invitationId);
  }
}
