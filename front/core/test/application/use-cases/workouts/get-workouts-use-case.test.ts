import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { GetWorkoutsUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  type GetWorkoutsInput,
  ok,
  type Result,
  type Workout,
  type WorkoutById,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
  type WorkoutListItem,
} from "../../../../src/domain";

const athlete = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "ana@example.com",
  name: "Ana",
  surname: "García",
  role: "USER",
};

const trainer = {
  id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  role: "TRAINER",
};

const workout: WorkoutListItem = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Pierna",
  description: null,
  workoutDate: "2026-11-02",
  userId: athlete.id,
  trainerId: null,
  status: "DRAFT",
  exercises: [],
  athlete,
  trainer: null,
  mesocycleId: null,
  mesocycleWeekNumber: null,
};

const prescribed: WorkoutListItem = {
  ...workout,
  id: "8c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f",
  trainerId: trainer.id,
  trainer,
};

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  listCalls = 0;
  otherCalls = 0;
  queries: (GetWorkoutsInput | undefined)[] = [];

  constructor(private readonly result: Result<WorkoutListItem[], DomainError>) {}

  async getAll(query?: GetWorkoutsInput): Promise<Result<WorkoutListItem[], DomainError>> {
    this.listCalls += 1;
    this.queries.push(query);
    return this.result;
  }

  async getById(): Promise<Result<WorkoutById, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async create(): Promise<Result<Workout, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async update(): Promise<Result<Workout, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async addExercise(): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async updateExercise(): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async removeExercise(): Promise<Result<void, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async reorderExercises(): Promise<Result<WorkoutDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("GetWorkoutsUseCase", () => {
  it("returns the workouts the repository answers with", async () => {
    // Arrange
    const useCase = new GetWorkoutsUseCase(new FakeWorkoutRepository(ok([workout])));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok([workout]));
  });

  it("keeps the athlete and the trainer of each workout, and a missing trainer as null", async () => {
    // Arrange
    const useCase = new GetWorkoutsUseCase(new FakeWorkoutRepository(ok([workout, prescribed])));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok([workout, prescribed]));
    expect(result.ok && result.value.map((item) => item.athlete)).toEqual([athlete, athlete]);
    expect(result.ok && result.value.map((item) => item.trainer)).toEqual([null, trainer]);
  });

  it("lists the workouts once and calls nothing else", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok([]));
    const useCase = new GetWorkoutsUseCase(repository);

    // Act
    await useCase.execute();

    // Assert
    expect(repository.listCalls).toBe(1);
    expect(repository.otherCalls).toBe(0);
  });

  it("forwards the standalone query to the repository", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok([workout]));
    const useCase = new GetWorkoutsUseCase(repository);

    // Act
    await useCase.execute({ standalone: true });

    // Assert
    expect(repository.queries).toEqual([{ standalone: true }]);
  });

  it.each(["UNAUTHORIZED", "FORBIDDEN"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new GetWorkoutsUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute();

      // Assert
      expect(result).toBe(failure);
    },
  );
});
