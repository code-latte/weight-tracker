import type { DomainError, ExerciseFavorite, Result } from "../../../domain";
import type { ExerciseFavoriteRepositoryInterface, UseCaseInterface } from "../../ports";

/**
 * Marks an exercise as a favorite. Whether the user may see the exercise, whether it is archived and what happens
 * when it already is a favorite belong to the API: the id is forwarded unchanged.
 */
export default class MarkExerciseFavoriteUseCase implements UseCaseInterface<
  string,
  ExerciseFavorite
> {
  constructor(private readonly exerciseFavoriteRepository: ExerciseFavoriteRepositoryInterface) {}

  execute(exerciseId: string): Promise<Result<ExerciseFavorite, DomainError>> {
    return this.exerciseFavoriteRepository.mark(exerciseId);
  }
}
