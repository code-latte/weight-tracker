import type {
  CreateWorkoutTemplateFromWorkoutInput,
  DomainError,
  Result,
  WorkoutTemplate,
} from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Saves a workout, as stored, as a new template owned by its trainer. The workout itself is not changed.
 */
export default class CreateWorkoutTemplateFromWorkoutUseCase implements UseCaseInterface<
  CreateWorkoutTemplateFromWorkoutInput,
  WorkoutTemplate
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({
    workoutId,
  }: CreateWorkoutTemplateFromWorkoutInput): Promise<Result<WorkoutTemplate, DomainError>> {
    return this.workoutTemplateRepository.createFromWorkout(workoutId);
  }
}
