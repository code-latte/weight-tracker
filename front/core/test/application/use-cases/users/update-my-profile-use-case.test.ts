import { describe, expect, it } from "vitest";
import type { UserRepositoryInterface } from "../../../../src/application";
import { UpdateMyProfileUseCase } from "../../../../src/application";
import {
  type CreatedUserResponse,
  DomainError,
  type ExerciseLastPerformance,
  fail,
  ok,
  type Result,
  type UpdateProfileRequest,
  type UserProfile,
  type UserSummary,
} from "../../../../src/domain";

const request: UpdateProfileRequest = { name: "Ana", surname: "García" };

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

  async getMyExerciseLastPerformance(): Promise<
    Result<ExerciseLastPerformance | null, DomainError>
  > {
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async updateMe(updateRequest: UpdateProfileRequest): Promise<Result<UserProfile, DomainError>> {
    this.updates.push(updateRequest);
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

describe("UpdateMyProfileUseCase", () => {
  it("returns the updated profile the repository answers with", async () => {
    // Arrange
    const useCase = new UpdateMyProfileUseCase(new FakeUserRepository(ok(profile)));

    // Act
    const result = await useCase.execute(request);

    // Assert
    expect(result).toEqual(ok(profile));
  });

  it("sends the trimmed name and surname to the repository, once", async () => {
    // Arrange
    const repository = new FakeUserRepository(ok(profile));
    const useCase = new UpdateMyProfileUseCase(repository);

    // Act
    await useCase.execute({ name: "  Ana ", surname: "García  " });

    // Assert
    expect(repository.updates).toEqual([{ name: "Ana", surname: "García" }]);
    expect(repository.getCalls).toBe(0);
  });

  it.each([
    ["name", { name: "Al", surname: "García" }],
    ["surname", { name: "Ana", surname: "   " }],
    ["name", { name: "a".repeat(101), surname: "García" }],
  ] as const)(
    "fails with VALIDATION on an invalid %s without calling the repository",
    async (_field, invalid) => {
      // Arrange
      const repository = new FakeUserRepository(ok(profile));
      const useCase = new UpdateMyProfileUseCase(repository);

      // Act
      const result = await useCase.execute(invalid);

      // Assert
      expect(result.ok).toBe(false);
      expect(!result.ok && result.error.code).toBe("VALIDATION");
      expect(repository.updates).toEqual([]);
    },
  );

  it.each(["VALIDATION", "UNAUTHORIZED"] as const)(
    "returns the repository failure %s unchanged",
    async (code) => {
      // Arrange
      const failure = fail(new DomainError(code, "Rejected"));
      const useCase = new UpdateMyProfileUseCase(new FakeUserRepository(failure));

      // Act
      const result = await useCase.execute(request);

      // Assert
      expect(result).toBe(failure);
    },
  );
});
