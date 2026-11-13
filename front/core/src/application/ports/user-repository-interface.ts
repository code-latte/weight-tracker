import type {
  CreatedUserResponse,
  CreateUserRequest,
  DomainError,
  ExerciseHistorySession,
  ExerciseLastPerformance,
  Result,
  UpdateProfileRequest,
  UserActivationSummary,
  UserProfile,
  UserSummary,
} from "../../domain";

export default interface UserRepositoryInterface {
  getMe(): Promise<Result<UserProfile, DomainError>>;
  /** The user's current trainer, or `null` when they have none. */
  getTrainer(userId: string): Promise<Result<UserProfile | null, DomainError>>;
  /** The signed-in user's last performance of a catalog exercise, or `null` when they never did it. */
  getMyExerciseLastPerformance(
    exerciseId: string,
  ): Promise<Result<ExerciseLastPerformance | null, DomainError>>;
  /** The latest `limit` sessions in which a user did a catalog exercise, in the order the API returns them (newest first). */
  getExerciseHistory(
    userId: string,
    exerciseId: string,
    limit: number,
  ): Promise<Result<ExerciseHistorySession[], DomainError>>;
  updateMe(request: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>>;
  getAll(): Promise<Result<UserSummary[], DomainError>>;
  create(request: CreateUserRequest): Promise<Result<CreatedUserResponse, DomainError>>;
  activate(id: string): Promise<Result<UserActivationSummary, DomainError>>;
  deactivate(id: string): Promise<Result<UserActivationSummary, DomainError>>;
}
