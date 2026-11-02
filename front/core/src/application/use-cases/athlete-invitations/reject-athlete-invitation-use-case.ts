import type {
  AthleteInvitation,
  DomainError,
  InvitationResponseInput,
  Result,
} from "../../../domain";
import type { AthleteInvitationRepositoryInterface, UseCaseInterface } from "../../ports";

/** Rejects an invitation with its raw token. Whether the token and the invitation are valid belongs to the API. */
export default class RejectAthleteInvitationUseCase implements UseCaseInterface<
  InvitationResponseInput,
  AthleteInvitation
> {
  constructor(private readonly athleteInvitationRepository: AthleteInvitationRepositoryInterface) {}

  execute({
    invitationId,
    ...request
  }: InvitationResponseInput): Promise<Result<AthleteInvitation, DomainError>> {
    return this.athleteInvitationRepository.reject(invitationId, request);
  }
}
