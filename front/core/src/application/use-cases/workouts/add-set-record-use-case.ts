import type { AddSetRecordInput, DomainError, Result, WorkoutDetail } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Records a set actually performed. Whether the workout is started and the caller may record is decided by the API.
 */
export default class AddSetRecordUseCase implements UseCaseInterface<
  AddSetRecordInput,
  WorkoutDetail
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    exerciseId,
    ...request
  }: AddSetRecordInput): Promise<Result<WorkoutDetail, DomainError>> {
    return this.workoutRepository.addSetRecord(workoutId, exerciseId, request);
  }
}
