import type {
  CreateMesocycleRequest,
  DefineMesocycleWeekRequest,
  DomainError,
  Mesocycle,
  RedefineMesocycleWeekRequest,
  Result,
  UpdateMesocycleRequest,
} from "../../domain";

export default interface MesocycleRepositoryInterface {
  getAll(): Promise<Result<Mesocycle[], DomainError>>;
  getById(id: string): Promise<Result<Mesocycle, DomainError>>;
  getByAthlete(userId: string): Promise<Result<Mesocycle[], DomainError>>;
  create(request: CreateMesocycleRequest): Promise<Result<Mesocycle, DomainError>>;
  update(id: string, request: UpdateMesocycleRequest): Promise<Result<Mesocycle, DomainError>>;
  defineWeek(
    mesocycleId: string,
    request: DefineMesocycleWeekRequest,
  ): Promise<Result<Mesocycle, DomainError>>;
  redefineWeek(
    mesocycleId: string,
    weekNumber: number,
    request: RedefineMesocycleWeekRequest,
  ): Promise<Result<Mesocycle, DomainError>>;
}
