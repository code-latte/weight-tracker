import type { DomainError, Result, ReviewWorkoutInput, WorkoutDetail } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Marks a completed workout as reviewed. Whether the results changed since `resultsUpdatedAt` is decided by the API.
 */
export default class ReviewWorkoutUseCase implements UseCaseInterface<
  ReviewWorkoutInput,
  WorkoutDetail
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
    ...request
  }: ReviewWorkoutInput): Promise<Result<WorkoutDetail, DomainError>> {
    return this.workoutRepository.review(workoutId, request);
  }
}
