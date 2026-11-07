import type { DomainError, RemoveSetRecordInput, Result, WorkoutDetail } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Removes a recorded set. Whether it can still be removed is decided by the API.
 */
export default class RemoveSetRecordUseCase implements UseCaseInterface<
  RemoveSetRecordInput,
  WorkoutDetail
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    exerciseId,
    setRecordId,
  }: RemoveSetRecordInput): Promise<Result<WorkoutDetail, DomainError>> {
    return this.workoutRepository.removeSetRecord(workoutId, exerciseId, setRecordId);
  }
}
