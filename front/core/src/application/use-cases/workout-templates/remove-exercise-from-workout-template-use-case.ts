import type { DomainError, RemoveExerciseFromWorkoutTemplateInput, Result } from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Removes a catalog exercise from a template.
 */
export default class RemoveExerciseFromWorkoutTemplateUseCase implements UseCaseInterface<
  RemoveExerciseFromWorkoutTemplateInput,
  void
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({
    templateId,
    exerciseId,
  }: RemoveExerciseFromWorkoutTemplateInput): Promise<Result<void, DomainError>> {
    return this.workoutTemplateRepository.removeExercise(templateId, exerciseId);
  }
}
