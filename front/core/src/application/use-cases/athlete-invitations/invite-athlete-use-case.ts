import type {
  CreatedAthleteInvitation,
  DomainError,
  InviteAthleteRequest,
  Result,
} from "../../../domain";
import type { AthleteInvitationRepositoryInterface, UseCaseInterface } from "../../ports";

/** Invites an email to become the caller's athlete. Whether that email can be invited belongs to the API: the request is
 * forwarded unchanged. */
export default class InviteAthleteUseCase implements UseCaseInterface<
  InviteAthleteRequest,
  CreatedAthleteInvitation
> {
  constructor(private readonly athleteInvitationRepository: AthleteInvitationRepositoryInterface) {}

  execute(request: InviteAthleteRequest): Promise<Result<CreatedAthleteInvitation, DomainError>> {
    return this.athleteInvitationRepository.invite(request);
  }
}
