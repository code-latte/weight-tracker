import type {
  DomainError,
  ExerciseHistorySession,
  GetUserExerciseHistoryInput,
  Result,
} from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/**
 * Reads the latest sessions in which a user did a catalog exercise. Which sessions count, and the figures of each, are
 * decided by the API.
 */
export default class GetUserExerciseHistoryUseCase implements UseCaseInterface<
  GetUserExerciseHistoryInput,
  ExerciseHistorySession[]
> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute({
    userId,
    exerciseId,
    limit,
  }: GetUserExerciseHistoryInput): Promise<Result<ExerciseHistorySession[], DomainError>> {
    return this.userRepository.getExerciseHistory(userId, exerciseId, limit);
  }
}
