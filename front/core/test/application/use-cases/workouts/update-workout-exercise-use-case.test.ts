import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { UpdateWorkoutExerciseUseCase } from "../../../../src/application";
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
const exerciseId = "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9";

const workoutExercise: WorkoutExerciseDetail = {
  id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
  exerciseId,
  effort: "MID",
  perSide: true,
  restMinutes: 0.75,
  sets: [{ count: 2, target: { type: "Reps", value: 15, maxValue: null } }],
  superSeriesId: null,
};

const body: AddExerciseRequest & { exerciseId: string } = {
  exerciseId,
  effort: "MID",
  sets: [
    { count: 2, targetType: "Reps", target: 15 },
    { count: 1, targetType: "MaxReps" },
  ],
  perSide: true,
  restMinutes: 0.75,
};

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly updates: { workoutId: string; exerciseId: string; request: AddExerciseRequest }[] = [];
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

  async addExercise(): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async updateExercise(
    workoutIdOfCall: string,
    exerciseIdOfCall: string,
    request: AddExerciseRequest,
  ): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    this.updates.push({ workoutId: workoutIdOfCall, exerciseId: exerciseIdOfCall, request });
    return this.result;
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

describe("UpdateWorkoutExerciseUseCase", () => {
  it("returns the workout exercise the repository answers with", async () => {
    // Arrange
    const useCase = new UpdateWorkoutExerciseUseCase(
      new FakeWorkoutRepository(ok(workoutExercise)),
    );

    // Act
    const result = await useCase.execute({ workoutId, ...body });

    // Assert
    expect(result).toEqual(ok(workoutExercise));
  });

  it("puts the catalog exercise id in the path and keeps it in the body, once", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(workoutExercise));
    const useCase = new UpdateWorkoutExerciseUseCase(repository);

    // Act
    await useCase.execute({ workoutId, ...body });

    // Assert
    expect(repository.updates).toEqual([{ workoutId, exerciseId, request: body }]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["VALIDATION", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new UpdateWorkoutExerciseUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ workoutId, ...body });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
