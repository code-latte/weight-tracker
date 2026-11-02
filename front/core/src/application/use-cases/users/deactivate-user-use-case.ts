import type { DomainError, Result, UserActivationSummary } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/**
 * Switches another user's account off, so they can no longer log in nor be given workouts. Who may do it, and on
 * which account, belong to the API: the id is forwarded unchanged.
 */
export default class DeactivateUserUseCase implements UseCaseInterface<
  string,
  UserActivationSummary
> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(id: string): Promise<Result<UserActivationSummary, DomainError>> {
    return this.userRepository.deactivate(id);
  }
}
