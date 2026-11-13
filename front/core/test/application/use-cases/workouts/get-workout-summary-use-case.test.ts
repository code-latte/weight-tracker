import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { GetWorkoutSummaryUseCase } from "../../../../src/application";
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
  type WorkoutObservations,
  type WorkoutSummary,
} from "../../../../src/domain";

const workoutId = "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f";

const summary: WorkoutSummary = {
  workoutId,
  setsInTarget: { inTarget: 27, evaluated: 29 },
  volume: 16913.4,
  exercises: [
    {
      workoutExerciseId: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
      exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
      setsInTarget: { inTarget: 3, evaluated: 3 },
      volume: 3190,
      topSet: { reps: 7, durationSeconds: null, weight: 130 },
      previous: {
        workoutId: "2b3c4d5e-6f70-4819-9a0b-1c2d3e4f5a6b",
        workoutDate: "2026-10-27",
        topSet: { reps: 6, durationSeconds: null, weight: 120 },
      },
    },
    {
      workoutExerciseId: "3c4d5e6f-7081-492a-8b1c-2d3e4f5a6b7c",
      exerciseId: "4d5e6f70-8192-4a3b-9c2d-3e4f5a6b7c8d",
      setsInTarget: { inTarget: 0, evaluated: 0 },
      volume: null,
      topSet: null,
      previous: null,
    },
  ],
};

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly reads: string[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<WorkoutSummary, DomainError>) {}

  async getAll(): Promise<Result<WorkoutListItem[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getById(): Promise<Result<WorkoutById, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getSummary(workoutId: string): Promise<Result<WorkoutSummary, DomainError>> {
    this.reads.push(workoutId);
    return this.result;
  }

  async getObservations(): Promise<Result<WorkoutObservations, DomainError>> {
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

describe("GetWorkoutSummaryUseCase", () => {
  it("returns the summary the repository answers with, keeping every null", async () => {
    // Arrange
    const useCase = new GetWorkoutSummaryUseCase(new FakeWorkoutRepository(ok(summary)));

    // Act
    const result = await useCase.execute({ workoutId });

    // Assert
    expect(result).toEqual(ok(summary));
  });

  it("forwards the workout id once and calls nothing else", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(summary));
    const useCase = new GetWorkoutSummaryUseCase(repository);

    // Act
    await useCase.execute({ workoutId });

    // Assert
    expect(repository.reads).toEqual([workoutId]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["UNAUTHORIZED", "FORBIDDEN", "NOT_FOUND"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new GetWorkoutSummaryUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ workoutId });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
