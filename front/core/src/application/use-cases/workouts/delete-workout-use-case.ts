import type { DeleteWorkoutInput, DomainError, Result } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Deletes a workout with its exercises. Which workouts may be deleted (drafts only, and only by whom) is decided by
 * the API.
 */
export default class DeleteWorkoutUseCase implements UseCaseInterface<DeleteWorkoutInput, void> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({ id }: DeleteWorkoutInput): Promise<Result<void, DomainError>> {
    return this.workoutRepository.delete(id);
  }
}
