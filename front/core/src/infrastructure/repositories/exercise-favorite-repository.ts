import type { ExerciseFavoriteRepositoryInterface, HttpClientInterface } from "../../application";
import { type DomainError, type ExerciseFavorite, fail, ok, type Result } from "../../domain";
import { mapHttpError } from "../http";

export default class ExerciseFavoriteRepository implements ExerciseFavoriteRepositoryInterface {
  constructor(private readonly httpClient: HttpClientInterface) {}

  async getAll(): Promise<Result<ExerciseFavorite[], DomainError>> {
    try {
      const response = await this.httpClient.request<ExerciseFavorite[]>({
        method: "GET",
        path: "/exercise-favorites",
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async mark(exerciseId: string): Promise<Result<ExerciseFavorite, DomainError>> {
    try {
      const response = await this.httpClient.request<ExerciseFavorite>({
        method: "PUT",
        path: `/exercise-favorites/${exerciseId}`,
      });

      return ok(response);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }

  async unmark(exerciseId: string): Promise<Result<void, DomainError>> {
    try {
      await this.httpClient.request<void>({
        method: "DELETE",
        path: `/exercise-favorites/${exerciseId}`,
      });

      return ok(undefined);
    } catch (error) {
      return fail(mapHttpError(error));
    }
  }
}
