import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { BreakSuperSeriesUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type Result,
  type SuperSeries,
  type SuperSeriesId,
  type Workout,
  type WorkoutById,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
  type WorkoutListItem,
} from "../../../../src/domain";

const workoutId = "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f";
const superSeriesId = 3;

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly breaks: { workoutId: string; superSeriesId: SuperSeriesId }[] = [];
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

  async removeExercise(): Promise<Result<void, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async reorderExercises(): Promise<Result<WorkoutDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async createSuperSeries(): Promise<Result<SuperSeries, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async breakSuperSeries(
    workoutIdOfCall: string,
    superSeriesId: SuperSeriesId,
  ): Promise<Result<void, DomainError>> {
    this.breaks.push({ workoutId: workoutIdOfCall, superSeriesId });
    return this.result;
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

describe("BreakSuperSeriesUseCase", () => {
  it("returns the empty success the repository answers with", async () => {
    // Arrange
    const useCase = new BreakSuperSeriesUseCase(new FakeWorkoutRepository(ok(undefined)));

    // Act
    const result = await useCase.execute({ workoutId, superSeriesId });

    // Assert
    expect(result).toEqual(ok(undefined));
  });

  it("forwards the workout and superseries ids unchanged, once", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(undefined));
    const useCase = new BreakSuperSeriesUseCase(repository);

    // Act
    await useCase.execute({ workoutId, superSeriesId });

    // Assert
    expect(repository.breaks).toEqual([{ workoutId, superSeriesId }]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["VALIDATION", "FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new BreakSuperSeriesUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ workoutId, superSeriesId });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
