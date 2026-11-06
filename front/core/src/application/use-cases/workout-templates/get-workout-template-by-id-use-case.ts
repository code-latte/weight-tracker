import type {
  DomainError,
  GetWorkoutTemplateByIdInput,
  Result,
  WorkoutTemplate,
} from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Reads one template with its exercises in order.
 */
export default class GetWorkoutTemplateByIdUseCase implements UseCaseInterface<
  GetWorkoutTemplateByIdInput,
  WorkoutTemplate
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({ id }: GetWorkoutTemplateByIdInput): Promise<Result<WorkoutTemplate, DomainError>> {
    return this.workoutTemplateRepository.getById(id);
  }
}
