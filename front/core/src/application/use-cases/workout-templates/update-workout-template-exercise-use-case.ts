import type {
  DomainError,
  Result,
  UpdateWorkoutTemplateExerciseInput,
  WorkoutExerciseDetail,
} from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Replaces the effort and sets of a template's exercise. The catalog exercise id goes in the path and, repeated, in
 * the body.
 */
export default class UpdateWorkoutTemplateExerciseUseCase implements UseCaseInterface<
  UpdateWorkoutTemplateExerciseInput,
  WorkoutExerciseDetail
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({
    templateId,
    ...request
  }: UpdateWorkoutTemplateExerciseInput): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    return this.workoutTemplateRepository.updateExercise(templateId, request.exerciseId, request);
  }
}
