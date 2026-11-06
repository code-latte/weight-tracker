import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { GetWorkoutByIdUseCase } from "../../../../src/application";
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
} from "../../../../src/domain";

const workoutDetail: WorkoutById = {
  id: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
  name: "Pierna",
  description: null,
  workoutDate: "2026-11-02",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  status: "DRAFT",
  mesocycleId: null,
  mesocycleWeekNumber: null,
  exercises: [
    {
      id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
      exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
      effort: "HIGH",
      perSide: false,
      restMinutes: null,
      sets: [{ count: 3, target: { type: "REPS", value: 10, maxValue: null } }],
      superSeriesId: null,
    },
  ],
  athlete: {
    id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
    email: "ana@example.com",
    name: "Ana",
    surname: "García",
    role: "USER",
  },
  trainer: {
    id: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
    email: "luis@example.com",
    name: "Luis",
    surname: "Pérez",
    role: "TRAINER",
  },
};

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly reads: string[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<WorkoutById, DomainError>) {}

  async getAll(): Promise<Result<WorkoutListItem[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getById(id: string): Promise<Result<WorkoutById, DomainError>> {
    this.reads.push(id);
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
}

describe("GetWorkoutByIdUseCase", () => {
  it("returns the workout the repository answers with, athlete and trainer included", async () => {
    // Arrange
    const useCase = new GetWorkoutByIdUseCase(new FakeWorkoutRepository(ok(workoutDetail)));

    // Act
    const result = await useCase.execute({ id: workoutDetail.id });

    // Assert
    expect(result).toEqual(ok(workoutDetail));
  });

  it("forwards the id once and calls nothing else", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(workoutDetail));
    const useCase = new GetWorkoutByIdUseCase(repository);

    // Act
    await useCase.execute({ id: workoutDetail.id });

    // Assert
    expect(repository.reads).toEqual([workoutDetail.id]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["UNAUTHORIZED", "FORBIDDEN", "NOT_FOUND"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new GetWorkoutByIdUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ id: workoutDetail.id });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
