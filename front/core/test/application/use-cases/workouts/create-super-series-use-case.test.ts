import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { CreateSuperSeriesUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type CreateSuperSeriesRequest,
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
const exerciseIds = [
  "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c40",
  "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c41",
];
const superSeries: SuperSeries = { id: 3 };

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly creations: { workoutId: string; request: CreateSuperSeriesRequest }[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<SuperSeries, DomainError>) {}

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

  async createSuperSeries(
    workoutIdOfCall: string,
    request: CreateSuperSeriesRequest,
  ): Promise<Result<SuperSeries, DomainError>> {
    this.creations.push({ workoutId: workoutIdOfCall, request });
    return this.result;
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

describe("CreateSuperSeriesUseCase", () => {
  it("returns the superseries the repository answers with", async () => {
    // Arrange
    const useCase = new CreateSuperSeriesUseCase(new FakeWorkoutRepository(ok(superSeries)));

    // Act
    const result = await useCase.execute({ workoutId, exerciseIds });

    // Assert
    expect(result).toEqual(ok(superSeries));
  });

  it("splits the workout id from the body and forwards the entry ids in order, once", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(superSeries));
    const useCase = new CreateSuperSeriesUseCase(repository);

    // Act
    await useCase.execute({ workoutId, exerciseIds });

    // Assert
    expect(repository.creations).toEqual([{ workoutId, request: { exerciseIds } }]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["VALIDATION", "FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new CreateSuperSeriesUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ workoutId, exerciseIds });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
