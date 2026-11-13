import { describe, expect, it } from "vitest";
import type { UserRepositoryInterface } from "../../../../src/application";
import { DeactivateUserUseCase } from "../../../../src/application";
import {
  type CreatedUserResponse,
  DomainError,
  type ExerciseLastPerformance,
  fail,
  ok,
  type Result,
  type UserActivationSummary,
  type UserProfile,
  type UserSummary,
} from "../../../../src/domain";

const id = "1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e";

const user: UserActivationSummary = {
  id,
  email: "luis@example.com",
  name: "Luis",
  surname: "Pérez",
  role: "TRAINER",
  isActive: false,
  deactivatedAtUtc: "2026-10-20T09:30:00Z",
};

class FakeUserRepository implements UserRepositoryInterface {
  readonly ids: string[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<UserActivationSummary, DomainError>) {}

  async deactivate(userId: string): Promise<Result<UserActivationSummary, DomainError>> {
    this.ids.push(userId);
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

  async updateMe(): Promise<Result<UserProfile, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getAll(): Promise<Result<UserSummary[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async create(): Promise<Result<CreatedUserResponse, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async activate(): Promise<Result<UserActivationSummary, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("DeactivateUserUseCase", () => {
  it("returns the user the repository answers with", async () => {
    // Arrange
    const useCase = new DeactivateUserUseCase(new FakeUserRepository(ok(user)));

    // Act
    const result = await useCase.execute(id);

    // Assert
    expect(result).toEqual(ok(user));
  });

  it("forwards the id unchanged, once, and calls nothing else", async () => {
    // Arrange
    const repository = new FakeUserRepository(ok(user));
    const useCase = new DeactivateUserUseCase(repository);

    // Act
    await useCase.execute(id);

    // Assert
    expect(repository.ids).toEqual([id]);
    expect(repository.otherCalls).toBe(0);
  });

  it.each(["FORBIDDEN", "NOT_FOUND", "CONFLICT"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new DeactivateUserUseCase(new FakeUserRepository(failure));

      // Act
      const result = await useCase.execute(id);

      // Assert
      expect(result).toBe(failure);
    },
  );
});
