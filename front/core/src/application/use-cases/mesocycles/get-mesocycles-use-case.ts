import type { DomainError, Mesocycle, Result } from "../../../domain";
import type { MesocycleRepositoryInterface, UseCaseInterface } from "../../ports";

/** Lists the mesocycles the caller may see. Which ones that is, per role, is decided by the API. */
export default class GetMesocyclesUseCase implements UseCaseInterface<void, Mesocycle[]> {
  constructor(private readonly mesocycleRepository: MesocycleRepositoryInterface) {}

  execute(): Promise<Result<Mesocycle[], DomainError>> {
    return this.mesocycleRepository.getAll();
  }
}
