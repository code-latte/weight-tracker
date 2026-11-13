import type { DomainError, ExerciseLastPerformance, Result } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/**
 * Reads the signed-in user's last performance of a catalog exercise, or `null` when they never did it. Which workout
 * counts as the last one is decided by the API.
 */
export default class GetMyExerciseLastPerformanceUseCase implements UseCaseInterface<
  string,
  ExerciseLastPerformance | null
> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(exerciseId: string): Promise<Result<ExerciseLastPerformance | null, DomainError>> {
    return this.userRepository.getMyExerciseLastPerformance(exerciseId);
  }
}
