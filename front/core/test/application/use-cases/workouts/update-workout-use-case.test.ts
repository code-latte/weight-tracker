import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { UpdateWorkoutUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type Result,
  type SuperSeries,
  type UpdateWorkoutRequest,
  type Workout,
  type WorkoutById,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
  type WorkoutListItem,
  type WorkoutObservations,
  type WorkoutSummary,
} from "../../../../src/domain";

const workout: Workout = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Pierna B",
  description: null,
  workoutDate: "2026-11-03",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: null,
  status: "DRAFT",
  mesocycleId: null,
  mesocycleWeekNumber: null,
  startedAt: null,
  completedAt: null,
  reviewedAt: null,
  reviewedBy: null,
  resultsUpdatedAt: null,
  exercises: [],
};

const placement = { mesocycleId: "1d2c3b4a-5f6e-4d7c-8b9a-0f1e2d3c4b5a", mesocycleWeekNumber: 2 };

const placedWorkout: Workout = { ...workout, ...placement };

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
    return this.result;
  }

  async update(id: string, request: UpdateWorkoutRequest): Promise<Result<Workout, DomainError>> {
    this.updates.push({ id, request });
    return this.result;
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

  it("forwards the mesocycle placement unchanged", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(placedWorkout));
    const useCase = new UpdateWorkoutUseCase(repository);

    // Act
    await useCase.execute({ id: workout.id, ...body, ...placement });

    // Assert
    expect(repository.updates).toEqual([{ id: workout.id, request: { ...body, ...placement } }]);
  });

  it("adds no mesocycle placement when it is omitted", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(workout));
    const useCase = new UpdateWorkoutUseCase(repository);

    // Act
    await useCase.execute({ id: workout.id, ...body });

    // Assert
    expect(repository.updates[0]?.request).not.toHaveProperty("mesocycleId");
    expect(repository.updates[0]?.request).not.toHaveProperty("mesocycleWeekNumber");
  });

  it("returns the mesocycle placement the repository answers with", async () => {
    // Arrange
    const useCase = new UpdateWorkoutUseCase(new FakeWorkoutRepository(ok(placedWorkout)));

    // Act
    const result = await useCase.execute({ id: workout.id, ...body, ...placement });

    // Assert
    expect(result).toEqual(ok(placedWorkout));
  });

  it.each(["VALIDATION", "FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
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
