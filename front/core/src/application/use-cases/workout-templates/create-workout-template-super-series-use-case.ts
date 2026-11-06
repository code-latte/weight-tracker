import type {
  CreateWorkoutTemplateSuperSeriesInput,
  DomainError,
  Result,
  SuperSeries,
} from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Groups template entries in a superseries. Which entries may be grouped is decided by the API.
 */
export default class CreateWorkoutTemplateSuperSeriesUseCase implements UseCaseInterface<
  CreateWorkoutTemplateSuperSeriesInput,
  SuperSeries
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({
    templateId,
    ...request
  }: CreateWorkoutTemplateSuperSeriesInput): Promise<Result<SuperSeries, DomainError>> {
    return this.workoutTemplateRepository.createSuperSeries(templateId, request);
  }
}
