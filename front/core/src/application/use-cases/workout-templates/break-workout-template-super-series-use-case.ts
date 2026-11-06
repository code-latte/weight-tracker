import type { BreakWorkoutTemplateSuperSeriesInput, DomainError, Result } from "../../../domain";
import type { UseCaseInterface, WorkoutTemplateRepositoryInterface } from "../../ports";

/**
 * Breaks a template's superseries, leaving its members where they are.
 */
export default class BreakWorkoutTemplateSuperSeriesUseCase implements UseCaseInterface<
  BreakWorkoutTemplateSuperSeriesInput,
  void
> {
  constructor(private readonly workoutTemplateRepository: WorkoutTemplateRepositoryInterface) {}

  execute({
    templateId,
    superSeriesId,
  }: BreakWorkoutTemplateSuperSeriesInput): Promise<Result<void, DomainError>> {
    return this.workoutTemplateRepository.breakSuperSeries(templateId, superSeriesId);
  }
}
