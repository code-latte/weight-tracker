import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { RemoveExerciseFromWorkoutUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type Result,
  type SuperSeries,
  type Workout,
  type WorkoutById,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
  type WorkoutListItem,
  type WorkoutSummary,
} from "../../../../src/domain";

const workoutId = "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f";
const exerciseId = "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9";

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly removals: { workoutId: string; exerciseId: string }[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<void, DomainError>) {}

  async getAll(): Promise<Result<WorkoutListItem[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getById(): Promise<Result<WorkoutById, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getSummary(): Promise<Result<WorkoutSummary, DomainError>> {
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

  async delete(): Promise<Result<void, DomainError>> {
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

  async removeExercise(
    workoutIdOfCall: string,
    exerciseIdOfCall: string,
  ): Promise<Result<void, DomainError>> {
    this.removals.push({ workoutId: workoutIdOfCall, exerciseId: exerciseIdOfCall });
    return this.result;
  }

  async reorderExercises(): Promise<Result<WorkoutDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async createSuperSeries(): Promise<Result<SuperSeries, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async breakSuperSeries(): Promise<Result<void, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async addSetRecord(): Promise<Result<WorkoutDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async updateSetRecord(): Promise<Result<WorkoutDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async removeSetRecord(): Promise<Result<WorkoutDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async reorderSetRecords(): Promise<Result<WorkoutDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async review(): Promise<Result<WorkoutDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async changeStatus(): Promise<Result<WorkoutDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("RemoveExerciseFromWorkoutUseCase", () => {
  it("returns the empty success the repository answers with", async () => {
    // Arrange
    const useCase = new RemoveExerciseFromWorkoutUseCase(new FakeWorkoutRepository(ok(undefined)));

    // Act
    const result = await useCase.execute({ workoutId, exerciseId });

    // Assert
    expect(result).toEqual(ok(undefined));
  });

  it("forwards the workout and catalog exercise ids, once", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(undefined));
    const useCase = new RemoveExerciseFromWorkoutUseCase(repository);

    // Act
    await useCase.execute({ workoutId, exerciseId });

    // Assert
    expect(repository.removals).toEqual([{ workoutId, exerciseId }]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["NOT_FOUND", "CONFLICT", "FORBIDDEN"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new RemoveExerciseFromWorkoutUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ workoutId, exerciseId });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
