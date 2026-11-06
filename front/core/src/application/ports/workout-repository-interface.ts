import type {
  AddExerciseRequest,
  CreateSuperSeriesRequest,
  CreateWorkoutRequest,
  DomainError,
  GetWorkoutsInput,
  ReorderWorkoutExercisesRequest,
  Result,
  SuperSeries,
  SuperSeriesId,
  UpdateWorkoutRequest,
  Workout,
  WorkoutById,
  WorkoutDetail,
  WorkoutExerciseDetail,
  WorkoutListItem,
} from "../../domain";

export default interface WorkoutRepositoryInterface {
  getAll(query?: GetWorkoutsInput): Promise<Result<WorkoutListItem[], DomainError>>;
  getById(id: string): Promise<Result<WorkoutById, DomainError>>;
  create(request: CreateWorkoutRequest): Promise<Result<Workout, DomainError>>;
  update(id: string, request: UpdateWorkoutRequest): Promise<Result<Workout, DomainError>>;
  addExercise(
    workoutId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>>;
  updateExercise(
    workoutId: string,
    exerciseId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>>;
  removeExercise(workoutId: string, exerciseId: string): Promise<Result<void, DomainError>>;
  reorderExercises(
    workoutId: string,
    request: ReorderWorkoutExercisesRequest,
  ): Promise<Result<WorkoutDetail, DomainError>>;
  createSuperSeries(
    workoutId: string,
    request: CreateSuperSeriesRequest,
  ): Promise<Result<SuperSeries, DomainError>>;
  breakSuperSeries(
    workoutId: string,
    superSeriesId: SuperSeriesId,
  ): Promise<Result<void, DomainError>>;
}
