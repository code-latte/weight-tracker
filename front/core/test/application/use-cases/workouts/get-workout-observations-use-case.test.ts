import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { GetWorkoutObservationsUseCase } from "../../../../src/application";
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
  type WorkoutObservation,
  type WorkoutObservations,
  type WorkoutSummary,
} from "../../../../src/domain";

/** The contract types `data` as `Record<string, never>`, so the numbers a fixture holds need this cast. */
const dataOf = (values: Record<string, number | number[]>) => values as WorkoutObservation["data"];

const workoutId = "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f";

const observations: WorkoutObservations = {
  observations: [
    {
      id: "a1b2c3d4-0000-4000-8000-000000000001",
      code: "WORKOUT_INCOMPLETE",
      severity: "WARNING",
      exerciseId: null,
      priority: null,
      data: dataOf({ recordedSets: 9, prescribedSets: 15, completionPercent: 60 }),
    },
    {
      id: "a1b2c3d4-0000-4000-8000-000000000002",
      code: "WEIGHT_PERSONAL_RECORD",
      severity: "SUCCESS",
      exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
      priority: "MEDIUM",
      data: dataOf({ previousMaxKg: 120, currentMaxKg: 130, changePercent: 8.33 }),
    },
  ],
  highlights: [
    { observationId: "a1b2c3d4-0000-4000-8000-000000000002", rank: 1, priority: "MEDIUM" },
  ],
};

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly reads: string[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<WorkoutObservations, DomainError>) {}

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

  async getObservations(workoutId: string): Promise<Result<WorkoutObservations, DomainError>> {
    this.reads.push(workoutId);
    return this.result;
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

describe("GetWorkoutObservationsUseCase", () => {
  it("returns the observations the repository answers with, keeping every null", async () => {
    // Arrange
    const useCase = new GetWorkoutObservationsUseCase(new FakeWorkoutRepository(ok(observations)));

    // Act
    const result = await useCase.execute({ workoutId });

    // Assert
    expect(result).toEqual(ok(observations));
  });

  it("forwards the workout id once and calls nothing else", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(observations));
    const useCase = new GetWorkoutObservationsUseCase(repository);

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
      const useCase = new GetWorkoutObservationsUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ workoutId });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
