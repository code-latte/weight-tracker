import type { DomainError, GetWorkoutByIdInput, Result, WorkoutById } from "../../../domain";
import type { UseCaseInterface, WorkoutRepositoryInterface } from "../../ports";

/** Reads one workout with its exercises, its athlete and its trainer. Whether the caller may see it is decided by the API. */
export default class GetWorkoutByIdUseCase implements UseCaseInterface<
  GetWorkoutByIdInput,
  WorkoutById
> {
  constructor(private readonly workoutRepository: WorkoutRepositoryInterface) {}

  execute({ id }: GetWorkoutByIdInput): Promise<Result<WorkoutById, DomainError>> {
    return this.workoutRepository.getById(id);
  }
}
