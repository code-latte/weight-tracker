import type {
  DomainError,
  GetWorkoutTemplatesInput,
  Result,
  WorkoutTemplate,
} from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Lists the templates the caller may see: a trainer's own, or every template for an admin, optionally only those of
 * `trainerId`. Which ones that is, per role, is decided by the API.
 */
export default class GetWorkoutTemplatesUseCase implements UseCaseInterface<
  GetWorkoutTemplatesInput,
  WorkoutTemplate[]
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute(input: GetWorkoutTemplatesInput = {}): Promise<Result<WorkoutTemplate[], DomainError>> {
    return this.workoutTemplateRepository.getAll(input);
  }
}
