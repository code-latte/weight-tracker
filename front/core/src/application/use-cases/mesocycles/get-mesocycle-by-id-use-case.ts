import type { DomainError, GetMesocycleByIdInput, Mesocycle, Result } from "../../../domain";
import type { MesocycleRepositoryInterface, UseCaseInterface } from "../../ports";

/** Reads one mesocycle with its weeks. Whether the caller may see it is decided by the API. */
export default class GetMesocycleByIdUseCase implements UseCaseInterface<
  GetMesocycleByIdInput,
  Mesocycle
> {
  constructor(private readonly mesocycleRepository: MesocycleRepositoryInterface) {}

  execute({ id }: GetMesocycleByIdInput): Promise<Result<Mesocycle, DomainError>> {
    return this.mesocycleRepository.getById(id);
  }
}
