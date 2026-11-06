import type { HttpClientInterface, WorkoutTemplateRepositoryInterface } from "../../application";
import {
  type AddExerciseRequest,
  type CreateSuperSeriesRequest,
  type CreateWorkoutFromTemplateRequest,
  type CreateWorkoutTemplateRequest,
  type DomainError,
  fail,
  type GetWorkoutTemplatesInput,
  ok,
  type ReorderWorkoutExercisesRequest,
  type Result,
  type SuperSeries,
  type SuperSeriesId,
  type UpdateWorkoutTemplateRequest,
  type Workout,
  type WorkoutExerciseDetail,
  type WorkoutTemplate,
} from "../../domain";
import { mapHttpError } from "../http";

export default class WorkoutTemplateRepository implements WorkoutTemplateRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async getAll(
    query: GetWorkoutTemplatesInput = {},
  ): Promise<Result<WorkoutTemplate[], DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutTemplate[]>({
        method: "GET",
        path: query.trainerId
          ? `/workout-templates?trainerId=${encodeURIComponent(query.trainerId)}`
          : "/workout-templates",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async getById(id: string): Promise<Result<WorkoutTemplate, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutTemplate>({
        method: "GET",
        path: `/workout-templates/${id}`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async create(
    request: CreateWorkoutTemplateRequest,
  ): Promise<Result<WorkoutTemplate, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutTemplate>({
        method: "POST",
        path: "/workout-templates",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async update(
    id: string,
    request: UpdateWorkoutTemplateRequest,
  ): Promise<Result<WorkoutTemplate, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutTemplate>({
        method: "PUT",
        path: `/workout-templates/${id}`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async delete(id: string): Promise<Result<void, DomainError>> {
    try {
      await this.httpClient.request<void>({
        method: "DELETE",
        path: `/workout-templates/${id}`,
      });

      return ok(undefined);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async addExercise(
    templateId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutExerciseDetail>({
        method: "POST",
        path: `/workout-templates/${templateId}/exercises`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async updateExercise(
    templateId: string,
    exerciseId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutExerciseDetail>({
        method: "PUT",
        path: `/workout-templates/${templateId}/exercises/${exerciseId}`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async removeExercise(templateId: string, exerciseId: string): Promise<Result<void, DomainError>> {
    try {
      await this.httpClient.request<void>({
        method: "DELETE",
        path: `/workout-templates/${templateId}/exercises/${exerciseId}`,
      });

      return ok(undefined);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async reorderExercises(
    templateId: string,
    request: ReorderWorkoutExercisesRequest,
  ): Promise<Result<WorkoutTemplate, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutTemplate>({
        method: "PUT",
        path: `/workout-templates/${templateId}/exercises/order`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async createSuperSeries(
    templateId: string,
    request: CreateSuperSeriesRequest,
  ): Promise<Result<SuperSeries, DomainError>> {
    try {
      const response = await this.httpClient.request<SuperSeries>({
        method: "POST",
        path: `/workout-templates/${templateId}/superseries`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async breakSuperSeries(
    templateId: string,
    superSeriesId: SuperSeriesId,
  ): Promise<Result<void, DomainError>> {
    try {
      await this.httpClient.request<void>({
        method: "DELETE",
        path: `/workout-templates/${templateId}/superseries/${superSeriesId}`,
      });

      return ok(undefined);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async createWorkout(
    templateId: string,
    request: CreateWorkoutFromTemplateRequest,
  ): Promise<Result<Workout, DomainError>> {
    try {
      const response = await this.httpClient.request<Workout>({
        method: "POST",
        path: `/workout-templates/${templateId}/workouts`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async createFromWorkout(workoutId: string): Promise<Result<WorkoutTemplate, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutTemplate>({
        method: "POST",
        path: `/workouts/${workoutId}/templates`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
