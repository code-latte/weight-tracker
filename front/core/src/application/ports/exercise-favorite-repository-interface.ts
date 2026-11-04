import type { DomainError, ExerciseFavorite, Result } from "../../domain";

export default interface ExerciseFavoriteRepositoryInterface {
  getAll(): Promise<Result<ExerciseFavorite[], DomainError>>;
  mark(exerciseId: string): Promise<Result<ExerciseFavorite, DomainError>>;
  unmark(exerciseId: string): Promise<Result<void, DomainError>>;
}
