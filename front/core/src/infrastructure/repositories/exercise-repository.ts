import type { ExerciseRepositoryInterface, HttpClientInterface } from "../../application";
import {
  type CreateExerciseRequest,
  type DomainError,
  type Exercise,
  fail,
  ok,
  type Result,
} from "../../domain";
import { mapHttpError } from "../http";

export default class ExerciseRepository implements ExerciseRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async getAll(): Promise<Result<Exercise[], DomainError>> {
    try {
      const response = await this.httpClient.request<Exercise[]>({
        method: "GET",
        path: "/exercises",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async create(request: CreateExerciseRequest): Promise<Result<Exercise, DomainError>> {
    try {
      const response = await this.httpClient.request<Exercise>({
        method: "POST",
        path: "/exercises",
        body: request,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
