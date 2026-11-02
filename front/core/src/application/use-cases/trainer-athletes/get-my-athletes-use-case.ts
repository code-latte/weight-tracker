import type { DomainError, Result, TrainerAthlete } from "../../../domain";
import type { TrainerAthleteRepositoryInterface, UseCaseInterface } from "../../ports";

/** Lists the caller's current athletes. Who may call it is decided by the API. */
export default class GetMyAthletesUseCase implements UseCaseInterface<void, TrainerAthlete[]> {
  constructor(private readonly trainerAthleteRepository: TrainerAthleteRepositoryInterface) {}

  execute(): Promise<Result<TrainerAthlete[], DomainError>> {
    return this.trainerAthleteRepository.getMine();
  }
}
