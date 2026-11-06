import type { CreateMesocycleRequest, DomainError, Mesocycle, Result } from "../../../domain";
import type { MesocycleRepositoryInterface, UseCaseInterface } from "../../ports";

/**
 * Creates a mesocycle for an athlete. Whether `trainerId` is required depends on the caller's role, and the API
 * decides: the input is forwarded unchanged.
 */
export default class CreateMesocycleUseCase implements UseCaseInterface<
  CreateMesocycleRequest,
  Mesocycle
> {
  constructor(private readonly mesocycleRepository: MesocycleRepositoryInterface) {}

  execute(request: CreateMesocycleRequest): Promise<Result<Mesocycle, DomainError>> {
    return this.mesocycleRepository.create(request);
  }
}
