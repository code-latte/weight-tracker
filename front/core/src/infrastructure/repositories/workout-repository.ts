import type { HttpClientInterface, WorkoutRepositoryInterface } from "../../application";
import {
  type AddExerciseRequest,
  type AddOrUpdateSetRecordRequest,
  type ChangeWorkoutStatusRequest,
  type CreateSuperSeriesRequest,
  type CreateWorkoutRequest,
  type DomainError,
  fail,
  ok,
  type ReorderSetRecordsRequest,
  type ReorderWorkoutExercisesRequest,
  type Result,
  type ReviewWorkoutRequest,
  type SuperSeries,
  type SuperSeriesId,
  type UpdateWorkoutRequest,
  type Workout,
  type WorkoutById,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
  type GetWorkoutsInput,
  type WorkoutListItem,
} from "../../domain";
import { mapHttpError } from "../http";

export default class WorkoutRepository implements WorkoutRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async getAll(query: GetWorkoutsInput = {}): Promise<Result<WorkoutListItem[], DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutListItem[]>({
        method: "GET",
        path: query.standalone ? "/workouts?standalone=true" : "/workouts",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async getById(id: string): Promise<Result<WorkoutById, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutById>({
        method: "GET",
        path: `/workouts/${id}`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async create(request: CreateWorkoutRequest): Promise<Result<Workout, DomainError>> {
    try {
      const response = await this.httpClient.request<Workout>({
        method: "POST",
        path: "/workouts",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async update(id: string, request: UpdateWorkoutRequest): Promise<Result<Workout, DomainError>> {
    try {
      const response = await this.httpClient.request<Workout>({
        method: "PUT",
        path: `/workouts/${id}`,
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
        path: `/workouts/${id}`,
      });

      return ok(undefined);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async addExercise(
    workoutId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutExerciseDetail>({
        method: "POST",
        path: `/workouts/${workoutId}/exercises`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async updateExercise(
    workoutId: string,
    exerciseId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutExerciseDetail>({
        method: "PUT",
        path: `/workouts/${workoutId}/exercises/${exerciseId}`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async removeExercise(workoutId: string, exerciseId: string): Promise<Result<void, DomainError>> {
    try {
      await this.httpClient.request<void>({
        method: "DELETE",
        path: `/workouts/${workoutId}/exercises/${exerciseId}`,
      });

      return ok(undefined);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async reorderExercises(
    workoutId: string,
    request: ReorderWorkoutExercisesRequest,
  ): Promise<Result<WorkoutDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutDetail>({
        method: "PUT",
        path: `/workouts/${workoutId}/exercises/order`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async createSuperSeries(
    workoutId: string,
    request: CreateSuperSeriesRequest,
  ): Promise<Result<SuperSeries, DomainError>> {
    try {
      const response = await this.httpClient.request<SuperSeries>({
        method: "POST",
        path: `/workouts/${workoutId}/superseries`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async breakSuperSeries(
    workoutId: string,
    superSeriesId: SuperSeriesId,
  ): Promise<Result<void, DomainError>> {
    try {
      await this.httpClient.request<void>({
        method: "DELETE",
        path: `/workouts/${workoutId}/superseries/${superSeriesId}`,
      });

      return ok(undefined);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async addSetRecord(
    workoutId: string,
    exerciseId: string,
    request: AddOrUpdateSetRecordRequest,
  ): Promise<Result<WorkoutDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutDetail>({
        method: "POST",
        path: `/workouts/${workoutId}/exercises/${exerciseId}/set-records`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async updateSetRecord(
    workoutId: string,
    exerciseId: string,
    setRecordId: string,
    request: AddOrUpdateSetRecordRequest,
  ): Promise<Result<WorkoutDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutDetail>({
        method: "PUT",
        path: `/workouts/${workoutId}/exercises/${exerciseId}/set-records/${setRecordId}`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async removeSetRecord(
    workoutId: string,
    exerciseId: string,
    setRecordId: string,
  ): Promise<Result<WorkoutDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutDetail>({
        method: "DELETE",
        path: `/workouts/${workoutId}/exercises/${exerciseId}/set-records/${setRecordId}`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async reorderSetRecords(
    workoutId: string,
    exerciseId: string,
    request: ReorderSetRecordsRequest,
  ): Promise<Result<WorkoutDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutDetail>({
        method: "PUT",
        path: `/workouts/${workoutId}/exercises/${exerciseId}/set-records/order`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async review(
    workoutId: string,
    request: ReviewWorkoutRequest,
  ): Promise<Result<WorkoutDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutDetail>({
        method: "PUT",
        path: `/workouts/${workoutId}/review`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async changeStatus(
    workoutId: string,
    request: ChangeWorkoutStatusRequest,
  ): Promise<Result<WorkoutDetail, DomainError>> {
    try {
      const response = await this.httpClient.request<WorkoutDetail>({
        method: "PUT",
        path: `/workouts/${workoutId}/status`,
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
