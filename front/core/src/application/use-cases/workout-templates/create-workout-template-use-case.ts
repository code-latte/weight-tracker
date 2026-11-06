import type {
  CreateWorkoutTemplateRequest,
  DomainError,
  Result,
  WorkoutTemplate,
} from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Creates a template without exercises. Whether `trainerId` is required depends on the caller's role, and the API
 * decides: the input is forwarded unchanged.
 */
export default class CreateWorkoutTemplateUseCase implements UseCaseInterface<
  CreateWorkoutTemplateRequest,
  WorkoutTemplate
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute(request: CreateWorkoutTemplateRequest): Promise<Result<WorkoutTemplate, DomainError>> {
    return this.workoutTemplateRepository.create(request);
  }
}
