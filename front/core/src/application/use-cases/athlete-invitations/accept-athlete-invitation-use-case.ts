import type {
  AthleteInvitation,
  DomainError,
  InvitationResponseInput,
  Result,
} from "../../../domain";
import type { AthleteInvitationRepositoryInterface, UseCaseInterface } from "../../ports";

/** Accepts an invitation with its raw token. Whether the token and the invitation are valid belongs to the API. */
export default class AcceptAthleteInvitationUseCase implements UseCaseInterface<
  InvitationResponseInput,
  AthleteInvitation
> {
  constructor(private readonly athleteInvitationRepository: AthleteInvitationRepositoryInterface) {}

  execute({
    invitationId,
    ...request
  }: InvitationResponseInput): Promise<Result<AthleteInvitation, DomainError>> {
    return this.athleteInvitationRepository.accept(invitationId, request);
  }
}
