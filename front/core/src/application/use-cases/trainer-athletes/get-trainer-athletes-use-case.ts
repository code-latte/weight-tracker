import type { DomainError, Result, TrainerAthlete } from "../../../domain";
import type { TrainerAthleteRepositoryInterface, UseCaseInterface } from "../../ports";

/** Lists the current athletes of the given trainer. Who may call it is decided by the API. */
export default class GetTrainerAthletesUseCase implements UseCaseInterface<
  string,
  TrainerAthlete[]
> {
  constructor(private readonly trainerAthleteRepository: TrainerAthleteRepositoryInterface) {}

  execute(trainerId: string): Promise<Result<TrainerAthlete[], DomainError>> {
    return this.trainerAthleteRepository.getByTrainer(trainerId);
  }
}
