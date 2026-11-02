import type { HttpClientInterface, TrainerAthleteRepositoryInterface } from "../../application";
import {
  type DomainError,
  fail,
  ok,
  type Result,
  type TrainerAthlete,
  type TrainerAthleteRelationship,
  type TrainerAthleteRequest,
} from "../../domain";
import { mapHttpError } from "../http";

export default class TrainerAthleteRepository implements TrainerAthleteRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async getMine(): Promise<Result<TrainerAthlete[], DomainError>> {
    try {
      const response = await this.httpClient.request<TrainerAthlete[]>({
        method: "GET",
        path: "/trainer-athletes",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async getByTrainer(trainerId: string): Promise<Result<TrainerAthlete[], DomainError>> {
    try {
      const response = await this.httpClient.request<TrainerAthlete[]>({
        method: "GET",
        path: `/trainer-athletes/${trainerId}`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async link(
    trainerId: string,
    request: TrainerAthleteRequest,
  ): Promise<Result<TrainerAthleteRelationship, DomainError>> {
    try {
      const response = await this.httpClient.request<TrainerAthleteRelationship>({
        method: "POST",
        path: `/trainer-athletes/${trainerId}`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async end(
    trainerId: string,
    request: TrainerAthleteRequest,
  ): Promise<Result<TrainerAthleteRelationship, DomainError>> {
    try {
      const response = await this.httpClient.request<TrainerAthleteRelationship>({
        method: "PUT",
        path: `/trainer-athletes/${trainerId}/end`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
