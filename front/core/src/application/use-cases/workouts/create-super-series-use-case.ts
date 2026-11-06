import type { CreateSuperSeriesInput, DomainError, Result, SuperSeries } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Groups workout entries in a new superseries. Whether they can be grouped is decided by the API.
 */
export default class CreateSuperSeriesUseCase implements UseCaseInterface<
  CreateSuperSeriesInput,
  SuperSeries
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    ...request
  }: CreateSuperSeriesInput): Promise<Result<SuperSeries, DomainError>> {
    return this.workoutRepository.createSuperSeries(workoutId, request);
  }
}
