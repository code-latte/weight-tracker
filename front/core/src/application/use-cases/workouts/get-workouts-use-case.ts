import type { DomainError, Result, WorkoutListItem } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/** Lists the workouts the caller may see. Which ones that is, per role, is decided by the API. */
export default class GetWorkoutsUseCase implements UseCaseInterface<void, WorkoutListItem[]> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute(): Promise<Result<WorkoutListItem[], DomainError>> {
    return this.workoutRepository.getAll();
  }
}
