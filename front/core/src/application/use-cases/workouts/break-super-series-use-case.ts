import type { BreakSuperSeriesInput, DomainError, Result } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Ungroups a superseries. Its exercises stay in the workout.
 */
export default class BreakSuperSeriesUseCase implements UseCaseInterface<
  BreakSuperSeriesInput,
  void
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({ workoutId, superSeriesId }: BreakSuperSeriesInput): Promise<Result<void, DomainError>> {
    return this.workoutRepository.breakSuperSeries(workoutId, superSeriesId);
  }
}
