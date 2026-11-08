import { describe, expect, it } from "vitest";
import type { UserRepositoryInterface } from "../../../../src/application";
import { GetUserTrainerUseCase } from "../../../../src/application";
import {
  type CreatedUserResponse,
  DomainError,
  fail,
  ok,
  type Result,
  type UserProfile,
  type UserSummary,
} from "../../../../src/domain";

const profile: UserProfile = {
  id: "5f1c2f0e-9d5b-4a53-8c7e-0d6f7c1a2b3c",
  email: "trainer@example.com",
  name: "Ana",
  surname: "García",
  role: "TRAINER",
};

class FakeUserRepository implements UserRepositoryInterface {
  readonly trainerRequests: string[] = [];

  constructor(private readonly result: Result<UserProfile | null, DomainError>) {}

  async getMe(): Promise<Result<UserProfile, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async getTrainer(userId: string): Promise<Result<UserProfile | null, DomainError>> {
    this.trainerRequests.push(userId);
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

const userId = "2c3d4e5f-6a7b-4c8d-9e0f-1a2b3c4d5e6f";

describe("GetUserTrainerUseCase", () => {
  it("asks the repository for the given user's trainer", async () => {
    // Arrange
    const repository = new FakeUserRepository(ok(profile));
    const useCase = new GetUserTrainerUseCase(repository);

    // Act
    await useCase.execute(userId);

    // Assert
    expect(repository.trainerRequests).toEqual([userId]);
  });

  it("returns the trainer the repository answers with", async () => {
    // Arrange
    const useCase = new GetUserTrainerUseCase(new FakeUserRepository(ok(profile)));

    // Act
    const result = await useCase.execute(userId);

    // Assert
    expect(result).toEqual(ok(profile));
  });

  it("returns null when the user has no trainer", async () => {
    // Arrange
    const useCase = new GetUserTrainerUseCase(new FakeUserRepository(ok(null)));

    // Act
    const result = await useCase.execute(userId);

    // Assert
    expect(result).toEqual(ok(null));
  });

  it("returns the repository failure unchanged", async () => {
    // Arrange
    const failure = fail(new DomainError("FORBIDDEN", "Not allowed"));
    const useCase = new GetUserTrainerUseCase(new FakeUserRepository(failure));

    // Act
    const result = await useCase.execute(userId);

    // Assert
    expect(result).toBe(failure);
  });
});
