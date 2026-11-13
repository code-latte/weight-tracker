import type {
  DomainError,
  GetWorkoutObservationsInput,
  Result,
  WorkoutObservations,
} from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/**
 * Reads the observations the API generated when a workout was completed, and which of them are highlighted. What is
 * observed, its priority and the choice of highlights are decided by the API.
 */
export default class GetWorkoutObservationsUseCase implements UseCaseInterface<
  GetWorkoutObservationsInput,
  WorkoutObservations
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({
    workoutId,
  }: GetWorkoutObservationsInput): Promise<Result<WorkoutObservations, DomainError>> {
    return this.workoutRepository.getObservations(workoutId);
  }
}
