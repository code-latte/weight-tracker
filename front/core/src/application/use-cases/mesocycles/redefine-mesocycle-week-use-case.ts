import type { DomainError, Mesocycle, RedefineMesocycleWeekInput, Result } from "../../../domain";
import type { MesocycleRepositoryInterface, UseCaseInterface } from "../../ports";

/** Replaces the training type and objective of a defined week, found by its number. Who may do it is decided by the API. */
export default class RedefineMesocycleWeekUseCase implements UseCaseInterface<
  RedefineMesocycleWeekInput,
  Mesocycle
> {
  constructor(private readonly mesocycleRepository: MesocycleRepositoryInterface) {}

  execute({
    mesocycleId,
    weekNumber,
    ...request
  }: RedefineMesocycleWeekInput): Promise<Result<Mesocycle, DomainError>> {
    return this.mesocycleRepository.redefineWeek(mesocycleId, weekNumber, request);
  }
}
