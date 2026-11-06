import type {
  AddExerciseRequest,
  CreateSuperSeriesRequest,
  CreateWorkoutFromTemplateRequest,
  CreateWorkoutTemplateRequest,
  DomainError,
  GetWorkoutTemplatesInput,
  ReorderWorkoutExercisesRequest,
  Result,
  SuperSeries,
  SuperSeriesId,
  UpdateWorkoutTemplateRequest,
  Workout,
  WorkoutExerciseDetail,
  WorkoutTemplate,
} from "../../domain";

export default interface WorkoutTemplateRepositoryInterface {
  getAll(query?: GetWorkoutTemplatesInput): Promise<Result<WorkoutTemplate[], DomainError>>;
  getById(id: string): Promise<Result<WorkoutTemplate, DomainError>>;
  create(request: CreateWorkoutTemplateRequest): Promise<Result<WorkoutTemplate, DomainError>>;
  update(
    id: string,
    request: UpdateWorkoutTemplateRequest,
  ): Promise<Result<WorkoutTemplate, DomainError>>;
  delete(id: string): Promise<Result<void, DomainError>>;
  addExercise(
    templateId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>>;
  updateExercise(
    templateId: string,
    exerciseId: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>>;
  removeExercise(templateId: string, exerciseId: string): Promise<Result<void, DomainError>>;
  reorderExercises(
    templateId: string,
    request: ReorderWorkoutExercisesRequest,
  ): Promise<Result<WorkoutTemplate, DomainError>>;
  createSuperSeries(
    templateId: string,
    request: CreateSuperSeriesRequest,
  ): Promise<Result<SuperSeries, DomainError>>;
  breakSuperSeries(
    templateId: string,
    superSeriesId: SuperSeriesId,
  ): Promise<Result<void, DomainError>>;
  createWorkout(
    templateId: string,
    request: CreateWorkoutFromTemplateRequest,
  ): Promise<Result<Workout, DomainError>>;
  /** Saves a workout as a new template owned by its trainer: `POST /workouts/{workoutId}/templates`. */
  createFromWorkout(workoutId: string): Promise<Result<WorkoutTemplate, DomainError>>;
}
