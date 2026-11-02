import type { AthleteInvitationRepositoryInterface, HttpClientInterface } from "../../application";
import {
  type AthleteInvitation,
  type CreatedAthleteInvitation,
  type DomainError,
  fail,
  type InvitationTokenRequest,
  type InviteAthleteRequest,
  ok,
  type Result,
} from "../../domain";
import { mapHttpError } from "../http";

export default class AthleteInvitationRepository implements AthleteInvitationRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async getAll(): Promise<Result<AthleteInvitation[], DomainError>> {
    try {
      const response = await this.httpClient.request<AthleteInvitation[]>({
        method: "GET",
        path: "/athlete-invitations",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async invite(
    request: InviteAthleteRequest,
  ): Promise<Result<CreatedAthleteInvitation, DomainError>> {
    try {
      const response = await this.httpClient.request<CreatedAthleteInvitation>({
        method: "POST",
        path: "/athlete-invitations",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async revoke(invitationId: string): Promise<Result<AthleteInvitation, DomainError>> {
    try {
      const response = await this.httpClient.request<AthleteInvitation>({
        method: "PUT",
        path: `/athlete-invitations/${invitationId}/revoke`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async accept(
    invitationId: string,
    request: InvitationTokenRequest,
  ): Promise<Result<AthleteInvitation, DomainError>> {
    try {
      const response = await this.httpClient.request<AthleteInvitation>({
        method: "PUT",
        path: `/athlete-invitations/${invitationId}/accept`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async reject(
    invitationId: string,
    request: InvitationTokenRequest,
  ): Promise<Result<AthleteInvitation, DomainError>> {
    try {
      const response = await this.httpClient.request<AthleteInvitation>({
        method: "PUT",
        path: `/athlete-invitations/${invitationId}/reject`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
