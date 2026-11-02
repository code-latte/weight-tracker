import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { AddExerciseToWorkoutUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type AddExerciseRequest,
  type Result,
  type Workout,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
  type WorkoutListItem,
} from "../../../../src/domain";

const workoutId = "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f";

const workoutExercise: WorkoutExerciseDetail = {
  id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
  exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
  effort: "HIGH",
  perSide: true,
  restMinutes: 1.5,
  sets: [
    { count: 3, target: { type: "REPS", value: 10, maxValue: null } },
    { count: 1, target: { type: "Minutes", minutes: 2 } },
  ],
  superSeriesId: null,
};

const body: AddExerciseRequest = {
  exerciseId: workoutExercise.exerciseId,
  effort: "HIGH",
  sets: [
    { count: 3, targetType: "REPS", target: 10 },
    { count: 1, targetType: "REPS_RANGE", target: 8, maxTarget: 12 },
    { count: 1, targetType: "Minutes", targetMinutes: 2 },
  ],
  perSide: true,
  restMinutes: 1.5,
};

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly additions: { workoutId: string; request: AddExerciseRequest }[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<WorkoutExerciseDetail, DomainError>) {}

  async getAll(): Promise<Result<WorkoutListItem[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getById(): Promise<Result<WorkoutDetail, DomainError>> {
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

  async addExercise(
    id: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    this.additions.push({ workoutId: id, request });
    return this.result;
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

describe("AddExerciseToWorkoutUseCase", () => {
  it("returns the workout exercise the repository answers with", async () => {
    // Arrange
    const useCase = new AddExerciseToWorkoutUseCase(new FakeWorkoutRepository(ok(workoutExercise)));

    // Act
    const result = await useCase.execute({ workoutId, ...body });

    // Assert
    expect(result).toEqual(ok(workoutExercise));
  });

  it("splits the workout id from the body and forwards the body unchanged, once", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(workoutExercise));
    const useCase = new AddExerciseToWorkoutUseCase(repository);

    // Act
    await useCase.execute({ workoutId, ...body });

    // Assert
    expect(repository.additions).toEqual([{ workoutId, request: body }]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["VALIDATION", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new AddExerciseToWorkoutUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ workoutId, ...body });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
