import type { DomainError, Result, UserActivationSummary } from "../../../domain";
import type { UseCaseInterface, UserRepositoryInterface } from "../../ports";

/**
 * Switches a deactivated account back on. Who may do it, and whether the account is already active, belong to
 * the API: the id is forwarded unchanged.
 */
export default class ActivateUserUseCase implements UseCaseInterface<
  string,
  UserActivationSummary
> {
  constructor(private readonly userRepository: UserRepositoryInterface) {}

  execute(id: string): Promise<Result<UserActivationSummary, DomainError>> {
    return this.userRepository.activate(id);
  }
}
