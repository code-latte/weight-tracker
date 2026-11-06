import { describe, expect, it } from "vitest";
import type { MesocycleRepositoryInterface } from "../../../../src/application";
import { GetMesocycleByIdUseCase } from "../../../../src/application";
import { DomainError, fail, ok, type Mesocycle, type Result } from "../../../../src/domain";

const mesocycle: Mesocycle = {
  id: "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d",
  trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  athleteId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  name: "Bloque de fuerza",
  objective: "Subir la sentadilla",
  startDate: "2026-11-09",
  durationInWeeks: 4,
  weeks: [{ weekNumber: 1, trainingWeekType: "ADAPTATION", objective: "Técnica" }],
  trainer: {
    id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
    email: "luis@example.com",
    name: "Luis",
    surname: "Pérez",
    role: "TRAINER",
  },
  athlete: {
    id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
    email: "ana@example.com",
    name: "Ana",
    surname: "García",
    role: "USER",
  },
};

class FakeMesocycleRepository implements MesocycleRepositoryInterface {
  readonly reads: string[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<Mesocycle, DomainError>) {}

  async getAll(): Promise<Result<Mesocycle[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getById(id: string): Promise<Result<Mesocycle, DomainError>> {
    this.reads.push(id);
    return this.result;
  }

  async getByAthlete(): Promise<Result<Mesocycle[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async create(): Promise<Result<Mesocycle, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async update(): Promise<Result<Mesocycle, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async defineWeek(): Promise<Result<Mesocycle, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async redefineWeek(): Promise<Result<Mesocycle, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("GetMesocycleByIdUseCase", () => {
  it("returns the mesocycle the repository answers with", async () => {
    // Arrange
    const useCase = new GetMesocycleByIdUseCase(new FakeMesocycleRepository(ok(mesocycle)));

    // Act
    const result = await useCase.execute({ id: mesocycle.id });

    // Assert
    expect(result).toEqual(ok(mesocycle));
  });

  it("reads the given id once and calls nothing else", async () => {
    // Arrange
    const repository = new FakeMesocycleRepository(ok(mesocycle));
    const useCase = new GetMesocycleByIdUseCase(repository);

    // Act
    await useCase.execute({ id: mesocycle.id });

    // Assert
    expect(repository.reads).toEqual([mesocycle.id]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["FORBIDDEN", "NOT_FOUND"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new GetMesocycleByIdUseCase(new FakeMesocycleRepository(failure));

      // Act
      const result = await useCase.execute({ id: mesocycle.id });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
