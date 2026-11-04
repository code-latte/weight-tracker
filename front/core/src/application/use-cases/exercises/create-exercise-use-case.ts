import type { CreateExerciseRequest, DomainError, Exercise, Result } from "../../../domain";
import type { ExerciseRepositoryInterface, UseCaseInterface } from "../../ports";

/**
 * Creates a custom exercise owned by the caller. Who may create one and what a valid exercise is are the API's
 * rules: the input is forwarded unchanged.
 */
export default class CreateExerciseUseCase implements UseCaseInterface<
  CreateExerciseRequest,
  Exercise
> {
  constructor(private readonly exerciseRepository: ExerciseRepositoryInterface) {}

  execute(request: CreateExerciseRequest): Promise<Result<Exercise, DomainError>> {
    return this.exerciseRepository.create(request);
  }
}
