import { describe, expect, it } from "vitest";
import type { TrainerAthleteRepositoryInterface } from "../../../../src/application";
import { GetMyAthletesUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type Result,
  type TrainerAthlete,
  type TrainerAthleteRelationship,
} from "../../../../src/domain";

const athleteId = "9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d";

const athlete: TrainerAthlete = {
  id: "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e",
  athleteId,
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  trainerName: "Ana",
  trainerSurname: "García",
  startedAtUtc: "2026-10-03T08:00:00Z",
};

class FakeRepository implements TrainerAthleteRepositoryInterface {
  readonly calls: unknown[][] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<TrainerAthlete[], DomainError>) {}

  async getMine(...args: unknown[]): Promise<Result<TrainerAthlete[], DomainError>> {
    this.calls.push(args);
    return this.result;
  }

  async getByTrainer(): Promise<Result<TrainerAthlete[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async link(): Promise<Result<TrainerAthleteRelationship, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async end(): Promise<Result<TrainerAthleteRelationship, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("GetMyAthletesUseCase", () => {
  it("returns what the repository answers with", async () => {
    // Arrange
    const useCase = new GetMyAthletesUseCase(new FakeRepository(ok([athlete])));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok([athlete]));
  });

  it("forwards the input unchanged, once, and calls nothing else", async () => {
    // Arrange
    const repository = new FakeRepository(ok([athlete]));
    const useCase = new GetMyAthletesUseCase(repository);

    // Act
    await useCase.execute();

    // Assert
    expect(repository.calls).toEqual([[]]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["UNAUTHORIZED", "FORBIDDEN", "UNEXPECTED"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new GetMyAthletesUseCase(new FakeRepository(failure));

      // Act
      const result = await useCase.execute();

      // Assert
      expect(result).toBe(failure);
    },
  );
});
