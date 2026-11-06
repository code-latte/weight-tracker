import type { CreateWorkoutFromTemplateInput, DomainError, Result, Workout } from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Creates a workout from a template, for an athlete on a date or in a mesocycle week. Its trainer is the template's,
 * and the placement rules are the API's: the input is forwarded unchanged.
 */
export default class CreateWorkoutFromTemplateUseCase implements UseCaseInterface<
  CreateWorkoutFromTemplateInput,
  Workout
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({
    templateId,
    ...request
  }: CreateWorkoutFromTemplateInput): Promise<Result<Workout, DomainError>> {
    return this.workoutTemplateRepository.createWorkout(templateId, request);
  }
}
