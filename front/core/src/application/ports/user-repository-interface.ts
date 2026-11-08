import type {
  CreatedUserResponse,
  CreateUserRequest,
  DomainError,
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
  updateMe(request: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>>;
  getAll(): Promise<Result<UserSummary[], DomainError>>;
  create(request: CreateUserRequest): Promise<Result<CreatedUserResponse, DomainError>>;
  activate(id: string): Promise<Result<UserActivationSummary, DomainError>>;
  deactivate(id: string): Promise<Result<UserActivationSummary, DomainError>>;
}
