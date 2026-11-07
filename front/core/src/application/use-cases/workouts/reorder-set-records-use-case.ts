import type { DomainError, ReorderSetRecordsInput, Result, WorkoutDetail } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Sends the full new order of an exercise's set records. Whether the list matches the exercise is decided by the API.
 */
export default class ReorderSetRecordsUseCase implements UseCaseInterface<
  ReorderSetRecordsInput,
  WorkoutDetail
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    exerciseId,
    ...request
  }: ReorderSetRecordsInput): Promise<Result<WorkoutDetail, DomainError>> {
    return this.workoutRepository.reorderSetRecords(workoutId, exerciseId, request);
  }
}
