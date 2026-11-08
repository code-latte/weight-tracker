import type { DomainError, Result, UserProfile } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/** Reads a user's current trainer, or `null` when they have none. Who may ask is decided by the API. */
export default class GetUserTrainerUseCase implements UseCaseInterface<string, UserProfile | null> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(userId: string): Promise<Result<UserProfile | null, DomainError>> {
    return this.userRepository.getTrainer(userId);
  }
}
