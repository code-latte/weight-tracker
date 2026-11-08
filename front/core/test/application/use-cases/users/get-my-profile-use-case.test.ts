import { describe, expect, it } from "vitest";
import type { UserRepositoryInterface } from "../../../../src/application";
import { GetMyProfileUseCase } from "../../../../src/application";
import {
  type CreatedUserResponse,
  DomainError,
  fail,
  ok,
  type Result,
  type UpdateProfileRequest,
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
  getCalls = 0;
  readonly updates: UpdateProfileRequest[] = [];

  constructor(private readonly result: Result<UserProfile, DomainError>) {}

  async getMe(): Promise<Result<UserProfile, DomainError>> {
    this.getCalls += 1;
    return this.result;
  }

  async getTrainer(): Promise<Result<UserProfile | null, DomainError>> {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async updateMe(request: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>> {
    this.updates.push(request);
    return this.result;
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

describe("GetMyProfileUseCase", () => {
  it("returns the profile the repository answers with", async () => {
    // Arrange
    const useCase = new GetMyProfileUseCase(new FakeUserRepository(ok(profile)));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok(profile));
  });

  it("reads the profile once and updates nothing", async () => {
    // Arrange
    const repository = new FakeUserRepository(ok(profile));
    const useCase = new GetMyProfileUseCase(repository);

    // Act
    await useCase.execute();

    // Assert
    expect(repository.getCalls).toBe(1);
    expect(repository.updates).toEqual([]);
  });

  it("returns the repository failure unchanged", async () => {
    // Arrange
    const failure = fail(new DomainError("UNAUTHORIZED", "Invalid token"));
    const useCase = new GetMyProfileUseCase(new FakeUserRepository(failure));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toBe(failure);
  });
});
