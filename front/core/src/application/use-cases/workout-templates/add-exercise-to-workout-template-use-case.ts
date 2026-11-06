import type {
  AddExerciseToWorkoutTemplateInput,
  DomainError,
  Result,
  WorkoutExerciseDetail,
} from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Adds a catalog exercise to a template with its effort and sets.
 */
export default class AddExerciseToWorkoutTemplateUseCase implements UseCaseInterface<
  AddExerciseToWorkoutTemplateInput,
  WorkoutExerciseDetail
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({
    templateId,
    ...request
  }: AddExerciseToWorkoutTemplateInput): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    return this.workoutTemplateRepository.addExercise(templateId, request);
  }
}
