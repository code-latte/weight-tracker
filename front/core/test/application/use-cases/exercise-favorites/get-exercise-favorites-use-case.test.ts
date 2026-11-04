import { describe, expect, it } from "vitest";
import type { ExerciseFavoriteRepositoryInterface } from "../../../../src/application";
import { GetExerciseFavoritesUseCase } from "../../../../src/application";
import { DomainError, type ExerciseFavorite, fail, ok, type Result } from "../../../../src/domain";

const favorite: ExerciseFavorite = {
  exerciseId: "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9",
  createdAtUtc: "2026-11-03T09:30:00Z",
};

class FakeExerciseFavoriteRepository implements ExerciseFavoriteRepositoryInterface {
  getAllCalls = 0;
  otherCalls = 0;

  constructor(private readonly result: Result<ExerciseFavorite[], DomainError>) {}

  async getAll(): Promise<Result<ExerciseFavorite[], DomainError>> {
    this.getAllCalls += 1;
    return this.result;
  }

  async mark(): Promise<Result<ExerciseFavorite, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async unmark(): Promise<Result<void, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("GetExerciseFavoritesUseCase", () => {
  it("returns the favorites the repository answers with", async () => {
    // Arrange
    const useCase = new GetExerciseFavoritesUseCase(
      new FakeExerciseFavoriteRepository(ok([favorite])),
    );

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toEqual(ok([favorite]));
  });

  it("calls the repository once and touches nothing else", async () => {
    // Arrange
    const repository = new FakeExerciseFavoriteRepository(ok([favorite]));
    const useCase = new GetExerciseFavoritesUseCase(repository);

    // Act
    await useCase.execute();

    // Assert
    expect([repository.getAllCalls, repository.otherCalls]).toEqual([1, 0]);
  });

  it("returns the repository failure unchanged", async () => {
    // Arrange
    const failure = fail(new DomainError("UNAUTHORIZED", "No session"));
    const useCase = new GetExerciseFavoritesUseCase(new FakeExerciseFavoriteRepository(failure));

    // Act
    const result = await useCase.execute();

    // Assert
    expect(result).toBe(failure);
  });
});
