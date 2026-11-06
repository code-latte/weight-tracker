import type {
  DomainError,
  ReorderWorkoutTemplateExercisesInput,
  Result,
  WorkoutTemplate,
} from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Sends the full new order of a template's exercises. Whether the list matches the template is decided by the API.
 */
export default class ReorderWorkoutTemplateExercisesUseCase implements UseCaseInterface<
  ReorderWorkoutTemplateExercisesInput,
  WorkoutTemplate
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({
    templateId,
    ...request
  }: ReorderWorkoutTemplateExercisesInput): Promise<Result<WorkoutTemplate, DomainError>> {
    return this.workoutTemplateRepository.reorderExercises(templateId, request);
  }
}
