import type { DefineMesocycleWeekInput, DomainError, Mesocycle, Result } from "../../../domain";
import type { MesocycleRepositoryInterface, UseCaseInterface } from "../../ports";

/** Defines a week of a mesocycle that has none yet. Who may do it is decided by the API. */
export default class DefineMesocycleWeekUseCase implements UseCaseInterface<
  DefineMesocycleWeekInput,
  Mesocycle
> {
  constructor(private readonly mesocycleRepository: MesocycleRepositoryInterface) {}

  execute({
    mesocycleId,
    ...request
  }: DefineMesocycleWeekInput): Promise<Result<Mesocycle, DomainError>> {
    return this.mesocycleRepository.defineWeek(mesocycleId, request);
  }
}
