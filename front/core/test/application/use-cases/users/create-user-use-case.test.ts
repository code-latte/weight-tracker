import { describe, expect, it } from "vitest";
import type { UserRepositoryInterface } from "../../../../src/application";
import { CreateUserUseCase } from "../../../../src/application";
import {
  type CreatedUserResponse,
  type CreateUserRequest,
  DomainError,
  type ExerciseHistorySession,
  type ExerciseLastPerformance,
  fail,
  ok,
  type Result,
  type UserProfile,
  type UserSummary,
} from "../../../../src/domain";

const request: CreateUserRequest = {
  email: "new@example.com",
  password: "Str0ng!Passw0rd",
  role: 3,
  name: "Luis",
  surname: "Pérez",
  dateBirth: "1990-05-17",
};

const created: CreatedUserResponse = {
  id: "7a2b3c4d-1e2f-4a5b-9c8d-0e1f2a3b4c5d",
  email: "new@example.com",
  name: "Luis",
  surname: "Pérez",
  dateBirth: "1990-05-17",
  role: "ADMIN",
};

class FakeUserRepository implements UserRepositoryInterface {
  readonly creations: CreateUserRequest[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<CreatedUserResponse, DomainError>) {}

  async create(
    createRequest: CreateUserRequest,
  ): Promise<Result<CreatedUserResponse, DomainError>> {
    this.creations.push(createRequest);
    return this.result;
  }

  async getMe(): Promise<Result<UserProfile, DomainError>> {
    this.otherCalls += 1;
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

  async getExerciseHistory(): Promise<Result<ExerciseHistorySession[], DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async updateMe(): Promise<Result<UserProfile, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getAll(): Promise<Result<UserSummary[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async activate(): Promise<Result<UserSummary, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async deactivate(): Promise<Result<UserSummary, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("CreateUserUseCase", () => {
  it("returns the created user the repository answers with", async () => {
    // Arrange
    const useCase = new CreateUserUseCase(new FakeUserRepository(ok(created)));

    // Act
    const result = await useCase.execute(request);

    // Assert
    expect(result).toEqual(ok(created));
  });

  it("forwards the request unchanged, once, and calls nothing else", async () => {
    // Arrange
    const repository = new FakeUserRepository(ok(created));
    const useCase = new CreateUserUseCase(repository);

    // Act
    await useCase.execute(request);

    // Assert
    expect(repository.creations).toEqual([request]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["VALIDATION", "FORBIDDEN", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new CreateUserUseCase(new FakeUserRepository(failure));

      // Act
      const result = await useCase.execute(request);

      // Assert
      expect(result).toBe(failure);
    },
  );
});
