import type { DomainError, Mesocycle, Result, UpdateMesocycleInput } from "../../../domain";
import type { MesocycleRepositoryInterface, UseCaseInterface } from "../../ports";

/** Replaces a mesocycle's name, objective, start date and duration. Who may do it is decided by the API. */
export default class UpdateMesocycleUseCase implements UseCaseInterface<
  UpdateMesocycleInput,
  Mesocycle
> {
  constructor(private readonly mesocycleRepository: MesocycleRepositoryInterface) {}

  execute({ id, ...request }: UpdateMesocycleInput): Promise<Result<Mesocycle, DomainError>> {
    return this.mesocycleRepository.update(id, request);
  }
}
