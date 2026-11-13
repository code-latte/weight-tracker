import { describe, expect, it } from "vitest";
import type { UserRepositoryInterface } from "../../../../src/application";
import { GetUserExerciseHistoryUseCase } from "../../../../src/application";
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

const userId = "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c";
const exerciseId = "9b8a7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d";

const sessions: ExerciseHistorySession[] = [
  {
    workoutId: "7b0c1f7e-3a54-4d4e-9d0e-1a2b3c4d5e6f",
    workoutDate: "2026-11-12",
    maxWeight: 130,
    volume: 3190,
    estimatedOneRepMax: 160.3,
  },
  {
    workoutId: "2b3c4d5e-6f70-4819-9a0b-1c2d3e4f5a6b",
    workoutDate: "2026-10-27",
    maxWeight: null,
    volume: null,
    estimatedOneRepMax: null,
  },
];

class FakeUserRepository implements UserRepositoryInterface {
  readonly requests: { userId: string; exerciseId: string; limit: number }[] = [];

  constructor(private readonly result: Result<ExerciseHistorySession[], DomainError>) {}

  async getMe(): Promise<Result<UserProfile, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getTrainer(): Promise<Result<UserProfile | null, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getMyExerciseLastPerformance(): Promise<
    Result<ExerciseLastPerformance | null, DomainError>
  > {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getExerciseHistory(
    userId: string,
    exerciseId: string,
    limit: number,
  ): Promise<Result<ExerciseHistorySession[], DomainError>> {
    this.requests.push({ userId, exerciseId, limit });
    return this.result;
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

describe("GetUserExerciseHistoryUseCase", () => {
  it("asks the repository for the given user, exercise and limit", async () => {
    // Arrange
    const repository = new FakeUserRepository(ok(sessions));
    const useCase = new GetUserExerciseHistoryUseCase(repository);

    // Act
    await useCase.execute({ userId, exerciseId, limit: 5 });

    // Assert
    expect(repository.requests).toEqual([{ userId, exerciseId, limit: 5 }]);
  });

  it("returns the sessions the repository answers with, keeping every null", async () => {
    // Arrange
    const useCase = new GetUserExerciseHistoryUseCase(new FakeUserRepository(ok(sessions)));

    // Act
    const result = await useCase.execute({ userId, exerciseId, limit: 5 });

    // Assert
    expect(result).toEqual(ok(sessions));
  });

  it("returns an empty list when the user never did the exercise", async () => {
    // Arrange
    const useCase = new GetUserExerciseHistoryUseCase(new FakeUserRepository(ok([])));

    // Act
    const result = await useCase.execute({ userId, exerciseId, limit: 5 });

    // Assert
    expect(result).toEqual(ok([]));
  });

  it("returns the repository failure unchanged", async () => {
    // Arrange
    const failure = fail(new DomainError("FORBIDDEN", "Not your athlete"));
    const useCase = new GetUserExerciseHistoryUseCase(new FakeUserRepository(failure));

    // Act
    const result = await useCase.execute({ userId, exerciseId, limit: 5 });

    // Assert
    expect(result).toBe(failure);
  });
});
