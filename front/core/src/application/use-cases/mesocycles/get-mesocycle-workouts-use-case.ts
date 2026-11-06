import type {
  DomainError,
  GetMesocycleWorkoutsInput,
  Result,
  WorkoutListItem,
} from "../../../domain";
import type { MesocycleRepositoryInterface, UseCaseInterface } from "../../ports";

/**
 * Lists every workout placed in a mesocycle, each with its week number, in the API's order. Which workouts belong to
 * it, and whether the caller may see them, is decided by the API.
 */
export default class GetMesocycleWorkoutsUseCase implements UseCaseInterface<
  GetMesocycleWorkoutsInput,
  WorkoutListItem[]
> {
  constructor(private readonly mesocycleRepository: MesocycleRepositoryInterface) {}

  execute({
    mesocycleId,
  }: GetMesocycleWorkoutsInput): Promise<Result<WorkoutListItem[], DomainError>> {
    return this.mesocycleRepository.getWorkouts(mesocycleId);
  }
}
