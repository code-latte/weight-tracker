import { describe, expect, it } from "vitest";
import type { MesocycleRepositoryInterface } from "../../../../src/application";
import { GetMesocycleWorkoutsUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type Mesocycle,
  type Result,
  type WorkoutListItem,
} from "../../../../src/domain";

const mesocycleId = "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d";

const athlete = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "USER",
};

const firstWeek: WorkoutListItem = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Pierna",
  description: null,
  workoutDate: "2026-11-11",
  userId: athlete.id,
  trainerId: null,
  status: "DRAFT",
  exercises: [],
  athlete,
  trainer: null,
  mesocycleId,
  mesocycleWeekNumber: 1,
  completedAt: null,
  reviewedAt: null,
  reviewedBy: null,
  resultsUpdatedAt: null,
};

const secondWeek: WorkoutListItem = {
  ...firstWeek,
  id: "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f",
  name: "Empuje",
  workoutDate: "2026-11-18",
  mesocycleWeekNumber: 2,
};

class FakeMesocycleRepository implements MesocycleRepositoryInterface {
  readonly reads: string[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<WorkoutListItem[], DomainError>) {}

  async getAll(): Promise<Result<Mesocycle[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getById(): Promise<Result<Mesocycle, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getWorkouts(mesocycleId: string): Promise<Result<WorkoutListItem[], DomainError>> {
    this.reads.push(mesocycleId);
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

describe("GetMesocycleWorkoutsUseCase", () => {
  it("returns the workouts the repository answers with, in the same order", async () => {
    // Arrange
    const useCase = new GetMesocycleWorkoutsUseCase(
      new FakeMesocycleRepository(ok([secondWeek, firstWeek])),
    );

    // Act
    const result = await useCase.execute({ mesocycleId });

    // Assert
    expect(result).toEqual(ok([secondWeek, firstWeek]));
  });

  it("reads the given mesocycle once and calls nothing else", async () => {
    // Arrange
    const repository = new FakeMesocycleRepository(ok([]));
    const useCase = new GetMesocycleWorkoutsUseCase(repository);

    // Act
    await useCase.execute({ mesocycleId });

    // Assert
    expect(repository.reads).toEqual([mesocycleId]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["FORBIDDEN", "NOT_FOUND", "NETWORK"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new GetMesocycleWorkoutsUseCase(new FakeMesocycleRepository(failure));

      // Act
      const result = await useCase.execute({ mesocycleId });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
