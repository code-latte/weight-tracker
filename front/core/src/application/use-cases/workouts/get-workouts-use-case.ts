import type { DomainError, GetWorkoutsInput, Result, WorkoutListItem } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Lists the workouts the caller may see, or only those not placed in a mesocycle when `standalone` is set. Which ones
 * that is, per role and per placement, is decided by the API.
 */
export default class GetWorkoutsUseCase implements UseCaseInterface<
  GetWorkoutsInput,
  WorkoutListItem[]
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute(input: GetWorkoutsInput = {}): Promise<Result<WorkoutListItem[], DomainError>> {
    return this.workoutRepository.getAll(input);
  }
}
