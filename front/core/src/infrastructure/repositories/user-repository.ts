import type { HttpClientInterface, UserRepositoryInterface } from "../../application";
import {
  type CreatedUserResponse,
  type CreateUserRequest,
  type DomainError,
  fail,
  ok,
  type Result,
  type UpdateProfileRequest,
  type UserActivationSummary,
  type UserProfile,
  type UserSummary,
} from "../../domain";
import { mapHttpError } from "../http";

export default class UserRepository implements UserRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async getMe(): Promise<Result<UserProfile, DomainError>> {
    try {
      const response = await this.httpClient.request<UserProfile>({
        method: "GET",
        path: "/users/me",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async updateMe(request: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>> {
    try {
      const response = await this.httpClient.request<UserProfile>({
        method: "PUT",
        path: "/users/me",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async getAll(): Promise<Result<UserSummary[], DomainError>> {
    try {
      const response = await this.httpClient.request<UserSummary[]>({
        method: "GET",
        path: "/users",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async create(request: CreateUserRequest): Promise<Result<CreatedUserResponse, DomainError>> {
    try {
      const response = await this.httpClient.request<CreatedUserResponse>({
        method: "POST",
        path: "/users",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async activate(id: string): Promise<Result<UserActivationSummary, DomainError>> {
    try {
      const response = await this.httpClient.request<UserActivationSummary>({
        method: "PUT",
        path: `/users/${id}/activate`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async deactivate(id: string): Promise<Result<UserActivationSummary, DomainError>> {
    try {
      const response = await this.httpClient.request<UserActivationSummary>({
        method: "PUT",
        path: `/users/${id}/deactivate`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
