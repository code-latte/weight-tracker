import type { DomainError, Result } from "../../../domain";
import type { ExerciseFavoriteRepositoryInterface, UseCaseInterface } from "../../ports";

/**
 * Removes an exercise from the user's favorites. Removing one that is not a favorite is the API's call: the id is
 * forwarded unchanged.
 */
export default class UnmarkExerciseFavoriteUseCase implements UseCaseInterface<string, void> {
  constructor(private readonly exerciseFavoriteRepository: ExerciseFavoriteRepositoryInterface) {}

  execute(exerciseId: string): Promise<Result<void, DomainError>> {
    return this.exerciseFavoriteRepository.unmark(exerciseId);
  }
}
