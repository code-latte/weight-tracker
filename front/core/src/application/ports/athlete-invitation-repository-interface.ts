import type {
  AthleteInvitation,
  CreatedAthleteInvitation,
  DomainError,
  InvitationTokenRequest,
  InviteAthleteRequest,
  Result,
} from "../../domain";

export default interface AthleteInvitationRepositoryInterface {
  getAll(): Promise<Result<AthleteInvitation[], DomainError>>;
  invite(request: InviteAthleteRequest): Promise<Result<CreatedAthleteInvitation, DomainError>>;
  revoke(invitationId: string): Promise<Result<AthleteInvitation, DomainError>>;
  accept(
    invitationId: string,
    request: InvitationTokenRequest,
  ): Promise<Result<AthleteInvitation, DomainError>>;
  reject(
    invitationId: string,
    request: InvitationTokenRequest,
  ): Promise<Result<AthleteInvitation, DomainError>>;
}
