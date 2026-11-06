import type {
  DomainError,
  Result,
  UpdateWorkoutTemplateInput,
  WorkoutTemplate,
} from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Replaces a template's name and description.
 */
export default class UpdateWorkoutTemplateUseCase implements UseCaseInterface<
  UpdateWorkoutTemplateInput,
  WorkoutTemplate
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({
    id,
    ...request
  }: UpdateWorkoutTemplateInput): Promise<Result<WorkoutTemplate, DomainError>> {
    return this.workoutTemplateRepository.update(id, request);
  }
}
