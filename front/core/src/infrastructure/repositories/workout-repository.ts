import type { HttpClientInterface, WorkoutRepositoryInterface } from "../../application";
import {
  type AddExerciseRequest,
  type CreateWorkoutRequest,
  type DomainError,
  fail,
  ok,
  type ReorderWorkoutExercisesRequest,
  type Result,
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
}
