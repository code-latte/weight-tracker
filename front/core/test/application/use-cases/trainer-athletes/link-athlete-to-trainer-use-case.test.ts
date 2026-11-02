import { describe, expect, it } from "vitest";
import type { TrainerAthleteRepositoryInterface } from "../../../../src/application";
import { LinkAthleteToTrainerUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type Result,
  type TrainerAthlete,
  type TrainerAthleteRelationship,
} from "../../../../src/domain";

const trainerId = "3c4d5e6f-7a8b-4c9d-8e0f-1a2b3c4d5e6f";
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

const relationship: TrainerAthleteRelationship = {
  id: athlete.id,
  trainerId,
  trainerName: "Ana",
  trainerSurname: "García",
  athleteId,
  athleteName: "Luis",
  athleteSurname: "Pérez",
  status: "ACTIVE",
  startedAtUtc: "2026-10-03T08:00:00Z",
  finishedAtUtc: null,
};

class FakeRepository implements TrainerAthleteRepositoryInterface {
  readonly calls: unknown[][] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<TrainerAthleteRelationship, DomainError>) {}

  async link(...args: unknown[]): Promise<Result<TrainerAthleteRelationship, DomainError>> {
    this.calls.push(args);
    return this.result;
  }

  async getMine(): Promise<Result<TrainerAthlete[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getByTrainer(): Promise<Result<TrainerAthlete[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async end(): Promise<Result<TrainerAthleteRelationship, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("LinkAthleteToTrainerUseCase", () => {
  it("returns what the repository answers with", async () => {
    // Arrange
    const useCase = new LinkAthleteToTrainerUseCase(new FakeRepository(ok(relationship)));

    // Act
    const result = await useCase.execute({ trainerId, athleteId });

    // Assert
    expect(result).toEqual(ok(relationship));
  });

  it("forwards the input unchanged, once, and calls nothing else", async () => {
    // Arrange
    const repository = new FakeRepository(ok(relationship));
    const useCase = new LinkAthleteToTrainerUseCase(repository);

    // Act
    await useCase.execute({ trainerId, athleteId });

    // Assert
    expect(repository.calls).toEqual([[trainerId, { athleteId }]]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new LinkAthleteToTrainerUseCase(new FakeRepository(failure));

      // Act
      const result = await useCase.execute({ trainerId, athleteId });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
