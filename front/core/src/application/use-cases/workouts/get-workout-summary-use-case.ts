import type { DomainError, GetWorkoutSummaryInput, Result, WorkoutSummary } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Reads the stats the API computes for one workout. What counts as in target, as volume or as the previous execution
 * is decided by the API.
 */
export default class GetWorkoutSummaryUseCase implements UseCaseInterface<
  GetWorkoutSummaryInput,
  WorkoutSummary
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({ workoutId }: GetWorkoutSummaryInput): Promise<Result<WorkoutSummary, DomainError>> {
    return this.workoutRepository.getSummary(workoutId);
  }
}
