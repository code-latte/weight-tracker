import type {
  DomainError,
  Result,
  TrainerAthleteInput,
  TrainerAthleteRelationship,
} from "../../../domain";
import type { TrainerAthleteRepositoryInterface, UseCaseInterface } from "../../ports";

/** Ends the active relationship between a trainer and an athlete. Who may end it belongs to the API. */
export default class EndTrainerAthleteUseCase implements UseCaseInterface<
  TrainerAthleteInput,
  TrainerAthleteRelationship
> {
  constructor(private readonly trainerAthleteRepository: TrainerAthleteRepositoryInterface) {}

  execute({
    trainerId,
    ...request
  }: TrainerAthleteInput): Promise<Result<TrainerAthleteRelationship, DomainError>> {
    return this.trainerAthleteRepository.end(trainerId, request);
  }
}
