import { describe, expect, it } from "vitest";
import type { WorkoutRepositoryInterface } from "../../../../src/application";
import { ReviewWorkoutUseCase } from "../../../../src/application";
import {
  DomainError,
  fail,
  ok,
  type Result,
  type ReviewWorkoutRequest,
  type SuperSeries,
  type Workout,
  type WorkoutById,
  type WorkoutDetail,
  type WorkoutExerciseDetail,
  type WorkoutListItem,
} from "../../../../src/domain";

const workoutId = "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f";
const request: ReviewWorkoutRequest = { resultsUpdatedAt: "2026-11-02T10:15:00Z" };

const workoutDetail: WorkoutDetail = {
  id: workoutId,
  name: "Pierna",
  description: null,
  workoutDate: "2026-11-02",
  userId: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  trainerId: "6a2d3f1e-0e6c-4b64-9d8f-1e7a8d2b3c4d",
  status: "STARTED",
  mesocycleId: null,
  mesocycleWeekNumber: null,
  completedAt: null,
  reviewedAt: null,
  reviewedBy: null,
  resultsUpdatedAt: "2026-11-02T10:15:00Z",
  exercises: [
    {
      id: "0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
      exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
      effort: "HIGH",
      perSide: false,
      restMinutes: null,
      sets: [],
      superSeriesId: null,
      setRecords: [
        {
          id: "4c5d6e7f-8091-4b2c-9d3e-4f5a6b7c8d9e",
          reps: 8,
          weight: 60.5,
          registeredAtUtc: "2026-11-02T10:15:00Z",
        },
      ],
    },
  ],
};

class FakeWorkoutRepository implements WorkoutRepositoryInterface {
  readonly calls: { workoutId: string; request: ReviewWorkoutRequest }[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<WorkoutDetail, DomainError>) {}

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

  async review(
    workoutIdOfCall: string,
    request: ReviewWorkoutRequest,
  ): Promise<Result<WorkoutDetail, DomainError>> {
    this.calls.push({ workoutId: workoutIdOfCall, request });
    return this.result;
  }

  async changeStatus(): Promise<Result<WorkoutDetail, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("ReviewWorkoutUseCase", () => {
  it("returns the reviewed workout the repository answers with", async () => {
    // Arrange
    const useCase = new ReviewWorkoutUseCase(new FakeWorkoutRepository(ok(workoutDetail)));

    // Act
    const result = await useCase.execute({ workoutId, ...request });

    // Assert
    expect(result).toEqual(ok(workoutDetail));
  });

  it("splits the workout id from the body and forwards the results timestamp, once", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(workoutDetail));
    const useCase = new ReviewWorkoutUseCase(repository);

    // Act
    await useCase.execute({ workoutId, ...request });

    // Assert
    expect(repository.calls).toEqual([{ workoutId, request }]);
    expect(repository.otherCalls).toBe(0);
  });

  it("forwards a null results timestamp unchanged", async () => {
    // Arrange
    const repository = new FakeWorkoutRepository(ok(workoutDetail));
    const useCase = new ReviewWorkoutUseCase(repository);

    // Act
    await useCase.execute({ workoutId, resultsUpdatedAt: null });

    // Assert
    expect(repository.calls).toEqual([{ workoutId, request: { resultsUpdatedAt: null } }]);
  });

  it.each(["VALIDATION", "FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new ReviewWorkoutUseCase(new FakeWorkoutRepository(failure));

      // Act
      const result = await useCase.execute({ workoutId, ...request });

      // Assert
      expect(result).toBe(failure);
    },
  );
});
