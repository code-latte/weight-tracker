import type { DomainError, ExerciseFavorite, Result } from "../../../domain";
import type { ExerciseFavoriteRepositoryInterface, UseCaseInterface } from "../../ports";

/** Lists the signed-in user's favorite exercises, newest first. The API scopes the list to the caller. */
export default class GetExerciseFavoritesUseCase implements UseCaseInterface<
  void,
  ExerciseFavorite[]
> {
  constructor(private readonly exerciseFavoriteRepository: ExerciseFavoriteRepositoryInterface) {}

  execute(): Promise<Result<ExerciseFavorite[], DomainError>> {
    return this.exerciseFavoriteRepository.getAll();
  }
}
