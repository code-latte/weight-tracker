import { describe, expect, it } from "vitest";
import type { ExerciseFavoriteRepositoryInterface } from "../../../../src/application";
import { UnmarkExerciseFavoriteUseCase } from "../../../../src/application";
import { DomainError, type ExerciseFavorite, fail, ok, type Result } from "../../../../src/domain";

const exerciseId = "1f2e3d4c-5b6a-4978-8695-a4b3c2d1e0f9";

class FakeExerciseFavoriteRepository implements ExerciseFavoriteRepositoryInterface {
  readonly unmarked: string[] = [];
  otherCalls = 0;

  constructor(private readonly result: Result<void, DomainError>) {}

  async unmark(id: string): Promise<Result<void, DomainError>> {
    this.unmarked.push(id);
    return this.result;
  }

  async getAll(): Promise<Result<ExerciseFavorite[], DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }

  async mark(): Promise<Result<ExerciseFavorite, DomainError>> {
    this.otherCalls += 1;
    return fail(new DomainError("UNEXPECTED", "Not used"));
  }
}

describe("UnmarkExerciseFavoriteUseCase", () => {
  it("returns the empty success the repository answers with", async () => {
    // Arrange
    const useCase = new UnmarkExerciseFavoriteUseCase(
      new FakeExerciseFavoriteRepository(ok(undefined)),
    );

    // Act
    const result = await useCase.execute(exerciseId);

    // Assert
    expect(result).toEqual(ok(undefined));
  });

  it("forwards the exercise id once and touches nothing else", async () => {
    // Arrange
    const repository = new FakeExerciseFavoriteRepository(ok(undefined));
    const useCase = new UnmarkExerciseFavoriteUseCase(repository);

    // Act
    await useCase.execute(exerciseId);

    // Assert
    expect([repository.unmarked, repository.otherCalls]).toEqual([[exerciseId], 0]);
  });

  it("returns the repository failure unchanged", async () => {
    // Arrange
    const failure = fail(new DomainError("NOT_FOUND", "The exercise does not exist"));
    const useCase = new UnmarkExerciseFavoriteUseCase(new FakeExerciseFavoriteRepository(failure));

    // Act
    const result = await useCase.execute(exerciseId);

    // Assert
    expect(result).toBe(failure);
  });
});
