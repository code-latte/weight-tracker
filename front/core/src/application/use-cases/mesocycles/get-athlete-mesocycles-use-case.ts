import type { DomainError, GetAthleteMesocyclesInput, Mesocycle, Result } from "../../../domain";
import type { MesocycleRepositoryInterface, UseCaseInterface } from "../../ports";

/** Lists the mesocycles planned for one athlete. Who may call it, and which ones they get, is decided by the API. */
export default class GetAthleteMesocyclesUseCase implements UseCaseInterface<
  GetAthleteMesocyclesInput,
  Mesocycle[]
> {
  constructor(private readonly mesocycleRepository: MesocycleRepositoryInterface) {}

  execute({ userId }: GetAthleteMesocyclesInput): Promise<Result<Mesocycle[], DomainError>> {
    return this.mesocycleRepository.getByAthlete(userId);
  }
}
