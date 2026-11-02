import type { AthleteInvitation, DomainError, Result } from "../../../domain";
import type { AthleteInvitationRepositoryInterface, UseCaseInterface } from "../../ports";

/** Lists the invitations the caller may see. Which ones that is, per role, is decided by the API. */
export default class GetAthleteInvitationsUseCase implements UseCaseInterface<
  void,
  AthleteInvitation[]
> {
  constructor(private readonly athleteInvitationRepository: AthleteInvitationRepositoryInterface) {}

  execute(): Promise<Result<AthleteInvitation[], DomainError>> {
    return this.athleteInvitationRepository.getAll();
  }
}
