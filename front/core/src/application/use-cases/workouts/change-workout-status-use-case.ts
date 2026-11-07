import type { ChangeWorkoutStatusInput, DomainError, Result, WorkoutDetail } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Moves a workout to another status. Which transitions the caller may make is decided by the API.
 */
export default class ChangeWorkoutStatusUseCase implements UseCaseInterface<
  ChangeWorkoutStatusInput,
  WorkoutDetail
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    ...request
  }: ChangeWorkoutStatusInput): Promise<Result<WorkoutDetail, DomainError>> {
    return this.workoutRepository.changeStatus(workoutId, request);
  }
}
