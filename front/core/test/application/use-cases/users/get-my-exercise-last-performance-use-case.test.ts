import { describe, expect, it } from "vitest";
import type { UserRepositoryInterface } from "../../../../src/application";
import { GetMyExerciseLastPerformanceUseCase } from "../../../../src/application";
import {
  type CreatedUserResponse,
  DomainError,
  type ExerciseHistorySession,
  type ExerciseLastPerformance,
  fail,
  ok,
  type Result,
  type UserProfile,
  type UserSummary,
} from "../../../../src/domain";

const lastPerformance: ExerciseLastPerformance = {
  workoutDate: "2026-11-10",
  effort: "HIGH",
  sets: [
    { reps: 10, durationSeconds: null, weight: 20 },
    { reps: 10, durationSeconds: null, weight: null },
  ],
};

class FakeUserRepository implements UserRepositoryInterface {
  readonly exerciseRequests: string[] = [];

  constructor(private readonly result: Result<ExerciseLastPerformance | null, DomainError>) {}

  async getMe(): Promise<Result<UserProfile, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getTrainer(): Promise<Result<UserProfile | null, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getMyExerciseLastPerformance(
    exerciseId: string,
  ): Promise<Result<ExerciseLastPerformance | null, DomainError>> {
    this.exerciseRequests.push(exerciseId);
    return this.result;
  }

  async getExerciseHistory(): Promise<Result<ExerciseHistorySession[], DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async updateMe(): Promise<Result<UserProfile, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getAll(): Promise<Result<UserSummary[], DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async create(): Promise<Result<CreatedUserResponse, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async activate(): Promise<Result<UserSummary, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async deactivate(): Promise<Result<UserSummary, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

const exerciseId = "9b8a7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d";

describe("GetMyExerciseLastPerformanceUseCase", () => {
  it("asks the repository for the given exercise", async () => {
    // Arrange
    const repository = new FakeUserRepository(ok(lastPerformance));
    const useCase = new GetMyExerciseLastPerformanceUseCase(repository);

    // Act
    await useCase.execute(exerciseId);

    // Assert
    expect(repository.exerciseRequests).toEqual([exerciseId]);
  });

  it("returns the last performance the repository answers with", async () => {
    // Arrange
    const useCase = new GetMyExerciseLastPerformanceUseCase(
      new FakeUserRepository(ok(lastPerformance)),
    );

    // Act
    const result = await useCase.execute(exerciseId);

    // Assert
    expect(result).toEqual(ok(lastPerformance));
  });

  it("returns null when the user never did the exercise", async () => {
    // Arrange
    const useCase = new GetMyExerciseLastPerformanceUseCase(new FakeUserRepository(ok(null)));

    // Act
    const result = await useCase.execute(exerciseId);

    // Assert
    expect(result).toEqual(ok(null));
  });

  it("returns the repository failure unchanged", async () => {
    // Arrange
    const failure = fail(new DomainError("NOT_FOUND", "Exercise not found"));
    const useCase = new GetMyExerciseLastPerformanceUseCase(new FakeUserRepository(failure));

    // Act
    const result = await useCase.execute(exerciseId);

    // Assert
    expect(result).toBe(failure);
  });
});
