import type {
  DomainError,
  Result,
  TrainerAthlete,
  TrainerAthleteRelationship,
  TrainerAthleteRequest,
} from "../../domain";

export default interface TrainerAthleteRepositoryInterface {
  getMine(): Promise<Result<TrainerAthlete[], DomainError>>;
  getByTrainer(trainerId: string): Promise<Result<TrainerAthlete[], DomainError>>;
  link(
    trainerId: string,
    request: TrainerAthleteRequest,
  ): Promise<Result<TrainerAthleteRelationship, DomainError>>;
  end(
    trainerId: string,
    request: TrainerAthleteRequest,
  ): Promise<Result<TrainerAthleteRelationship, DomainError>>;
}
