import type {
  AddExerciseRequest,
  AddOrUpdateSetRecordRequest,
  ChangeWorkoutStatusRequest,
  CreateSuperSeriesRequest,
  CreateWorkoutRequest,
  DomainError,
  GetWorkoutsInput,
  ReorderSetRecordsRequest,
  ReorderWorkoutExercisesRequest,
  Result,
  ReviewWorkoutRequest,
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
  addSetRecord(
    workoutId: string,
    exerciseId: string,
    request: AddOrUpdateSetRecordRequest,
  ): Promise<Result<WorkoutDetail, DomainError>>;
  updateSetRecord(
    workoutId: string,
    exerciseId: string,
    setRecordId: string,
    request: AddOrUpdateSetRecordRequest,
  ): Promise<Result<WorkoutDetail, DomainError>>;
  removeSetRecord(
    workoutId: string,
    exerciseId: string,
    setRecordId: string,
  ): Promise<Result<WorkoutDetail, DomainError>>;
  reorderSetRecords(
    workoutId: string,
    exerciseId: string,
    request: ReorderSetRecordsRequest,
  ): Promise<Result<WorkoutDetail, DomainError>>;
  review(
    workoutId: string,
    request: ReviewWorkoutRequest,
  ): Promise<Result<WorkoutDetail, DomainError>>;
  changeStatus(
    workoutId: string,
    request: ChangeWorkoutStatusRequest,
  ): Promise<Result<WorkoutDetail, DomainError>>;
}
