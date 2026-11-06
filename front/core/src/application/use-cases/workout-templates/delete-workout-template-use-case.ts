import type { DeleteWorkoutTemplateInput, DomainError, Result } from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Deletes a template. The workouts created from it are not changed.
 */
export default class DeleteWorkoutTemplateUseCase implements UseCaseInterface<
  DeleteWorkoutTemplateInput,
  void
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({ id }: DeleteWorkoutTemplateInput): Promise<Result<void, DomainError>> {
    return this.workoutTemplateRepository.delete(id);
  }
}
