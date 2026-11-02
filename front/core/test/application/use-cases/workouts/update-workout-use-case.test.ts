import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { UpdateWorkoutUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type Result,
  type UpdateWorkoutRequest,
  type Workout,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
  type WorkoutListItem,
} from "../../../../src/domain";

const workout: Workout = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Pierna B",
  description: null,
  workoutDate: "2026-11-03",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: null,
  status: "DRAFT",
  exercises: [],
};

const body: UpdateWorkoutRequest = {
  name: "Pierna B",
  description: null,
  workoutDate: "2026-11-03",
  userId: workout.userId,
};

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly updates: { id: string; request: UpdateWorkoutRequest }[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<Workout, DomainError>) {}

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
    return this.result;
  }

  async update(id: string, request: UpdateWorkoutRequest): Promise<Result<Workout, DomainError>> {
    this.updates.push({ id, request });
    return this.result;
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

describe("UpdateWorkoutUseCase", () => {
  it("returns the updated workout the repository answers with", async () => {
    // Arrange
    const useCase = new UpdateWorkoutUseCase(new FakeWorkoutRepository(ok(workout)));

    // Act
    const result = await useCase.execute({ id: workout.id, ...body });

    // Assert
    expect(result).toEqual(ok(workout));
  });

  it("splits the id from the body and forwards the body unchanged, once", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(workout));
    const useCase = new UpdateWorkoutUseCase(repository);

    // Act
    await useCase.execute({ id: workout.id, ...body });

    // Assert
    expect(repository.updates).toEqual([{ id: workout.id, request: body }]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["VALIDATION", "FORBIDDEN", "NOT_FOUND"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new UpdateWorkoutUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ id: workout.id, ...body });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
