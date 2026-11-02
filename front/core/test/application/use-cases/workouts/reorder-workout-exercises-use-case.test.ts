import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { ReorderWorkoutExercisesUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type ReorderWorkoutExercisesRequest,
  type Result,
  type Workout,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
  type WorkoutListItem,
} from "../../../../src/domain";

const workoutId = "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f";
const exerciseIds = [
  "2a3b4c5d-6e7f-4809-9a1b-2c3d4e5f6a7b",
  "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
];

const workoutDetail: WorkoutDetail = {
  id: workoutId,
  name: "Pierna",
  description: null,
  workoutDate: "2026-11-02",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: null,
  status: "DRAFT",
  exercises: exerciseIds.map((exerciseId, index) => ({
    id: `0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4${index}`,
    exerciseId,
    effort: "HIGH",
    perSide: false,
    restMinutes: null,
    sets: [],
    superSeriesId: null,
  })),
};

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly reorders: { workoutId: string; request: ReorderWorkoutExercisesRequest }[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<WorkoutDetail, DomainError>) {}

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

  async updateExercise(): Promise<Result<WorkoutExerciseDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async removeExercise(): Promise<Result<void, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async reorderExercises(
    workoutIdOfCall: string,
    request: ReorderWorkoutExercisesRequest,
  ): Promise<Result<WorkoutDetail, DomainError>> {
    this.reorders.push({ workoutId: workoutIdOfCall, request });
    return this.result;
  }
}

describe("ReorderWorkoutExercisesUseCase", () => {
  it("returns the reordered workout the repository answers with", async () => {
    // Arrange
    const useCase = new ReorderWorkoutExercisesUseCase(
      new FakeWorkoutRepository(ok(workoutDetail)),
    );

    // Act
    const result = await useCase.execute({ workoutId, exerciseIds });

    // Assert
    expect(result).toEqual(ok(workoutDetail));
  });

  it("splits the workout id from the body and forwards the ids in order, once", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(workoutDetail));
    const useCase = new ReorderWorkoutExercisesUseCase(repository);

    // Act
    await useCase.execute({ workoutId, exerciseIds });

    // Assert
    expect(repository.reorders).toEqual([{ workoutId, request: { exerciseIds } }]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["VALIDATION", "FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new ReorderWorkoutExercisesUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ workoutId, exerciseIds });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
