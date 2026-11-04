import type { CreateExerciseRequest, DomainError, Exercise, Result } from "../../domain";

export default interface ExerciseRepositoryInterface {
  getAll(): Promise<Result<Exercise[], DomainError>>;
  create(request: CreateExerciseRequest): Promise<Result<Exercise, DomainError>>;
}
