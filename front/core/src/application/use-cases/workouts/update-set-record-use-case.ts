import type { DomainError, Result, UpdateSetRecordInput, WorkoutDetail } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Corrects the reps and weight of a recorded set. Whether it can still change is decided by the API.
 */
export default class UpdateSetRecordUseCase implements UseCaseInterface<
  UpdateSetRecordInput,
  WorkoutDetail
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    exerciseId,
    setRecordId,
    ...request
  }: UpdateSetRecordInput): Promise<Result<WorkoutDetail, DomainError>> {
    return this.workoutRepository.updateSetRecord(workoutId, exerciseId, setRecordId, request);
  }
}
