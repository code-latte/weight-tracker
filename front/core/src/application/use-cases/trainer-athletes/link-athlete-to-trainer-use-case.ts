import type {
  DomainError,
  Result,
  TrainerAthleteInput,
  TrainerAthleteRelationship,
} from "../../../domain";
import type { TrainerAthleteRepositoryInterface, UseCaseInterface } from "../../ports";

/** Links an athlete to a trainer. Which roles qualify, and the one active trainer per athlete, belong to the API. */
export default class LinkAthleteToTrainerUseCase implements UseCaseInterface<
  TrainerAthleteInput,
  TrainerAthleteRelationship
> {
  constructor(private readonly trainerAthleteRepository: TrainerAthleteRepositoryInterface) {}

  execute({
    trainerId,
    ...request
  }: TrainerAthleteInput): Promise<Result<TrainerAthleteRelationship, DomainError>> {
    return this.trainerAthleteRepository.link(trainerId, request);
  }
}
