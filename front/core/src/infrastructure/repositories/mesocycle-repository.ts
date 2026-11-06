import type { HttpClientInterface, MesocycleRepositoryInterface } from "../../application";
import {
  type CreateMesocycleRequest,
  type DefineMesocycleWeekRequest,
  type DomainError,
  fail,
  type Mesocycle,
  ok,
  type RedefineMesocycleWeekRequest,
  type Result,
  type UpdateMesocycleRequest,
  type WorkoutListItem,
} from "../../domain";
import { mapHttpError } from "../http";

export default class MesocycleRepository implements MesocycleRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async getAll(): Promise<Result<Mesocycle[], DomainError>> {
    try {
      const response = await this.httpClient.request<Mesocycle[]>({
        method: "GET",
        path: "/mesocycles",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async getById(id: string): Promise<Result<Mesocycle, DomainError>> {
    try {
      const response = await this.httpClient.request<Mesocycle>({
        method: "GET",
        path: `/mesocycles/${id}`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async getWorkouts(mesocycleId: string): Promise<Result<WorkoutListItem[], DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutListItem[]>({
        method: "GET",
        path: `/mesocycles/${mesocycleId}/workouts`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async getByAthlete(userId: string): Promise<Result<Mesocycle[], DomainError>> {
    try {
      const response = await this.httpClient.request<Mesocycle[]>({
        method: "GET",
        path: `/users/${userId}/mesocycles`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async create(request: CreateMesocycleRequest): Promise<Result<Mesocycle, DomainError>> {
    try {
      const response = await this.httpClient.request<Mesocycle>({
        method: "POST",
        path: "/mesocycles",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async update(
    id: string,
    request: UpdateMesocycleRequest,
  ): Promise<Result<Mesocycle, DomainError>> {
    try {
      const response = await this.httpClient.request<Mesocycle>({
        method: "PUT",
        path: `/mesocycles/${id}`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async defineWeek(
    mesocycleId: string,
    request: DefineMesocycleWeekRequest,
  ): Promise<Result<Mesocycle, DomainError>> {
    try {
      const response = await this.httpClient.request<Mesocycle>({
        method: "POST",
        path: `/mesocycles/${mesocycleId}/weeks`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async redefineWeek(
    mesocycleId: string,
    weekNumber: number,
    request: RedefineMesocycleWeekRequest,
  ): Promise<Result<Mesocycle, DomainError>> {
    try {
      const response = await this.httpClient.request<Mesocycle>({
        method: "PUT",
        path: `/mesocycles/${mesocycleId}/weeks/${weekNumber}`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
